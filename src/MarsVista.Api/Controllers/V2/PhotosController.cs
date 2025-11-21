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
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(
        IPhotoQueryServiceV2 photoQueryService,
        ILogger<PhotosController> logger)
    {
        _photoQueryService = photoQueryService;
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

        return Ok(response);
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
