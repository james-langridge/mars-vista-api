using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Entities;
using MarsVista.Core.Helpers;
using MarsVista.Api.Models.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of v2 photo query service
/// Handles complex filtering, pagination, sorting, and field selection
/// </summary>
public class PhotoQueryServiceV2 : IPhotoQueryServiceV2
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PhotoQueryServiceV2> _logger;

    public PhotoQueryServiceV2(MarsVistaDbContext context, ILogger<PhotoQueryServiceV2> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<PhotoResource>>> QueryPhotosAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Build query with ALL filters (including Mars time) at the database level
        var query = BuildQuery(parameters);

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, parameters);

        // Apply pagination
        var skip = (parameters.PageNumber - 1) * parameters.PageSize;
        var paginatedQuery = query.Skip(skip).Take(parameters.PageSize);

        // Eager load related entities if needed
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            paginatedQuery = paginatedQuery
                .Include(p => p.Rover)
                .Include(p => p.Camera);
        }

        // Execute query (everything happens in database now!)
        var photos = await paginatedQuery.ToListAsync(cancellationToken);

        // Map to DTOs
        var photoDtos = photos.Select(p => MapToPhotoResource(p, parameters)).ToList();

        // Build pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

        var response = new ApiResponse<List<PhotoResource>>(photoDtos)
        {
            Meta = new ResponseMeta
            {
                TotalCount = totalCount,
                ReturnedCount = photoDtos.Count,
                Query = BuildQueryMetadata(parameters)
            },
            Pagination = new PaginationInfo
            {
                Page = parameters.PageNumber,
                PerPage = parameters.PageSize,
                TotalPages = totalPages
            }
        };

        return response;
    }

    public async Task<PhotoResource?> GetPhotoByIdAsync(
        int id,
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Photos.Where(p => p.Id == id);

        // Eager load if requested
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            query = query.Include(p => p.Rover).Include(p => p.Camera);
        }

        var photo = await query.FirstOrDefaultAsync(cancellationToken);

        return photo == null ? null : MapToPhotoResource(photo, parameters);
    }

    public async Task<List<PhotoResource>> GetPhotosByIdsAsync(
        List<int> ids,
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Photos.Where(p => ids.Contains(p.Id));

        // Eager load if requested
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            query = query.Include(p => p.Rover).Include(p => p.Camera);
        }

        // Maintain the order of the requested IDs
        var photos = await query.ToListAsync(cancellationToken);

        // Create a dictionary for quick lookup
        var photoDict = photos.ToDictionary(p => p.Id);

        // Return photos in the order they were requested (skip missing ones)
        return ids
            .Where(id => photoDict.ContainsKey(id))
            .Select(id => MapToPhotoResource(photoDict[id], parameters))
            .ToList();
    }

    public async Task<int> GetPhotoCountAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(parameters);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<PhotoStatisticsResponse> GetStatisticsAsync(
        PhotoQueryParameters parameters,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(parameters);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get date range
        var minDate = await query.MinAsync(p => (DateTime?)p.EarthDate, cancellationToken);
        var maxDate = await query.MaxAsync(p => (DateTime?)p.EarthDate, cancellationToken);

        var response = new PhotoStatisticsResponse
        {
            TotalPhotos = totalCount,
            Period = new PeriodInfo
            {
                From = minDate?.ToString("yyyy-MM-dd"),
                To = maxDate?.ToString("yyyy-MM-dd")
            }
        };

        // Group by the requested dimension - all return unified StatisticsGroup list
        response.Groups = groupBy.ToLower() switch
        {
            "camera" => await GetCameraStatistics(query, totalCount, cancellationToken),
            "rover" => await GetRoverStatistics(query, totalCount, cancellationToken),
            "sol" => await GetSolStatistics(query, totalCount, cancellationToken),
            _ => new List<StatisticsGroup>()
        };

        return response;
    }

    /// <summary>
    /// Get statistics grouped by camera
    /// </summary>
    private async Task<List<StatisticsGroup>> GetCameraStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Camera.Name)
            .Select(g => new
            {
                Key = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Key,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by rover
    /// </summary>
    private async Task<List<StatisticsGroup>> GetRoverStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Rover.Name)
            .Select(g => new
            {
                Key = g.Key,
                Count = g.Count(),
                MinSol = g.Min(p => p.Sol),
                MaxSol = g.Max(p => p.Sol)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Key,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0,
            AvgPerSol = s.MaxSol > s.MinSol ? Math.Round(s.Count / (double)(s.MaxSol - s.MinSol + 1), 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by sol (limited to top 100 sols)
    /// </summary>
    private async Task<List<StatisticsGroup>> GetSolStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => new { p.Sol, p.EarthDate })
            .Select(g => new
            {
                Sol = g.Key.Sol,
                EarthDate = g.Key.EarthDate,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(100) // Limit to top 100 sols to avoid huge responses
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Sol.ToString(),
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0,
            EarthDate = s.EarthDate?.ToString("yyyy-MM-dd")
        }).ToList();
    }

    /// <summary>
    /// Build the base filtered query
    /// </summary>
    private IQueryable<Photo> BuildQuery(PhotoQueryParameters parameters)
    {
        var query = _context.Photos.AsNoTracking().AsQueryable();

        // Filter by NASA ID (partial match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(parameters.NasaId))
        {
            var nasaIdPattern = parameters.NasaId.Trim();
            query = query.Where(p => EF.Functions.ILike(p.NasaId, $"%{nasaIdPattern}%"));
        }

        // Filter by rovers
        if (parameters.RoverList.Count > 0)
        {
            query = query.Where(p => parameters.RoverList.Contains(p.Rover.Name.ToLower()));
        }

        // Filter by cameras
        if (parameters.CameraList.Count > 0)
        {
            query = query.Where(p => parameters.CameraList.Contains(p.Camera.Name.ToUpper()));
        }

        // Filter by sol range
        if (parameters.SolMin.HasValue)
        {
            query = query.Where(p => p.Sol >= parameters.SolMin.Value);
        }

        if (parameters.SolMax.HasValue)
        {
            query = query.Where(p => p.Sol <= parameters.SolMax.Value);
        }

        // Filter by date range
        if (parameters.DateMinParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate >= parameters.DateMinParsed.Value);
        }

        if (parameters.DateMaxParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate <= parameters.DateMaxParsed.Value);
        }

        // Mars time filtering (database-level using indexed mars_time_hour column)
        if (parameters.MarsTimeGoldenHour == true)
        {
            // Golden hour: hours 5-7 (morning) and 17-19 (evening)
            query = query.Where(p => p.MarsTimeHour.HasValue &&
                (p.MarsTimeHour.Value >= 5 && p.MarsTimeHour.Value <= 7 ||
                 p.MarsTimeHour.Value >= 17 && p.MarsTimeHour.Value <= 19));
        }
        else if (parameters.MarsTimeMinParsed.HasValue || parameters.MarsTimeMaxParsed.HasValue)
        {
            // Range-based Mars time filtering by hour
            if (parameters.MarsTimeMinParsed.HasValue)
            {
                var minHour = parameters.MarsTimeMinParsed.Value.Hours;
                query = query.Where(p => p.MarsTimeHour.HasValue && p.MarsTimeHour.Value >= minHour);
            }

            if (parameters.MarsTimeMaxParsed.HasValue)
            {
                var maxHour = parameters.MarsTimeMaxParsed.Value.Hours;
                query = query.Where(p => p.MarsTimeHour.HasValue && p.MarsTimeHour.Value <= maxHour);
            }
        }

        // Filter by location - site/drive ranges
        if (parameters.SiteMin.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value >= parameters.SiteMin.Value);
        }

        if (parameters.SiteMax.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value <= parameters.SiteMax.Value);
        }

        if (parameters.DriveMin.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value >= parameters.DriveMin.Value);
        }

        if (parameters.DriveMax.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value <= parameters.DriveMax.Value);
        }

        // Location proximity search (requires site, drive, and radius)
        if (parameters.Site.HasValue && parameters.Drive.HasValue && parameters.LocationRadius.HasValue)
        {
            var targetSite = parameters.Site.Value;
            var targetDrive = parameters.Drive.Value;
            var radius = parameters.LocationRadius.Value;

            query = query.Where(p =>
                p.Site.HasValue && p.Drive.HasValue &&
                p.Site.Value == targetSite &&
                p.Drive.Value >= targetDrive - radius &&
                p.Drive.Value <= targetDrive + radius);
        }

        // Filter by image dimensions
        if (parameters.MinWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value >= parameters.MinWidth.Value);
        }

        if (parameters.MaxWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value <= parameters.MaxWidth.Value);
        }

        if (parameters.MinHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value >= parameters.MinHeight.Value);
        }

        if (parameters.MaxHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value <= parameters.MaxHeight.Value);
        }

        // Filter by sample type
        if (parameters.SampleTypeList.Count > 0)
        {
            query = query.Where(p => parameters.SampleTypeList.Contains(p.SampleType));
        }

        // Filter by aspect ratio using indexed computed column (50%+ faster)
        if (parameters.AspectRatioParsed.HasValue)
        {
            var (targetWidth, targetHeight) = parameters.AspectRatioParsed.Value;
            var aspectRatio = (decimal)targetWidth / targetHeight;
            var tolerance = 0.1m; // 10% tolerance for aspect ratio matching

            // For 16:9 (1.777), accept ratios between 1.6 and 1.95
            var minRatio = aspectRatio - tolerance;
            var maxRatio = aspectRatio + tolerance;

            // Use indexed AspectRatio column instead of calculating in query
            query = query.Where(p => p.AspectRatio.HasValue &&
                                   p.AspectRatio.Value >= minRatio &&
                                   p.AspectRatio.Value <= maxRatio);
        }

        // Filter by camera angles
        if (parameters.MastElevationMin.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value >= parameters.MastElevationMin.Value);
        }

        if (parameters.MastElevationMax.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value <= parameters.MastElevationMax.Value);
        }

        if (parameters.MastAzimuthMin.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value >= parameters.MastAzimuthMin.Value);
        }

        if (parameters.MastAzimuthMax.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value <= parameters.MastAzimuthMax.Value);
        }

        return query;
    }

    /// <summary>
    /// Apply sorting to the query
    /// </summary>
    private IQueryable<Photo> ApplySorting(IQueryable<Photo> query, PhotoQueryParameters parameters)
    {
        if (parameters.SortFields.Count == 0)
        {
            // Default sort: most recent first
            return query.OrderByDescending(p => p.DateTakenUtc);
        }

        IOrderedQueryable<Photo>? orderedQuery = null;

        foreach (var sortField in parameters.SortFields)
        {
            var isDescending = sortField.Direction == SortDirection.Descending;

            // First sort field uses OrderBy/OrderByDescending
            // Subsequent fields use ThenBy/ThenByDescending
            if (orderedQuery == null)
            {
                orderedQuery = sortField.Field switch
                {
                    "id" => isDescending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
                    "sol" => isDescending ? query.OrderByDescending(p => p.Sol) : query.OrderBy(p => p.Sol),
                    "earth_date" => isDescending ? query.OrderByDescending(p => p.EarthDate) : query.OrderBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? query.OrderByDescending(p => p.DateTakenUtc) : query.OrderBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? query.OrderByDescending(p => p.Camera.Name) : query.OrderBy(p => p.Camera.Name),
                    "created_at" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    _ => isDescending ? query.OrderByDescending(p => p.DateTakenUtc) : query.OrderBy(p => p.DateTakenUtc)
                };
            }
            else
            {
                orderedQuery = sortField.Field switch
                {
                    "id" => isDescending ? orderedQuery.ThenByDescending(p => p.Id) : orderedQuery.ThenBy(p => p.Id),
                    "sol" => isDescending ? orderedQuery.ThenByDescending(p => p.Sol) : orderedQuery.ThenBy(p => p.Sol),
                    "earth_date" => isDescending ? orderedQuery.ThenByDescending(p => p.EarthDate) : orderedQuery.ThenBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? orderedQuery.ThenByDescending(p => p.DateTakenUtc) : orderedQuery.ThenBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? orderedQuery.ThenByDescending(p => p.Camera.Name) : orderedQuery.ThenBy(p => p.Camera.Name),
                    "created_at" => isDescending ? orderedQuery.ThenByDescending(p => p.CreatedAt) : orderedQuery.ThenBy(p => p.CreatedAt),
                    _ => orderedQuery
                };
            }
        }

        return orderedQuery ?? query.OrderByDescending(p => p.DateTakenUtc);
    }

    /// <summary>
    /// Map Photo entity to PhotoResource DTO with field selection
    /// </summary>
    private PhotoResource MapToPhotoResource(Photo photo, PhotoQueryParameters parameters)
    {
        var hasFieldSelection = parameters.FieldList.Count > 0;
        var includeRover = parameters.IncludeList.Contains("rover");
        var includeCamera = parameters.IncludeList.Contains("camera");

        // Helper to check if a field should be included
        bool ShouldInclude(string field) => !hasFieldSelection || parameters.FieldList.Contains(field);

        // Helper to check if any field in a group should be included
        bool ShouldIncludeAny(params string[] fields) => !hasFieldSelection || fields.Any(f => parameters.FieldList.Contains(f));

        // Build images object (nested structure)
        PhotoImages? images = null;
        if (ShouldIncludeAny("images", "img_src_small", "img_src_medium", "img_src_large", "img_src_full"))
        {
            images = new PhotoImages
            {
                Small = !string.IsNullOrEmpty(photo.ImgSrcSmall) ? photo.ImgSrcSmall : null,
                Medium = !string.IsNullOrEmpty(photo.ImgSrcMedium) ? photo.ImgSrcMedium : null,
                Large = !string.IsNullOrEmpty(photo.ImgSrcLarge) ? photo.ImgSrcLarge : null,
                Full = !string.IsNullOrEmpty(photo.ImgSrcFull) ? photo.ImgSrcFull : null
            };
        }

        // Build dimensions object
        PhotoDimensions? dimensions = null;
        if (ShouldIncludeAny("dimensions", "width", "height") && photo.Width.HasValue && photo.Height.HasValue)
        {
            dimensions = new PhotoDimensions
            {
                Width = photo.Width.Value,
                Height = photo.Height.Value
            };
        }

        // Build location object with coordinates (only if there's actual location data)
        PhotoLocation? location = null;
        if (ShouldIncludeAny("location", "site", "drive", "xyz"))
        {
            // Only create location if we have site, drive, or XYZ coordinates
            if (photo.Site.HasValue || photo.Drive.HasValue || !string.IsNullOrEmpty(photo.Xyz))
            {
                PhotoCoordinates? coordinates = null;
                if (!string.IsNullOrEmpty(photo.Xyz))
                {
                    // Parse XYZ string "(35.4362,22.5714,-9.46445)" to coordinates
                    if (MarsVista.Core.Helpers.MarsTimeHelper.TryParseXYZ(photo.Xyz, out var parsed))
                    {
                        coordinates = new PhotoCoordinates
                        {
                            X = parsed.X,
                            Y = parsed.Y,
                            Z = parsed.Z
                        };
                    }
                }

                location = new PhotoLocation
                {
                    Site = photo.Site,
                    Drive = photo.Drive,
                    Coordinates = coordinates
                };
            }
        }

        // Build telemetry object
        PhotoTelemetry? telemetry = null;
        if (ShouldIncludeAny("telemetry", "mast_az", "mast_el", "mast_azimuth", "mast_elevation", "spacecraft_clock"))
        {
            if (photo.MastAz.HasValue || photo.MastEl.HasValue || photo.SpacecraftClock.HasValue)
            {
                telemetry = new PhotoTelemetry
                {
                    MastAzimuth = photo.MastAz,
                    MastElevation = photo.MastEl,
                    SpacecraftClock = photo.SpacecraftClock
                };
            }
        }

        // Include raw_data only for "complete" field set
        object? rawData = null;
        if (parameters.FieldSetParsed == FieldSetType.Complete && photo.RawData != null)
        {
            // Convert JsonDocument to object for serialization
            rawData = System.Text.Json.JsonSerializer.Deserialize<object>(photo.RawData.RootElement.GetRawText());
        }

        var attributes = new PhotoAttributes
        {
            NasaId = ShouldInclude("nasa_id") ? photo.NasaId : null,
            Sol = ShouldInclude("sol") ? photo.Sol : null,
            EarthDate = ShouldInclude("earth_date") ? photo.EarthDate?.ToString("yyyy-MM-dd") : null,
            DateTakenUtc = ShouldInclude("date_taken_utc") ? photo.DateTakenUtc : null,
            DateTakenMars = ShouldInclude("date_taken_mars") ? photo.DateTakenMars : null,
            Images = images,
            Dimensions = dimensions,
            SampleType = ShouldInclude("sample_type") ? photo.SampleType : null,
            Location = location,
            Telemetry = telemetry,
            Title = ShouldInclude("title") ? photo.Title : null,
            Caption = ShouldInclude("caption") ? photo.Caption : null,
            Credit = ShouldInclude("credit") ? photo.Credit : null,
            CreatedAt = ShouldInclude("created_at") ? photo.CreatedAt : null,
            // Legacy field for backwards compatibility
            ImgSrc = ShouldInclude("img_src") ? photo.ImgSrcLarge : null,
            // Raw NASA data (only for complete field set)
            RawData = rawData
        };

        PhotoRelationships? relationships = null;

        if (includeRover || includeCamera)
        {
            relationships = new PhotoRelationships
            {
                Rover = includeRover ? new ResourceReference
                {
                    Id = photo.Rover.Name.ToLowerInvariant(),
                    Type = "rover",
                    Attributes = new
                    {
                        name = photo.Rover.Name,
                        status = photo.Rover.Status
                    }
                } : null,
                Camera = includeCamera ? new CameraReference
                {
                    Id = photo.Camera.Name,
                    Type = "camera",
                    Attributes = new CameraAttributes
                    {
                        FullName = photo.Camera.FullName
                    }
                } : null
            };
        }

        return new PhotoResource
        {
            Id = photo.Id,
            Type = "photo",
            Attributes = attributes,
            Relationships = relationships
        };
    }

    /// <summary>
    /// Build query metadata for response
    /// </summary>
    private Dictionary<string, object> BuildQueryMetadata(PhotoQueryParameters parameters)
    {
        var metadata = new Dictionary<string, object>();

        if (parameters.RoverList.Count > 0)
            metadata["rovers"] = parameters.RoverList;

        if (parameters.CameraList.Count > 0)
            metadata["cameras"] = parameters.CameraList;

        if (parameters.SolMin.HasValue)
            metadata["sol_min"] = parameters.SolMin.Value;

        if (parameters.SolMax.HasValue)
            metadata["sol_max"] = parameters.SolMax.Value;

        if (parameters.DateMin != null)
            metadata["date_min"] = parameters.DateMin;

        if (parameters.DateMax != null)
            metadata["date_max"] = parameters.DateMax;

        return metadata;
    }

    /// <summary>
    /// Build query without Mars time filters (for client-side evaluation path)
    /// </summary>
    private IQueryable<Photo> BuildQueryWithoutMarsTime(PhotoQueryParameters parameters)
    {
        var query = _context.Photos.AsQueryable();

        // Filter by rovers
        if (parameters.RoverList.Count > 0)
        {
            query = query.Where(p => parameters.RoverList.Contains(p.Rover.Name.ToLower()));
        }

        // Filter by cameras
        if (parameters.CameraList.Count > 0)
        {
            query = query.Where(p => parameters.CameraList.Contains(p.Camera.Name.ToUpper()));
        }

        // Filter by sol range
        if (parameters.SolMin.HasValue)
        {
            query = query.Where(p => p.Sol >= parameters.SolMin.Value);
        }

        if (parameters.SolMax.HasValue)
        {
            query = query.Where(p => p.Sol <= parameters.SolMax.Value);
        }

        // Filter by date range
        if (parameters.DateMinParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate >= parameters.DateMinParsed.Value);
        }

        if (parameters.DateMaxParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate <= parameters.DateMaxParsed.Value);
        }

        // Mars time filters require non-empty DateTakenMars
        if (parameters.MarsTimeMinParsed.HasValue || parameters.MarsTimeMaxParsed.HasValue ||
            parameters.MarsTimeGoldenHour == true)
        {
            query = query.Where(p => !string.IsNullOrEmpty(p.DateTakenMars));
        }

        // All other filters remain the same (location, dimensions, sample type, etc.)
        // Copy from BuildQuery method...

        // Filter by location - site/drive ranges
        if (parameters.SiteMin.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value >= parameters.SiteMin.Value);
        }

        if (parameters.SiteMax.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value <= parameters.SiteMax.Value);
        }

        if (parameters.DriveMin.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value >= parameters.DriveMin.Value);
        }

        if (parameters.DriveMax.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value <= parameters.DriveMax.Value);
        }

        // Location proximity search
        if (parameters.Site.HasValue && parameters.Drive.HasValue && parameters.LocationRadius.HasValue)
        {
            var targetSite = parameters.Site.Value;
            var targetDrive = parameters.Drive.Value;
            var radius = parameters.LocationRadius.Value;

            query = query.Where(p =>
                p.Site.HasValue && p.Drive.HasValue &&
                p.Site.Value == targetSite &&
                p.Drive.Value >= targetDrive - radius &&
                p.Drive.Value <= targetDrive + radius);
        }

        // Filter by image dimensions
        if (parameters.MinWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value >= parameters.MinWidth.Value);
        }

        if (parameters.MaxWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value <= parameters.MaxWidth.Value);
        }

        if (parameters.MinHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value >= parameters.MinHeight.Value);
        }

        if (parameters.MaxHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value <= parameters.MaxHeight.Value);
        }

        // Filter by sample type
        if (parameters.SampleTypeList.Count > 0)
        {
            query = query.Where(p => parameters.SampleTypeList.Contains(p.SampleType));
        }

        // Filter by camera angles
        if (parameters.MastAzimuthMin.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value >= parameters.MastAzimuthMin.Value);
        }

        if (parameters.MastAzimuthMax.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value <= parameters.MastAzimuthMax.Value);
        }

        if (parameters.MastElevationMin.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value >= parameters.MastElevationMin.Value);
        }

        if (parameters.MastElevationMax.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value <= parameters.MastElevationMax.Value);
        }

        return query;
    }

    /// <summary>
    /// Apply Mars time filtering in memory
    /// </summary>
    private List<Photo> ApplyMarsTimeFiltering(List<Photo> photos, PhotoQueryParameters parameters)
    {
        if (parameters.MarsTimeGoldenHour == true)
        {
            return photos.Where(p => MarsTimeHelper.IsGoldenHour(p.DateTakenMars)).ToList();
        }

        if (parameters.MarsTimeMinParsed.HasValue || parameters.MarsTimeMaxParsed.HasValue)
        {
            return photos.Where(p =>
            {
                if (!MarsTimeHelper.TryExtractTimeFromTimestamp(p.DateTakenMars, out var marsTime))
                    return false;

                if (parameters.MarsTimeMinParsed.HasValue && marsTime < parameters.MarsTimeMinParsed.Value)
                    return false;

                if (parameters.MarsTimeMaxParsed.HasValue && marsTime > parameters.MarsTimeMaxParsed.Value)
                    return false;

                return true;
            }).ToList();
        }

        return photos;
    }

    /// <summary>
    /// Apply sorting in memory
    /// </summary>
    private List<Photo> ApplySortingInMemory(List<Photo> photos, PhotoQueryParameters parameters)
    {
        if (parameters.SortFields.Count == 0)
        {
            return photos.OrderByDescending(p => p.DateTakenUtc).ToList();
        }

        IOrderedEnumerable<Photo>? orderedPhotos = null;

        foreach (var sortField in parameters.SortFields)
        {
            var isDescending = sortField.Direction == SortDirection.Descending;

            if (orderedPhotos == null)
            {
                orderedPhotos = sortField.Field switch
                {
                    "id" => isDescending ? photos.OrderByDescending(p => p.Id) : photos.OrderBy(p => p.Id),
                    "sol" => isDescending ? photos.OrderByDescending(p => p.Sol) : photos.OrderBy(p => p.Sol),
                    "earth_date" => isDescending ? photos.OrderByDescending(p => p.EarthDate) : photos.OrderBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? photos.OrderByDescending(p => p.DateTakenUtc) : photos.OrderBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? photos.OrderByDescending(p => p.Camera?.Name) : photos.OrderBy(p => p.Camera?.Name),
                    "created_at" => isDescending ? photos.OrderByDescending(p => p.CreatedAt) : photos.OrderBy(p => p.CreatedAt),
                    _ => photos.OrderByDescending(p => p.DateTakenUtc)
                };
            }
            else
            {
                orderedPhotos = sortField.Field switch
                {
                    "id" => isDescending ? orderedPhotos.ThenByDescending(p => p.Id) : orderedPhotos.ThenBy(p => p.Id),
                    "sol" => isDescending ? orderedPhotos.ThenByDescending(p => p.Sol) : orderedPhotos.ThenBy(p => p.Sol),
                    "earth_date" => isDescending ? orderedPhotos.ThenByDescending(p => p.EarthDate) : orderedPhotos.ThenBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? orderedPhotos.ThenByDescending(p => p.DateTakenUtc) : orderedPhotos.ThenBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? orderedPhotos.ThenByDescending(p => p.Camera?.Name) : orderedPhotos.ThenBy(p => p.Camera?.Name),
                    "created_at" => isDescending ? orderedPhotos.ThenByDescending(p => p.CreatedAt) : orderedPhotos.ThenBy(p => p.CreatedAt),
                    _ => orderedPhotos
                };
            }
        }

        return orderedPhotos?.ToList() ?? photos.OrderByDescending(p => p.DateTakenUtc).ToList();
    }
}
