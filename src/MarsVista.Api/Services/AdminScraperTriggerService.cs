using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Options;
using MarsVista.Core.Repositories;
using MarsVista.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MarsVista.Api.Services;

/// <summary>
/// Service that coordinates scraper triggers with job tracking.
/// Executes scraper operations as background tasks with progress reporting.
/// </summary>
public interface IAdminScraperTriggerService
{
    /// <summary>
    /// Trigger incremental scrape for all active rovers (like daily cron)
    /// </summary>
    Task<ScraperJob> TriggerIncrementalAsync(int? lookbackSols = null);

    /// <summary>
    /// Trigger scrape for a specific sol
    /// </summary>
    Task<ScraperJob> TriggerSolAsync(string rover, int sol);

    /// <summary>
    /// Trigger scrape for a sol range
    /// </summary>
    Task<ScraperJob> TriggerRangeAsync(string rover, int startSol, int endSol, int delayMs = 500);

    /// <summary>
    /// Trigger full rover re-scrape (dangerous)
    /// </summary>
    Task<ScraperJob> TriggerFullAsync(string rover);

    /// <summary>
    /// Get current mission sol for a rover from NASA API
    /// </summary>
    Task<int?> GetCurrentMissionSolAsync(string rover);
}

public class AdminScraperTriggerService : IAdminScraperTriggerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IScraperJobTracker _jobTracker;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ScraperScheduleOptions> _scraperOptions;
    private readonly ILogger<AdminScraperTriggerService> _logger;

    public AdminScraperTriggerService(
        IServiceScopeFactory scopeFactory,
        IScraperJobTracker jobTracker,
        IHttpClientFactory httpClientFactory,
        IOptions<ScraperScheduleOptions> scraperOptions,
        ILogger<AdminScraperTriggerService> logger)
    {
        _scopeFactory = scopeFactory;
        _jobTracker = jobTracker;
        _httpClientFactory = httpClientFactory;
        _scraperOptions = scraperOptions;
        _logger = logger;
    }

    public async Task<ScraperJob> TriggerIncrementalAsync(int? lookbackSols = null)
    {
        var effectiveLookback = lookbackSols ?? _scraperOptions.Value.LookbackSols;
        var activeRovers = _scraperOptions.Value.ActiveRovers ?? new List<string> { "perseverance", "curiosity" };

        var job = _jobTracker.StartJob(
            type: "incremental",
            rover: "all",
            startSol: null,
            endSol: null,
            lookbackSols: effectiveLookback);

        // Run in background
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scrapers = scope.ServiceProvider.GetServices<IScraperService>().ToList();
                var context = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
                var stateRepo = scope.ServiceProvider.GetRequiredService<IScraperStateRepository>();

                var totalPhotos = 0;
                var totalSolsCompleted = 0;
                var totalSolsFailed = 0;

                foreach (var roverName in activeRovers)
                {
                    if (_jobTracker.IsCancellationRequested(job.Id))
                    {
                        _jobTracker.CompleteJob(job.Id, totalPhotos, totalSolsCompleted, totalSolsFailed, "Cancelled by user");
                        return;
                    }

                    var scraper = scrapers.FirstOrDefault(s =>
                        s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

                    if (scraper == null) continue;

                    // Get current mission sol
                    var currentSol = await GetCurrentMissionSolAsync(roverName);
                    if (!currentSol.HasValue) continue;

                    var startSol = Math.Max(1, currentSol.Value - effectiveLookback);
                    var endSol = currentSol.Value;

                    for (var sol = startSol; sol <= endSol; sol++)
                    {
                        if (_jobTracker.IsCancellationRequested(job.Id)) break;

                        try
                        {
                            var added = await scraper.ScrapeSolAsync(sol);
                            totalPhotos += added;
                            totalSolsCompleted++;
                            _jobTracker.UpdateProgress(job.Id, sol, totalPhotos, totalSolsCompleted, totalSolsFailed);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error scraping {Rover} sol {Sol}", roverName, sol);
                            totalSolsFailed++;
                        }
                    }
                }

                _jobTracker.CompleteJob(job.Id, totalPhotos, totalSolsCompleted, totalSolsFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Incremental scrape job {JobId} failed", job.Id);
                _jobTracker.FailJob(job.Id, ex.Message);
            }
        });

        return job;
    }

    public async Task<ScraperJob> TriggerSolAsync(string rover, int sol)
    {
        var job = _jobTracker.StartJob(
            type: "sol",
            rover: rover.ToLower(),
            startSol: sol,
            endSol: sol);

        job.TotalSols = 1;

        // Run in background
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scrapers = scope.ServiceProvider.GetServices<IScraperService>().ToList();

                var scraper = scrapers.FirstOrDefault(s =>
                    s.RoverName.Equals(rover, StringComparison.OrdinalIgnoreCase));

                if (scraper == null)
                {
                    _jobTracker.FailJob(job.Id, $"No scraper found for rover {rover}");
                    return;
                }

                _jobTracker.UpdateProgress(job.Id, sol, 0, 0, 0);

                var photosAdded = await scraper.ScrapeSolAsync(sol);

                _jobTracker.CompleteJob(job.Id, photosAdded, 1, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sol scrape job {JobId} failed", job.Id);
                _jobTracker.FailJob(job.Id, ex.Message);
            }
        });

        return await Task.FromResult(job);
    }

    public async Task<ScraperJob> TriggerRangeAsync(string rover, int startSol, int endSol, int delayMs = 500)
    {
        var job = _jobTracker.StartJob(
            type: "range",
            rover: rover.ToLower(),
            startSol: startSol,
            endSol: endSol);

        // Run in background
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scrapers = scope.ServiceProvider.GetServices<IScraperService>().ToList();

                var scraper = scrapers.FirstOrDefault(s =>
                    s.RoverName.Equals(rover, StringComparison.OrdinalIgnoreCase));

                if (scraper == null)
                {
                    _jobTracker.FailJob(job.Id, $"No scraper found for rover {rover}");
                    return;
                }

                var totalPhotos = 0;
                var solsCompleted = 0;
                var solsFailed = 0;

                for (var sol = startSol; sol <= endSol; sol++)
                {
                    if (_jobTracker.IsCancellationRequested(job.Id))
                    {
                        _jobTracker.CompleteJob(job.Id, totalPhotos, solsCompleted, solsFailed, "Cancelled by user");
                        return;
                    }

                    try
                    {
                        var added = await scraper.ScrapeSolAsync(sol);
                        totalPhotos += added;
                        solsCompleted++;
                        _jobTracker.UpdateProgress(job.Id, sol, totalPhotos, solsCompleted, solsFailed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scraping sol {Sol}", sol);
                        solsFailed++;
                    }

                    // Delay between sols to avoid overwhelming the API
                    if (sol < endSol && delayMs > 0)
                    {
                        await Task.Delay(delayMs);
                    }
                }

                _jobTracker.CompleteJob(job.Id, totalPhotos, solsCompleted, solsFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Range scrape job {JobId} failed", job.Id);
                _jobTracker.FailJob(job.Id, ex.Message);
            }
        });

        return await Task.FromResult(job);
    }

    public async Task<ScraperJob> TriggerFullAsync(string rover)
    {
        // Get current mission sol to determine end
        var currentSol = await GetCurrentMissionSolAsync(rover);
        if (!currentSol.HasValue)
        {
            throw new InvalidOperationException($"Could not determine current sol for {rover}");
        }

        var job = _jobTracker.StartJob(
            type: "full",
            rover: rover.ToLower(),
            startSol: 1,
            endSol: currentSol.Value);

        // Run in background (same as range but from sol 1)
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scrapers = scope.ServiceProvider.GetServices<IScraperService>().ToList();

                var scraper = scrapers.FirstOrDefault(s =>
                    s.RoverName.Equals(rover, StringComparison.OrdinalIgnoreCase));

                if (scraper == null)
                {
                    _jobTracker.FailJob(job.Id, $"No scraper found for rover {rover}");
                    return;
                }

                var totalPhotos = 0;
                var solsCompleted = 0;
                var solsFailed = 0;
                var delayMs = 200; // Shorter delay for full scrape since it's long anyway

                for (var sol = 1; sol <= currentSol.Value; sol++)
                {
                    if (_jobTracker.IsCancellationRequested(job.Id))
                    {
                        _jobTracker.CompleteJob(job.Id, totalPhotos, solsCompleted, solsFailed, "Cancelled by user");
                        return;
                    }

                    try
                    {
                        var added = await scraper.ScrapeSolAsync(sol);
                        totalPhotos += added;
                        solsCompleted++;
                        _jobTracker.UpdateProgress(job.Id, sol, totalPhotos, solsCompleted, solsFailed);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scraping sol {Sol}", sol);
                        solsFailed++;
                    }

                    // Delay between sols
                    if (sol < currentSol.Value)
                    {
                        await Task.Delay(delayMs);
                    }
                }

                _jobTracker.CompleteJob(job.Id, totalPhotos, solsCompleted, solsFailed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full scrape job {JobId} failed", job.Id);
                _jobTracker.FailJob(job.Id, ex.Message);
            }
        });

        return job;
    }

    public async Task<int?> GetCurrentMissionSolAsync(string roverName)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("NASA");
            var roverLower = roverName.ToLower();

            string url;
            if (roverLower == "perseverance")
            {
                url = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&num=1";
            }
            else if (roverLower == "curiosity")
            {
                url = "https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc&per_page=1&condition_1=msl:mission";
            }
            else
            {
                // Spirit and Opportunity are completed missions
                return null;
            }

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (roverLower == "perseverance")
            {
                if (root.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
                {
                    if (images[0].TryGetProperty("sol", out var sol))
                    {
                        return sol.GetInt32();
                    }
                }
            }
            else if (roverLower == "curiosity")
            {
                if (root.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                {
                    if (items[0].TryGetProperty("sol", out var sol))
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
