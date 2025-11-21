using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Models.V2;
using MarsVista.Api.Services.V2;
using MarsVista.Api.Validators.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// v2 Photos API - Unified endpoint with powerful filtering
/// </summary>
[ApiController]
[Route("api/v2/photos")]
[Tags("V2 - Photos")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoQueryServiceV2 _photoQueryService;
    private readonly ICachingServiceV2 _cachingService;
    private readonly ILogger<PhotosController> _logger;

    // Active rovers that are still transmitting photos
    private static readonly HashSet<string> ActiveRovers = new(StringComparer.OrdinalIgnoreCase)
    {
        "curiosity",
        "perseverance"
    };

    public PhotosController(
        IPhotoQueryServiceV2 photoQueryService,
        ICachingServiceV2 cachingService,
        ILogger<PhotosController> logger)
    {
        _photoQueryService = photoQueryService;
        _cachingService = cachingService;
        _logger = logger;
    }

    /// <summary>
    /// Query photos with advanced filtering
    /// </summary>
    /// <remarks>
    /// Unified endpoint supporting:
    /// - Multiple rovers: ?rovers=curiosity,perseverance
    /// - Multiple cameras: ?cameras=FHAZ,NAVCAM,MAST
    /// - Sol ranges: ?sol_min=100&sol_max=200
    /// - Date ranges: ?date_min=2023-01-01&date_max=2023-12-31
    /// - Sorting: ?sort=-earth_date,camera
    /// - Field selection: ?fields=id,img_src,sol
    /// - Include related: ?include=rover,camera
    /// - Pagination: ?page=1&per_page=25 (max 100)
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PhotoResource>>), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    [ProducesResponseType(304)] // Not Modified
    public async Task<IActionResult> QueryPhotos(
        [FromQuery] PhotoQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Validate query parameters
        var validationError = QueryParameterValidator.ValidatePhotoQuery(parameters, Request.Path);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Query photos
        var response = await _photoQueryService.QueryPhotosAsync(parameters, cancellationToken);

        // Add links for navigation
        response = response with
        {
            Links = BuildNavigationLinks(parameters, response.Pagination!)
        };

        // Generate ETag for response
        var etag = _cachingService.GenerateETag(response);

        // Check If-None-Match header for conditional request
        var requestETag = Request.Headers["If-None-Match"].FirstOrDefault();
        if (_cachingService.CheckETag(requestETag, etag))
        {
            // Client has the current version - return 304 Not Modified
            Response.Headers["ETag"] = $"\"{etag}\"";
            return StatusCode(304);
        }

        // Determine if querying active or inactive rovers
        var isActiveRover = DetermineIfActiveRover(parameters);

        // Set caching headers
        Response.Headers["ETag"] = $"\"{etag}\"";
        Response.Headers["Cache-Control"] = _cachingService.GetCacheControlHeader(isActiveRover);

        return Ok(response);
    }

    /// <summary>
    /// Get a specific photo by ID
    /// </summary>
    /// <param name="id">Photo ID</param>
    /// <param name="parameters">Optional field selection and include parameters</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PhotoResource>), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    [ProducesResponseType(304)] // Not Modified
    public async Task<IActionResult> GetPhoto(
        int id,
        [FromQuery] PhotoQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Validate query parameters (for fields/include)
        var validationError = QueryParameterValidator.ValidatePhotoQuery(parameters, Request.Path);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var photo = await _photoQueryService.GetPhotoByIdAsync(id, parameters, cancellationToken);

        if (photo == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Photo with ID {id} not found",
                Instance = Request.Path
            });
        }

        var response = new ApiResponse<PhotoResource>(photo)
        {
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/photos/{id}"
            }
        };

        // Generate ETag for response
        var etag = _cachingService.GenerateETag(response);

        // Check If-None-Match header for conditional request
        var requestETag = Request.Headers["If-None-Match"].FirstOrDefault();
        if (_cachingService.CheckETag(requestETag, etag))
        {
            Response.Headers["ETag"] = $"\"{etag}\"";
            return StatusCode(304);
        }

        // Individual photos from inactive rovers can be cached aggressively
        // Determine rover from the photo's relationships
        var isActiveRover = photo.Relationships?.Rover?.Id != null &&
                           ActiveRovers.Contains(photo.Relationships.Rover.Id);

        // Set caching headers
        Response.Headers["ETag"] = $"\"{etag}\"";
        Response.Headers["Cache-Control"] = _cachingService.GetCacheControlHeader(isActiveRover);

        return Ok(response);
    }

    /// <summary>
    /// Get photo statistics and analytics
    /// </summary>
    /// <remarks>
    /// Returns aggregated statistics for photos matching the query.
    /// Supports grouping by: camera, rover, sol
    /// Example: /api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2023-01-01
    /// </remarks>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<PhotoStatisticsResponse>), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] PhotoQueryParameters parameters,
        [FromQuery] string? group_by,
        CancellationToken cancellationToken)
    {
        // Validate group_by parameter
        var validGroupBy = new[] { "camera", "rover", "sol" };
        if (string.IsNullOrWhiteSpace(group_by) || !validGroupBy.Contains(group_by.ToLower()))
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "Invalid or missing group_by parameter",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "group_by",
                        Value = group_by ?? "null",
                        Message = "Must be one of: camera, rover, sol",
                        Example = "camera"
                    }
                }
            });
        }

        // Validate other query parameters
        var validationError = QueryParameterValidator.ValidatePhotoQuery(parameters, Request.Path);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        // Get statistics
        var stats = await _photoQueryService.GetStatisticsAsync(parameters, group_by, cancellationToken);

        var response = new ApiResponse<PhotoStatisticsResponse>(stats)
        {
            Meta = new ResponseMeta
            {
                Query = new Dictionary<string, object>
                {
                    ["group_by"] = group_by.ToLower(),
                    ["total_photos"] = stats.TotalPhotos
                }
            },
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}{Request.Path}?{Request.QueryString}"
            }
        };

        // Generate ETag
        var etag = _cachingService.GenerateETag(response);

        // Check If-None-Match header
        var requestETag = Request.Headers["If-None-Match"].FirstOrDefault();
        if (_cachingService.CheckETag(requestETag, etag))
        {
            Response.Headers["ETag"] = $"\"{etag}\"";
            return StatusCode(304);
        }

        // Determine if querying active or inactive rovers
        var isActiveRover = DetermineIfActiveRover(parameters);

        // Set caching headers
        Response.Headers["ETag"] = $"\"{etag}\"";
        Response.Headers["Cache-Control"] = _cachingService.GetCacheControlHeader(isActiveRover);

        return Ok(response);
    }

    /// <summary>
    /// Batch retrieve photos by IDs
    /// </summary>
    /// <remarks>
    /// Efficiently retrieve multiple photos in a single request.
    /// Maximum 100 IDs per request.
    /// </remarks>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<List<PhotoResource>>), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> BatchGetPhotos(
        [FromBody] BatchPhotoRequest request,
        [FromQuery] PhotoQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        // Validate request
        if (request.Ids == null || request.Ids.Count == 0)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "No photo IDs provided",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "ids",
                        Value = "null or empty",
                        Message = "At least one photo ID is required",
                        Example = "[123456, 123457]"
                    }
                }
            });
        }

        if (request.Ids.Count > 100)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "Too many photo IDs requested",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "ids",
                        Value = request.Ids.Count.ToString(),
                        Message = "Maximum 100 IDs per batch request",
                        Example = "100"
                    }
                }
            });
        }

        // Retrieve photos
        var photos = await _photoQueryService.GetPhotosByIdsAsync(request.Ids, parameters, cancellationToken);

        var response = new ApiResponse<List<PhotoResource>>(photos)
        {
            Meta = new ResponseMeta
            {
                TotalCount = photos.Count,
                ReturnedCount = photos.Count,
                Query = new Dictionary<string, object>
                {
                    ["ids_requested"] = request.Ids.Count,
                    ["ids_found"] = photos.Count
                }
            },
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/photos/batch"
            }
        };

        // Generate ETag
        var etag = _cachingService.GenerateETag(response);

        // Check If-None-Match header
        var requestETag = Request.Headers["If-None-Match"].FirstOrDefault();
        if (_cachingService.CheckETag(requestETag, etag))
        {
            Response.Headers["ETag"] = $"\"{etag}\"";
            return StatusCode(304);
        }

        // For batch requests, use moderate caching (1 hour)
        Response.Headers["ETag"] = $"\"{etag}\"";
        Response.Headers["Cache-Control"] = "public, max-age=3600, must-revalidate";

        return Ok(response);
    }

    /// <summary>
    /// Determine if the query includes only active rovers
    /// If any inactive rover is included, treat as inactive for more aggressive caching
    /// </summary>
    private bool DetermineIfActiveRover(PhotoQueryParameters parameters)
    {
        // If no rovers specified, could include inactive rovers - treat as active (conservative)
        if (parameters.RoverList.Count == 0)
            return true;

        // Check if all specified rovers are active
        return parameters.RoverList.All(r => ActiveRovers.Contains(r));
    }

    /// <summary>
    /// Build navigation links for pagination
    /// </summary>
    private ResponseLinks BuildNavigationLinks(PhotoQueryParameters parameters, PaginationInfo pagination)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var queryParams = BuildQueryString(parameters);

        var links = new ResponseLinks
        {
            Self = $"{baseUrl}?{queryParams}&page={parameters.PageNumber}&per_page={parameters.PageSize}"
        };

        // Add first/last links
        if (pagination.TotalPages.HasValue)
        {
            links = links with
            {
                First = $"{baseUrl}?{queryParams}&page=1&per_page={parameters.PageSize}",
                Last = $"{baseUrl}?{queryParams}&page={pagination.TotalPages}&per_page={parameters.PageSize}"
            };
        }

        // Add next link
        if (pagination.Page.HasValue && pagination.TotalPages.HasValue && pagination.Page < pagination.TotalPages)
        {
            links = links with
            {
                Next = $"{baseUrl}?{queryParams}&page={pagination.Page + 1}&per_page={parameters.PageSize}"
            };
        }

        // Add previous link
        if (pagination.Page.HasValue && pagination.Page > 1)
        {
            links = links with
            {
                Previous = $"{baseUrl}?{queryParams}&page={pagination.Page - 1}&per_page={parameters.PageSize}"
            };
        }

        return links;
    }

    /// <summary>
    /// Build query string from parameters (excluding page/per_page)
    /// </summary>
    private string BuildQueryString(PhotoQueryParameters parameters)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(parameters.Rovers))
            parts.Add($"rovers={Uri.EscapeDataString(parameters.Rovers)}");

        if (!string.IsNullOrWhiteSpace(parameters.Cameras))
            parts.Add($"cameras={Uri.EscapeDataString(parameters.Cameras)}");

        if (parameters.SolMin.HasValue)
            parts.Add($"sol_min={parameters.SolMin}");

        if (parameters.SolMax.HasValue)
            parts.Add($"sol_max={parameters.SolMax}");

        if (!string.IsNullOrWhiteSpace(parameters.DateMin))
            parts.Add($"date_min={Uri.EscapeDataString(parameters.DateMin)}");

        if (!string.IsNullOrWhiteSpace(parameters.DateMax))
            parts.Add($"date_max={Uri.EscapeDataString(parameters.DateMax)}");

        if (!string.IsNullOrWhiteSpace(parameters.Sort))
            parts.Add($"sort={Uri.EscapeDataString(parameters.Sort)}");

        if (!string.IsNullOrWhiteSpace(parameters.Fields))
            parts.Add($"fields={Uri.EscapeDataString(parameters.Fields)}");

        if (!string.IsNullOrWhiteSpace(parameters.Include))
            parts.Add($"include={Uri.EscapeDataString(parameters.Include)}");

        return string.Join("&", parts);
    }
}
