using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of v2 rover query service with two-level caching
/// </summary>
public class RoverQueryServiceV2 : IRoverQueryServiceV2
{
    private readonly MarsVistaDbContext _context;
    private readonly ICachingServiceV2 _cachingService;
    private readonly ILogger<RoverQueryServiceV2> _logger;

    public RoverQueryServiceV2(
        MarsVistaDbContext context,
        ICachingServiceV2 cachingService,
        ILogger<RoverQueryServiceV2> logger)
    {
        _context = context;
        _cachingService = cachingService;
        _logger = logger;
    }

    public async Task<List<RoverResource>> GetAllRoversAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _cachingService.GenerateCacheKey("rovers", "list");

        var rovers = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var dbRovers = await _context.Rovers
                    .Include(r => r.Cameras)
                    .OrderBy(r => r.Name)
                    .ToListAsync(cancellationToken);

                return dbRovers.Select(r => new RoverResource
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
            },
            _cachingService.GetStaticResourceCacheOptions());

        return rovers ?? new List<RoverResource>();
    }

    public async Task<RoverResource?> GetRoverBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = _cachingService.GenerateCacheKey("rover", slug.ToLowerInvariant());

        var rover = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var dbRover = await _context.Rovers
                    .Include(r => r.Cameras)
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

                if (dbRover == null)
                    return null;

                var cameras = dbRover.Cameras.Select(c => new CameraResource
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
                    Id = dbRover.Name.ToLowerInvariant(),
                    Type = "rover",
                    Attributes = new RoverAttributes
                    {
                        Name = dbRover.Name,
                        LandingDate = dbRover.LandingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                        LaunchDate = dbRover.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                        Status = dbRover.Status,
                        MaxSol = dbRover.MaxSol ?? 0,
                        MaxDate = dbRover.MaxDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                        TotalPhotos = dbRover.TotalPhotos ?? 0
                    },
                    Relationships = new RoverRelationships
                    {
                        Cameras = cameras
                    }
                };
            },
            _cachingService.GetStaticResourceCacheOptions());

        return rover;
    }

    public async Task<RoverManifest?> GetRoverManifestAsync(string slug, CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

        if (rover == null)
            return null;

        // Determine if this is an active rover for cache duration
        var isActiveRover = rover.Status?.ToLowerInvariant() == "active";

        // Include photo count in cache key for auto-invalidation when new photos are added
        var photoCount = await _context.Photos.CountAsync(p => p.RoverId == rover.Id, cancellationToken);
        var cacheKey = _cachingService.GenerateCacheKey("manifest", slug.ToLowerInvariant(), photoCount);

        var manifest = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
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
            },
            _cachingService.GetManifestCacheOptions(isActiveRover));

        return manifest;
    }

    public async Task<List<CameraResource>> GetRoverCamerasAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = _cachingService.GenerateCacheKey("cameras", slug.ToLowerInvariant());

        var cameras = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var dbRover = await _context.Rovers
                    .Include(r => r.Cameras)
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

                if (dbRover == null)
                    return new List<CameraResource>();

                // Get photo counts and sol ranges for each camera
                var cameraStats = await _context.Photos
                    .Where(p => p.RoverId == dbRover.Id)
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

                return dbRover.Cameras.Select(c => new CameraResource
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
                            Id = dbRover.Name.ToLowerInvariant(),
                            Type = "rover"
                        }
                    }
                }).OrderBy(c => c.Attributes.Name).ToList();
            },
            _cachingService.GetStaticResourceCacheOptions());

        return cameras ?? new List<CameraResource>();
    }
}
