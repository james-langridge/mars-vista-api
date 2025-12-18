using System.Text.Json;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Options;
using MarsVista.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarsVista.Scraper.Services;

public interface IIncrementalScraperService
{
    /// <summary>
    /// Run incremental scrape for all active rovers
    /// </summary>
    Task<IncrementalScrapeResult> ScrapeAllRoversAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run incremental scrape for a specific rover
    /// </summary>
    Task<RoverScrapeResult> ScrapeRoverAsync(string roverName, CancellationToken cancellationToken = default);
}

public class IncrementalScraperService : IIncrementalScraperService
{
    private const int MaxCurrentSolRetries = 3;
    private const int StuckJobThresholdHours = 1;

    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly IScraperStateRepository _stateRepository;
    private readonly MarsVistaDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ScraperScheduleOptions> _scraperOptions;
    private readonly ILogger<IncrementalScraperService> _logger;

    public IncrementalScraperService(
        IEnumerable<IScraperService> scrapers,
        IScraperStateRepository stateRepository,
        MarsVistaDbContext context,
        IHttpClientFactory httpClientFactory,
        IOptions<ScraperScheduleOptions> scraperOptions,
        ILogger<IncrementalScraperService> logger)
    {
        _scrapers = scrapers;
        _stateRepository = stateRepository;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _scraperOptions = scraperOptions;
        _logger = logger;
    }

    public async Task<IncrementalScrapeResult> ScrapeAllRoversAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting incremental scrape for all active rovers");

        // Clean up stuck jobs before starting
        await CleanupStuckJobsAsync(cancellationToken);

        var results = new List<RoverScrapeResult>();
        var overallStartTime = DateTime.UtcNow;

        // Create job history record
        var jobHistory = new ScraperJobHistory
        {
            JobStartedAt = overallStartTime,
            Status = "in_progress",
            CreatedAt = overallStartTime
        };
        _context.ScraperJobHistories.Add(jobHistory);
        await _context.SaveChangesAsync(cancellationToken);

        // Get active rovers from configuration (default to Perseverance and Curiosity)
        var activeRovers = _scraperOptions.Value.ActiveRovers ?? new List<string>();
        if (activeRovers.Count == 0)
        {
            activeRovers = new List<string> { "perseverance", "curiosity" }; // Default to active rovers
        }

        _logger.LogInformation("Active rovers: {Rovers}", string.Join(", ", activeRovers));

        var roverDetails = new List<ScraperJobRoverDetails>();

