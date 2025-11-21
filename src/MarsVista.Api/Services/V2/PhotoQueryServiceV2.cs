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

    public async Task<int> GetPhotoCountAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(parameters);
        return await query.CountAsync(cancellationToken);
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

        var attributes = new PhotoAttributes
        {
            ImgSrc = ShouldInclude("img_src") ? photo.ImgSrcLarge : null,
            Sol = ShouldInclude("sol") ? photo.Sol : null,
            EarthDate = ShouldInclude("earth_date") ? photo.EarthDate?.ToString("yyyy-MM-dd") : null,
            DateTakenUtc = ShouldInclude("date_taken_utc") ? photo.DateTakenUtc : null,
            DateTakenMars = ShouldInclude("date_taken_mars") ? photo.DateTakenMars : null,
            Width = ShouldInclude("width") ? photo.Width : null,
            Height = ShouldInclude("height") ? photo.Height : null,
            SampleType = ShouldInclude("sample_type") ? photo.SampleType : null,
            ImgSrcSmall = ShouldInclude("img_src_small") ? photo.ImgSrcSmall : null,
            ImgSrcMedium = ShouldInclude("img_src_medium") ? photo.ImgSrcMedium : null,
            ImgSrcLarge = ShouldInclude("img_src_large") ? photo.ImgSrcLarge : null,
            ImgSrcFull = ShouldInclude("img_src_full") ? photo.ImgSrcFull : null,
            Site = ShouldInclude("site") ? photo.Site : null,
            Drive = ShouldInclude("drive") ? photo.Drive : null,
            Xyz = ShouldInclude("xyz") ? photo.Xyz : null,
            MastAz = ShouldInclude("mast_az") ? photo.MastAz : null,
            MastEl = ShouldInclude("mast_el") ? photo.MastEl : null,
            Title = ShouldInclude("title") ? photo.Title : null,
            Caption = ShouldInclude("caption") ? photo.Caption : null,
            CreatedAt = ShouldInclude("created_at") ? photo.CreatedAt : null
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
