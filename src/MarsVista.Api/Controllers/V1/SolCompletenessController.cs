using MarsVista.Core.Data;
using MarsVista.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Internal API for sol completeness tracking.
/// Provides visibility into which sols have been successfully scraped.
/// Protected by InternalApiMiddleware (X-Internal-Secret header).
/// </summary>
[ApiController]
[Route("api/v1/internal/completeness")]
[ApiExplorerSettings(IgnoreApi = true)]
public class SolCompletenessController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly ISolCompletenessRepository _completenessRepository;
    private readonly ILogger<SolCompletenessController> _logger;

    public SolCompletenessController(
        MarsVistaDbContext context,
        ISolCompletenessRepository completenessRepository,
        ILogger<SolCompletenessController> logger)
    {
        _context = context;
        _completenessRepository = completenessRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get completeness summary for a rover.
    /// </summary>
    [HttpGet("{roverName}")]
    public async Task<IActionResult> GetSummary(string roverName)
    {
        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        var summary = await _completenessRepository.GetSummaryAsync(rover.Id);
        return Ok(summary);
    }

    /// <summary>
    /// Get list of failed sols for a rover.
    /// </summary>
    [HttpGet("{roverName}/failed")]
    public async Task<IActionResult> GetFailedSols(string roverName, [FromQuery] int limit = 100)
    {
        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        var failedSols = await _completenessRepository.GetFailedSolsAsync(rover.Id, limit);

        return Ok(new
        {
            rover = roverName,
            count = failedSols.Count,
            sols = failedSols.Select(s => new
            {
                sol = s.Sol,
                photo_count = s.PhotoCount,
                consecutive_failures = s.ConsecutiveFailures,
                last_attempt = s.LastScrapeAttempt,
                last_error = s.LastError
            })
        });
    }

    /// <summary>
    /// Get list of sols by status for a rover.
    /// </summary>
    [HttpGet("{roverName}/status/{status}")]
    public async Task<IActionResult> GetSolsByStatus(string roverName, string status)
    {
        var validStatuses = new[] { "pending", "success", "partial", "failed", "empty" };
        if (!validStatuses.Contains(status.ToLower()))
        {
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });
        }

        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        var sols = await _completenessRepository.GetByStatusAsync(rover.Id, status.ToLower());

        return Ok(new
        {
            rover = roverName,
            status,
            count = sols.Count,
            sols = sols.Select(s => new
            {
                sol = s.Sol,
                photo_count = s.PhotoCount,
                last_attempt = s.LastScrapeAttempt,
                last_error = s.LastError
            })
        });
    }

    /// <summary>
    /// Trigger backfill of completeness data from existing photos.
    /// Useful after initial migration or to refresh counts.
    /// </summary>
    [HttpPost("{roverName}/backfill")]
    public async Task<IActionResult> TriggerBackfill(string roverName)
    {
        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower());

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{roverName}' not found" });
        }

        _logger.LogInformation("Triggering completeness backfill for {Rover}", roverName);

        await _completenessRepository.BackfillFromPhotosAsync(rover.Id);

        var summary = await _completenessRepository.GetSummaryAsync(rover.Id);

        _logger.LogInformation(
            "Backfill complete for {Rover}: {TotalSols} sols, {TotalPhotos} photos",
            roverName, summary.TotalSols, summary.TotalPhotos);

        return Ok(new
        {
            message = "Backfill complete",
            summary
        });
    }

    /// <summary>
    /// Get all rovers' completeness summaries.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllSummaries()
    {
        var rovers = await _context.Rovers.AsNoTracking().ToListAsync();
        var summaries = new List<SolCompletenessSummary>();

        foreach (var rover in rovers)
        {
            var summary = await _completenessRepository.GetSummaryAsync(rover.Id);
            summaries.Add(summary);
        }

        return Ok(new
        {
            rovers = summaries
        });
    }
}
