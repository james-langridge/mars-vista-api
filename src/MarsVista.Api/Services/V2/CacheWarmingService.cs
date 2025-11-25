using System.Diagnostics;
using Microsoft.Extensions.Options;
using MarsVista.Api.Options;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Configuration options for cache warming
/// </summary>
public class CacheWarmingOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Whether to warm caches on application startup
    /// </summary>
    public bool WarmOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to warm manifest caches (expensive operation)
    /// </summary>
    public bool WarmManifests { get; set; } = false;

    /// <summary>
    /// Interval in minutes for logging cache stats
    /// </summary>
    public int StatsLoggingIntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Service for pre-populating caches on application startup
/// </summary>
public interface ICacheWarmingService
{
    /// <summary>
    /// Warm all configured caches
    /// </summary>
    Task WarmCachesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service that pre-populates caches to ensure fast response times from startup
/// </summary>
public class CacheWarmingService : ICacheWarmingService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICachingServiceV2 _cachingService;
    private readonly IOptions<CacheWarmingOptions> _options;
    private readonly ILogger<CacheWarmingService> _logger;

    private static readonly string[] AllRovers = { "curiosity", "perseverance", "opportunity", "spirit" };
    private static readonly string[] InactiveRovers = { "opportunity", "spirit" };

    public CacheWarmingService(
        IServiceProvider serviceProvider,
        ICachingServiceV2 cachingService,
        IOptions<CacheWarmingOptions> options,
        ILogger<CacheWarmingService> logger)
    {
        _serviceProvider = serviceProvider;
        _cachingService = cachingService;
        _options = options;
        _logger = logger;
    }

    public async Task WarmCachesAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Value.WarmOnStartup)
        {
            _logger.LogInformation("Cache warming disabled by configuration");
            return;
        }

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Starting cache warming...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var roverServiceV2 = scope.ServiceProvider.GetRequiredService<IRoverQueryServiceV2>();
            var roverServiceV1 = scope.ServiceProvider.GetRequiredService<IRoverQueryService>();

            // Warm v2 rovers list
            _logger.LogDebug("Warming v2 rovers list cache...");
            await roverServiceV2.GetAllRoversAsync(cancellationToken);

            // Warm individual rover and camera caches (v2)
            foreach (var rover in AllRovers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Warming v2 cache for rover: {Rover}", rover);
                await roverServiceV2.GetRoverBySlugAsync(rover, cancellationToken);
                await roverServiceV2.GetRoverCamerasAsync(rover, cancellationToken);
            }

            // Warm v1 rovers list
            _logger.LogDebug("Warming v1 rovers list cache...");
            await roverServiceV1.GetAllRoversAsync(cancellationToken);

            // Warm individual rover caches (v1)
            foreach (var rover in AllRovers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Warming v1 cache for rover: {Rover}", rover);
                await roverServiceV1.GetRoverByNameAsync(rover, cancellationToken);
            }

            // Optionally warm manifests (expensive operation)
            if (_options.Value.WarmManifests)
            {
                _logger.LogDebug("Warming manifest caches...");

                // Always warm inactive rover manifests (never change, worth caching)
                foreach (var rover in InactiveRovers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Warming manifest for inactive rover: {Rover}", rover);
                    await roverServiceV2.GetRoverManifestAsync(rover, cancellationToken);
                    await roverServiceV1.GetManifestAsync(rover, cancellationToken);
                }
            }

            _logger.LogInformation(
                "Cache warming completed successfully in {ElapsedMs}ms",
                sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Cache warming cancelled after {ElapsedMs}ms",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache warming failed after {ElapsedMs}ms - application will continue with cold caches",
                sw.ElapsedMilliseconds);
        }
    }
}
