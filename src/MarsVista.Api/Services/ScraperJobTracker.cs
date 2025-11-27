using System.Collections.Concurrent;

namespace MarsVista.Api.Services;

/// <summary>
/// Tracks the state of manually triggered scraper jobs.
/// Singleton service - job state persists for the lifetime of the application.
/// </summary>
public interface IScraperJobTracker
{
    /// <summary>
    /// Start a new scraper job
    /// </summary>
    ScraperJob StartJob(string type, string rover, int? startSol, int? endSol, int? lookbackSols = null);

    /// <summary>
    /// Get a job by ID
    /// </summary>
    ScraperJob? GetJob(string jobId);

    /// <summary>
    /// Get all jobs (most recent first)
    /// </summary>
    IReadOnlyList<ScraperJob> GetAllJobs();

    /// <summary>
    /// Get active (running) jobs
    /// </summary>
    IReadOnlyList<ScraperJob> GetActiveJobs();

    /// <summary>
    /// Update job progress
    /// </summary>
    void UpdateProgress(string jobId, int currentSol, int photosAdded, int solsCompleted, int solsFailed);

    /// <summary>
    /// Mark job as completed
    /// </summary>
    void CompleteJob(string jobId, int totalPhotos, int solsSucceeded, int solsFailed, string? errorMessage = null);

    /// <summary>
    /// Mark job as failed
    /// </summary>
    void FailJob(string jobId, string errorMessage);

    /// <summary>
    /// Request job cancellation (sets flag, caller must check and stop work)
    /// </summary>
    bool RequestCancel(string jobId);

    /// <summary>
    /// Check if job cancellation was requested
    /// </summary>
    bool IsCancellationRequested(string jobId);

    /// <summary>
    /// Clean up old completed jobs (keeps last N jobs)
    /// </summary>
    void CleanupOldJobs(int keepCount = 100);
}

/// <summary>
/// Represents a scraper job with its current state
/// </summary>
public class ScraperJob
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "incremental", "sol", "range", "full"
    public string Rover { get; set; } = string.Empty;
    public int? StartSol { get; set; }
    public int? EndSol { get; set; }
    public int? LookbackSols { get; set; }
    public string Status { get; set; } = "started"; // "started", "in_progress", "completed", "failed", "cancelled"
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Progress tracking
    public int CurrentSol { get; set; }
    public int PhotosAdded { get; set; }
    public int SolsCompleted { get; set; }
    public int SolsFailed { get; set; }
    public int TotalSols { get; set; }
    public double ProgressPercent => TotalSols > 0 ? Math.Round((double)SolsCompleted / TotalSols * 100, 1) : 0;

    // Timing
    public int ElapsedSeconds => (int)(DateTime.UtcNow - StartedAt).TotalSeconds;
    public int? EstimatedSecondsRemaining
    {
        get
        {
            if (SolsCompleted <= 0 || TotalSols <= 0) return null;
            var remaining = TotalSols - SolsCompleted;
            var avgSecondsPerSol = ElapsedSeconds / (double)SolsCompleted;
            return (int)(remaining * avgSecondsPerSol);
        }
    }

    // Result
    public string? ErrorMessage { get; set; }

    // Cancellation
    internal bool CancellationRequested { get; set; }
}

public class ScraperJobTracker : IScraperJobTracker
{
    private readonly ConcurrentDictionary<string, ScraperJob> _jobs = new();
    private readonly ILogger<ScraperJobTracker> _logger;

    public ScraperJobTracker(ILogger<ScraperJobTracker> logger)
    {
        _logger = logger;
    }

    public ScraperJob StartJob(string type, string rover, int? startSol, int? endSol, int? lookbackSols = null)
    {
        var jobId = Guid.NewGuid().ToString("N")[..12];
        var job = new ScraperJob
        {
            Id = jobId,
            Type = type,
            Rover = rover,
            StartSol = startSol,
            EndSol = endSol,
            LookbackSols = lookbackSols,
            Status = "started",
            StartedAt = DateTime.UtcNow,
            TotalSols = startSol.HasValue && endSol.HasValue ? endSol.Value - startSol.Value + 1 : 0
        };

        _jobs.TryAdd(jobId, job);
        _logger.LogInformation("Started scraper job {JobId}: {Type} for {Rover}", jobId, type, rover);

        return job;
    }

    public ScraperJob? GetJob(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) ? job : null;
    }

    public IReadOnlyList<ScraperJob> GetAllJobs()
    {
        return _jobs.Values.OrderByDescending(j => j.StartedAt).ToList();
    }

    public IReadOnlyList<ScraperJob> GetActiveJobs()
    {
        return _jobs.Values
            .Where(j => j.Status is "started" or "in_progress")
            .OrderByDescending(j => j.StartedAt)
            .ToList();
    }

    public void UpdateProgress(string jobId, int currentSol, int photosAdded, int solsCompleted, int solsFailed)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = "in_progress";
            job.CurrentSol = currentSol;
            job.PhotosAdded = photosAdded;
            job.SolsCompleted = solsCompleted;
            job.SolsFailed = solsFailed;
        }
    }

    public void CompleteJob(string jobId, int totalPhotos, int solsSucceeded, int solsFailed, string? errorMessage = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = solsFailed > 0 ? "partial" : "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.PhotosAdded = totalPhotos;
            job.SolsCompleted = solsSucceeded;
            job.SolsFailed = solsFailed;
            job.ErrorMessage = errorMessage;

            _logger.LogInformation(
                "Completed scraper job {JobId}: {Status}, {Photos} photos, {Sols} sols",
                jobId, job.Status, totalPhotos, solsSucceeded);
        }
    }

    public void FailJob(string jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = "failed";
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = errorMessage;

            _logger.LogError("Scraper job {JobId} failed: {Error}", jobId, errorMessage);
        }
    }

    public bool RequestCancel(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            if (job.Status is "started" or "in_progress")
            {
                job.CancellationRequested = true;
                _logger.LogInformation("Cancellation requested for job {JobId}", jobId);
                return true;
            }
        }
        return false;
    }

    public bool IsCancellationRequested(string jobId)
    {
        return _jobs.TryGetValue(jobId, out var job) && job.CancellationRequested;
    }

    public void CleanupOldJobs(int keepCount = 100)
    {
        var completed = _jobs.Values
            .Where(j => j.Status is "completed" or "failed" or "cancelled" or "partial")
            .OrderByDescending(j => j.CompletedAt)
            .Skip(keepCount)
            .ToList();

        foreach (var job in completed)
        {
            _jobs.TryRemove(job.Id, out _);
        }

        if (completed.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old scraper jobs", completed.Count);
        }
    }
}
