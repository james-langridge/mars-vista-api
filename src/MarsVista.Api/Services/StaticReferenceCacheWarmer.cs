namespace MarsVista.Api.Services;

/// <summary>
/// Loads <see cref="IStaticReferenceCache"/> at process startup so the first
/// photo request does not pay the DB roundtrip + lock-acquisition cost.
///
/// Runs after the host has applied migrations and (in development) seeded the
/// database, because IHostedServices start after Program.cs's pre-Run
/// migration/seed block.
///
/// Best-effort: a failure here logs a warning but does not block startup. If
/// the load returned empty, the cache's lazy fallback retries on the first
/// request. We never want to fail-fast on a DB blip during a Railway deploy.
/// </summary>
public class StaticReferenceCacheWarmer : IHostedService
{
    private readonly IStaticReferenceCache _cache;
    private readonly ILogger<StaticReferenceCacheWarmer> _logger;

    public StaticReferenceCacheWarmer(
        IStaticReferenceCache cache,
        ILogger<StaticReferenceCacheWarmer> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cache.EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "StaticReferenceCacheWarmer failed at startup - cache will fall back to lazy load on first request");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
