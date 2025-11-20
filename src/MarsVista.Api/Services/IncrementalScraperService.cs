using System.Text.Json;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using MarsVista.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public interface IIncrementalScraperService
{
    Task<IncrementalScrapeResult> ScrapeIncrementalAsync(
        string roverName,
        int lookbackSols = 7,
        CancellationToken cancellationToken = default);

    Task ResetStateAsync(string roverName, int sol);
}

public class IncrementalScrapeResult
{
    public string RoverName { get; set; } = string.Empty;
    public int StartSol { get; set; }
    public int EndSol { get; set; }
    public int TotalSols { get; set; }
    public int PhotosAdded { get; set; }
    public int SuccessfulSols { get; set; }
    public int SkippedSols { get; set; }
    public List<int> FailedSols { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class IncrementalScraperService : IIncrementalScraperService
{
    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly IScraperStateRepository _stateRepository;
    private readonly MarsVistaDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IncrementalScraperService> _logger;

    public IncrementalScraperService(
        IEnumerable<IScraperService> scrapers,
        IScraperStateRepository stateRepository,
        MarsVistaDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<IncrementalScraperService> logger)
    {
        _scrapers = scrapers;
        _stateRepository = stateRepository;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IncrementalScrapeResult> ScrapeIncrementalAsync(
        string roverName,
        int lookbackSols = 7,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new IncrementalScrapeResult { RoverName = roverName };

        try
        {
            // Get scraper for this rover
            var scraper = _scrapers.FirstOrDefault(s =>
                s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

            if (scraper == null)
            {
                result.Success = false;
                result.ErrorMessage = $"No scraper found for rover: {roverName}";
                return result;
            }

            // Get or create scraper state
            var state = await _stateRepository.GetByRoverNameAsync(roverName);
            if (state == null)
            {
                // Initialize state - will scrape from the beginning
                state = new ScraperState
                {
                    RoverName = roverName,
                    LastScrapedSol = 0,
                    LastScrapeTimestamp = DateTime.UtcNow,
                    LastScrapeStatus = "success",
                    PhotosAddedLastRun = 0
                };
                state = await _stateRepository.CreateAsync(state);
                _logger.LogInformation(
                    "Initialized scraper state for {RoverName} - will perform full scrape",
                    roverName);
            }

            // Mark scrape as in progress
            state.LastScrapeStatus = "in_progress";
            state.LastScrapeTimestamp = DateTime.UtcNow;
            state.ErrorMessage = null;
            await _stateRepository.UpdateAsync(state);

            // Calculate sol range
            var startSol = Math.Max(0, state.LastScrapedSol - lookbackSols);

            // Query NASA to get the actual current mission sol
            var currentMissionSol = await GetCurrentMissionSolFromNasaAsync(roverName, cancellationToken);
            var endSol = currentMissionSol;

            _logger.LogInformation(
                "Current mission sol for {RoverName}: {CurrentSol}",
                roverName, currentMissionSol);

            result.StartSol = startSol;
            result.EndSol = endSol;
            result.TotalSols = endSol - startSol + 1;

            _logger.LogInformation(
                "Starting incremental scrape for {RoverName}: sols {StartSol}-{EndSol} ({TotalSols} sols, lookback: {Lookback})",
                roverName, startSol, endSol, result.TotalSols, lookbackSols);

            // Scrape each sol
            var totalPhotos = 0;
            for (var sol = startSol; sol <= endSol && !cancellationToken.IsCancellationRequested; sol++)
            {
                try
                {
                    var count = await scraper.ScrapeSolAsync(sol, cancellationToken);
                    totalPhotos += count;

                    if (count > 0)
                    {
                        result.SuccessfulSols++;
                        _logger.LogInformation(
                            "{RoverName} sol {Sol}: {Count} photos added",
                            roverName, sol, count);
                    }
                    else
                    {
                        result.SkippedSols++;
                    }

                    // Small delay to be nice to NASA's servers
                    if (sol < endSol)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    result.FailedSols.Add(sol);
                    _logger.LogError(ex, "Failed to scrape {RoverName} sol {Sol}", roverName, sol);
                    // Continue with next sol instead of stopping
                }
            }

            result.PhotosAdded = totalPhotos;
            result.Duration = DateTime.UtcNow - startTime;
            result.Success = !cancellationToken.IsCancellationRequested;

            // Update state
            state.LastScrapedSol = endSol;
            state.LastScrapeTimestamp = DateTime.UtcNow;
            state.LastScrapeStatus = result.Success ? "success" : "failed";
            state.PhotosAddedLastRun = totalPhotos;
            state.ErrorMessage = result.FailedSols.Count > 0
                ? $"Failed to scrape {result.FailedSols.Count} sols: {string.Join(", ", result.FailedSols)}"
                : null;

            await _stateRepository.UpdateAsync(state);

            _logger.LogInformation(
                "Incremental scrape completed for {RoverName}: {Photos} photos added in {Duration}s",
                roverName, totalPhotos, (int)result.Duration.TotalSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental scrape failed for {RoverName}", roverName);

            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.Duration = DateTime.UtcNow - startTime;

            // Update state to failed
            var state = await _stateRepository.GetByRoverNameAsync(roverName);
            if (state != null)
            {
                state.LastScrapeStatus = "failed";
                state.LastScrapeTimestamp = DateTime.UtcNow;
                state.ErrorMessage = ex.Message;
                await _stateRepository.UpdateAsync(state);
            }

            return result;
        }
    }

    public async Task ResetStateAsync(string roverName, int sol)
    {
        var state = await _stateRepository.GetByRoverNameAsync(roverName);
        if (state == null)
        {
            state = new ScraperState
            {
                RoverName = roverName,
                LastScrapedSol = sol,
                LastScrapeTimestamp = DateTime.UtcNow,
                LastScrapeStatus = "success",
                PhotosAddedLastRun = 0
            };
            await _stateRepository.CreateAsync(state);
        }
        else
        {
            state.LastScrapedSol = sol;
            state.LastScrapeTimestamp = DateTime.UtcNow;
            state.LastScrapeStatus = "success";
            state.PhotosAddedLastRun = 0;
            state.ErrorMessage = null;
            await _stateRepository.UpdateAsync(state);
        }

        _logger.LogInformation(
            "Reset scraper state for {RoverName} to sol {Sol}",
            roverName, sol);
    }

    private async Task<int> GetCurrentMissionSolFromNasaAsync(string roverName, CancellationToken cancellationToken = default)
    {
        var roverLower = roverName.ToLower();

        // For inactive rovers (missions complete), use hardcoded final sol values
        if (roverLower != "curiosity" && roverLower != "perseverance")
        {
            return GetInactiveRoverMaxSol(roverName);
        }

        // For active rovers, query NASA API - fail if unavailable
        var httpClient = _httpClientFactory.CreateClient("NASA");
        string url;

        if (roverLower == "perseverance")
        {
            // Perseverance uses /rss/api/ endpoint with latest=true
            url = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true";

            _logger.LogDebug("Querying NASA API for current mission sol: {Url}", url);

            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("latest_sol", out var latestSolElement))
            {
                var sol = latestSolElement.GetInt32();
                _logger.LogInformation(
                    "Successfully retrieved current mission sol for {RoverName} from NASA: {Sol}",
                    roverName, sol);
                return sol;
            }

            throw new InvalidOperationException(
                $"NASA API response missing 'latest_sol' property for {roverName}");
        }
        else // Curiosity
        {
            // Curiosity uses /api/v1/raw_image_items/ endpoint
            url = "https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc&per_page=1&condition_1=msl:mission";

            _logger.LogDebug("Querying NASA API for current mission sol: {Url}", url);

            var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonDoc = JsonDocument.Parse(content);

            if (jsonDoc.RootElement.TryGetProperty("items", out var items) &&
                items.GetArrayLength() > 0)
            {
                var firstItem = items[0];
                if (firstItem.TryGetProperty("sol", out var solElement))
                {
                    var sol = solElement.GetInt32();
                    _logger.LogInformation(
                        "Successfully retrieved current mission sol for {RoverName} from NASA: {Sol}",
                        roverName, sol);
                    return sol;
                }
            }

            throw new InvalidOperationException(
                $"NASA API response missing expected 'items' or 'sol' property for {roverName}");
        }
    }

    private int GetInactiveRoverMaxSol(string roverName)
    {
        // Final sol values for inactive rovers (missions complete)
        // For active rovers (Curiosity, Perseverance), query NASA API instead
        return roverName.ToLower() switch
        {
            "opportunity" => 5111,  // Mission ended sol 5111
            "spirit" => 2208,       // Mission ended sol 2208
            _ => throw new ArgumentException($"Unknown inactive rover: {roverName}")
        };
    }
}
