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
    /// Get available scrapers
    /// </summary>
    [HttpGet]
    public IActionResult GetScrapers()
    {
        var scraperInfo = _scrapers.Select(s => new
        {
            rover = s.RoverName,
            scrapeUrl = $"/api/scraper/{s.RoverName.ToLower()}",
            scrapeSolUrl = $"/api/scraper/{s.RoverName.ToLower()}/sol/{{sol}}"
        });

        return Ok(scraperInfo);
    }
}
