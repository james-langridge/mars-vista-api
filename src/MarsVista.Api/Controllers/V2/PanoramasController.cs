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
    /// <param name="min_photos">Minimum number of photos in panorama</param>
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
        [FromQuery] int page = 1,
        [FromQuery] int per_page = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        if (page < 1)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "Page number must be >= 1",
                Instance = Request.Path
            });
        }

        if (per_page < 1 || per_page > 100)
        {
            return BadRequest(new ApiError
            {
                Type = "/errors/validation-error",
                Title = "Validation Error",
                Status = 400,
                Detail = "Per page must be between 1 and 100",
                Instance = Request.Path
            });
        }

        var response = await _panoramaService.GetPanoramasAsync(
            rovers,
            sol_min,
            sol_max,
            min_photos,
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
}
