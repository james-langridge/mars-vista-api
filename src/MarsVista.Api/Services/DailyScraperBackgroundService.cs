using MarsVista.Api.Options;
using Microsoft.Extensions.Options;

namespace MarsVista.Api.Services;

public class DailyScraperBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ScraperScheduleOptions _options;
    private readonly ILogger<DailyScraperBackgroundService> _logger;

    public DailyScraperBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<ScraperScheduleOptions> options,
        ILogger<DailyScraperBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Daily scraper background service is disabled in configuration");
            return;
        }

        _logger.LogInformation(
            "Daily scraper background service started. Running every {Hours} hours at {Hour}:00 UTC for rovers: {Rovers}",
            _options.IntervalHours, _options.RunAtUtcHour, string.Join(", ", _options.ActiveRovers));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var nextRunTime = CalculateNextRunTime();
                var delay = nextRunTime - DateTime.UtcNow;

                if (delay.TotalMilliseconds > 0)
                {
                    _logger.LogInformation(
                        "Next scrape scheduled for {NextRun} UTC (in {Hours}h {Minutes}m)",
                        nextRunTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        (int)delay.TotalHours,
                        delay.Minutes);

                    await Task.Delay(delay, stoppingToken);
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                // Run scraper
                await RunScheduledScrapeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Daily scraper background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily scraper background service");
                // Wait a bit before retrying to avoid tight error loops
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Daily scraper background service stopped");
    }

    private async Task RunScheduledScrapeAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting scheduled incremental scrape for active rovers");

        var startTime = DateTime.UtcNow;
        var totalPhotos = 0;
        var successfulRovers = 0;
        var failedRovers = new List<string>();

        // Create a scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var incrementalScraper = scope.ServiceProvider.GetRequiredService<IIncrementalScraperService>();

        foreach (var roverName in _options.ActiveRovers)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Scheduled scrape cancelled before completing all rovers");
                break;
            }

            try
            {
                _logger.LogInformation(
                    "Scraping {RoverName} with {Lookback}-sol lookback...",
                    roverName, _options.LookbackSols);

                var result = await incrementalScraper.ScrapeIncrementalAsync(
                    roverName,
                    _options.LookbackSols,
                    stoppingToken);

                if (result.Success)
                {
                    successfulRovers++;
                    totalPhotos += result.PhotosAdded;

                    _logger.LogInformation(
                        "✓ {RoverName}: {Photos} photos added (sols {StartSol}-{EndSol}, {Duration}s)",
                        roverName, result.PhotosAdded, result.StartSol, result.EndSol,
                        (int)result.Duration.TotalSeconds);
                }
                else
                {
                    failedRovers.Add(roverName);
                    _logger.LogError(
                        "✗ {RoverName}: Scrape failed - {Error}",
                        roverName, result.ErrorMessage);
                }

                // Small delay between rovers to be nice to NASA's servers
                if (_options.ActiveRovers.IndexOf(roverName) < _options.ActiveRovers.Count - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (Exception ex)
            {
                failedRovers.Add(roverName);
                _logger.LogError(ex, "Failed to scrape {RoverName}", roverName);
            }
        }

        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation(
            "Scheduled scrape completed: {Successful}/{Total} rovers succeeded, {Photos} photos added, {Failed} failed, duration: {Duration}s",
            successfulRovers, _options.ActiveRovers.Count, totalPhotos,
            failedRovers.Count, (int)duration.TotalSeconds);

        if (failedRovers.Count > 0)
        {
            _logger.LogWarning("Failed rovers: {FailedRovers}", string.Join(", ", failedRovers));
        }
    }

    private DateTime CalculateNextRunTime()
    {
        var now = DateTime.UtcNow;
        var targetHour = _options.RunAtUtcHour;

        // Calculate next run time
        var nextRun = new DateTime(now.Year, now.Month, now.Day, targetHour, 0, 0, DateTimeKind.Utc);

        // If we've already passed today's target time, schedule for tomorrow
        if (now >= nextRun)
        {
            nextRun = nextRun.AddHours(_options.IntervalHours);
        }

        return nextRun;
    }
}
