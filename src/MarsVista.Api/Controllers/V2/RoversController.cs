using Microsoft.AspNetCore.Mvc;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// v2 Rovers API - Rover resources with slug-based routing
/// </summary>
[ApiController]
[Route("api/v2/rovers")]
[Tags("V2 - Rovers")]
public class RoversController : ControllerBase
{
    private readonly IRoverQueryServiceV2 _roverQueryService;
    private readonly ILogger<RoversController> _logger;

    public RoversController(
        IRoverQueryServiceV2 roverQueryService,
        ILogger<RoversController> logger)
    {
        _roverQueryService = roverQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rovers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RoverResource>>), 200)]
    public async Task<IActionResult> GetRovers(CancellationToken cancellationToken)
    {
        var rovers = await _roverQueryService.GetAllRoversAsync(cancellationToken);

        var response = new ApiResponse<List<RoverResource>>(rovers)
        {
            Meta = new ResponseMeta
            {
                ReturnedCount = rovers.Count
            },
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/rovers"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific rover by slug
    /// </summary>
    /// <param name="slug">Rover slug (curiosity, perseverance, opportunity, spirit)</param>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<RoverResource>), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetRover(string slug, CancellationToken cancellationToken)
    {
        var rover = await _roverQueryService.GetRoverBySlugAsync(slug, cancellationToken);

        if (rover == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Rover '{slug}' not found",
                Instance = Request.Path,
                Errors = new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "slug",
                        Value = slug,
                        Message = "Invalid rover slug",
                        Example = "curiosity"
                    }
                }
            });
        }

        var response = new ApiResponse<RoverResource>(rover)
        {
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/rovers/{slug}"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get rover manifest (photo history by sol)
    /// </summary>
    /// <param name="slug">Rover slug</param>
    [HttpGet("{slug}/manifest")]
    [ProducesResponseType(typeof(ApiResponse<RoverManifest>), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetManifest(string slug, CancellationToken cancellationToken)
    {
        var manifest = await _roverQueryService.GetRoverManifestAsync(slug, cancellationToken);

        if (manifest == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Rover '{slug}' not found",
                Instance = Request.Path
            });
        }

        var response = new ApiResponse<RoverManifest>(manifest)
        {
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/rovers/{slug}/manifest"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get cameras for a specific rover
    /// </summary>
    /// <param name="slug">Rover slug</param>
    [HttpGet("{slug}/cameras")]
    [ProducesResponseType(typeof(ApiResponse<List<CameraResource>>), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetCameras(string slug, CancellationToken cancellationToken)
    {
        var cameras = await _roverQueryService.GetRoverCamerasAsync(slug, cancellationToken);

        if (cameras.Count == 0)
        {
            // Check if rover exists
            var rover = await _roverQueryService.GetRoverBySlugAsync(slug, cancellationToken);
            if (rover == null)
            {
                return NotFound(new ApiError
                {
                    Type = "/errors/not-found",
                    Title = "Not Found",
                    Status = 404,
                    Detail = $"Rover '{slug}' not found",
                    Instance = Request.Path
                });
            }
        }

        var response = new ApiResponse<List<CameraResource>>(cameras)
        {
            Meta = new ResponseMeta
            {
                ReturnedCount = cameras.Count
            },
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/rovers/{slug}/cameras"
            }
        };

        return Ok(response);
    }
}
