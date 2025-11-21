using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Helpers;

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
                .Select(r => r.Trim().ToLowerInvariant())
                .ToList();
            query = query.Where(p => roverList.Contains(p.Rover.Name.ToLower()));
        }

        if (solMin.HasValue)
        {
            query = query.Where(p => p.Sol >= solMin.Value);
        }

        if (solMax.HasValue)
        {
            query = query.Where(p => p.Sol <= solMax.Value);
        }

        // Get all candidate photos ordered by time
        var photos = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.Rover.Name)
            .ThenBy(p => p.Sol)
            .ThenBy(p => p.Site)
            .ThenBy(p => p.Drive)
            .ThenBy(p => p.SpacecraftClock)
            .ToListAsync(cancellationToken);

        // Detect panorama sequences
        var panoramas = DetectPanoramas(photos, minPhotos ?? MinPhotosForPanorama);

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
    /// Detect panorama sequences from a list of photos
    /// </summary>
    private List<PanoramaSequence> DetectPanoramas(List<Entities.Photo> photos, int minPhotos)
    {
        var panoramas = new List<PanoramaSequence>();

        // Group by rover, sol, site, and drive
        var groups = photos
            .GroupBy(p => new
            {
                Rover = p.Rover.Name,
                p.Sol,
                Site = p.Site ?? 0,
                Drive = p.Drive ?? 0,
                Camera = p.Camera.Name
            })
            .Where(g => g.Count() >= minPhotos);

        foreach (var group in groups)
        {
            // Sort by spacecraft clock
            var groupPhotos = group.OrderBy(p => p.SpacecraftClock).ToList();

            var currentSequence = new List<Entities.Photo>();
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
                                Photos = new List<Entities.Photo>(currentSequence)
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
                    Photos = new List<Entities.Photo>(currentSequence)
                });
            }
        }

        return panoramas;
    }

    /// <summary>
    /// Check if a sequence qualifies as a panorama
    /// </summary>
    private bool IsValidPanorama(List<Entities.Photo> photos)
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

        // Generate panorama ID
        var panoramaId = $"pano_{rover}_{sol}_{sequence.GetHashCode():X8}";

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
        public List<Entities.Photo> Photos { get; set; } = new();
    }
}
