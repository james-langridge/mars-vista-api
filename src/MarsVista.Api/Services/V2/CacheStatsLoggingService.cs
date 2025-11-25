using Microsoft.Extensions.Options;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Background service that periodically logs cache statistics
/// </summary>
public class CacheStatsLoggingService : BackgroundService
{
    private readonly ICachingServiceV2 _cachingService;
    private readonly IOptions<CacheWarmingOptions> _options;
    private readonly ILogger<CacheStatsLoggingService> _logger;

    public CacheStatsLoggingService(
        ICachingServiceV2 cachingService,
        IOptions<CacheWarmingOptions> options,
        ILogger<CacheStatsLoggingService> logger)
    {
        _cachingService = cachingService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.Value.StatsLoggingIntervalMinutes);

        _logger.LogDebug(
            "Cache stats logging service started with {Interval} minute interval",
            _options.Value.StatsLoggingIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);

                var stats = _cachingService.GetCacheStats();

                // Only log if there's been activity
                if (stats.TotalRequests > 0)
                {
                    _logger.LogInformation(
                        "Cache stats - L1 Hits: {L1Hits}, L2 Hits: {L2Hits}, Misses: {Misses}, " +
                        "Hit Rate: {HitRate}, Sets: {Sets}, Invalidations: {Invalidations}, " +
                        "Redis Connected: {RedisConnected}",
                        stats.L1Hits,
                        stats.L2Hits,
                        stats.Misses,
                        stats.HitRateFormatted,
                        stats.Sets,
                        stats.Invalidations,
                        _cachingService.IsRedisConnected);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, no logging needed
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error logging cache stats");
            }
        }

        _logger.LogDebug("Cache stats logging service stopped");
    }
}
