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

            // Get photos added in the most recent scraper job
            var mostRecentJob = await _context.ScraperJobHistories
                .OrderByDescending(j => j.JobStartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            long photosAddedToday = 0;
            DateTime? lastScrapeAt = null;

            if (mostRecentJob != null)
            {
                lastScrapeAt = mostRecentJob.JobStartedAt;

                // Sum photos added across all rovers in this job
                photosAddedToday = await _context.ScraperJobRoverDetails
                    .Where(d => d.JobHistoryId == mostRecentJob.Id)
                    .SumAsync(d => (long)d.PhotosAdded, cancellationToken);
            }

            return new DatabaseStatisticsDto
            {
                TotalPhotos = totalPhotos,
                PhotosAddedToday = photosAddedToday,
                RoverCount = roverCount,
                EarliestPhotoDate = earliestDate?.ToString("yyyy-MM-dd"),
                LatestPhotoDate = latestDate?.ToString("yyyy-MM-dd"),
                LastScrapeAt = lastScrapeAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database statistics");
            throw;
        }
    }
}
