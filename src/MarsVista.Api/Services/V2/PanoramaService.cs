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
    private const float ElevationToleranceDegrees = 2.0f; // Photos within 2 degrees elevation
    private const float MinAzimuthRangeDegrees = 30.0f; // At least 30 degrees coverage
    private const int MinPhotosForPanorama = 3; // At least 3 photos
    private const float MaxTimeDeltaSeconds = 300.0f; // Max 5 minutes between photos

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
        var query = _context.Photos
            .Where(p => p.Site.HasValue &&
                       p.Drive.HasValue &&
                       p.MastAz.HasValue &&
                       p.MastEl.HasValue &&
                       p.SpacecraftClock.HasValue);

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
        }

        if (solMax.HasValue)
        {
            query = query.Where(p => p.Sol <= solMax.Value);
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

                _logger.LogInformation(
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

        var allPanoramas = new List<PanoramaSequence>();
        var panoramaIndex = 0;

        // Process each sol independently to limit memory usage
        foreach (var sol in sols)
        {
            var solPhotos = await query
                .Where(p => p.Sol == sol)
                .Include(p => p.Rover)
                .Include(p => p.Camera)
                .AsNoTracking() // Don't track entities for read-only operations
                .OrderBy(p => p.RoverId)
                .ThenBy(p => p.Site)
                .ThenBy(p => p.Drive)
                .ThenBy(p => p.SpacecraftClock)
                .ToListAsync(cancellationToken);

            // Detect panoramas for this sol
            var solPanoramas = DetectPanoramasOptimized(solPhotos, minPhotos ?? MinPhotosForPanorama, ref panoramaIndex);
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

        // Convert to resources
        var resources = paginatedPanoramas.Select(p => ToPanoramaResource(p)).ToList();

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
                       p.SpacecraftClock.HasValue);

        var photos = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.SpacecraftClock)
            .ToListAsync(cancellationToken);

        var panoramas = DetectPanoramas(photos, MinPhotosForPanorama);

        if (sequenceIndex < 0 || sequenceIndex >= panoramas.Count)
            return null;

        return ToPanoramaResource(panoramas[sequenceIndex]);
    }

    /// <summary>
    /// Optimized panorama detection that processes sol batches
    /// </summary>
    private List<PanoramaSequence> DetectPanoramasOptimized(List<Photo> photos, int minPhotos, ref int panoramaIndex)
    {
        var panoramas = new List<PanoramaSequence>();

        // Group by rover, sol, site, drive, and camera
        // Use IDs for grouping to avoid navigation property issues
        var groups = photos
            .GroupBy(p => new
            {
                p.RoverId,
                p.Sol,
                Site = p.Site ?? 0,
                Drive = p.Drive ?? 0,
                p.CameraId
            })
            .Where(g => g.Count() >= minPhotos);

        foreach (var group in groups)
        {
            // Sort by spacecraft clock
            var groupPhotos = group.OrderBy(p => p.SpacecraftClock).ToList();

            var currentSequence = new List<Photo>();
            float? baseElevation = null;

            for (int i = 0; i < groupPhotos.Count; i++)
            {
                var photo = groupPhotos[i];

                if (currentSequence.Count == 0)
                {
                    // Start new sequence
                    currentSequence.Add(photo);
                    baseElevation = photo.MastEl;
                }
                else
                {
                    var lastPhoto = currentSequence[^1];

                    // Check if this photo continues the sequence
                    var elevationDiff = Math.Abs((photo.MastEl ?? 0) - (baseElevation ?? 0));
                    var timeDelta = (photo.SpacecraftClock ?? 0) - (lastPhoto.SpacecraftClock ?? 0);

                    if (elevationDiff <= ElevationToleranceDegrees &&
                        timeDelta <= MaxTimeDeltaSeconds &&
                        timeDelta > 0)
                    {
                        // Continue current sequence
                        currentSequence.Add(photo);
                    }
                    else
                    {
                        // End current sequence, check if it's valid
                        if (IsValidPanorama(currentSequence))
                        {
                            panoramas.Add(new PanoramaSequence
                            {
                                Photos = new List<Photo>(currentSequence),
                                Index = panoramaIndex++
                            });
                        }

                        // Start new sequence
                        currentSequence.Clear();
                        currentSequence.Add(photo);
                        baseElevation = photo.MastEl;
                    }
                }
            }

            // Check final sequence
            if (IsValidPanorama(currentSequence))
            {
                panoramas.Add(new PanoramaSequence
                {
                    Photos = new List<Photo>(currentSequence),
                    Index = panoramaIndex++
                });
            }
        }

        return panoramas;
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
            return false;

        // Check azimuth range
        var azimuths = photos.Select(p => p.MastAz ?? 0).ToList();
        var azimuthRange = azimuths.Max() - azimuths.Min();

        return azimuthRange >= MinAzimuthRangeDegrees;
    }

    /// <summary>
    /// Convert panorama sequence to resource DTO
    /// </summary>
    private PanoramaResource ToPanoramaResource(PanoramaSequence sequence)
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

        // Get Mars time range
        string? marsTimeStart = null;
        string? marsTimeEnd = null;
        if (!string.IsNullOrEmpty(firstPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(firstPhoto.DateTakenMars, out var startTime))
        {
            marsTimeStart = MarsTimeHelper.FormatMarsTime(startTime);
        }
        if (!string.IsNullOrEmpty(lastPhoto.DateTakenMars) &&
            MarsTimeHelper.TryExtractTimeFromTimestamp(lastPhoto.DateTakenMars, out var endTime))
        {
            marsTimeEnd = MarsTimeHelper.FormatMarsTime(endTime);
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
                AvgElevation = avgElevation
            },
            Links = new PanoramaLinks
            {
                DownloadSet = $"/api/v2/panoramas/{panoramaId}/download"
            }
        };
    }

    /// <summary>
    /// Internal class to represent a detected panorama sequence
    /// </summary>
    private class PanoramaSequence
    {
        public List<Photo> Photos { get; set; } = new();
        public int Index { get; set; }
    }
}
