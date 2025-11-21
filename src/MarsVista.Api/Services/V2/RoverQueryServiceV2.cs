using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Data;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of v2 rover query service
/// </summary>
public class RoverQueryServiceV2 : IRoverQueryServiceV2
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<RoverQueryServiceV2> _logger;

    public RoverQueryServiceV2(MarsVistaDbContext context, ILogger<RoverQueryServiceV2> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoverResource>> GetAllRoversAsync(CancellationToken cancellationToken = default)
    {
        var rovers = await _context.Rovers
            .Include(r => r.Cameras)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return rovers.Select(r => new RoverResource
        {
            Id = r.Name.ToLowerInvariant(),
            Type = "rover",
            Attributes = new RoverAttributes
            {
                Name = r.Name,
                LandingDate = r.LandingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                LaunchDate = r.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                Status = r.Status,
                MaxSol = r.MaxSol ?? 0,
                MaxDate = r.MaxDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                TotalPhotos = r.TotalPhotos ?? 0
            }
        }).ToList();
    }

    public async Task<RoverResource?> GetRoverBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

        if (rover == null)
            return null;

        var cameras = rover.Cameras.Select(c => new CameraResource
        {
            Id = c.Name,
            Type = "camera",
            Attributes = new CameraResourceAttributes
            {
                Name = c.Name,
                FullName = c.FullName
            }
        }).ToList();

        return new RoverResource
        {
            Id = rover.Name.ToLowerInvariant(),
            Type = "rover",
            Attributes = new RoverAttributes
            {
                Name = rover.Name,
                LandingDate = rover.LandingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                LaunchDate = rover.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                Status = rover.Status,
                MaxSol = rover.MaxSol ?? 0,
                MaxDate = rover.MaxDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                TotalPhotos = rover.TotalPhotos ?? 0
            },
            Relationships = new RoverRelationships
            {
                Cameras = cameras
            }
        };
    }

    public async Task<RoverManifest?> GetRoverManifestAsync(string slug, CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

        if (rover == null)
            return null;

        // Get photos grouped by sol
        var photosBySol = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .GroupBy(p => new { p.Sol, p.EarthDate })
            .Select(g => new
            {
                Sol = g.Key.Sol,
                EarthDate = g.Key.EarthDate,
                TotalPhotos = g.Count(),
                Cameras = g.Select(p => p.Camera.Name).Distinct().ToList()
            })
            .OrderBy(g => g.Sol)
            .ToListAsync(cancellationToken);

        var photos = photosBySol.Select(p => new PhotosBySol
        {
            Sol = p.Sol,
            EarthDate = p.EarthDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            TotalPhotos = p.TotalPhotos,
            Cameras = p.Cameras
        }).ToList();

        return new RoverManifest
        {
            Id = rover.Name.ToLowerInvariant(),
            Type = "manifest",
            Attributes = new ManifestAttributes
            {
                Name = rover.Name,
                LandingDate = rover.LandingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                LaunchDate = rover.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                Status = rover.Status,
                MaxSol = rover.MaxSol ?? 0,
                MaxDate = rover.MaxDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                TotalPhotos = rover.TotalPhotos ?? 0,
                Photos = photos
            }
        };
    }

    public async Task<List<CameraResource>> GetRoverCamerasAsync(string slug, CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

        if (rover == null)
            return new List<CameraResource>();

        // Get photo counts and sol ranges for each camera
        var cameraStats = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .GroupBy(p => p.CameraId)
            .Select(g => new
            {
                CameraId = g.Key,
                PhotoCount = g.Count(),
                FirstSol = g.Min(p => p.Sol),
                LastSol = g.Max(p => p.Sol)
            })
            .ToListAsync(cancellationToken);

        var cameraStatsDict = cameraStats.ToDictionary(s => s.CameraId);

        return rover.Cameras.Select(c => new CameraResource
        {
            Id = c.Name,
            Type = "camera",
            Attributes = new CameraResourceAttributes
            {
                Name = c.Name,
                FullName = c.FullName,
                PhotoCount = cameraStatsDict.TryGetValue(c.Id, out var stats) ? stats.PhotoCount : 0,
                FirstPhotoSol = cameraStatsDict.TryGetValue(c.Id, out var stats2) ? stats2.FirstSol : null,
                LastPhotoSol = cameraStatsDict.TryGetValue(c.Id, out var stats3) ? stats3.LastSol : null
            },
            Relationships = new CameraRelationships
            {
                Rover = new ResourceReference
                {
                    Id = rover.Name.ToLowerInvariant(),
                    Type = "rover"
                }
            }
        }).OrderBy(c => c.Attributes.Name).ToList();
    }
}
