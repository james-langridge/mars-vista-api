using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;
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
    private readonly PanoramaDetector _detector;

    // Performance optimization: Limit sol range to prevent loading all photos into memory
    // TODO: Long-term solution should pre-compute panoramas in a dedicated table (see .claude/decisions/PANORAMA_OPTIMIZATION.md)
    private const int DefaultSolRangeLimit = 500; // Default to most recent 500 sols when no range specified

    public PanoramaService(
        MarsVistaDbContext context,
        ILogger<PanoramaService> logger,
        IPhotoQueryServiceV2 photoService,
        PanoramaDetector detector)
    {
        _context = context;
        _logger = logger;
        _photoService = photoService;
        _detector = detector;
    }

    public async Task<ApiResponse<List<PanoramaResource>>> GetPanoramasAsync(
        string? rovers = null,
        int? solMin = null,
        int? solMax = null,
        int? minPhotos = null,
        string? stitchStatus = null,
        string? stitchMethod = null,
        string? mosaicType = null,
        string? quality = null,
        double? minRating = null,
        string? sort = null,
        string? order = null,
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
                       PanoramaDetector.PanoramicCameras.Contains(p.Camera.Name));

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
            var solPanoramas = _detector.DetectPanoramasOptimized(solPhotos, minPhotos ?? PanoramaDetector.MinPhotosForPanorama, ref panoramaIndex);

            if (sols.Count <= 5 || solPanoramas.Count > 0)
            {
                _logger.LogDebug("Sol {Sol}: {PhotoCount} photos, {PanoramaCount} panoramas detected",
                    sol, solPhotos.Count, solPanoramas.Count);
            }

            allPanoramas.AddRange(solPanoramas);
        }

        // Apply in-memory filters on detected panoramas
        var filtered = (IEnumerable<PanoramaSequence>)allPanoramas;

        if (!string.IsNullOrWhiteSpace(mosaicType))
        {
            var isMr = mosaicType.Equals("multi_row", StringComparison.OrdinalIgnoreCase);
            filtered = filtered.Where(p => p.IsMultiRow == isMr);
        }

        if (!string.IsNullOrWhiteSpace(quality))
        {
            filtered = filtered.Where(p =>
            {
                var azimuths = p.Photos.Select(ph => ph.MastAz ?? 0);
                var coverage = azimuths.Max() - azimuths.Min();
                var positions = p.Photos.Select(ph => Math.Round(ph.MastAz ?? 0)).Distinct().Count();
                return PanoramaDetector.GetQualityTier(coverage, positions).Equals(quality, StringComparison.OrdinalIgnoreCase);
            });
        }

        var panoramas = filtered.ToList();

        // Build panorama IDs for all remaining panoramas (needed for stitch/rating filters)
        var allPanoramaIds = panoramas.Select(p => GetPanoramaId(p)).ToList();

        // Load stitch statuses and ratings for filtering
        var allStitchStatuses = allPanoramaIds.Count > 0
            ? await _context.StitchedPanoramas
                .AsNoTracking()
                .Where(s => allPanoramaIds.Contains(s.PanoramaId))
                .ToDictionaryAsync(s => s.PanoramaId, cancellationToken)
            : new Dictionary<string, StitchedPanorama>();

        var allRatingAggregates = allPanoramaIds.Count > 0
            ? await _context.PanoramaRatings
                .AsNoTracking()
                .Where(r => allPanoramaIds.Contains(r.PanoramaId))
                .GroupBy(r => r.PanoramaId)
                .Select(g => new { PanoramaId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
                .ToDictionaryAsync(
                    r => r.PanoramaId,
                    r => new RatingAggregate(r.Avg, r.Count),
                    cancellationToken)
            : new Dictionary<string, RatingAggregate>();

        // Apply stitch/rating filters
        if (!string.IsNullOrWhiteSpace(stitchStatus))
        {
            panoramas = panoramas.Where(p =>
            {
                var pid = GetPanoramaId(p);
                if (stitchStatus.Equals("not_started", StringComparison.OrdinalIgnoreCase))
                    return !allStitchStatuses.ContainsKey(pid);
                return allStitchStatuses.TryGetValue(pid, out var s) &&
                       s.Status.Equals(stitchStatus, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        if (!string.IsNullOrWhiteSpace(stitchMethod))
        {
            panoramas = panoramas.Where(p =>
            {
                var pid = GetPanoramaId(p);
                return allStitchStatuses.TryGetValue(pid, out var s) &&
                       s.StitchMethod != null &&
                       s.StitchMethod.Equals(stitchMethod, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        if (minRating.HasValue)
        {
            panoramas = panoramas.Where(p =>
            {
                var pid = GetPanoramaId(p);
                return allRatingAggregates.TryGetValue(pid, out var r) && r.Average >= minRating.Value;
            }).ToList();
        }

        // Apply sorting
        var sortField = sort?.ToLowerInvariant() ?? "sol";
        var isAscending = order?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true;

        panoramas = sortField switch
        {
            "rating" => isAscending
                ? panoramas.OrderBy(p => allRatingAggregates.TryGetValue(GetPanoramaId(p), out var r) ? r.Average : 0).ToList()
                : panoramas.OrderByDescending(p => allRatingAggregates.TryGetValue(GetPanoramaId(p), out var r) ? r.Average : 0).ToList(),
            "coverage" => isAscending
                ? panoramas.OrderBy(p => p.Photos.Select(ph => ph.MastAz ?? 0).Max() - p.Photos.Select(ph => ph.MastAz ?? 0).Min()).ToList()
                : panoramas.OrderByDescending(p => p.Photos.Select(ph => ph.MastAz ?? 0).Max() - p.Photos.Select(ph => ph.MastAz ?? 0).Min()).ToList(),
            "photos" => isAscending
                ? panoramas.OrderBy(p => p.Photos.Count).ToList()
                : panoramas.OrderByDescending(p => p.Photos.Count).ToList(),
            _ => panoramas // Default: sol order (already in sol order from detection)
        };

        // Apply pagination
        var totalCount = panoramas.Count;
        var paginatedPanoramas = panoramas
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Build stitch/rating dictionaries for just the paginated results
        var paginatedIds = new HashSet<string>(paginatedPanoramas.Select(p => GetPanoramaId(p)));

        var stitchStatuses = allStitchStatuses
            .Where(kv => paginatedIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var ratingAggregates = allRatingAggregates
            .Where(kv => paginatedIds.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        // Convert to resources
        var resources = paginatedPanoramas.Select(p => ToPanoramaResource(p, stitchStatuses, ratingAggregates: ratingAggregates)).ToList();

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
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId, cancellationToken);

        var stitchStatuses = stitchRecord != null
            ? new Dictionary<string, StitchedPanorama> { { panoramaId, stitchRecord } }
            : null;

        // Load rating aggregate for this panorama
        var ratingData = await _context.PanoramaRatings
            .AsNoTracking()
            .Where(r => r.PanoramaId == panoramaId)
            .GroupBy(r => r.PanoramaId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var ratingAggregates = ratingData != null
            ? new Dictionary<string, RatingAggregate> { { panoramaId, new RatingAggregate(ratingData.Avg, ratingData.Count) } }
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

        return ToPanoramaResource(sequence, stitchStatuses, photoResources, ratingAggregates);
    }

    public async Task<RatingResponse> UpsertRatingAsync(
        string panoramaId,
        string clientId,
        int rating,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.PanoramaRatings
            .AsTracking()
            .FirstOrDefaultAsync(r => r.PanoramaId == panoramaId && r.ClientId == clientId, cancellationToken);

        if (existing != null)
        {
            existing.Rating = rating;
        }
        else
        {
            _context.PanoramaRatings.Add(new PanoramaRating
            {
                PanoramaId = panoramaId,
                Rating = rating,
                ClientId = clientId
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetRatingAsync(panoramaId, clientId, cancellationToken);
    }

    public async Task<RatingResponse> GetRatingAsync(
        string panoramaId,
        string? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await _context.PanoramaRatings
            .AsNoTracking()
            .Where(r => r.PanoramaId == panoramaId)
            .GroupBy(r => r.PanoramaId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        int? userRating = null;
        if (!string.IsNullOrEmpty(clientId))
        {
            userRating = await _context.PanoramaRatings
                .AsNoTracking()
                .Where(r => r.PanoramaId == panoramaId && r.ClientId == clientId)
                .Select(r => (int?)r.Rating)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new RatingResponse
        {
            AverageRating = aggregate != null ? Math.Round(aggregate.Avg, 1) : 0,
            RatingCount = aggregate?.Count ?? 0,
            UserRating = userRating
        };
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
                       PanoramaDetector.PanoramicCameras.Contains(p.Camera.Name));

        var photos = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.RoverId)
            .ThenBy(p => p.Site)
            .ThenBy(p => p.Drive)
            .ThenBy(p => p.SpacecraftClock)
            .ThenBy(p => p.MastEl) // Must match GetPanoramasAsync ordering for consistent IDs
            .ToListAsync(cancellationToken);

        var panoramas = _detector.DetectPanoramas(photos, PanoramaDetector.MinPhotosForPanorama);

        if (sequenceIndex < 0 || sequenceIndex >= panoramas.Count)
            return null;

        return panoramas[sequenceIndex];
    }

    /// <summary>
    /// Convert panorama sequence to resource DTO
    /// </summary>
    private PanoramaResource ToPanoramaResource(PanoramaSequence sequence,
        Dictionary<string, StitchedPanorama>? stitchStatuses = null,
        List<PhotoResource>? photoResources = null,
        Dictionary<string, RatingAggregate>? ratingAggregates = null)
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
        var quality = PanoramaDetector.GetQualityTier(coverageDegrees, uniquePositions);

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

        // Build StitchInfo from stitch status and rating aggregates
        // Always include StitchInfo for consistent API responses (never null)
        StitchedPanorama? stitchRecord = null;
        stitchStatuses?.TryGetValue(panoramaId, out stitchRecord);
        RatingAggregate? ratingAgg = null;
        ratingAggregates?.TryGetValue(panoramaId, out ratingAgg);

        var stitchInfo = new StitchInfo
        {
            Status = stitchRecord?.Status ?? "not_started",
            Method = stitchRecord?.StitchMethod,
            Width = stitchRecord?.Status == "completed" ? stitchRecord.ImageWidth : null,
            Height = stitchRecord?.Status == "completed" ? stitchRecord.ImageHeight : null,
            AverageRating = ratingAgg != null ? Math.Round(ratingAgg.Average, 1) : null,
            RatingCount = ratingAgg?.Count
        };

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
                    : null,
                Stitch = stitchInfo
            },
            Links = new PanoramaLinks
            {
                StitchedPreview = stitchRecord?.Status == "completed"
                    ? $"/stitch/{panoramaId}/image"
                    : null,
                DownloadSet = $"/api/v2/panoramas/{panoramaId}/download"
            }
        };
    }

    /// <summary>
    /// Get panorama ID string from a sequence
    /// </summary>
    private static string GetPanoramaId(PanoramaSequence sequence)
    {
        var first = sequence.Photos.First();
        return $"pano_{first.Rover.Name.ToLowerInvariant()}_{first.Sol}_{sequence.Index}";
    }

    /// <summary>
    /// Rating aggregate for a panorama
    /// </summary>
    private record RatingAggregate(double Average, int Count);
}
