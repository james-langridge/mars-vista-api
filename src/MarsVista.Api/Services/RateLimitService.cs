using Microsoft.Extensions.Caching.Memory;

namespace MarsVista.Api.Services;

/// <summary>
/// In-memory rate limiting service using MemoryCache.
/// Thread-safe implementation for tracking user request counts.
///
/// ⚠️ Limitation: Only works for single-instance deployments.
/// For multi-instance (Railway auto-scaling), migrate to Redis-based distributed cache.
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Rate limit configurations by tier
    private static readonly Dictionary<string, (int hourly, int daily)> TierLimits = new()
    {
        { "free", (10000, 100000) },
        { "pro", (50000, 1000000) }
    };

    public RateLimitService(IMemoryCache cache, ILogger<RateLimitService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public (int hourlyLimit, int dailyLimit) GetLimitsForTier(string tier)
    {
        if (TierLimits.TryGetValue(tier.ToLowerInvariant(), out var limits))
        {
            return limits;
        }

        // Default to free tier if unknown tier
        _logger.LogWarning("Unknown tier {Tier}, defaulting to free tier limits", tier);
        return TierLimits["free"];
    }

    public async Task<(bool allowed, int hourlyRemaining, int dailyRemaining, long hourlyResetAt, long dailyResetAt)> CheckRateLimitAsync(
        string userEmail,
        string tier)
    {
        var (hourlyLimit, dailyLimit) = GetLimitsForTier(tier);
        var now = DateTime.UtcNow;

        // Calculate window boundaries
        var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        var hourlyResetAt = ((DateTimeOffset)hourStart.AddHours(1)).ToUnixTimeSeconds();
        var dailyResetAt = ((DateTimeOffset)dayStart.AddDays(1)).ToUnixTimeSeconds();

        // Cache keys for tracking counts
        var hourlyKey = $"ratelimit:hourly:{userEmail}:{hourStart:yyyyMMddHH}";
        var dailyKey = $"ratelimit:daily:{userEmail}:{dayStart:yyyyMMdd}";

        await _lock.WaitAsync();
        try
        {
            // Get current counts from cache (default to 0 if not exists)
            var hourlyCount = _cache.GetOrCreate(hourlyKey, entry =>
            {
                entry.AbsoluteExpiration = hourStart.AddHours(1);
                return 0;
            });

            var dailyCount = _cache.GetOrCreate(dailyKey, entry =>
            {
                entry.AbsoluteExpiration = dayStart.AddDays(1);
                return 0;
            });

            // Check if limits would be exceeded
            var hourlyAllowed = hourlyCount < hourlyLimit;
            var dailyAllowed = dailyLimit == -1 || dailyCount < dailyLimit; // -1 = unlimited

            var allowed = hourlyAllowed && dailyAllowed;

            if (allowed)
            {
                // Increment counts
                _cache.Set(hourlyKey, hourlyCount + 1, hourStart.AddHours(1));
                _cache.Set(dailyKey, dailyCount + 1, dayStart.AddDays(1));

                var hourlyRemaining = Math.Max(0, hourlyLimit - (hourlyCount + 1));
                var dailyRemaining = dailyLimit == -1 ? int.MaxValue : Math.Max(0, dailyLimit - (dailyCount + 1));

                return (true, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt);
            }
            else
            {
                // Rate limit exceeded
                var hourlyRemaining = Math.Max(0, hourlyLimit - hourlyCount);
                var dailyRemaining = dailyLimit == -1 ? int.MaxValue : Math.Max(0, dailyLimit - dailyCount);

                _logger.LogWarning(
                    "Rate limit exceeded for {Email} (tier: {Tier}). Hourly: {HourlyCount}/{HourlyLimit}, Daily: {DailyCount}/{DailyLimit}",
                    userEmail, tier, hourlyCount, hourlyLimit, dailyCount, dailyLimit);

                return (false, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
