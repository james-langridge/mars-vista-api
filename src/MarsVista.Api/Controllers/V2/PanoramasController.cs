using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// Controller for panorama detection and retrieval
/// </summary>
[ApiController]
[Route("api/v2/panoramas")]
[Tags("V2 - Advanced Features")]
public class PanoramasController : ControllerBase
{
    private readonly IPanoramaService _panoramaService;
    private readonly ILogger<PanoramasController> _logger;

    private static readonly HashSet<string> ValidSorts = new(StringComparer.OrdinalIgnoreCase)
        { "sol", "rating", "coverage", "photos" };
    private static readonly HashSet<string> ValidOrders = new(StringComparer.OrdinalIgnoreCase)
        { "asc", "desc" };
    private static readonly HashSet<string> ValidStitchStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "completed", "failed", "processing", "not_started" };
    private static readonly HashSet<string> ValidStitchMethods = new(StringComparer.OrdinalIgnoreCase)
        { "feature_match", "telemetry_projection" };
    private static readonly HashSet<string> ValidMosaicTypes = new(StringComparer.OrdinalIgnoreCase)
        { "single_row", "multi_row" };
    private static readonly HashSet<string> ValidQualities = new(StringComparer.OrdinalIgnoreCase)
        { "partial", "half", "wide", "full" };

    public PanoramasController(
        IPanoramaService panoramaService,
        ILogger<PanoramasController> logger)
    {
        _panoramaService = panoramaService;
        _logger = logger;
    }

    /// <summary>
    /// Get all detected panoramas
    /// </summary>
    /// <param name="rovers">Comma-separated list of rover names (curiosity, perseverance, etc.)</param>
    /// <param name="sol_min">Minimum sol</param>
    /// <param name="sol_max">Maximum sol</param>
    /// <param name="min_photos">Only return panoramas with at least this many photos</param>
    /// <param name="stitch_status">Filter by stitch status: completed, failed, processing, not_started</param>
    /// <param name="stitch_method">Filter by stitch method: feature_match, telemetry_projection</param>
    /// <param name="mosaic_type">Filter by mosaic type: single_row, multi_row</param>
    /// <param name="quality">Filter by quality tier: partial, half, wide, full</param>
    /// <param name="min_rating">Minimum average rating (1.0-5.0)</param>
    /// <param name="sort">Sort field: sol (default), rating, coverage, photos</param>
    /// <param name="order">Sort direction: asc, desc (default)</param>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="per_page">Items per page (default: 25, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PanoramaResource>>), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> GetPanoramas(
        [FromQuery] string? rovers = null,
        [FromQuery] int? sol_min = null,
        [FromQuery] int? sol_max = null,
        [FromQuery] int? min_photos = null,
        [FromQuery] string? stitch_status = null,
        [FromQuery] string? stitch_method = null,
        [FromQuery] string? mosaic_type = null,
        [FromQuery] string? quality = null,
        [FromQuery] double? min_rating = null,
        [FromQuery] string? sort = null,
        [FromQuery] string? order = null,
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 25,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return ValidationError("Page number must be >= 1");

        if (per_page < 1 || per_page > 100)
            return ValidationError("Per page must be between 1 and 100");

        if (min_rating.HasValue && (min_rating.Value < 1.0 || min_rating.Value > 5.0))
            return ValidationError("min_rating must be between 1.0 and 5.0");

        if (sort != null && !ValidSorts.Contains(sort))
            return ValidationError($"Invalid sort value '{sort}'. Valid values: sol, rating, coverage, photos");

        if (order != null && !ValidOrders.Contains(order))
            return ValidationError($"Invalid order value '{order}'. Valid values: asc, desc");

        if (stitch_status != null && !ValidStitchStatuses.Contains(stitch_status))
            return ValidationError($"Invalid stitch_status value '{stitch_status}'. Valid values: completed, failed, processing, not_started");

        if (stitch_method != null && !ValidStitchMethods.Contains(stitch_method))
            return ValidationError($"Invalid stitch_method value '{stitch_method}'. Valid values: feature_match, telemetry_projection");

        if (mosaic_type != null && !ValidMosaicTypes.Contains(mosaic_type))
            return ValidationError($"Invalid mosaic_type value '{mosaic_type}'. Valid values: single_row, multi_row");

        if (quality != null && !ValidQualities.Contains(quality))
            return ValidationError($"Invalid quality value '{quality}'. Valid values: partial, half, wide, full");

        var response = await _panoramaService.GetPanoramasAsync(
            rovers,
            sol_min,
            sol_max,
            min_photos,
            stitch_status,
            stitch_method,
            mosaic_type,
            quality,
            min_rating,
            sort,
            order,
            page,
            per_page,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get a specific panorama by ID
    /// </summary>
    /// <param name="id">Panorama ID (e.g., pano_curiosity_1000_14)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PanoramaResource), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetPanoramaById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var panorama = await _panoramaService.GetPanoramaByIdAsync(id, cancellationToken);

        if (panorama == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Panorama with ID '{id}' not found",
                Instance = Request.Path
            });
        }

        return Ok(panorama);
    }

    /// <summary>
    /// Rate a panorama (1-5 stars). Requires API key. Same key can update their rating.
    /// </summary>
    /// <param name="id">Panorama ID</param>
    /// <param name="request">Rating request body</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{id}/rating")]
    [ProducesResponseType(typeof(RatingResponse), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    [ProducesResponseType(typeof(ApiError), 401)]
    public async Task<IActionResult> RatePanorama(
        string id,
        [FromBody] RatingRequest request,
        CancellationToken cancellationToken = default)
    {
        var clientId = HttpContext.Items["ApiKeyId"]?.ToString();
        if (string.IsNullOrEmpty(clientId))
        {
            return Unauthorized(new ApiError
            {
                Type = "/errors/unauthorized",
                Title = "Unauthorized",
                Status = 401,
                Detail = "API key required to rate panoramas",
                Instance = Request.Path
            });
        }

        if (!IsValidPanoramaIdFormat(id))
            return ValidationError("Invalid panorama ID format. Expected: pano_{rover}_{sol}_{index}");

        if (request.Rating < 1 || request.Rating > 5)
            return ValidationError("Rating must be between 1 and 5");

        var result = await _panoramaService.UpsertRatingAsync(id, clientId, request.Rating, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get rating information for a panorama
    /// </summary>
    /// <param name="id">Panorama ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id}/rating")]
    [ProducesResponseType(typeof(RatingResponse), 200)]
    public async Task<IActionResult> GetRating(
        string id,
        CancellationToken cancellationToken = default)
    {
        var clientId = HttpContext.Items["ApiKeyId"]?.ToString();
        var result = await _panoramaService.GetRatingAsync(id, clientId, cancellationToken);
        return Ok(result);
    }

    private static bool IsValidPanoramaIdFormat(string id)
    {
        var parts = id.Split('_');
        return parts.Length >= 4 &&
               parts[0] == "pano" &&
               int.TryParse(parts[^2], out _) &&
               int.TryParse(parts[^1], out _);
    }

    private BadRequestObjectResult ValidationError(string detail)
    {
        return BadRequest(new ApiError
        {
            Type = "/errors/validation-error",
            Title = "Validation Error",
            Status = 400,
            Detail = detail,
            Instance = Request.Path
        });
    }
}
