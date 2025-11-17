using MarsVista.Api.Data;
using MarsVista.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class RoverQueryService : IRoverQueryService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<RoverQueryService> _logger;

    public RoverQueryService(
        MarsVistaDbContext context,
        ILogger<RoverQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoverDto>> GetAllRoversAsync(CancellationToken cancellationToken = default)
    {
        var rovers = await _context.Rovers
            .Include(r => r.Cameras)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var roverDtos = new List<RoverDto>();

        foreach (var rover in rovers)
        {
            var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

            roverDtos.Add(new RoverDto
            {
                Id = rover.Id,
                Name = rover.Name,
                LandingDate = rover.LandingDate.HasValue ? rover.LandingDate.Value.ToString("yyyy-MM-dd") : "",
                LaunchDate = rover.LaunchDate.HasValue ? rover.LaunchDate.Value.ToString("yyyy-MM-dd") : "",
                Status = rover.Status,
                MaxSol = stats.MaxSol,
                MaxDate = stats.MaxDate,
                TotalPhotos = stats.TotalPhotos,
                Cameras = rover.Cameras
                    .Select(c => new CameraDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        FullName = c.FullName
                    })
                    .ToList()
            });
        }

        return roverDtos;
    }

    public async Task<RoverDto?> GetRoverByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

        if (rover == null)
        {
            return null;
        }

        var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

        return new RoverDto
        {
            Id = rover.Id,
            Name = rover.Name,
            LandingDate = rover.LandingDate.HasValue ? rover.LandingDate.Value.ToString("yyyy-MM-dd") : "",
            LaunchDate = rover.LaunchDate.HasValue ? rover.LaunchDate.Value.ToString("yyyy-MM-dd") : "",
            Status = rover.Status,
            MaxSol = stats.MaxSol,
            MaxDate = stats.MaxDate,
            TotalPhotos = stats.TotalPhotos,
            Cameras = rover.Cameras
                .Select(c => new CameraDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    FullName = c.FullName
                })
                .ToList()
        };
    }

    public async Task<PhotoManifestDto?> GetManifestAsync(
        string roverName,
        CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower(), cancellationToken);

        if (rover == null)
        {
            return null;
        }

        var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

        // Use raw SQL for efficient grouping and aggregation (10-100x faster than EF Core GroupBy)
        // This query groups photos by sol, counts them, and aggregates camera names using PostgreSQL
        var sql = @"
            SELECT
                p.sol,
                MIN(p.earth_date) as earth_date,
                COUNT(*)::int as total_photos,
                ARRAY_AGG(DISTINCT c.name ORDER BY c.name) as cameras
            FROM photos p
            INNER JOIN cameras c ON p.camera_id = c.id
            WHERE p.rover_id = {0}
            GROUP BY p.sol
            ORDER BY p.sol";

        var photosBySol = await _context.Database
            .SqlQueryRaw<ManifestSolData>(sql, rover.Id)
            .ToListAsync(cancellationToken);

        return new PhotoManifestDto
        {
            Name = rover.Name,
            LandingDate = rover.LandingDate.HasValue ? rover.LandingDate.Value.ToString("yyyy-MM-dd") : "",
            LaunchDate = rover.LaunchDate.HasValue ? rover.LaunchDate.Value.ToString("yyyy-MM-dd") : "",
            Status = rover.Status,
            MaxSol = stats.MaxSol,
            MaxDate = stats.MaxDate,
            TotalPhotos = stats.TotalPhotos,
            Photos = photosBySol.Select(s => new PhotosBySolDto
            {
                Sol = s.Sol,
                EarthDate = s.EarthDate?.ToString("yyyy-MM-dd") ?? "",
                TotalPhotos = s.TotalPhotos,
                Cameras = s.Cameras?.ToList() ?? new List<string>()
            }).ToList()
        };
    }

    // Helper class for raw SQL query results (PostgreSQL-specific types)
    private class ManifestSolData
    {
        public int Sol { get; set; }
        public DateTime? EarthDate { get; set; }
        public int TotalPhotos { get; set; }
        public string[]? Cameras { get; set; }
    }

    private async Task<(int MaxSol, string MaxDate, int TotalPhotos)> GetRoverStatsAsync(
        int roverId,
        CancellationToken cancellationToken)
    {
        // Use aggregation queries instead of loading all photos into memory
        var totalPhotos = await _context.Photos
            .Where(p => p.RoverId == roverId)
            .CountAsync(cancellationToken);

        if (totalPhotos == 0)
        {
            return (0, "", 0);
        }

        var maxSol = await _context.Photos
            .Where(p => p.RoverId == roverId)
            .MaxAsync(p => (int?)p.Sol, cancellationToken) ?? 0;

        var maxEarthDate = await _context.Photos
            .Where(p => p.RoverId == roverId)
            .MaxAsync(p => p.EarthDate, cancellationToken);

        var maxDate = maxEarthDate.HasValue ? maxEarthDate.Value.ToString("yyyy-MM-dd") : "";

        return (maxSol, maxDate, totalPhotos);
    }
}
