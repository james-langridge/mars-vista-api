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
                    Status = result.Success ? "success" : "failed",
                    ErrorMessage = result.ErrorMessage,
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

            // Get current mission sol from NASA API
            var currentSol = await GetCurrentMissionSolAsync(roverNameLower, cancellationToken);
            if (!currentSol.HasValue)
            {
                _logger.LogWarning("Could not determine current sol for {Rover}", roverName);
                result.Success = false;
                result.ErrorMessage = "Could not determine current mission sol";
                return result;
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

            // Scrape each sol in the range
            for (var sol = startSol; sol <= endSol; sol++)
            {
                try
                {
                    var photosAdded = await scraper.ScrapeSolAsync(sol, cancellationToken);
                    totalPhotos += photosAdded;
                    solsSucceeded++;

                    if (photosAdded > 0)
                    {
                        _logger.LogInformation("Sol {Sol}: {Count} photos added", sol, photosAdded);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scraping sol {Sol}", sol);
                    solsFailed++;
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
            result.Success = solsFailed == 0;

            _logger.LogInformation(
                "Completed {Rover}: {Photos} photos added, {Succeeded}/{Total} sols succeeded",
                roverName, totalPhotos, solsSucceeded, result.SolsAttempted);

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
    public string RoverName { get; set; } = "";
    public int PhotosAdded { get; set; }
    public int StartSol { get; set; }
    public int EndSol { get; set; }
    public int SolsAttempted { get; set; }
    public int SolsSucceeded { get; set; }
    public int SolsFailed { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
