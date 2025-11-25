namespace MarsVista.Api.Services.V2;

/// <summary>
/// Background service that warms caches on application startup
/// Runs asynchronously to avoid blocking the application from starting
/// </summary>
public class CacheWarmingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmingBackgroundService> _logger;

    // Delay before starting cache warming to let the app fully initialize
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);

    public CacheWarmingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CacheWarmingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug(
            "Cache warming background service starting in {Delay}s...",
            StartupDelay.TotalSeconds);

        try
        {
            // Small delay to let app fully start and database connections initialize
            await Task.Delay(StartupDelay, stoppingToken);

            using var scope = _serviceProvider.CreateScope();
            var warmingService = scope.ServiceProvider.GetRequiredService<ICacheWarmingService>();

            await warmingService.WarmCachesAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Cache warming background service cancelled during shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in cache warming background service");
        }
    }
}
