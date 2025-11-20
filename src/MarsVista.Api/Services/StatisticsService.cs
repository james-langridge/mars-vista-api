using MarsVista.Api.Data;
using MarsVista.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class StatisticsService : IStatisticsService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(MarsVistaDbContext context, ILogger<StatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DatabaseStatisticsDto> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get total photos count
            var totalPhotos = await _context.Photos.LongCountAsync(cancellationToken);

            // Get rover count
            var roverCount = await _context.Rovers.CountAsync(cancellationToken);

            // Get earliest and latest photo dates
            var earliestDate = await _context.Photos
                .Where(p => p.EarthDate.HasValue)
                .OrderBy(p => p.EarthDate)
                .Select(p => p.EarthDate)
                .FirstOrDefaultAsync(cancellationToken);

            var latestDate = await _context.Photos
                .Where(p => p.EarthDate.HasValue)
                .OrderByDescending(p => p.EarthDate)
                .Select(p => p.EarthDate)
                .FirstOrDefaultAsync(cancellationToken);

            // Get photos added in the last 7 days from scraper jobs
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var recentJobs = await _context.ScraperJobHistories
                .Where(j => j.JobStartedAt >= sevenDaysAgo)
                .Select(j => j.Id)
                .ToListAsync(cancellationToken);

            long photosAddedLast7Days = 0;
            if (recentJobs.Any())
            {
                photosAddedLast7Days = await _context.ScraperJobRoverDetails
                    .Where(d => recentJobs.Contains(d.JobHistoryId))
                    .SumAsync(d => (long)d.PhotosAdded, cancellationToken);
            }

            // Get most recent scraper job timestamp
            var mostRecentJob = await _context.ScraperJobHistories
                .OrderByDescending(j => j.JobStartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return new DatabaseStatisticsDto
            {
                TotalPhotos = totalPhotos,
                PhotosAddedLast7Days = photosAddedLast7Days,
                RoverCount = roverCount,
                EarliestPhotoDate = earliestDate?.ToString("yyyy-MM-dd"),
                LatestPhotoDate = latestDate?.ToString("yyyy-MM-dd"),
                LastScrapeAt = mostRecentJob?.JobStartedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database statistics");
            throw;
        }
    }
}
