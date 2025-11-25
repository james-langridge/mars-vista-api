using MarsVista.Core.Data;
using MarsVista.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class PhotoQueryService : IPhotoQueryService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PhotoQueryService> _logger;

    public PhotoQueryService(
        MarsVistaDbContext context,
        ILogger<PhotoQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<PhotoDto> Photos, int TotalCount)> QueryPhotosAsync(
        string roverName,
        int? sol = null,
        DateTime? earthDate = null,
        string? camera = null,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        page = Math.Max(1, page);
        perPage = Math.Clamp(perPage, 1, 100);

        // Start with base query - NO Include()! Direct Select() projection is more efficient
        // EF Core will automatically join the related tables when referenced in Select()
        var query = _context.Photos
            .AsNoTracking() // Don't track entities for read-only operations
            .Where(p => p.Rover.Name.ToLower() == roverName.ToLower());

        // Apply filters
        if (sol.HasValue)
        {
            query = query.Where(p => p.Sol == sol.Value);
        }

        if (earthDate.HasValue)
        {
            // PostgreSQL requires DateTime with Kind=UTC for timestamp with time zone columns
            // Use date range comparison for better index usage (avoid .Date property)
            var startDate = DateTime.SpecifyKind(earthDate.Value.Date, DateTimeKind.Utc);
            var endDate = startDate.AddDays(1);
            query = query.Where(p => p.EarthDate.HasValue &&
                                   p.EarthDate.Value >= startDate &&
                                   p.EarthDate.Value < endDate);
        }

        if (!string.IsNullOrWhiteSpace(camera))
        {
            query = query.Where(p => p.Camera.Name.ToLower() == camera.ToLower());
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Order by camera and ID for consistent results, then apply pagination and projection
        // By removing Include() and going straight to Select(), we only fetch the columns we need
        var photos = await query
            .OrderBy(p => p.CameraId)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Sol = p.Sol,
                ImgSrc = !string.IsNullOrEmpty(p.ImgSrcMedium) ? p.ImgSrcMedium :
                         !string.IsNullOrEmpty(p.ImgSrcLarge) ? p.ImgSrcLarge :
                         !string.IsNullOrEmpty(p.ImgSrcFull) ? p.ImgSrcFull : "",
                EarthDate = p.EarthDate.HasValue ? p.EarthDate.Value.ToString("yyyy-MM-dd") : "",
                Camera = new CameraDto
                {
                    Id = p.Camera.Id,
                    Name = p.Camera.Name,
                    FullName = p.Camera.FullName
                },
                Rover = new RoverSummaryDto
                {
                    Id = p.Rover.Id,
                    Name = p.Rover.Name,
                    LandingDate = p.Rover.LandingDate.HasValue ? p.Rover.LandingDate.Value.ToString("yyyy-MM-dd") : "",
                    LaunchDate = p.Rover.LaunchDate.HasValue ? p.Rover.LaunchDate.Value.ToString("yyyy-MM-dd") : "",
                    Status = p.Rover.Status
                }
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Queried {Count} photos for {Rover} (sol: {Sol}, date: {Date}, camera: {Camera}, page: {Page})",
            photos.Count, roverName, sol, earthDate?.ToString("yyyy-MM-dd") ?? "null", camera ?? "null", page);

        return (photos, totalCount);
    }

    public async Task<(List<PhotoDto> Photos, int TotalCount)> GetLatestPhotosAsync(
        string roverName,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Find the maximum sol for this rover
        var maxSol = await _context.Photos
            .Where(p => p.Rover.Name.ToLower() == roverName.ToLower())
            .MaxAsync(p => (int?)p.Sol, cancellationToken);

        if (!maxSol.HasValue)
        {
            return (new List<PhotoDto>(), 0);
        }

        // Query photos for the latest sol
        return await QueryPhotosAsync(
            roverName,
            sol: maxSol.Value,
            page: page,
            perPage: perPage,
            cancellationToken: cancellationToken);
    }

    public async Task<PhotoDto?> GetPhotoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        // No Include() needed - EF Core optimizes joins when using Select() projection
        var photo = await _context.Photos
            .Where(p => p.Id == id)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Sol = p.Sol,
                ImgSrc = !string.IsNullOrEmpty(p.ImgSrcMedium) ? p.ImgSrcMedium :
                         !string.IsNullOrEmpty(p.ImgSrcLarge) ? p.ImgSrcLarge :
                         !string.IsNullOrEmpty(p.ImgSrcFull) ? p.ImgSrcFull : "",
                EarthDate = p.EarthDate.HasValue ? p.EarthDate.Value.ToString("yyyy-MM-dd") : "",
                Camera = new CameraDto
                {
                    Id = p.Camera.Id,
                    Name = p.Camera.Name,
                    FullName = p.Camera.FullName
                },
                Rover = new RoverSummaryDto
                {
                    Id = p.Rover.Id,
                    Name = p.Rover.Name,
                    LandingDate = p.Rover.LandingDate.HasValue ? p.Rover.LandingDate.Value.ToString("yyyy-MM-dd") : "",
                    LaunchDate = p.Rover.LaunchDate.HasValue ? p.Rover.LaunchDate.Value.ToString("yyyy-MM-dd") : "",
                    Status = p.Rover.Status
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return photo;
    }
}
