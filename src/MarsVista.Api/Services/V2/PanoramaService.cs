using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of panorama detection service
/// Detects panoramic sequences based on location, time, and camera telemetry
/// </summary>
public class PanoramaService : IPanoramaService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PanoramaService> _logger;
    private readonly IPhotoQueryServiceV2 _photoService;

    // Panorama detection parameters
    private const float MinAzimuthRangeDegrees = 30.0f; // At least 30 degrees coverage
    private const int MinPhotosForPanorama = 3; // At least 3 photos
    private const int MinUniquePositions = 3; // At least 3 unique azimuth positions (stitchable)
    private const float MaxTimeDeltaSeconds = 300.0f; // Max 5 minutes between photos

    // Multi-row mosaic detection parameters
    private const float ElevationTierGapDegrees = 5.0f; // Min gap between sorted elevations to start a new tier
    private const float MinGridCompleteness = 0.40f; // Multi-row mosaic must fill 40% of grid cells

    // Only cameras designed for panoramic imaging — excludes spectrometers (ChemCam, SuperCam RMI),
    // hazard cameras (fixed FOV), arm cameras (MAHLI, SHERLOC), and descent/EDL cameras
    private static readonly HashSet<string> PanoramicCameras = new(StringComparer.OrdinalIgnoreCase)
    {
        "MAST", "NAVCAM",                               // Curiosity
        "MCZ_LEFT", "MCZ_RIGHT", "NAVCAM_LEFT", "NAVCAM_RIGHT", // Perseverance
        "PANCAM"                                         // Opportunity, Spirit
    };

    // Performance optimization: Limit sol range to prevent loading all photos into memory
    // TODO: Long-term solution should pre-compute panoramas in a dedicated table (see .claude/decisions/PANORAMA_OPTIMIZATION.md)
    private const int DefaultSolRangeLimit = 500; // Default to most recent 500 sols when no range specified

    public PanoramaService(
        MarsVistaDbContext context,
        ILogger<PanoramaService> logger,
        IPhotoQueryServiceV2 photoService)
    {
        _context = context;
        _logger = logger;
        _photoService = photoService;
    }

    public async Task<ApiResponse<List<PanoramaResource>>> GetPanoramasAsync(
        string? rovers = null,
        int? solMin = null,
        int? solMax = null,
        int? minPhotos = null,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        // Build query for photos that could be part of panoramas
        // Only include cameras designed for panoramic imaging
        var query = _context.Photos
            .Where(p => p.Site.HasValue &&
                       p.Drive.HasValue &&
                       p.MastAz.HasValue &&
                       p.MastEl.HasValue &&
                       p.SpacecraftClock.HasValue &&
                       PanoramicCameras.Contains(p.Camera.Name));

        // Apply filters
        if (!string.IsNullOrWhiteSpace(rovers))
        {
            var roverList = rovers.Split(',')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            // Use case-insensitive comparison via ILIKE (PostgreSQL)
            // EF Core will auto-join to Rover table for this filter
            query = query.Where(p => roverList.Any(r => EF.Functions.ILike(p.Rover.Name, r)));
        }

        if (solMin.HasValue)
        {
            query = query.Where(p => p.Sol >= solMin.Value);
            _logger.LogDebug("Applied solMin filter: {SolMin}", solMin.Value);
        }

        if (solMax.HasValue)
        {
            query = query.Where(p => p.Sol <= solMax.Value);
            _logger.LogDebug("Applied solMax filter: {SolMax}", solMax.Value);
        }

        // Performance optimization: If no sol range specified, default to recent sols
        // This prevents loading 200k+ photos into memory (which takes 2-3 minutes)
        if (!solMin.HasValue && !solMax.HasValue)
        {
            var maxSol = await query.MaxAsync(p => (int?)p.Sol, cancellationToken);
            if (maxSol.HasValue)
            {
                var defaultSolMin = Math.Max(0, maxSol.Value - DefaultSolRangeLimit);
                query = query.Where(p => p.Sol >= defaultSolMin);

                _logger.LogDebug(
                    "No sol range specified, defaulting to recent {SolCount} sols (sol {MinSol} to {MaxSol})",
                    DefaultSolRangeLimit, defaultSolMin, maxSol.Value);
            }
        }

        // OPTIMIZATION: Process panoramas in batches by sol to avoid loading all photos into memory
        // Get distinct sols that have potential panorama photos
        var sols = await query
            .Select(p => p.Sol)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Found {SolCount} distinct sols to process. First 5: [{Sols}]",
            sols.Count,
            string.Join(", ", sols.Take(5)));

        var allPanoramas = new List<PanoramaSequence>();

        // Process each sol independently to limit memory usage
        foreach (var sol in sols)
        {
            // Order by time, then by elevation to group photos at similar angles together
            // This prevents non-deterministic ordering when photos have the same spacecraft_clock
            // (bracketed exposures or multi-camera captures at the same instant)
            var solPhotos = await query
                .Where(p => p.Sol == sol)
                .Include(p => p.Rover)
                .Include(p => p.Camera)
                .AsNoTracking() // Don't track entities for read-only operations
                .OrderBy(p => p.RoverId)
                .ThenBy(p => p.Site)
                .ThenBy(p => p.Drive)
                .ThenBy(p => p.SpacecraftClock)
                .ThenBy(p => p.MastEl) // Group photos at similar elevations when same clock
                .ToListAsync(cancellationToken);

            // Detect panoramas for this sol - index resets per sol for stable IDs
            var panoramaIndex = 0;
            var solPanoramas = DetectPanoramasOptimized(solPhotos, minPhotos ?? MinPhotosForPanorama, ref panoramaIndex);

            if (sols.Count <= 5 || solPanoramas.Count > 0)
            {
                _logger.LogDebug("Sol {Sol}: {PhotoCount} photos, {PanoramaCount} panoramas detected",
                    sol, solPhotos.Count, solPanoramas.Count);
            }

            allPanoramas.AddRange(solPanoramas);
        }

        // Use the detected panoramas
        var panoramas = allPanoramas;

        // Apply pagination
        var totalCount = panoramas.Count;
        var paginatedPanoramas = panoramas
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Batch-load stitch statuses to avoid N+1 queries
        var panoramaIds = paginatedPanoramas.Select(p =>
        {
            var first = p.Photos.First();
            var r = first.Rover.Name.ToLowerInvariant();
            return $"pano_{r}_{first.Sol}_{p.Index}";
        }).ToList();

        var stitchStatuses = await _context.StitchedPanoramas
            .AsNoTracking()
            .Where(s => panoramaIds.Contains(s.PanoramaId) && s.Status == "completed")
            .ToDictionaryAsync(s => s.PanoramaId, cancellationToken);

        // Convert to resources
        var resources = paginatedPanoramas.Select(p => ToPanoramaResource(p, stitchStatuses)).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new ApiResponse<List<PanoramaResource>>(resources)
        {
            Meta = new ResponseMeta
            {
                TotalCount = totalCount,
                ReturnedCount = resources.Count
            },
            Pagination = new PaginationInfo
            {
                Page = pageNumber,
                PerPage = pageSize,
                TotalPages = totalPages
            }
        };
    }

    public async Task<PanoramaResource?> GetPanoramaByIdAsync(
        string panoramaId,
        CancellationToken cancellationToken = default)
    {
        var sequence = await DetectPanoramaSequenceByIdAsync(panoramaId, cancellationToken);
        if (sequence == null)
            return null;

        var stitchRecord = await _context.StitchedPanoramas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId && s.Status == "completed", cancellationToken);

        var stitchStatuses = stitchRecord != null
            ? new Dictionary<string, StitchedPanorama> { { panoramaId, stitchRecord } }
            : null;

        // Map constituent photos to PhotoResource for detail response
        var photoIds = sequence.Photos.Select(p => p.Id).ToList();
        var photoParams = new Models.V2.PhotoQueryParameters
        {
            Include = "rover,camera",
            FieldSet = "extended",
            IncludeList = new List<string> { "rover", "camera" },
            FieldSetParsed = Models.V2.FieldSetType.Extended
        };
        var photoResources = await _photoService.GetPhotosByIdsAsync(photoIds, photoParams, cancellationToken);

        return ToPanoramaResource(sequence, stitchStatuses, photoResources);
    }

    private async Task<PanoramaSequence?> DetectPanoramaSequenceByIdAsync(
        string panoramaId,
        CancellationToken cancellationToken)
    {
        // Parse panorama ID (format: "pano_curiosity_1000_14")
        var parts = panoramaId.Split('_');
        if (parts.Length != 4 || parts[0] != "pano")
            return null;

        var rover = parts[1];
        if (!int.TryParse(parts[2], out var sol))
            return null;
        if (!int.TryParse(parts[3], out var sequenceIndex))
            return null;

        // Get all panoramas for this rover and sol
        var query = _context.Photos
            .Where(p => p.Rover.Name.ToLower() == rover &&
                       p.Sol == sol &&
                       p.Site.HasValue &&
                       p.Drive.HasValue &&
                       p.MastAz.HasValue &&
                       p.MastEl.HasValue &&
                       p.SpacecraftClock.HasValue &&
                       PanoramicCameras.Contains(p.Camera.Name));

        var photos = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.RoverId)
            .ThenBy(p => p.Site)
            .ThenBy(p => p.Drive)
            .ThenBy(p => p.SpacecraftClock)
            .ThenBy(p => p.MastEl) // Must match GetPanoramasAsync ordering for consistent IDs
            .ToListAsync(cancellationToken);

        var panoramas = DetectPanoramas(photos, MinPhotosForPanorama);

        if (sequenceIndex < 0 || sequenceIndex >= panoramas.Count)
            return null;

        return panoramas[sequenceIndex];
    }

    /// <summary>
    /// Optimized panorama detection that processes sol batches.
    /// Groups photos by location/camera, splits on time gaps, then classifies
    /// as single-row or multi-row based on elevation tier clustering.
    /// </summary>
    private List<PanoramaSequence> DetectPanoramasOptimized(List<Photo> photos, int minPhotos, ref int panoramaIndex)
    {
        var panoramas = new List<PanoramaSequence>();

        // Group by rover, sol, site, drive, and camera
        var allGroups = photos
            .GroupBy(p => new
            {
                p.RoverId,
                p.Sol,
                Site = p.Site ?? 0,
                Drive = p.Drive ?? 0,
                p.CameraId
            })
            .ToList();

        _logger.LogDebug("DetectPanoramasOptimized: {PhotoCount} photos, {GroupCount} total groups, minPhotos={MinPhotos}",
            photos.Count, allGroups.Count, minPhotos);

        var groups = allGroups
            .Where(g => g.Count() >= minPhotos)
            .OrderBy(g => g.Key.RoverId)
            .ThenBy(g => g.Key.Sol)
            .ThenBy(g => g.Key.Site)
            .ThenBy(g => g.Key.Drive)
            .ThenBy(g => g.Key.CameraId)
            .ToList();

        _logger.LogDebug("DetectPanoramasOptimized: {QualifiedGroupCount} groups with >= {MinPhotos} photos",
            groups.Count, minPhotos);

        foreach (var group in groups)
        {
            var groupPhotos = group.OrderBy(p => p.SpacecraftClock).ThenBy(p => p.MastEl).ToList();

            _logger.LogDebug("Processing group: CameraId={CameraId}, Site={Site}, Drive={Drive}, {PhotoCount} photos",
                group.Key.CameraId, group.Key.Site, group.Key.Drive, groupPhotos.Count);

            // Step 1: Split into time-contiguous blocks (no elevation check)
            var blocks = BuildTimeContiguousBlocks(groupPhotos);

            // Step 2: For each block, cluster elevations and classify
            foreach (var block in blocks)
            {
                if (block.Count < minPhotos)
                    continue;

                var tiers = ClusterElevationTiers(block);

                if (tiers.Count <= 1)
                {
                    // Single-row: validate with existing criteria
                    if (IsValidPanorama(block))
                    {
                        var uniqueAz = block
                            .Select(p => Math.Round(p.MastAz ?? 0))
                            .Distinct()
                            .Count();

                        panoramas.Add(new PanoramaSequence
                        {
                            Photos = new List<Photo>(block),
                            Index = panoramaIndex++,
                            IsMultiRow = false,
                            ElevationTierCount = 1,
                            AzimuthColumnCount = uniqueAz
                        });
                    }
                }
                else
                {
                    // Multi-row candidate: validate with mosaic criteria
                    if (IsValidMosaic(block, tiers, out var mosaicMetrics))
                    {
                        var elevations = block.Select(p => p.MastEl ?? 0).ToList();

                        panoramas.Add(new PanoramaSequence
                        {
                            Photos = new List<Photo>(block),
                            Index = panoramaIndex++,
                            IsMultiRow = true,
                            ElevationTierCount = tiers.Count,
                            AzimuthColumnCount = mosaicMetrics!.MaxColumnsPerTier,
                            MinElevation = elevations.Min(),
                            MaxElevation = elevations.Max()
                        });
                    }
                }
            }
        }

        return panoramas;
    }

    /// <summary>
    /// Split photos into time-contiguous blocks using only the time gap threshold.
    /// No elevation check — multi-row mosaics sweep across elevation tiers continuously.
    /// </summary>
    private List<List<Photo>> BuildTimeContiguousBlocks(List<Photo> orderedPhotos)
    {
        var blocks = new List<List<Photo>>();
        var current = new List<Photo>();

        for (int i = 0; i < orderedPhotos.Count; i++)
        {
            if (current.Count == 0)
            {
                current.Add(orderedPhotos[i]);
            }
            else
            {
                var timeDelta = (orderedPhotos[i].SpacecraftClock ?? 0) - (current[^1].SpacecraftClock ?? 0);
                if (timeDelta <= MaxTimeDeltaSeconds && timeDelta >= 0)
                {
                    current.Add(orderedPhotos[i]);
                }
                else
                {
                    blocks.Add(current);
                    current = new List<Photo> { orderedPhotos[i] };
                }
            }
        }

        if (current.Count > 0)
            blocks.Add(current);

        return blocks;
    }

    /// <summary>
    /// Cluster unique elevations into tiers using adaptive gap-based grouping.
    /// Sorts unique rounded elevations, splits when consecutive gap >= ElevationTierGapDegrees.
    /// Returns the center value of each tier.
    /// </summary>
    private static List<float> ClusterElevationTiers(List<Photo> photos)
    {
        var uniqueElevations = photos
            .Select(p => MathF.Round(p.MastEl ?? 0))
            .Distinct()
            .OrderBy(e => e)
            .ToList();

        if (uniqueElevations.Count == 0)
            return new List<float>();

        var tiers = new List<float>();
        var currentTier = new List<float> { uniqueElevations[0] };

        for (int i = 1; i < uniqueElevations.Count; i++)
        {
            if (uniqueElevations[i] - uniqueElevations[i - 1] >= ElevationTierGapDegrees)
            {
                // Gap found — finalize current tier, start new one
                tiers.Add(currentTier.Average());
                currentTier = new List<float> { uniqueElevations[i] };
            }
            else
            {
                currentTier.Add(uniqueElevations[i]);
            }
        }

        tiers.Add(currentTier.Average());
        return tiers;
    }

    /// <summary>
    /// Map a photo's elevation to its nearest tier center value.
    /// </summary>
    private static float GetElevationTier(float elevation, List<float> tiers)
    {
        return tiers.OrderBy(t => Math.Abs(t - elevation)).First();
    }

    private record MosaicMetrics(int MaxColumnsPerTier, int FilledCells, float Completeness);

    /// <summary>
    /// Validate a multi-row mosaic: azimuth range >= 30°, unique positions >= 3,
    /// and grid completeness >= 40%. Returns computed grid metrics on success.
    /// </summary>
    private bool IsValidMosaic(List<Photo> photos, List<float> tiers, out MosaicMetrics? metrics)
    {
        metrics = null;

        if (photos.Count < MinPhotosForPanorama)
            return false;

        var azimuths = photos.Select(p => p.MastAz ?? 0).ToList();
        var azimuthRange = azimuths.Max() - azimuths.Min();

        if (azimuthRange < MinAzimuthRangeDegrees)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - azimuth range {Range}° < {Min}°", azimuthRange, MinAzimuthRangeDegrees);
            return false;
        }

        var uniquePositions = photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .Count();

        if (uniquePositions < MinUniquePositions)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - {UniquePos} unique positions < {Min}", uniquePositions, MinUniquePositions);
            return false;
        }

        // Grid structure: each tier must have enough columns for stitching
        var maxColumnsPerTier = tiers.Max(tier =>
        {
            var tierPhotos = photos.Where(p => GetElevationTier(p.MastEl ?? 0, tiers) == tier);
            return tierPhotos.Select(p => Math.Round(p.MastAz ?? 0)).Distinct().Count();
        });

        if (maxColumnsPerTier < MinUniquePositions)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - max {Cols} columns per tier < {Min} required for stitching",
                maxColumnsPerTier, MinUniquePositions);
            return false;
        }

        // Grid completeness: how many (tier, azimuth) cells are filled vs total possible
        var totalCells = tiers.Count * maxColumnsPerTier;
        var filledCells = 0;
        foreach (var tier in tiers)
        {
            var tierPhotos = photos.Where(p => GetElevationTier(p.MastEl ?? 0, tiers) == tier);
            filledCells += tierPhotos.Select(p => Math.Round(p.MastAz ?? 0)).Distinct().Count();
        }
        var completeness = (float)filledCells / totalCells;

        if (completeness < MinGridCompleteness)
        {
            _logger.LogDebug("IsValidMosaic: FAILED - grid completeness {Completeness:P0} < {Min:P0}",
                completeness, MinGridCompleteness);
            return false;
        }

        _logger.LogDebug("IsValidMosaic: PASSED - {Count} photos, {Tiers} tiers, {Range}° azimuth, {Completeness:P0} grid fill",
            photos.Count, tiers.Count, azimuthRange, completeness);
        metrics = new MosaicMetrics(maxColumnsPerTier, filledCells, completeness);
        return true;
    }

    /// <summary>
    /// Detect panorama sequences from a list of photos (legacy method for single sol)
    /// </summary>
    private List<PanoramaSequence> DetectPanoramas(List<Photo> photos, int minPhotos)
    {
        var panoramaIndex = 0;
        return DetectPanoramasOptimized(photos, minPhotos, ref panoramaIndex);
    }

    /// <summary>
    /// Check if a sequence qualifies as a panorama
    /// </summary>
    private bool IsValidPanorama(List<Photo> photos)
    {
        if (photos.Count < MinPhotosForPanorama)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - {Count} photos < {Min} required",
                photos.Count, MinPhotosForPanorama);
            return false;
        }

        // Check azimuth range
        var azimuths = photos.Select(p => p.MastAz ?? 0).ToList();
        var azimuthRange = azimuths.Max() - azimuths.Min();

        if (azimuthRange < MinAzimuthRangeDegrees)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - azimuth range {Range}° < {Min}° required",
                azimuthRange, MinAzimuthRangeDegrees);
            return false;
        }

        // Count unique positions (round to nearest degree to handle float precision)
        var uniquePositions = photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .Count();

        if (uniquePositions < MinUniquePositions)
        {
            _logger.LogDebug("IsValidPanorama: FAILED - {UniquePos} unique positions < {Min} required",
                uniquePositions, MinUniquePositions);
            return false;
        }

        _logger.LogDebug("IsValidPanorama: PASSED - {Count} photos, {Range}° coverage, {UniquePos} unique positions",
            photos.Count, azimuthRange, uniquePositions);
        return true;
    }

    /// <summary>
    /// Convert panorama sequence to resource DTO
    /// </summary>
    private PanoramaResource ToPanoramaResource(PanoramaSequence sequence,
        Dictionary<string, StitchedPanorama>? stitchStatuses = null,
        List<PhotoResource>? photoResources = null)
    {
        var firstPhoto = sequence.Photos.First();
        var lastPhoto = sequence.Photos.Last();
        var rover = firstPhoto.Rover.Name.ToLowerInvariant();
        var sol = firstPhoto.Sol;

        // Generate panorama ID using sequence index
        var panoramaId = $"pano_{rover}_{sol}_{sequence.Index}";

        // Calculate coverage
        var azimuths = sequence.Photos.Select(p => p.MastAz ?? 0).ToList();
        var coverageDegrees = azimuths.Max() - azimuths.Min();

        // Calculate unique positions (distinct camera angles, rounded to nearest degree)
        var uniqueAzimuths = sequence.Photos
            .Select(p => Math.Round(p.MastAz ?? 0))
            .Distinct()
            .OrderBy(a => a)
            .ToList();
        var uniquePositions = uniqueAzimuths.Count;

        // Calculate average spacing between positions
        float? avgPositionSpacing = null;
        if (uniquePositions > 1)
        {
            var totalSpacing = 0.0;
            for (int i = 1; i < uniqueAzimuths.Count; i++)
            {
                totalSpacing += uniqueAzimuths[i] - uniqueAzimuths[i - 1];
            }
            avgPositionSpacing = (float)(totalSpacing / (uniquePositions - 1));
        }

        // Calculate quality tier
        var quality = GetQualityTier(coverageDegrees, uniquePositions);

        // Get Mars time range (normalize so start <= end for reverse-sweep panoramas)
        string? marsTimeStart = null;
        string? marsTimeEnd = null;
        TimeSpan parsedStart = default, parsedEnd = default;
        bool hasStart = !string.IsNullOrEmpty(firstPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(firstPhoto.DateTakenMars, out parsedStart);
        bool hasEnd = !string.IsNullOrEmpty(lastPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(lastPhoto.DateTakenMars, out parsedEnd);
        if (hasStart) marsTimeStart = MarsTimeHelper.FormatMarsTime(parsedStart);
        if (hasEnd) marsTimeEnd = MarsTimeHelper.FormatMarsTime(parsedEnd);
        if (hasStart && hasEnd && parsedStart > parsedEnd)
        {
            (marsTimeStart, marsTimeEnd) = (marsTimeEnd, marsTimeStart);
        }

        // Average elevation
        var avgElevation = sequence.Photos.Average(p => p.MastEl ?? 0);

        // Build location
        PhotoLocation? location = null;
        if (firstPhoto.Site.HasValue && firstPhoto.Drive.HasValue)
        {
            PhotoCoordinates? coordinates = null;
            if (!string.IsNullOrEmpty(firstPhoto.Xyz) &&
                MarsTimeHelper.TryParseXYZ(firstPhoto.Xyz, out var parsed))
            {
                coordinates = new PhotoCoordinates
                {
                    X = parsed.X,
                    Y = parsed.Y,
                    Z = parsed.Z
                };
            }

            location = new PhotoLocation
            {
                Site = firstPhoto.Site,
                Drive = firstPhoto.Drive,
                Coordinates = coordinates
            };
        }

        return new PanoramaResource
        {
            Id = panoramaId,
            Type = "panorama",
            Photos = photoResources,
            Attributes = new PanoramaAttributes
            {
                Rover = rover,
                Sol = sol,
                MarsTimeStart = marsTimeStart,
                MarsTimeEnd = marsTimeEnd,
                TotalPhotos = sequence.Photos.Count,
                CoverageDegrees = coverageDegrees,
                Location = location,
                Camera = firstPhoto.Camera.Name,
                AvgElevation = avgElevation,
                UniquePositions = uniquePositions,
                AvgPositionSpacing = avgPositionSpacing,
                Quality = quality,
                MosaicType = sequence.IsMultiRow ? "multi_row" : "single_row",
                ElevationRows = sequence.ElevationTierCount,
                ElevationRangeData = sequence.IsMultiRow
                    ? new ElevationRange { Min = sequence.MinElevation, Max = sequence.MaxElevation }
                    : null,
                GridDimensions = sequence.IsMultiRow
                    ? $"{sequence.ElevationTierCount}x{sequence.AzimuthColumnCount}"
                    : null,
                VerticalCoverageDegrees = sequence.IsMultiRow
                    ? sequence.MaxElevation - sequence.MinElevation
                    : null
            },
            Links = new PanoramaLinks
            {
                StitchedPreview = stitchStatuses != null && stitchStatuses.ContainsKey(panoramaId)
                    ? $"/stitch/{panoramaId}/image"
                    : null,
                DownloadSet = $"/api/v2/panoramas/{panoramaId}/download"
            }
        };
    }

    /// <summary>
    /// Determine quality tier based on coverage and unique positions
    /// </summary>
    private static string GetQualityTier(float coverageDegrees, int uniquePositions)
    {
        if (coverageDegrees >= 300 && uniquePositions >= 10) return "full";
        if (coverageDegrees >= 200 && uniquePositions >= 7) return "wide";
        if (coverageDegrees >= 120 && uniquePositions >= 5) return "half";
        return "partial";
    }

    /// <summary>
    /// Internal class to represent a detected panorama sequence
    /// </summary>
    private class PanoramaSequence
    {
        public List<Photo> Photos { get; set; } = new();
        public int Index { get; set; }
        public bool IsMultiRow { get; set; }
        public int ElevationTierCount { get; set; } = 1;
        public int AzimuthColumnCount { get; set; }
        public float MinElevation { get; set; }
        public float MaxElevation { get; set; }
    }
}
