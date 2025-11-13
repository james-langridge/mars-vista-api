using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        IEnumerable<IScraperService> scrapers,
        ILogger<ScraperController> logger)
    {
        _scrapers = scrapers;
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
            _ => 1000
        };
    }
}