        foreach (var roverName in activeRovers)
        {
            try
            {
                var roverStartTime = DateTime.UtcNow;
                var result = await ScrapeRoverAsync(roverName, cancellationToken);
                results.Add(result);

                var roverDetail = new ScraperJobRoverDetails
                {
                    JobHistoryId = jobHistory.Id,
                    RoverName = roverName,
                    StartSol = result.StartSol,
                    EndSol = result.EndSol,
                    SolsAttempted = result.SolsAttempted,
                    SolsSucceeded = result.SolsSucceeded,
                    SolsFailed = result.SolsFailed,
                    PhotosAdded = result.PhotosAdded,
                    DurationSeconds = (int)(DateTime.UtcNow - roverStartTime).TotalSeconds,
                    Status = result.SolsFailed > 0 ? "partial" : (result.Success ? "success" : "failed"),
                    ErrorMessage = result.ErrorMessage,
                    FailedSols = result.GetFailedSolsJson(),
                    CreatedAt = DateTime.UtcNow
                };
                roverDetails.Add(roverDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping rover {Rover}", roverName);
                results.Add(new RoverScrapeResult
                {
                    RoverName = roverName,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                var roverDetail = new ScraperJobRoverDetails
                {
                    JobHistoryId = jobHistory.Id,
                    RoverName = roverName,
                    Status = "failed",
                    ErrorMessage = ex.Message,
                    CreatedAt = DateTime.UtcNow
                };
                roverDetails.Add(roverDetail);
            }
        }

        // Update job history with final stats
        var completedAt = DateTime.UtcNow;
        jobHistory.JobCompletedAt = completedAt;
        jobHistory.TotalDurationSeconds = (int)(completedAt - overallStartTime).TotalSeconds;
        jobHistory.TotalRoversAttempted = results.Count;
        jobHistory.TotalRoversSucceeded = results.Count(r => r.Success);
        jobHistory.TotalPhotosAdded = results.Sum(r => r.PhotosAdded);
        jobHistory.Status = results.All(r => r.Success) ? "success" :
                           results.Any(r => r.Success) ? "partial" : "failed";

        // Add rover details
        foreach (var detail in roverDetails)
        {
            _context.ScraperJobRoverDetails.Add(detail);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var totalPhotos = results.Sum(r => r.PhotosAdded);
        _logger.LogInformation(
            "Incremental scrape complete. Total photos added: {Total}, Duration: {Duration}s",
            totalPhotos, jobHistory.TotalDurationSeconds);

        return new IncrementalScrapeResult
        {
            TotalPhotosAdded = totalPhotos,
            RoverResults = results,
            Success = results.All(r => r.Success),
            DurationSeconds = jobHistory.TotalDurationSeconds ?? 0
        };
    }

    public async Task<RoverScrapeResult> ScrapeRoverAsync(string roverName, CancellationToken cancellationToken = default)
    {
        var roverNameLower = roverName.ToLower();
        _logger.LogInformation("Starting incremental scrape for {Rover}", roverName);

        var result = new RoverScrapeResult { RoverName = roverName };

        try
        {
            // Find the scraper for this rover
            var scraper = _scrapers.FirstOrDefault(s =>
                s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

            if (scraper == null)
            {
                _logger.LogWarning("No scraper found for rover {Rover}", roverName);
                result.Success = false;
                result.ErrorMessage = $"No scraper found for rover {roverName}";
                return result;
            }

            // Get current mission sol with retry and fallback logic
            var (currentSol, usedFallback) = await GetCurrentSolWithFallbackAsync(roverNameLower, cancellationToken);
            if (!currentSol.HasValue)
            {
                _logger.LogError("Could not determine current sol for {Rover} (both NASA API and database fallback failed)", roverName);
                result.Success = false;
                result.ErrorMessage = "Could not determine current mission sol (NASA API unavailable and no photos in database)";
                return result;
            }

            if (usedFallback)
            {
                _logger.LogWarning("Using database fallback for {Rover} current sol: {Sol} (NASA API unavailable)", roverName, currentSol.Value);
            }

            // Get or create scraper state
            var state = await _stateRepository.GetByRoverNameAsync(roverNameLower);
            if (state == null)
            {
                state = new ScraperState
                {
                    RoverName = roverNameLower,
                    LastScrapedSol = 0,
                    LastScrapeTimestamp = DateTime.UtcNow,
                    LastScrapeStatus = "pending"
                };
                state = await _stateRepository.CreateAsync(state);
            }

            // Calculate sol range to scrape (lookback window)
            var lookbackSols = _scraperOptions.Value.LookbackSols;
            var startSol = Math.Max(1, currentSol.Value - lookbackSols);
            var endSol = currentSol.Value;

            result.StartSol = startSol;
            result.EndSol = endSol;
            result.SolsAttempted = endSol - startSol + 1;

            _logger.LogInformation(
                "Scraping {Rover} sols {Start} to {End} (current mission sol: {Current})",
                roverName, startSol, endSol, currentSol.Value);

            // Update state to in_progress
            state.LastScrapeStatus = "in_progress";
            state.LastScrapeTimestamp = DateTime.UtcNow;
            await _stateRepository.UpdateAsync(state);

            var totalPhotos = 0;
            var solsSucceeded = 0;
            var solsFailed = 0;
            var failedSolDetails = new List<FailedSolInfo>();

            // Scrape each sol in the range
            for (var sol = startSol; sol <= endSol; sol++)
            {
                try
                {
                    var photosAdded = await scraper.ScrapeSolAsync(sol, cancellationToken);
                    totalPhotos += photosAdded;
                    solsSucceeded++;

                    _logger.LogInformation(
                        "Sol {Sol}: SUCCESS ({Count} photos)",
                        sol, photosAdded);
                }
                catch (Exception ex)
                {
                    var failedSolInfo = FailedSolInfo.FromException(sol, ex);
                    failedSolDetails.Add(failedSolInfo);
                    solsFailed++;

                    _logger.LogWarning(
                        "Sol {Sol}: FAILED - {ErrorType}: {ErrorMessage}",
                        sol, failedSolInfo.ErrorType, failedSolInfo.ErrorMessage);

                    // Log full exception at debug level for troubleshooting
                    _logger.LogDebug(ex, "Sol {Sol} exception details", sol);
                }
            }

            // Retry failed sols up to 3 more times with longer exponential backoff
            // NASA's Perseverance API is slow (~20-30s per sol) and occasionally times out
            const int maxRetries = 3;
            for (var retry = 1; retry <= maxRetries && failedSolDetails.Count > 0; retry++)
            {
                var solsToRetry = failedSolDetails.Select(f => f.Sol).ToList();
                _logger.LogInformation(
                    "Retry {Retry}/{Max}: Retrying {Count} failed sols for {Rover}",
                    retry, maxRetries, solsToRetry.Count, roverName);

                // Wait before retry (exponential backoff: 30s, 60s, 120s)
                var delaySeconds = 30 * (int)Math.Pow(2, retry - 1);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);

                foreach (var sol in solsToRetry)
                {
                    try
                    {
                        var photosAdded = await scraper.ScrapeSolAsync(sol, cancellationToken);
                        totalPhotos += photosAdded;
                        solsSucceeded++;
                        solsFailed--;

                        // Remove from failed list on success
                        failedSolDetails.RemoveAll(f => f.Sol == sol);

                        _logger.LogInformation(
                            "Sol {Sol}: RETRY SUCCESS ({Count} photos)",
                            sol, photosAdded);
                    }
                    catch (Exception ex)
                    {
                        // Update the failed sol info with latest error
                        var existingFailure = failedSolDetails.FirstOrDefault(f => f.Sol == sol);
                        if (existingFailure != null)
                        {
                            var newInfo = FailedSolInfo.FromException(sol, ex);
                            existingFailure.ErrorType = newInfo.ErrorType;
                            existingFailure.ErrorMessage = newInfo.ErrorMessage;
                        }

                        _logger.LogWarning(
                            "Sol {Sol}: RETRY {Retry} FAILED - {ErrorType}",
                            sol, retry, ex.GetType().Name);
                    }
                }
            }

            // Update state with results
            state.LastScrapedSol = endSol;
            state.LastScrapeTimestamp = DateTime.UtcNow;
            state.LastScrapeStatus = solsFailed == 0 ? "success" : "partial";
            state.PhotosAddedLastRun = totalPhotos;
            await _stateRepository.UpdateAsync(state);

            result.PhotosAdded = totalPhotos;
            result.SolsSucceeded = solsSucceeded;
            result.SolsFailed = solsFailed;
            result.FailedSolDetails = failedSolDetails;
            // Rover is successful if it completed (even with some sol failures)
            // Only mark as failed if the rover failed completely (e.g., couldn't determine sol)
            result.Success = true;

            if (solsFailed > 0)
            {
                // Build summary of failed sols for logging
                var failedSolsSummary = string.Join(", ",
                    failedSolDetails.Select(f => $"{f.Sol} ({f.ErrorType})"));

                _logger.LogWarning(
                    "Completed {Rover} with partial success: {Photos} photos added, {Succeeded}/{Total} sols succeeded, {Failed} sols failed. Failed sols: {FailedSols}",
                    roverName, totalPhotos, solsSucceeded, result.SolsAttempted, solsFailed, failedSolsSummary);
            }
            else
            {
                _logger.LogInformation(
                    "Completed {Rover}: {Photos} photos added, {Succeeded}/{Total} sols succeeded",
                    roverName, totalPhotos, solsSucceeded, result.SolsAttempted);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScrapeRoverAsync for {Rover}", roverName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Get current mission sol from NASA API
    /// </summary>
    private async Task<int?> GetCurrentMissionSolAsync(string roverName, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("NASA");

            // Different API endpoints for different rovers
            string url;
            if (roverName == "perseverance")
            {
                url = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&num=1";
            }
            else if (roverName == "curiosity")
            {
                url = "https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc&per_page=1&condition_1=msl:mission";
            }
            else
            {
                // Spirit and Opportunity are completed missions, return null
                return null;
            }

            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (roverName == "perseverance")
            {
                if (root.TryGetProperty("images", out var images) &&
                    images.GetArrayLength() > 0)
                {
                    var firstImage = images[0];
                    if (firstImage.TryGetProperty("sol", out var sol))
                    {
                        return sol.GetInt32();
                    }
                }
            }
            else if (roverName == "curiosity")
            {
                if (root.TryGetProperty("items", out var items) &&
                    items.GetArrayLength() > 0)
                {
                    var firstItem = items[0];
                    if (firstItem.TryGetProperty("sol", out var sol))
                    {
                        return sol.GetInt32();
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current sol for {Rover}", roverName);
            return null;
        }
    }

    /// <summary>
    /// Get current mission sol with retry logic and fallback to database max sol.
    /// </summary>
    /// <remarks>
    /// Limitation: When using database fallback, only max_sol + 1 is returned as the
    /// assumed current sol. If NASA API is unavailable for multiple days, the mission
    /// may have advanced several sols, but this method will only return one sol ahead.
    /// Combined with the lookback window, this means some recent sols may be missed
    /// until NASA API recovers. This is acceptable because:
    /// 1. The lookback window ensures we re-scrape recent sols (catching late arrivals)
    /// 2. Once NASA API recovers, the correct current sol will trigger full scraping
    /// 3. Idempotent scraping means no data is lost, just potentially delayed
    /// </remarks>
    private async Task<(int? Sol, bool UsedFallback)> GetCurrentSolWithFallbackAsync(
        string roverName,
        CancellationToken cancellationToken)
    {
        // Try NASA API with retry logic (3 attempts with exponential backoff)
        for (var attempt = 1; attempt <= MaxCurrentSolRetries; attempt++)
        {
            var sol = await GetCurrentMissionSolAsync(roverName, cancellationToken);
            if (sol.HasValue)
            {
                return (sol.Value, false);
            }

            if (attempt < MaxCurrentSolRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(
                    "NASA API failed for {Rover} current sol (attempt {Attempt}/{Max}). Retrying in {Delay}s...",
                    roverName, attempt, MaxCurrentSolRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogWarning(
            "NASA API failed for {Rover} after {Max} attempts. Falling back to database max sol",
            roverName, MaxCurrentSolRetries);

        // Fallback: use database max sol + 1
        var fallbackSol = await GetDatabaseMaxSolAsync(roverName, cancellationToken);
        if (fallbackSol.HasValue)
        {
            // Use max sol + 1 as the assumed current sol (photos may arrive for current sol)
            return (fallbackSol.Value + 1, true);
        }

        return (null, false);
    }

    /// <summary>
    /// Get the maximum sol from the database for a rover
    /// </summary>
    private async Task<int?> GetDatabaseMaxSolAsync(string roverName, CancellationToken cancellationToken)
    {
        var rover = await _context.Rovers
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName, cancellationToken);

        if (rover == null)
        {
            _logger.LogWarning("Rover {Rover} not found in database for fallback sol lookup", roverName);
            return null;
        }

        var maxSol = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .MaxAsync(p => (int?)p.Sol, cancellationToken);

        if (!maxSol.HasValue)
        {
            _logger.LogWarning("No photos found for {Rover} in database for fallback sol lookup", roverName);
            return null;
        }

        _logger.LogInformation("Database max sol for {Rover}: {MaxSol}", roverName, maxSol.Value);
        return maxSol.Value;
    }

    /// <summary>
    /// Clean up stuck jobs that have been in_progress for more than the threshold
    /// </summary>
    private async Task CleanupStuckJobsAsync(CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow.AddHours(-StuckJobThresholdHours);

        var stuckJobs = await _context.ScraperJobHistories
            .Where(j => j.Status == "in_progress" && j.JobStartedAt < threshold)
            .ToListAsync(cancellationToken);

        if (stuckJobs.Count == 0)
        {
            return;
        }

        _logger.LogWarning(
            "Found {Count} stuck job(s) older than {Hours} hour(s). Marking as failed",
            stuckJobs.Count, StuckJobThresholdHours);

        foreach (var job in stuckJobs)
        {
            job.Status = "failed";
            job.JobCompletedAt = DateTime.UtcNow;
            job.ErrorSummary = $"Job was stuck in in_progress status for more than {StuckJobThresholdHours} hour(s). Cleaned up on startup.";

            _logger.LogWarning(
                "Marked stuck job {JobId} (started at {StartedAt}) as failed",
                job.Id, job.JobStartedAt);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class IncrementalScrapeResult
{
    public int TotalPhotosAdded { get; set; }
    public List<RoverScrapeResult> RoverResults { get; set; } = new();
    public bool Success { get; set; }
    public int DurationSeconds { get; set; }
}

public class RoverScrapeResult
{
    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public string RoverName { get; set; } = "";
    public int PhotosAdded { get; set; }
    public int StartSol { get; set; }
    public int EndSol { get; set; }
    public int SolsAttempted { get; set; }
    public int SolsSucceeded { get; set; }
    public int SolsFailed { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<FailedSolInfo> FailedSolDetails { get; set; } = new();

    /// <summary>
    /// Serialize failed sol details to JSON for database storage
    /// </summary>
    public string? GetFailedSolsJson()
    {
        if (FailedSolDetails.Count == 0)
            return null;

        return JsonSerializer.Serialize(FailedSolDetails, SnakeCaseOptions);
    }
}

/// <summary>
/// Structured information about a failed sol scrape attempt
/// </summary>
public class FailedSolInfo
{
    public int Sol { get; set; }
    public string ErrorType { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Classify an exception into an error type for structured logging
    /// </summary>
    public static FailedSolInfo FromException(int sol, Exception ex)
    {
        var errorType = ex switch
        {
            HttpRequestException httpEx => ClassifyHttpError(httpEx),
            TaskCanceledException => "Timeout",
            JsonException => "ParseError",
            OperationCanceledException => "Cancelled",
            _ => "Unknown"
        };

        return new FailedSolInfo
        {
            Sol = sol,
            ErrorType = errorType,
            ErrorMessage = GetConciseErrorMessage(ex),
            Timestamp = DateTime.UtcNow
        };
    }

    private static string ClassifyHttpError(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue)
        {
            // Named codes for common scraper errors improve log readability
            // All other status codes fall through to HTTP_{code}
            return ex.StatusCode.Value switch
            {
                System.Net.HttpStatusCode.ServiceUnavailable => "HTTP_503",
                System.Net.HttpStatusCode.TooManyRequests => "HTTP_429",
                _ => $"HTTP_{(int)ex.StatusCode.Value}"
            };
        }

        // Network-level error without HTTP status
        return "NetworkError";
    }

    private static string GetConciseErrorMessage(Exception ex)
    {
        // Get the most relevant part of the error message (first 200 chars)
        var message = ex.Message;
        if (message.Length > 200)
            message = message[..200] + "...";
        return message;
    }
}
