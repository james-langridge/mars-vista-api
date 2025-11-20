using MarsVista.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Internal API for scraper monitoring and management.
/// Called by Next.js admin dashboard after validating Auth.js sessions.
/// Protected by InternalApiMiddleware (X-Internal-Secret header).
/// </summary>
[ApiController]
[Route("api/v1/internal/admin/scraper")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminScraperController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<AdminScraperController> _logger;

    public AdminScraperController(
        MarsVistaDbContext context,
        ILogger<AdminScraperController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get current scraper status for all rovers.
    /// Shows last scrape results, current state, and health indicators.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetScraperStatus()
    {
        try
        {
            var scraperStates = await _context.ScraperStates
                .OrderBy(s => s.RoverName)
                .ToListAsync();

            var scrapers = new List<object>();

            foreach (var state in scraperStates)
            {
                // Get total photos for this rover
                var rover = await _context.Rovers
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == state.RoverName.ToLower());

                var totalPhotos = 0;
                if (rover != null)
                {
                    totalPhotos = await _context.Photos
                        .Where(p => p.RoverId == rover.Id)
                        .CountAsync();
                }

                // Determine health status based on last scrape
                var healthStatus = DetermineHealthStatus(state);

                scrapers.Add(new
                {
                    roverName = state.RoverName,
                    lastScrapedSol = state.LastScrapedSol,
                    currentMissionSol = state.LastScrapedSol, // Would query NASA API in real-time
                    lastRunTimestamp = state.LastScrapeTimestamp,
                    lastRunStatus = state.LastScrapeStatus,
                    photosAddedLastRun = state.PhotosAddedLastRun,
                    errorMessage = state.ErrorMessage,
                    healthStatus = healthStatus,
                    totalPhotos = totalPhotos
                });
            }

            return Ok(new
            {
                scrapers = scrapers,
                nextScheduledRun = (DateTime?)null // Would come from cron schedule configuration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scraper status");
            return StatusCode(500, new { error = "Failed to retrieve scraper status" });
        }
    }

    /// <summary>
    /// Get recent scraper job history with optional filters.
    /// Returns paginated list of scraper runs with rover details.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetScraperHistory(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? rover = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Validate limit
            if (limit < 10 || limit > 100)
            {
                return BadRequest(new { error = "Limit must be between 10 and 100" });
            }

            // Build query
            var query = _context.ScraperJobHistories
                .Include(j => j.RoverDetails)
                .AsQueryable();

            // Apply date filters
            if (startDate.HasValue)
            {
                query = query.Where(j => j.JobStartedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(j => j.JobStartedAt <= endDate.Value);
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(j => j.Status == status.ToLower());
            }

            // Apply rover filter (filter at rover details level)
            if (!string.IsNullOrEmpty(rover))
            {
                query = query.Where(j => j.RoverDetails.Any(r =>
                    r.RoverName.ToLower() == rover.ToLower()));
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply ordering and pagination - fetch entities first
            var jobEntities = await query
                .OrderByDescending(j => j.JobStartedAt)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            // Map to response DTOs with JSON deserialization
            var jobs = jobEntities.Select(j => new
            {
                id = j.Id,
                startedAt = j.JobStartedAt,
                completedAt = j.JobCompletedAt,
                durationSeconds = j.TotalDurationSeconds,
                totalRoversAttempted = j.TotalRoversAttempted,
                totalRoversSucceeded = j.TotalRoversSucceeded,
                totalPhotosAdded = j.TotalPhotosAdded,
                status = j.Status,
                errorSummary = j.ErrorSummary,
                roverDetails = j.RoverDetails.Select(r => new
                {
                    roverName = r.RoverName,
                    startSol = r.StartSol,
                    endSol = r.EndSol,
                    solsAttempted = r.SolsAttempted,
                    solsSucceeded = r.SolsSucceeded,
                    solsFailed = r.SolsFailed,
                    photosAdded = r.PhotosAdded,
                    durationSeconds = r.DurationSeconds,
                    status = r.Status,
                    errorMessage = r.ErrorMessage,
                    failedSols = !string.IsNullOrEmpty(r.FailedSols)
                        ? System.Text.Json.JsonSerializer.Deserialize<List<int>>(r.FailedSols) ?? new List<int>()
                        : new List<int>()
                }).ToList()
            }).ToList();

            return Ok(new
            {
                jobs = jobs,
                total = total,
                limit = limit,
                offset = offset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scraper history");
            return StatusCode(500, new { error = "Failed to retrieve scraper history" });
        }
    }

    /// <summary>
    /// Get scraper performance metrics for a specified time period.
    /// Returns aggregated statistics about scraper runs.
    /// </summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetScraperMetrics([FromQuery] string period = "7d")
    {
        try
        {
            // Parse period
            DateTime cutoffDate = period switch
            {
                "24h" => DateTime.UtcNow.AddHours(-24),
                "7d" => DateTime.UtcNow.AddDays(-7),
                "30d" => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-7)
            };

            // Get jobs within period
            var jobs = await _context.ScraperJobHistories
                .Include(j => j.RoverDetails)
                .Where(j => j.JobStartedAt >= cutoffDate)
                .ToListAsync();

            var totalJobs = jobs.Count;
            var successfulJobs = jobs.Count(j => j.Status == "success");
            var failedJobs = jobs.Count(j => j.Status == "failed");
            var partialJobs = jobs.Count(j => j.Status == "partial");
            var successRate = totalJobs > 0 ? (double)successfulJobs / totalJobs * 100 : 0;

            var totalPhotosAdded = jobs.Sum(j => j.TotalPhotosAdded);
            var avgDuration = jobs.Any() && jobs.Any(j => j.TotalDurationSeconds.HasValue)
                ? (int)jobs.Where(j => j.TotalDurationSeconds.HasValue)
                    .Average(j => j.TotalDurationSeconds!.Value)
                : 0;

            // Get rover breakdown
            var roverBreakdown = new List<object>();
            var roverNames = jobs.SelectMany(j => j.RoverDetails)
                .Select(r => r.RoverName)
                .Distinct()
                .OrderBy(n => n);

            foreach (var roverName in roverNames)
            {
                var roverDetails = jobs.SelectMany(j => j.RoverDetails)
                    .Where(r => r.RoverName == roverName)
                    .ToList();

                var rover = await _context.Rovers
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

                var totalPhotos = 0;
                if (rover != null)
                {
                    totalPhotos = await _context.Photos
                        .Where(p => p.RoverId == rover.Id)
                        .CountAsync();
                }

                var photosAddedPeriod = roverDetails.Sum(r => r.PhotosAdded);
                var successfulRuns = roverDetails.Count(r => r.Status == "success");
                var failedRuns = roverDetails.Count(r => r.Status == "failed");
                var avgRoverDuration = roverDetails.Any()
                    ? (int)roverDetails.Average(r => r.DurationSeconds)
                    : 0;

                roverBreakdown.Add(new
                {
                    roverName = roverName,
                    totalPhotos = totalPhotos,
                    photosAddedPeriod = photosAddedPeriod,
                    successfulRuns = successfulRuns,
                    failedRuns = failedRuns,
                    averageDurationSeconds = avgRoverDuration
                });
            }

            return Ok(new
            {
                period = period,
                metrics = new
                {
                    totalJobs = totalJobs,
                    successfulJobs = successfulJobs,
                    failedJobs = failedJobs,
                    partialJobs = partialJobs,
                    successRate = Math.Round(successRate, 1),
                    totalPhotosAdded = totalPhotosAdded,
                    averageDurationSeconds = avgDuration,
                    roverBreakdown = roverBreakdown
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get scraper metrics");
            return StatusCode(500, new { error = "Failed to retrieve scraper metrics" });
        }
    }

    private string DetermineHealthStatus(Entities.ScraperState state)
    {
        // Determine health based on last scrape status and recency
        if (state.LastScrapeStatus == "failed")
        {
            return "error";
        }

        // Check if last scrape was recent (within 36 hours for daily cron)
        var timeSinceLastScrape = DateTime.UtcNow - state.LastScrapeTimestamp;
        if (timeSinceLastScrape.TotalHours > 36)
        {
            return "warning";
        }

        if (state.LastScrapeStatus == "in_progress")
        {
            return "warning";
        }

        return "healthy";
    }
}
