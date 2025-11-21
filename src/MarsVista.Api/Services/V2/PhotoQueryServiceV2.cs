using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Entities;
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
        // Build the base query with filters
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

        // Execute query
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

        // Group by the requested dimension
        switch (groupBy.ToLower())
        {
            case "camera":
                response.ByCamera = await GetCameraStatistics(query, totalCount, cancellationToken);
                break;

            case "rover":
                response.ByRover = await GetRoverStatistics(query, totalCount, cancellationToken);
                break;

            case "sol":
                response.BySol = await GetSolStatistics(query, cancellationToken);
                break;
        }

        return response;
    }

    /// <summary>
    /// Get statistics grouped by camera
    /// </summary>
    private async Task<List<CameraStatistics>> GetCameraStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Camera.Name)
            .Select(g => new
            {
                Camera = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new CameraStatistics
        {
            Camera = s.Camera,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by rover
    /// </summary>
    private async Task<List<RoverStatistics>> GetRoverStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Rover.Name)
            .Select(g => new
            {
                Rover = g.Key,
                Count = g.Count(),
                MinSol = g.Min(p => p.Sol),
                MaxSol = g.Max(p => p.Sol)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new RoverStatistics
        {
            Rover = s.Rover,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0,
            AvgPerSol = s.MaxSol > s.MinSol ? Math.Round(s.Count / (double)(s.MaxSol - s.MinSol + 1), 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by sol (limited to top 100 sols)
    /// </summary>
    private async Task<List<SolStatistics>> GetSolStatistics(
        IQueryable<Photo> query,
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

        return stats.Select(s => new SolStatistics
        {
            Sol = s.Sol,
            Count = s.Count,
            EarthDate = s.EarthDate?.ToString("yyyy-MM-dd")
        }).ToList();
    }

    /// <summary>
    /// Build the base filtered query
    /// </summary>
    private IQueryable<Photo> BuildQuery(PhotoQueryParameters parameters)
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

        // Build location object with coordinates
        PhotoLocation? location = null;
        if (ShouldIncludeAny("location", "site", "drive", "xyz"))
        {
            PhotoCoordinates? coordinates = null;
            if (!string.IsNullOrEmpty(photo.Xyz))
            {
                // Parse XYZ string "(35.4362,22.5714,-9.46445)" to coordinates
                if (MarsVista.Api.Helpers.MarsTimeHelper.TryParseXYZ(photo.Xyz, out var parsed))
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
            ImgSrc = ShouldInclude("img_src") ? photo.ImgSrcLarge : null
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
}
