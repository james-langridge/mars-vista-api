using MarsVista.Api.Data;
using MarsVista.Api.Repositories;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly IIncrementalScraperService _incrementalScraper;
    private readonly IScraperStateRepository _stateRepository;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        IEnumerable<IScraperService> scrapers,
        IIncrementalScraperService incrementalScraper,
        IScraperStateRepository stateRepository,
        ILogger<ScraperController> logger)
    {
        _scrapers = scrapers;
        _incrementalScraper = incrementalScraper;
        _stateRepository = stateRepository;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger scraping for a specific rover
    /// </summary>
    /// <param name="roverName">Rover name (e.g., "Perseverance")</param>
    /// <returns>Number of photos scraped</returns>
    [HttpPost("{roverName}")]
    public async Task<IActionResult> ScrapeRover(string roverName)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        _logger.LogInformation("Manual scrape triggered for {RoverName}", roverName);

        try
        {
            var count = await scraper.ScrapeAsync();
            return Ok(new
            {
                rover = roverName,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape failed for {RoverName}", roverName);
            return StatusCode(500, new { error = "Scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Scrape a specific sol for a rover
    /// </summary>
    /// <param name="roverName">Rover name</param>
    /// <param name="sol">Mars sol number</param>
    [HttpPost("{roverName}/sol/{sol}")]
    public async Task<IActionResult> ScrapeSol(string roverName, int sol)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        _logger.LogInformation("Manual scrape triggered for {RoverName} sol {Sol}", roverName, sol);

        try
        {
            var count = await scraper.ScrapeSolAsync(sol);
            return Ok(new
            {
                rover = roverName,
                sol,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape failed for {RoverName} sol {Sol}", roverName, sol);
            return StatusCode(500, new { error = "Scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Resume bulk scraping from the last scraped sol
    /// </summary>
    /// <param name="roverName">Rover name</param>
    /// <param name="endSol">Ending sol (inclusive, defaults to latest)</param>
    /// <param name="delayMs">Delay in milliseconds between requests (default 1000ms)</param>
    [HttpPost("{roverName}/bulk/resume")]
    public async Task<IActionResult> ResumeBulkScrape(
        string roverName,
        [FromQuery] int? endSol = null,
        [FromQuery] int delayMs = 1000)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        // Get database context through DI
        var context = HttpContext.RequestServices.GetRequiredService<MarsVistaDbContext>();

        var rover = await context.Rovers
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover not found: {roverName}" });
        }

        // Find the highest sol we've scraped
        var lastScrapedSol = rover.Photos.Any() ? rover.Photos.Max(p => p.Sol) : 0;
        var startSol = lastScrapedSol + 1;

        // Get expected max sol for this rover
        var actualEndSol = endSol ?? await GetLatestSolAsync(scraper);

        if (startSol > actualEndSol)
        {
            return Ok(new
            {
                rover = roverName,
                message = "Already caught up - all sols scraped",
                lastScrapedSol,
                expectedMaxSol = actualEndSol,
                resumeNotNeeded = true
            });
        }

        _logger.LogInformation(
            "Resume bulk scrape for {RoverName}: starting from sol {StartSol} (last scraped: {LastSol})",
            roverName, startSol, lastScrapedSol);

        // Use the standard bulk scrape with the calculated start sol
        return await BulkScrape(roverName, startSol, actualEndSol, delayMs);
    }

    /// <summary>
    /// Bulk scrape a range of sols for a rover
    /// </summary>
    /// <param name="roverName">Rover name</param>
    /// <param name="startSol">Starting sol (inclusive)</param>
    /// <param name="endSol">Ending sol (inclusive, defaults to latest)</param>
    /// <param name="delayMs">Delay in milliseconds between requests (default 1000ms)</param>
    [HttpPost("{roverName}/bulk")]
    public async Task<IActionResult> BulkScrape(
        string roverName,
        [FromQuery] int startSol = 1,
        [FromQuery] int? endSol = null,
        [FromQuery] int delayMs = 1000)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        // Validate parameters
        if (startSol < 0)
        {
            return BadRequest(new { error = "startSol must be >= 0" });
        }

        if (delayMs < 0 || delayMs > 10000)
        {
            return BadRequest(new { error = "delayMs must be between 0 and 10000" });
        }

        // Get latest sol if endSol not specified
        var actualEndSol = endSol ?? await GetLatestSolAsync(scraper);

        if (actualEndSol < startSol)
        {
            return BadRequest(new { error = "endSol must be >= startSol" });
        }

        var totalSols = actualEndSol - startSol + 1;

        _logger.LogInformation(
            "Bulk scrape triggered for {RoverName}: sols {StartSol}-{EndSol} ({TotalSols} sols), {DelayMs}ms delay",
            roverName, startSol, actualEndSol, totalSols, delayMs);

        var startTime = DateTime.UtcNow;
        var totalPhotos = 0;
        var successfulSols = 0;
        var skippedSols = 0;
        var failedSols = new List<int>();

        try
        {
            for (var sol = startSol; sol <= actualEndSol; sol++)
            {
                try
                {
                    var count = await scraper.ScrapeSolAsync(sol);
                    totalPhotos += count;

                    if (count > 0)
                    {
                        successfulSols++;
                        _logger.LogInformation(
                            "Sol {Sol}: {Count} photos scraped ({Progress}/{Total})",
                            sol, count, sol - startSol + 1, totalSols);
                    }
                    else
                    {
                        skippedSols++;
                        _logger.LogDebug("Sol {Sol}: 0 photos (already scraped or no photos)", sol);
                    }

                    // Add delay between requests (except for last one)
                    if (sol < actualEndSol && delayMs > 0)
                    {
                        await Task.Delay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    failedSols.Add(sol);
                    _logger.LogError(ex, "Failed to scrape sol {Sol}", sol);
                    // Continue with next sol instead of stopping
                }
            }

            var duration = DateTime.UtcNow - startTime;

            return Ok(new
            {
                rover = roverName,
                startSol,
                endSol = actualEndSol,
                totalSols,
                successfulSols,
                skippedSols,
                failedSols = failedSols.Count > 0 ? failedSols : null,
                totalPhotosScraped = totalPhotos,
                durationSeconds = (int)duration.TotalSeconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk scrape failed for {RoverName}", roverName);
            return StatusCode(500, new { error = "Bulk scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get scraping progress for a rover
    /// </summary>
    [HttpGet("{roverName}/progress")]
    public async Task<IActionResult> GetProgress(string roverName)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        // Get database context through DI
        var context = HttpContext.RequestServices.GetRequiredService<MarsVistaDbContext>();

        var rover = await context.Rovers
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover not found: {roverName}" });
        }

        var totalPhotos = rover.Photos.Count;
        var solsWithPhotos = rover.Photos.Select(p => p.Sol).Distinct().Count();
        var latestSol = rover.Photos.Any() ? rover.Photos.Max(p => p.Sol) : 0;
        var oldestSol = rover.Photos.Any() ? rover.Photos.Min(p => p.Sol) : 0;
        var lastPhotoDate = rover.Photos.Any() ? rover.Photos.Max(p => p.CreatedAt) : (DateTime?)null;

        // Get expected max sol for this rover
        var expectedMaxSol = await GetLatestSolAsync(scraper);

        // Calculate health status based on last update time
        var now = DateTime.UtcNow;
        var minutesSinceLastUpdate = lastPhotoDate.HasValue
            ? (now - lastPhotoDate.Value).TotalMinutes
            : 0;

        string status;
        string statusMessage;

        if (!lastPhotoDate.HasValue)
        {
            status = "idle";
            statusMessage = "No photos scraped yet";
        }
        else if (solsWithPhotos >= expectedMaxSol)
        {
            status = "complete";
            statusMessage = "All sols scraped";
        }
        else if (minutesSinceLastUpdate < 2)
        {
            status = "active";
            statusMessage = "Scraping in progress";
        }
        else if (minutesSinceLastUpdate < 5)
        {
            status = "slow";
            statusMessage = "Scraping slowly (possible network issues)";
        }
        else if (minutesSinceLastUpdate < 30)
        {
            status = "stalled";
            statusMessage = "Scraper appears stalled (no updates for 5+ minutes)";
        }
        else
        {
            status = "stopped";
            statusMessage = $"No activity for {(int)minutesSinceLastUpdate} minutes - likely stopped or NASA API down";
        }

        return Ok(new
        {
            rover = roverName,
            totalPhotos,
            solsScraped = solsWithPhotos,
            expectedTotalSols = expectedMaxSol,
            percentComplete = expectedMaxSol > 0 ? Math.Round((double)solsWithPhotos / expectedMaxSol * 100, 2) : 0,
            oldestSol,
            latestSol,
            lastPhotoScraped = lastPhotoDate,
            minutesSinceLastUpdate = lastPhotoDate.HasValue ? Math.Round(minutesSinceLastUpdate, 1) : (double?)null,
            status,
            statusMessage,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get available scrapers
    /// </summary>
    [HttpGet]
    public IActionResult GetScrapers()
    {
        var scraperInfo = _scrapers.Select(s => new
        {
            rover = s.RoverName,
            scrapeUrl = $"/api/scraper/{s.RoverName.ToLower()}",
            scrapeSolUrl = $"/api/scraper/{s.RoverName.ToLower()}/sol/{{sol}}",
            bulkScrapeUrl = $"/api/scraper/{s.RoverName.ToLower()}/bulk?startSol=1&endSol=100"
        });

        return Ok(scraperInfo);
    }

    /// <summary>
    /// Scrape a specific PDS volume for Opportunity rover
    /// </summary>
    /// <param name="volumeName">Volume name (e.g., "mer1po_0xxx" for PANCAM)</param>
    [HttpPost("opportunity/volume/{volumeName}")]
    public async Task<IActionResult> ScrapeOpportunityVolume(string volumeName)
    {
        var scraper = _scrapers.OfType<OpportunityScraper>().FirstOrDefault();

        if (scraper == null)
        {
            return NotFound(new { error = "Opportunity scraper not found" });
        }

        _logger.LogInformation("Manual volume scrape triggered for Opportunity: {VolumeName}", volumeName);

        try
        {
            var count = await scraper.ScrapeVolumeAsync(volumeName);
            return Ok(new
            {
                rover = "Opportunity",
                volume = volumeName,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Volume scrape failed for {VolumeName}", volumeName);
            return StatusCode(500, new { error = "Volume scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Scrape all PDS volumes for Opportunity rover
    /// </summary>
    [HttpPost("opportunity/all")]
    public async Task<IActionResult> ScrapeAllOpportunityVolumes()
    {
        var scraper = _scrapers.OfType<OpportunityScraper>().FirstOrDefault();

        if (scraper == null)
        {
            return NotFound(new { error = "Opportunity scraper not found" });
        }

        _logger.LogInformation("Scraping all Opportunity volumes");

        try
        {
            var count = await scraper.ScrapeAllVolumesAsync();
            return Ok(new
            {
                rover = "Opportunity",
                message = "All volumes scraped",
                totalPhotosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All volumes scrape failed for Opportunity");
            return StatusCode(500, new { error = "All volumes scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Scrape a specific PDS volume for Spirit rover
    /// </summary>
    /// <param name="volumeName">Volume name (e.g., "mer2ps_0xxx" for PANCAM)</param>
    [HttpPost("spirit/volume/{volumeName}")]
    public async Task<IActionResult> ScrapeSpiritVolume(string volumeName)
    {
        var scraper = _scrapers.OfType<SpiritScraper>().FirstOrDefault();

        if (scraper == null)
        {
            return NotFound(new { error = "Spirit scraper not found" });
        }

        _logger.LogInformation("Manual volume scrape triggered for Spirit: {VolumeName}", volumeName);

        try
        {
            var count = await scraper.ScrapeVolumeAsync(volumeName);
            return Ok(new
            {
                rover = "Spirit",
                volume = volumeName,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Volume scrape failed for {VolumeName}", volumeName);
            return StatusCode(500, new { error = "Volume scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Scrape all PDS volumes for Spirit rover
    /// </summary>
    [HttpPost("spirit/all")]
    public async Task<IActionResult> ScrapeAllSpiritVolumes()
    {
        var scraper = _scrapers.OfType<SpiritScraper>().FirstOrDefault();

        if (scraper == null)
        {
            return NotFound(new { error = "Spirit scraper not found" });
        }

        _logger.LogInformation("Scraping all Spirit volumes");

        try
        {
            var count = await scraper.ScrapeAllVolumesAsync();
            return Ok(new
            {
                rover = "Spirit",
                message = "All volumes scraped",
                totalPhotosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "All volumes scrape failed for Spirit");
            return StatusCode(500, new { error = "All volumes scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Incremental scrape - only fetch new photos since last scrape
    /// </summary>
    /// <param name="roverName">Rover name</param>
    /// <param name="lookbackSols">Number of sols to look back (default: 7)</param>
    [HttpPost("{roverName}/incremental")]
    public async Task<IActionResult> IncrementalScrape(
        string roverName,
        [FromQuery] int lookbackSols = 7)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        if (lookbackSols < 0 || lookbackSols > 100)
        {
            return BadRequest(new { error = "lookbackSols must be between 0 and 100" });
        }

        _logger.LogInformation(
            "Incremental scrape triggered for {RoverName} with lookback {Lookback} sols",
            roverName, lookbackSols);

        try
        {
            var result = await _incrementalScraper.ScrapeIncrementalAsync(roverName, lookbackSols);

            if (!result.Success)
            {
                return StatusCode(500, new
                {
                    error = "Incremental scrape failed",
                    message = result.ErrorMessage,
                    result
                });
            }

            return Ok(new
            {
                rover = result.RoverName,
                startSol = result.StartSol,
                endSol = result.EndSol,
                totalSols = result.TotalSols,
                photosAdded = result.PhotosAdded,
                successfulSols = result.SuccessfulSols,
                skippedSols = result.SkippedSols,
                failedSols = result.FailedSols.Count > 0 ? result.FailedSols : null,
                durationSeconds = (int)result.Duration.TotalSeconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental scrape failed for {RoverName}", roverName);
            return StatusCode(500, new { error = "Incremental scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get scraper status and last scrape info
    /// </summary>
    [HttpGet("{roverName}/status")]
    public async Task<IActionResult> GetScraperStatus(string roverName)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        var state = await _stateRepository.GetByRoverNameAsync(roverName);

        if (state == null)
        {
            return Ok(new
            {
                rover = roverName,
                status = "not_initialized",
                message = "No scraper state found - incremental scraping not yet configured"
            });
        }

        return Ok(new
        {
            rover = roverName,
            lastScrapedSol = state.LastScrapedSol,
            lastScrapeTimestamp = state.LastScrapeTimestamp,
            lastScrapeStatus = state.LastScrapeStatus,
            photosAddedLastRun = state.PhotosAddedLastRun,
            errorMessage = state.ErrorMessage,
            updatedAt = state.UpdatedAt
        });
    }

    /// <summary>
    /// Reset scraper state to a specific sol (admin only)
    /// </summary>
    [HttpPost("{roverName}/reset-state")]
    public async Task<IActionResult> ResetScraperState(
        string roverName,
        [FromQuery] int sol)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        if (sol < 0)
        {
            return BadRequest(new { error = "sol must be >= 0" });
        }

        _logger.LogInformation(
            "Resetting scraper state for {RoverName} to sol {Sol}",
            roverName, sol);

        try
        {
            await _incrementalScraper.ResetStateAsync(roverName, sol);

            return Ok(new
            {
                rover = roverName,
                message = $"Scraper state reset to sol {sol}",
                lastScrapedSol = sol,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset scraper state for {RoverName}", roverName);
            return StatusCode(500, new { error = "Failed to reset scraper state", message = ex.Message });
        }
    }

    /// <summary>
    /// Helper to get latest sol for a rover
    /// </summary>
    private async Task<int> GetLatestSolAsync(IScraperService scraper)
    {
        // For now, hardcode based on rover name
        // TODO: Could fetch from NASA API dynamically
        return scraper.RoverName switch
        {
            "Perseverance" => 1682,
            "Curiosity" => 4683,
            "Opportunity" => 5111,  // Mission ended on sol 5111
            "Spirit" => 2208,        // Mission ended on sol 2208
            _ => 1000
        };
    }
}
