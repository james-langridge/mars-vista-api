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

        // Get photos grouped by sol
        var photosBySol = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .Include(p => p.Camera)
            .GroupBy(p => p.Sol)
            .Select(g => new PhotosBySolDto
            {
                Sol = g.Key,
                TotalPhotos = g.Count(),
                Cameras = g.Select(p => p.Camera.Name).Distinct().ToList()
            })
            .OrderBy(p => p.Sol)
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
            Photos = photosBySol
        };
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
