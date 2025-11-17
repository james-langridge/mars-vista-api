using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/rovers")]
public class RoversController : ControllerBase
{
    private readonly IRoverQueryService _roverQueryService;
    private readonly IPhotoQueryService _photoQueryService;
    private readonly ILogger<RoversController> _logger;

    public RoversController(
        IRoverQueryService roverQueryService,
        IPhotoQueryService photoQueryService,
        ILogger<RoversController> logger)
    {
        _roverQueryService = roverQueryService;
        _photoQueryService = photoQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rovers with their metadata
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRovers(CancellationToken cancellationToken)
    {
        var rovers = await _roverQueryService.GetAllRoversAsync(cancellationToken);

        return Ok(new { rovers });
    }

    /// <summary>
    /// Get a specific rover by name
    /// </summary>
    /// <param name="name">Rover name (e.g., "perseverance")</param>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetRover(string name, CancellationToken cancellationToken)
    {
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        return Ok(new { rover });
    }

    /// <summary>
    /// Query photos for a specific rover
    /// </summary>
    /// <param name="name">Rover name</param>
    /// <param name="sol">Martian sol (optional)</param>
    /// <param name="earthDate">Earth date YYYY-MM-DD (optional)</param>
    /// <param name="camera">Camera name (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="perPage">Results per page (default: 25, max: 100)</param>
    [HttpGet("{name}/photos")]
    public async Task<IActionResult> GetPhotos(
        string name,
        [FromQuery] int? sol,
        [FromQuery(Name = "earth_date")] string? earthDate,
        [FromQuery] string? camera,
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate rover exists
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        // Parse earth_date if provided
        DateTime? parsedEarthDate = null;
        if (!string.IsNullOrWhiteSpace(earthDate))
        {
            if (!DateTime.TryParse(earthDate, out var date))
            {
                return BadRequest(new { error = "Invalid earth_date format. Use YYYY-MM-DD." });
            }
            parsedEarthDate = date;
        }

        var (photos, totalCount) = await _photoQueryService.QueryPhotosAsync(
            name,
            sol,
            parsedEarthDate,
            camera,
            page,
            perPage,
            cancellationToken);

        return Ok(new
        {
            photos,
            pagination = new
            {
                total_count = totalCount,
                page,
                per_page = perPage,
                total_pages = (int)Math.Ceiling(totalCount / (double)perPage)
            }
        });
    }

    /// <summary>
    /// Get the latest photos for a rover (highest sol)
    /// </summary>
    /// <param name="name">Rover name</param>
    /// <param name="page">Page number</param>
    /// <param name="perPage">Results per page</param>
    [HttpGet("{name}/latest")]
    public async Task<IActionResult> GetLatestPhotos(
        string name,
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate rover exists
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        var (photos, totalCount) = await _photoQueryService.GetLatestPhotosAsync(
            name,
            page,
            perPage,
            cancellationToken);

        return Ok(new
        {
            photos,
            pagination = new
            {
                total_count = totalCount,
                page,
                per_page = perPage,
                total_pages = (int)Math.Ceiling(totalCount / (double)perPage)
            }
        });
    }

    /// <summary>
    /// Alias for GetLatestPhotos - provided for backward compatibility with NASA Mars Photo API
    /// </summary>
    /// <param name="name">Rover name</param>
    /// <param name="page">Page number</param>
    /// <param name="perPage">Results per page</param>
    [HttpGet("{name}/latest_photos")]
    public async Task<IActionResult> GetLatestPhotosLegacy(
        string name,
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Simply delegate to GetLatestPhotos - same implementation
        return await GetLatestPhotos(name, page, perPage, cancellationToken);
    }
}
