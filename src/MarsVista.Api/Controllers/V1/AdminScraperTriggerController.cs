using MarsVista.Api.Filters;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Admin API for triggering and controlling scraper operations.
/// Protected by AdminAuthorization filter - requires admin role.
/// </summary>
[ApiController]
[Route("api/v1/admin/scraper")]
[AdminAuthorization]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminScraperTriggerController : ControllerBase
{
    private readonly IAdminScraperTriggerService _triggerService;
    private readonly IScraperJobTracker _jobTracker;
    private readonly ILogger<AdminScraperTriggerController> _logger;

    public AdminScraperTriggerController(
        IAdminScraperTriggerService triggerService,
        IScraperJobTracker jobTracker,
        ILogger<AdminScraperTriggerController> logger)
    {
        _triggerService = triggerService;
        _jobTracker = jobTracker;
        _logger = logger;
    }

    /// <summary>
    /// Trigger incremental scrape for all active rovers (like daily cron).
    /// Uses 7-sol lookback window by default.
    /// </summary>
    [HttpPost("trigger/incremental")]
    public async Task<IActionResult> TriggerIncremental([FromBody] IncrementalTriggerRequest? request = null)
    {
        try
        {
            var job = await _triggerService.TriggerIncrementalAsync(request?.LookbackSols);

            return Ok(new
            {
                jobId = job.Id,
                status = job.Status,
                type = job.Type,
                message = $"Incremental scrape started with {request?.LookbackSols ?? 7} sol lookback"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger incremental scrape");
            return StatusCode(500, new { error = "Failed to start incremental scrape", message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger scrape for a specific sol on a specific rover.
    /// </summary>
    [HttpPost("trigger/sol")]
    public async Task<IActionResult> TriggerSol([FromBody] SolTriggerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Rover))
        {
            return BadRequest(new { error = "Rover name is required" });
        }

        if (request.Sol < 0)
        {
            return BadRequest(new { error = "Sol must be >= 0" });
        }

        if (!IsValidRover(request.Rover))
        {
            return BadRequest(new { error = $"Invalid rover: {request.Rover}. Valid rovers: perseverance, curiosity" });
        }

        try
        {
            var job = await _triggerService.TriggerSolAsync(request.Rover, request.Sol);

            return Ok(new
            {
                jobId = job.Id,
                status = job.Status,
                type = job.Type,
                rover = job.Rover,
                sol = request.Sol,
                message = $"Sol {request.Sol} scrape started for {request.Rover}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger sol scrape");
            return StatusCode(500, new { error = "Failed to start sol scrape", message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger scrape for a range of sols on a specific rover.
    /// </summary>
    [HttpPost("trigger/range")]
    public async Task<IActionResult> TriggerRange([FromBody] RangeTriggerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Rover))
        {
            return BadRequest(new { error = "Rover name is required" });
        }

        if (request.StartSol < 0)
        {
            return BadRequest(new { error = "StartSol must be >= 0" });
        }

        if (request.EndSol < request.StartSol)
        {
            return BadRequest(new { error = "EndSol must be >= StartSol" });
        }

        if (!IsValidRover(request.Rover))
        {
            return BadRequest(new { error = $"Invalid rover: {request.Rover}. Valid rovers: perseverance, curiosity" });
        }

        var solCount = request.EndSol - request.StartSol + 1;
        if (solCount > 1000)
        {
            return BadRequest(new { error = "Sol range too large. Max 1000 sols per request." });
        }

        try
        {
            var job = await _triggerService.TriggerRangeAsync(
                request.Rover,
                request.StartSol,
                request.EndSol,
                request.DelayMs ?? 500);

            return Ok(new
            {
                jobId = job.Id,
                status = job.Status,
                type = job.Type,
                rover = job.Rover,
                startSol = request.StartSol,
                endSol = request.EndSol,
                totalSols = solCount,
                message = $"Range scrape started for {request.Rover}: sols {request.StartSol}-{request.EndSol}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger range scrape");
            return StatusCode(500, new { error = "Failed to start range scrape", message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger full rover re-scrape. This is dangerous and requires explicit confirmation.
    /// </summary>
    [HttpPost("trigger/full")]
    public async Task<IActionResult> TriggerFull([FromBody] FullTriggerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Rover))
        {
            return BadRequest(new { error = "Rover name is required" });
        }

        if (!request.Confirm)
        {
            return BadRequest(new { error = "You must set confirm=true to trigger a full re-scrape. This will take several hours." });
        }

        if (!IsValidRover(request.Rover))
        {
            return BadRequest(new { error = $"Invalid rover: {request.Rover}. Valid rovers: perseverance, curiosity" });
        }

        try
        {
            var job = await _triggerService.TriggerFullAsync(request.Rover);

            return Ok(new
            {
                jobId = job.Id,
                status = job.Status,
                type = job.Type,
                rover = job.Rover,
                startSol = job.StartSol,
                endSol = job.EndSol,
                totalSols = job.TotalSols,
                message = $"Full re-scrape started for {request.Rover}. This will take several hours.",
                warning = "Full re-scrape in progress. Do not restart the server until complete or cancelled."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger full scrape");
            return StatusCode(500, new { error = "Failed to start full scrape", message = ex.Message });
        }
    }

    /// <summary>
    /// Get status of a specific scraper job.
    /// </summary>
    [HttpGet("job/{jobId}")]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = _jobTracker.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }

        return Ok(new
        {
            jobId = job.Id,
            type = job.Type,
            rover = job.Rover,
            status = job.Status,
            startSol = job.StartSol,
            endSol = job.EndSol,
            currentSol = job.CurrentSol,
            totalSols = job.TotalSols,
            photosAdded = job.PhotosAdded,
            solsCompleted = job.SolsCompleted,
            solsFailed = job.SolsFailed,
            progressPercent = job.ProgressPercent,
            startedAt = job.StartedAt,
            completedAt = job.CompletedAt,
            elapsedSeconds = job.ElapsedSeconds,
            estimatedSecondsRemaining = job.EstimatedSecondsRemaining,
            errorMessage = job.ErrorMessage
        });
    }

    /// <summary>
    /// Get all active scraper jobs.
    /// </summary>
    [HttpGet("jobs/active")]
    public IActionResult GetActiveJobs()
    {
        var jobs = _jobTracker.GetActiveJobs();

        return Ok(new
        {
            count = jobs.Count,
            jobs = jobs.Select(j => new
            {
                jobId = j.Id,
                type = j.Type,
                rover = j.Rover,
                status = j.Status,
                currentSol = j.CurrentSol,
                progressPercent = j.ProgressPercent,
                photosAdded = j.PhotosAdded,
                startedAt = j.StartedAt,
                elapsedSeconds = j.ElapsedSeconds,
                estimatedSecondsRemaining = j.EstimatedSecondsRemaining
            })
        });
    }

    /// <summary>
    /// Get recent scraper job history (manual triggers).
    /// </summary>
    [HttpGet("jobs/history")]
    public IActionResult GetJobHistory([FromQuery] int limit = 20)
    {
        var allJobs = _jobTracker.GetAllJobs();
        var jobs = allJobs.Take(Math.Min(limit, 100)).ToList();

        return Ok(new
        {
            total = allJobs.Count,
            count = jobs.Count,
            jobs = jobs.Select(j => new
            {
                jobId = j.Id,
                type = j.Type,
                rover = j.Rover,
                status = j.Status,
                startSol = j.StartSol,
                endSol = j.EndSol,
                photosAdded = j.PhotosAdded,
                solsCompleted = j.SolsCompleted,
                solsFailed = j.SolsFailed,
                startedAt = j.StartedAt,
                completedAt = j.CompletedAt,
                errorMessage = j.ErrorMessage
            })
        });
    }

    /// <summary>
    /// Cancel a running scraper job.
    /// </summary>
    [HttpDelete("job/{jobId}")]
    public IActionResult CancelJob(string jobId)
    {
        var job = _jobTracker.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new { error = $"Job {jobId} not found" });
        }

        if (job.Status is not ("started" or "in_progress"))
        {
            return BadRequest(new { error = $"Job {jobId} is not running (status: {job.Status})" });
        }

        var cancelled = _jobTracker.RequestCancel(jobId);
        if (cancelled)
        {
            return Ok(new
            {
                jobId = jobId,
                message = "Cancellation requested. Job will stop after current sol completes.",
                status = job.Status
            });
        }

        return BadRequest(new { error = "Failed to cancel job" });
    }

    /// <summary>
    /// Get current mission sol for a rover from NASA API.
    /// </summary>
    [HttpGet("mission-sol/{rover}")]
    public async Task<IActionResult> GetMissionSol(string rover)
    {
        if (!IsValidRover(rover))
        {
            return BadRequest(new { error = $"Invalid rover: {rover}. Valid rovers: perseverance, curiosity" });
        }

        var sol = await _triggerService.GetCurrentMissionSolAsync(rover);
        if (!sol.HasValue)
        {
            return NotFound(new { error = $"Could not determine current sol for {rover}" });
        }

        return Ok(new
        {
            rover = rover.ToLower(),
            currentSol = sol.Value
        });
    }

    private static bool IsValidRover(string rover)
    {
        var validRovers = new[] { "perseverance", "curiosity" };
        return validRovers.Contains(rover.ToLower());
    }
}

// Request DTOs
public class IncrementalTriggerRequest
{
    /// <summary>
    /// Number of sols to look back from current mission sol. Default: 7
    /// </summary>
    public int? LookbackSols { get; set; }
}

public class SolTriggerRequest
{
    /// <summary>
    /// Rover name (perseverance or curiosity)
    /// </summary>
    public string Rover { get; set; } = string.Empty;

    /// <summary>
    /// Sol number to scrape
    /// </summary>
    public int Sol { get; set; }
}

public class RangeTriggerRequest
{
    /// <summary>
    /// Rover name (perseverance or curiosity)
    /// </summary>
    public string Rover { get; set; } = string.Empty;

    /// <summary>
    /// Starting sol (inclusive)
    /// </summary>
    public int StartSol { get; set; }

    /// <summary>
    /// Ending sol (inclusive)
    /// </summary>
    public int EndSol { get; set; }

    /// <summary>
    /// Delay between sols in milliseconds. Default: 500
    /// </summary>
    public int? DelayMs { get; set; }
}

public class FullTriggerRequest
{
    /// <summary>
    /// Rover name (perseverance or curiosity)
    /// </summary>
    public string Rover { get; set; } = string.Empty;

    /// <summary>
    /// Must be true to confirm full re-scrape
    /// </summary>
    public bool Confirm { get; set; }
}
