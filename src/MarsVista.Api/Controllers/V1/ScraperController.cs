using MarsVista.Core.Data;
using MarsVista.Core.Repositories;
using MarsVista.Scraper.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Internal API for on-demand scraping.
/// Allows triggering scrapes for specific sols from admin dashboard.
/// Protected by InternalApiMiddleware (X-Internal-Secret header).
/// </summary>
[ApiController]
[Route("api/v1/internal/scrape")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ScraperController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly ISolCompletenessRepository _completenessRepository;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        MarsVistaDbContext context,
        IEnumerable<IScraperService> scrapers,
        ISolCompletenessRepository completenessRepository,
        ILogger<ScraperController> logger)
    {
        _context = context;
        _scrapers = scrapers;
        _completenessRepository = completenessRepository;
        _logger = logger;
    }

    /// <summary>
    /// Scrape specific sols for a rover.
    /// </summary>
    /// <param name="roverName">Rover name (perseverance, curiosity)</param>
    /// <param name="request">List of sols to scrape</param>
    [HttpPost("{roverName}/sols")]
    public async Task<IActionResult> ScrapeSols(
        string roverName,
        [FromBody] ScrapeSolsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Sols == null || request.Sols.Count == 0)
        {
            return BadRequest(new { error = "sols array is required and must not be empty" });
        }

        if (request.Sols.Count > 100)
        {
            return BadRequest(new { error = "Maximum 100 sols per request" });
        }

        var roverNameLower = roverName.ToLower();

        // Find the scraper for this rover
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover '{roverName}'" });
        }

        // Get rover ID for completeness tracking
        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverNameLower, cancellationToken);

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        _logger.LogInformation(
            "Starting on-demand scrape for {Rover}: {Count} sols",
            roverName, request.Sols.Count);

        var results = new List<SolScrapeResult>();
        var totalPhotos = 0;
        var successCount = 0;
        var failCount = 0;

        foreach (var sol in request.Sols.OrderBy(s => s))
        {
            var result = new SolScrapeResult { Sol = sol };

            try
            {
                var photosAdded = await scraper.ScrapeSolAsync(sol, cancellationToken);

                // Get total photo count for this sol
                var solPhotoCount = await _context.Photos
                    .CountAsync(p => p.RoverId == rover.Id && p.Sol == sol, cancellationToken);

                // Record success in completeness
                await _completenessRepository.RecordSuccessAsync(rover.Id, sol, solPhotoCount);

                result.Success = true;
                result.PhotosAdded = photosAdded;
                result.TotalPhotos = solPhotoCount;
                totalPhotos += photosAdded;
                successCount++;

                _logger.LogInformation(
                    "Sol {Sol}: SUCCESS ({Added} new, {Total} total)",
                    sol, photosAdded, solPhotoCount);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (errorMessage.Length > 200)
                    errorMessage = errorMessage[..200] + "...";

                // Record failure in completeness
                await _completenessRepository.RecordFailureAsync(rover.Id, sol, errorMessage);

                result.Success = false;
                result.Error = errorMessage;
                failCount++;

                _logger.LogWarning(
                    "Sol {Sol}: FAILED - {Error}",
                    sol, errorMessage);
            }

            results.Add(result);
        }

        _logger.LogInformation(
            "On-demand scrape complete for {Rover}: {Success}/{Total} sols, {Photos} photos added",
            roverName, successCount, request.Sols.Count, totalPhotos);

        return Ok(new
        {
            rover = roverName,
            sols_requested = request.Sols.Count,
            sols_succeeded = successCount,
            sols_failed = failCount,
            photos_added = totalPhotos,
            results
        });
    }

    /// <summary>
    /// Retry all failed sols for a rover.
    /// </summary>
    [HttpPost("{roverName}/retry-failed")]
    public async Task<IActionResult> RetryFailedSols(
        string roverName,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var roverNameLower = roverName.ToLower();

        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverNameLower, cancellationToken);

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        // Get failed sols
        var failedSols = await _completenessRepository.GetFailedSolsAsync(rover.Id, limit);

        if (failedSols.Count == 0)
        {
            return Ok(new
            {
                rover = roverName,
                message = "No failed sols to retry",
                sols_retried = 0
            });
        }

        // Create request with failed sol numbers
        var request = new ScrapeSolsRequest
        {
            Sols = failedSols.Select(s => s.Sol).ToList()
        };

        // Delegate to ScrapeSols
        return await ScrapeSols(roverName, request, cancellationToken);
    }

    /// <summary>
    /// Scrape a range of sols for a rover.
    /// </summary>
    [HttpPost("{roverName}/range")]
    public async Task<IActionResult> ScrapeRange(
        string roverName,
        [FromQuery] int startSol,
        [FromQuery] int endSol,
        CancellationToken cancellationToken)
    {
        if (startSol < 0 || endSol < 0)
        {
            return BadRequest(new { error = "startSol and endSol must be non-negative" });
        }

        if (endSol < startSol)
        {
            return BadRequest(new { error = "endSol must be >= startSol" });
        }

        var solCount = endSol - startSol + 1;
        if (solCount > 100)
        {
            return BadRequest(new { error = $"Range too large ({solCount} sols). Maximum 100 sols per request." });
        }

        var sols = Enumerable.Range(startSol, solCount).ToList();
        var request = new ScrapeSolsRequest { Sols = sols };

        return await ScrapeSols(roverName, request, cancellationToken);
    }
}

public class ScrapeSolsRequest
{
    public List<int> Sols { get; set; } = new();
}

public class SolScrapeResult
{
    public int Sol { get; set; }
    public bool Success { get; set; }
    public int PhotosAdded { get; set; }
    public int TotalPhotos { get; set; }
    public string? Error { get; set; }
}
