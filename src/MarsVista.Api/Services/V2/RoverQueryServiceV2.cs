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
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Batch fetch stats for all rovers in a single query (matching V1 approach)
                var roverIds = dbRovers.Select(r => r.Id).ToArray();
                var allStats = await GetBatchRoverStatsAsync(roverIds, cancellationToken);
                var statsLookup = allStats.ToDictionary(s => s.RoverId);

                return dbRovers.Select(r =>
                {
                    var stats = statsLookup.GetValueOrDefault(r.Id);
                    return new RoverResource
                    {
                        Id = r.Name.ToLowerInvariant(),
                        Type = "rover",
                        Attributes = new RoverAttributes
                        {
                            Name = r.Name,
                            LandingDate = r.LandingDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                            LaunchDate = r.LaunchDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                            Status = r.Status,
                            MaxSol = stats?.MaxSol ?? 0,
                            MaxDate = stats?.MaxEarthDate.ToString("yyyy-MM-dd") ?? string.Empty,
                            TotalPhotos = stats?.TotalPhotos ?? 0
                        }
                    };
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
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == slug.ToLower(), cancellationToken);

                if (dbRover == null)
                    return null;

                // Get computed stats from photos table
                var stats = await GetRoverStatsAsync(dbRover.Id, cancellationToken);

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
                        MaxSol = stats.MaxSol,
                        MaxDate = stats.MaxDate,
                        TotalPhotos = stats.TotalPhotos
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
            .AsNoTracking()
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
                // Get computed stats from photos table
                var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

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
                        MaxSol = stats.MaxSol,
                        MaxDate = stats.MaxDate,
                        TotalPhotos = stats.TotalPhotos,
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

    /// <summary>
    /// Get computed stats for a single rover from the photos table
    /// </summary>
    private async Task<(int MaxSol, string MaxDate, int TotalPhotos)> GetRoverStatsAsync(
        int roverId,
        CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT
                COALESCE(COUNT(*), 0)::int as total_photos,
                COALESCE(MAX(sol), 0)::int as max_sol,
                COALESCE(MAX(earth_date), CURRENT_DATE) as max_earth_date
            FROM photos
            WHERE rover_id = {0}";

        var stats = await _context.Database
            .SqlQueryRaw<RoverStatsData>(sql, roverId)
            .FirstOrDefaultAsync(cancellationToken);

        if (stats == null || stats.TotalPhotos == 0)
        {
            return (0, "", 0);
        }

        var maxDate = stats.MaxEarthDate.ToString("yyyy-MM-dd");
        return (stats.MaxSol, maxDate, stats.TotalPhotos);
    }

    /// <summary>
    /// Batch fetch stats for multiple rovers in a single query
    /// </summary>
    private async Task<List<BatchRoverStatsData>> GetBatchRoverStatsAsync(
        int[] roverIds,
        CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT
                rover_id,
                COALESCE(COUNT(*), 0)::int as total_photos,
                COALESCE(MAX(sol), 0)::int as max_sol,
                COALESCE(MAX(earth_date), CURRENT_DATE) as max_earth_date
            FROM photos
            WHERE rover_id = ANY({0})
            GROUP BY rover_id";

        return await _context.Database
            .SqlQueryRaw<BatchRoverStatsData>(sql, roverIds)
            .ToListAsync(cancellationToken);
    }

    // Helper class for single rover stats query
    private class RoverStatsData
    {
        public int TotalPhotos { get; set; }
        public int MaxSol { get; set; }
        public DateTime MaxEarthDate { get; set; }
    }

    // Helper class for batch rover stats query
    private class BatchRoverStatsData
    {
        public int RoverId { get; set; }
        public int TotalPhotos { get; set; }
        public int MaxSol { get; set; }
        public DateTime MaxEarthDate { get; set; }
    }
}
