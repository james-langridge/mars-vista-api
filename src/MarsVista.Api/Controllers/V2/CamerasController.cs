using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Data;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// v2 Cameras API - Camera resources across all rovers
/// </summary>
[ApiController]
[Route("api/v2/cameras")]
[Tags("V2 - Cameras")]
public class CamerasController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<CamerasController> _logger;

    public CamerasController(MarsVistaDbContext context, ILogger<CamerasController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all cameras across all rovers
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CameraResource>>), 200)]
    public async Task<IActionResult> GetCameras(CancellationToken cancellationToken)
    {
        var cameras = await _context.Cameras
            .Include(c => c.Rover)
            .OrderBy(c => c.Rover.Name)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        // Get photo counts for each camera
        var cameraStats = await _context.Photos
            .GroupBy(p => p.CameraId)
            .Select(g => new
            {
                CameraId = g.Key,
                PhotoCount = g.Count(),
                FirstSol = g.Min(p => p.Sol),
                LastSol = g.Max(p => p.Sol)
            })
            .ToListAsync(cancellationToken);

        var statsDict = cameraStats.ToDictionary(s => s.CameraId);

        var cameraResources = cameras.Select(c => new CameraResource
        {
            Id = c.Name,
            Type = "camera",
            Attributes = new CameraResourceAttributes
            {
                Name = c.Name,
                FullName = c.FullName,
                PhotoCount = statsDict.TryGetValue(c.Id, out var stats) ? stats.PhotoCount : 0,
                FirstPhotoSol = statsDict.TryGetValue(c.Id, out var stats2) ? stats2.FirstSol : null,
                LastPhotoSol = statsDict.TryGetValue(c.Id, out var stats3) ? stats3.LastSol : null
            },
            Relationships = new CameraRelationships
            {
                Rover = new ResourceReference
                {
                    Id = c.Rover.Name.ToLowerInvariant(),
                    Type = "rover"
                }
            }
        }).ToList();

        var response = new ApiResponse<List<CameraResource>>(cameraResources)
        {
            Meta = new ResponseMeta
            {
                ReturnedCount = cameraResources.Count
            },
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/cameras"
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific camera by ID (name)
    /// </summary>
    /// <param name="id">Camera name (e.g., FHAZ, MAST)</param>
    /// <param name="rover">Optional rover filter</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CameraResource>), 200)]
    [ProducesResponseType(typeof(ApiError), 404)]
    public async Task<IActionResult> GetCamera(string id, [FromQuery] string? rover, CancellationToken cancellationToken)
    {
        var query = _context.Cameras.Include(c => c.Rover).Where(c => c.Name.ToUpper() == id.ToUpper());

        if (!string.IsNullOrWhiteSpace(rover))
        {
            query = query.Where(c => c.Rover.Name.ToLower() == rover.ToLower());
        }

        var camera = await query.FirstOrDefaultAsync(cancellationToken);

        if (camera == null)
        {
            return NotFound(new ApiError
            {
                Type = "/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Camera '{id}' not found{(string.IsNullOrWhiteSpace(rover) ? "" : $" for rover '{rover}'")}",
                Instance = Request.Path
            });
        }

        // Get photo count for this camera
        var photoCount = await _context.Photos.CountAsync(p => p.CameraId == camera.Id, cancellationToken);
        var firstSol = await _context.Photos.Where(p => p.CameraId == camera.Id).MinAsync(p => (int?)p.Sol, cancellationToken);
        var lastSol = await _context.Photos.Where(p => p.CameraId == camera.Id).MaxAsync(p => (int?)p.Sol, cancellationToken);

        var cameraResource = new CameraResource
        {
            Id = camera.Name,
            Type = "camera",
            Attributes = new CameraResourceAttributes
            {
                Name = camera.Name,
                FullName = camera.FullName,
                PhotoCount = photoCount,
                FirstPhotoSol = firstSol,
                LastPhotoSol = lastSol
            },
            Relationships = new CameraRelationships
            {
                Rover = new ResourceReference
                {
                    Id = camera.Rover.Name.ToLowerInvariant(),
                    Type = "rover",
                    Attributes = new
                    {
                        name = camera.Rover.Name,
                        status = camera.Rover.Status
                    }
                }
            }
        };

        var response = new ApiResponse<CameraResource>(cameraResource)
        {
            Links = new ResponseLinks
            {
                Self = $"{Request.Scheme}://{Request.Host}/api/v2/cameras/{id}"
            }
        };

        return Ok(response);
    }
}
