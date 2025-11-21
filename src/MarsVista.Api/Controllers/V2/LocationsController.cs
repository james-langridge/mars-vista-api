using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// Controller for location timeline data
/// </summary>
[ApiController]
[Route("api/v2/locations")]
[Tags("V2 - Advanced Features")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(
        ILocationService locationService,
        ILogger<LocationsController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    /// <summary>
    /// Get all unique locations visited by rovers
    /// </summary>
    /// <param name="rovers">Comma-separated list of rover names</param>
    /// <param name="sol_min">Minimum sol</param>
    /// <param name="sol_max">Maximum sol</param>
    /// <param name="min_photos">Minimum number of photos at location</param>
    /// <param name="page">Page number (1-indexed)</param>
    /// <param name="per_page">Items per page (default: 25, max: 100)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<LocationResource>>), 200)]
    [ProducesResponseType(typeof(ApiError), 400)]
    public async Task<IActionResult> GetLocations(
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

        var response = await _locationService.GetLocationsAsync(
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
    /// Get a specific location by ID
    /// </summary>
    /// <param name="id">Location ID (e.g., curiosity_79_1204)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LocationResource), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetLocationById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var location = await _locationService.GetLocationByIdAsync(id, cancellationToken);

        if (location == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Location with ID '{id}' not found",
                Instance = Request.Path
            });
        }

        return Ok(location);
    }
}
