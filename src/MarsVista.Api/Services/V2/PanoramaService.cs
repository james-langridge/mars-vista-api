using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Services;
using MarsVista.Api.Services;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Reads pre-computed panoramas from the panoramas table (populated by the
/// scraper via PanoramaTableBuilder) and maps them to API resources. Filtering,
/// sorting, and pagination all happen at the database level.
/// </summary>
public class PanoramaService : IPanoramaService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PanoramaService> _logger;
    private readonly IPhotoQueryServiceV2 _photoService;
    private readonly PanoramaDetector _detector;
    private readonly IStaticReferenceCache _referenceCache;

    // When no sol range is given, default to the most recent N sols per the
    // rover-filtered result, matching the previous detection-path default.
    private const int DefaultSolRangeLimit = 500;

    public PanoramaService(
        MarsVistaDbContext context,
        ILogger<PanoramaService> logger,
        IPhotoQueryServiceV2 photoService,
        PanoramaDetector detector,
        IStaticReferenceCache referenceCache)
    {
        _context = context;
        _logger = logger;
        _photoService = photoService;
        _detector = detector;
        _referenceCache = referenceCache;
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
        // Query the pre-computed panoramas table; all filtering, sorting, and
        // pagination happen in the database.
        var query = _context.Panoramas.AsNoTracking();

        // Rover filter via the static reference cache (rover_id, not a name join -
        // see story 052a). Unknown rover names resolve to nothing -> empty result.
        if (!string.IsNullOrWhiteSpace(rovers))
        {
            var roverIds = rovers.Split(',')
                .Select(r => r.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => _referenceCache.GetRoverIdByName(r))
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (roverIds.Count == 0)
            {
                return EmptyResponse(pageNumber, pageSize);
            }

            query = roverIds.Count == 1
                ? query.Where(p => p.RoverId == roverIds[0])
                : query.Where(p => roverIds.Contains(p.RoverId));
        }

        if (solMin.HasValue) query = query.Where(p => p.Sol >= solMin.Value);
        if (solMax.HasValue) query = query.Where(p => p.Sol <= solMax.Value);

        // No sol range -> default to the most recent N sols of the filtered set.
        if (!solMin.HasValue && !solMax.HasValue)
        {
            var maxSol = await query.MaxAsync(p => (int?)p.Sol, cancellationToken);
            if (maxSol.HasValue)
            {
                var defaultSolMin = Math.Max(0, maxSol.Value - DefaultSolRangeLimit);
                query = query.Where(p => p.Sol >= defaultSolMin);
            }
        }

        // min_photos is now a filter over the stored panoramas (total_photos >= N),
        // not a re-detection - so the panorama set and its ids are stable.
        if (minPhotos.HasValue) query = query.Where(p => p.TotalPhotos >= minPhotos.Value);

        if (!string.IsNullOrWhiteSpace(mosaicType))
        {
            var isMultiRow = mosaicType.Equals("multi_row", StringComparison.OrdinalIgnoreCase);
            query = query.Where(p => p.IsMultiRow == isMultiRow);
        }

        if (!string.IsNullOrWhiteSpace(quality))
        {
            var qualityTier = quality.ToLowerInvariant();
            query = query.Where(p => p.QualityTier == qualityTier);
        }

        // Stitch status/method filters via the stitched_panoramas table
        if (!string.IsNullOrWhiteSpace(stitchStatus))
        {
            if (stitchStatus.Equals("not_started", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => !_context.StitchedPanoramas.Any(s => s.PanoramaId == p.PanoramaId));
            }
            else
            {
                var status = stitchStatus.ToLowerInvariant();
                query = query.Where(p => _context.StitchedPanoramas.Any(s => s.PanoramaId == p.PanoramaId && s.Status == status));
            }
        }

        if (!string.IsNullOrWhiteSpace(stitchMethod))
        {
            var method = stitchMethod.ToLowerInvariant();
            query = query.Where(p => _context.StitchedPanoramas.Any(s => s.PanoramaId == p.PanoramaId && s.StitchMethod == method));
        }

        // min_rating filter via a pre-aggregated (non-correlated) subquery on ratings
        if (minRating.HasValue)
        {
            var qualifyingIds = _context.PanoramaRatings
                .GroupBy(r => r.PanoramaId)
                .Where(g => g.Average(r => r.Rating) >= minRating.Value)
                .Select(g => g.Key);
            query = query.Where(p => qualifyingIds.Contains(p.PanoramaId));
        }

        // Sorting
        var sortField = sort?.ToLowerInvariant() ?? "sol";
        var isAscending = order?.Equals("asc", StringComparison.OrdinalIgnoreCase) == true;

        query = sortField switch
        {
            "coverage" => isAscending
                ? query.OrderBy(p => p.CoverageDegrees)
                : query.OrderByDescending(p => p.CoverageDegrees),
            "photos" => isAscending
                ? query.OrderBy(p => p.TotalPhotos)
                : query.OrderByDescending(p => p.TotalPhotos),
            "rating" => isAscending
                ? query.OrderBy(p => _context.PanoramaRatings.Where(r => r.PanoramaId == p.PanoramaId).Average(r => (double?)r.Rating) ?? 0.0)
                : query.OrderByDescending(p => _context.PanoramaRatings.Where(r => r.PanoramaId == p.PanoramaId).Average(r => (double?)r.Rating) ?? 0.0),
            // Default: canonical order (sol, then rover-scoped sequence index)
            _ => query.OrderBy(p => p.Sol).ThenBy(p => p.RoverId).ThenBy(p => p.SequenceIndex)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var pageEntities = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Decorate the page with stitch status and rating aggregates
        var pageIds = pageEntities.Select(p => p.PanoramaId).ToList();

        var stitchMap = pageIds.Count > 0
            ? await _context.StitchedPanoramas.AsNoTracking()
                .Where(s => pageIds.Contains(s.PanoramaId))
                .ToDictionaryAsync(s => s.PanoramaId, cancellationToken)
            : new Dictionary<string, StitchedPanorama>();

        var ratingMap = pageIds.Count > 0
            ? await _context.PanoramaRatings.AsNoTracking()
                .Where(r => pageIds.Contains(r.PanoramaId))
                .GroupBy(r => r.PanoramaId)
                .Select(g => new { PanoramaId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
                .ToDictionaryAsync(x => x.PanoramaId, x => new RatingAggregate(x.Avg, x.Count), cancellationToken)
            : new Dictionary<string, RatingAggregate>();

        var resources = pageEntities.Select(p => MapEntityToResource(p, stitchMap, ratingMap)).ToList();

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

    private static ApiResponse<List<PanoramaResource>> EmptyResponse(int pageNumber, int pageSize) =>
        new(new List<PanoramaResource>())
        {
            Meta = new ResponseMeta { TotalCount = 0, ReturnedCount = 0 },
            Pagination = new PaginationInfo { Page = pageNumber, PerPage = pageSize, TotalPages = 0 }
        };

    public async Task<PanoramaResource?> GetPanoramaByIdAsync(
        string panoramaId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.Panoramas
            .AsNoTracking()
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .FirstOrDefaultAsync(p => p.PanoramaId == panoramaId, cancellationToken);

        if (entity == null)
            return null;

        var stitchRecord = await _context.StitchedPanoramas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId, cancellationToken);

        var stitchMap = stitchRecord != null
            ? new Dictionary<string, StitchedPanorama> { { panoramaId, stitchRecord } }
            : new Dictionary<string, StitchedPanorama>();

        var ratingData = await _context.PanoramaRatings
            .AsNoTracking()
            .Where(r => r.PanoramaId == panoramaId)
            .GroupBy(r => r.PanoramaId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        var ratingMap = ratingData != null
            ? new Dictionary<string, RatingAggregate> { { panoramaId, new RatingAggregate(ratingData.Avg, ratingData.Count) } }
            : new Dictionary<string, RatingAggregate>();

        // Load the constituent photos (by the stored photo_ids) for the detail response
        var photoParams = new Models.V2.PhotoQueryParameters
        {
            Include = "rover,camera",
            FieldSet = "extended",
            IncludeList = new List<string> { "rover", "camera" },
            FieldSetParsed = Models.V2.FieldSetType.Extended
        };
        var photoResources = await _photoService.GetPhotosByIdsAsync(entity.PhotoIds.ToList(), photoParams, cancellationToken);

        return MapEntityToResource(entity, stitchMap, ratingMap, photoResources);
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
    /// Map a stored panorama entity to a resource DTO. The entity already holds
    /// the presentation values (coverage, quality, mosaic geometry, normalized
    /// mars times, location), so no per-photo computation is needed.
    /// </summary>
    private static PanoramaResource MapEntityToResource(
        Panorama p,
        IReadOnlyDictionary<string, StitchedPanorama> stitchMap,
        IReadOnlyDictionary<string, RatingAggregate> ratingMap,
        List<PhotoResource>? photoResources = null)
    {
        stitchMap.TryGetValue(p.PanoramaId, out var stitchRecord);
        ratingMap.TryGetValue(p.PanoramaId, out var ratingAgg);

        PhotoLocation? location = null;
        if (p.Site.HasValue && p.Drive.HasValue)
        {
            PhotoCoordinates? coordinates = null;
            if (p.CoordinateX.HasValue && p.CoordinateY.HasValue && p.CoordinateZ.HasValue)
            {
                coordinates = new PhotoCoordinates
                {
                    X = p.CoordinateX.Value,
                    Y = p.CoordinateY.Value,
                    Z = p.CoordinateZ.Value
                };
            }

            location = new PhotoLocation
            {
                Site = p.Site,
                Drive = p.Drive,
                Coordinates = coordinates
            };
        }

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
            Id = p.PanoramaId,
            Type = "panorama",
            Photos = photoResources,
            Attributes = new PanoramaAttributes
            {
                Rover = p.Rover.Name.ToLowerInvariant(),
                Sol = p.Sol,
                MarsTimeStart = p.MarsTimeStart,
                MarsTimeEnd = p.MarsTimeEnd,
                TotalPhotos = p.TotalPhotos,
                CoverageDegrees = p.CoverageDegrees,
                Location = location,
                Camera = p.Camera.Name,
                AvgElevation = p.AvgElevation,
                UniquePositions = p.UniquePositions,
                AvgPositionSpacing = p.AvgPositionSpacing,
                Quality = p.QualityTier,
                MosaicType = p.IsMultiRow ? "multi_row" : "single_row",
                ElevationRows = p.ElevationTierCount,
                ElevationRangeData = p.IsMultiRow
                    ? new ElevationRange { Min = p.MinElevation ?? 0, Max = p.MaxElevation ?? 0 }
                    : null,
                GridDimensions = p.IsMultiRow
                    ? $"{p.ElevationTierCount}x{p.AzimuthColumnCount}"
                    : null,
                VerticalCoverageDegrees = p.IsMultiRow
                    ? (p.MaxElevation - p.MinElevation)
                    : null,
                Stitch = stitchInfo
            },
            Links = new PanoramaLinks
            {
                StitchedPreview = stitchRecord?.Status == "completed"
                    ? $"/stitch/{p.PanoramaId}/image"
                    : null,
                DownloadSet = $"/api/v2/panoramas/{p.PanoramaId}/download"
            }
        };
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
