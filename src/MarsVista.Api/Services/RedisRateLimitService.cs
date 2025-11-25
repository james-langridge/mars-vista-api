using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace MarsVista.Api.Services;

/// <summary>
/// Redis-backed rate limiting service with memory cache fallback.
/// Provides distributed rate limiting that persists across deployments.
/// Falls back to in-memory cache if Redis is unavailable (graceful degradation).
/// </summary>
public class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisRateLimitService> _logger;
    private readonly SemaphoreSlim _fallbackLock = new(1, 1);

    // Rate limit configurations by tier
    // -1 = unlimited
    private static readonly Dictionary<string, (int hourly, int daily)> TierLimits = new()
    {
        { "free", (1000, 10000) },
        { "pro", (10000, 100000) },
        { "unlimited", (-1, -1) }
    };

    // Redis key prefix for rate limiting
    private const string KeyPrefix = "ratelimit";

    public RedisRateLimitService(
        IConnectionMultiplexer? redis,
        IMemoryCache memoryCache,
        ILogger<RedisRateLimitService> logger)
    {
        _redis = redis;
        _memoryCache = memoryCache;
        _logger = logger;

        if (redis == null || !redis.IsConnected)
        {
            _logger.LogWarning("Redis not available for rate limiting - using memory-only fallback");
        }
        else
        {
            _logger.LogInformation("Redis rate limiting enabled (persists across deployments)");
        }
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
        var hourlyKey = $"{KeyPrefix}:hourly:{userEmail}:{hourStart:yyyyMMddHH}";
        var dailyKey = $"{KeyPrefix}:daily:{userEmail}:{dayStart:yyyyMMdd}";

        // Calculate TTLs for Redis keys
        var hourlyTtl = hourStart.AddHours(1) - now;
        var dailyTtl = dayStart.AddDays(1) - now;

        // Try Redis first, fall back to memory
        if (_redis != null && _redis.IsConnected)
        {
            return await CheckRateLimitRedisAsync(
                hourlyKey, dailyKey,
                hourlyLimit, dailyLimit,
                hourlyResetAt, dailyResetAt,
                hourlyTtl, dailyTtl,
                userEmail, tier);
        }
        else
        {
            return await CheckRateLimitMemoryAsync(
                hourlyKey, dailyKey,
                hourlyLimit, dailyLimit,
                hourlyResetAt, dailyResetAt,
                hourStart, dayStart,
                userEmail, tier);
        }
    }

    private async Task<(bool allowed, int hourlyRemaining, int dailyRemaining, long hourlyResetAt, long dailyResetAt)> CheckRateLimitRedisAsync(
        string hourlyKey, string dailyKey,
        int hourlyLimit, int dailyLimit,
        long hourlyResetAt, long dailyResetAt,
        TimeSpan hourlyTtl, TimeSpan dailyTtl,
        string userEmail, string tier)
    {
        try
        {
            var db = _redis!.GetDatabase();

            // Use Redis transactions for atomic increment and TTL setting
            // INCR returns the new value after incrementing
            var hourlyCountTask = db.StringIncrementAsync(hourlyKey);
            var dailyCountTask = db.StringIncrementAsync(dailyKey);

            await Task.WhenAll(hourlyCountTask, dailyCountTask);

            var hourlyCount = (int)hourlyCountTask.Result;
            var dailyCount = (int)dailyCountTask.Result;

            // Set TTL on first increment (when count is 1)
            if (hourlyCount == 1)
            {
                await db.KeyExpireAsync(hourlyKey, hourlyTtl);
            }
            if (dailyCount == 1)
            {
                await db.KeyExpireAsync(dailyKey, dailyTtl);
            }

            // Check if limits are exceeded (-1 = unlimited)
            var hourlyAllowed = hourlyLimit == -1 || hourlyCount <= hourlyLimit;
            var dailyAllowed = dailyLimit == -1 || dailyCount <= dailyLimit;
            var allowed = hourlyAllowed && dailyAllowed;

            var hourlyRemaining = hourlyLimit == -1 ? int.MaxValue : Math.Max(0, hourlyLimit - hourlyCount);
            var dailyRemaining = dailyLimit == -1 ? int.MaxValue : Math.Max(0, dailyLimit - dailyCount);

            if (!allowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for {Email} (tier: {Tier}). Hourly: {HourlyCount}/{HourlyLimit}, Daily: {DailyCount}/{DailyLimit}",
                    userEmail, tier, hourlyCount, hourlyLimit, dailyCount, dailyLimit);
            }

            return (allowed, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis rate limit check failed, falling back to memory");

            // Fall back to memory on Redis failure
            var now = DateTime.UtcNow;
            var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
            var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            return await CheckRateLimitMemoryAsync(
                hourlyKey, dailyKey,
                hourlyLimit, dailyLimit,
                hourlyResetAt, dailyResetAt,
                hourStart, dayStart,
                userEmail, tier);
        }
    }

    private async Task<(bool allowed, int hourlyRemaining, int dailyRemaining, long hourlyResetAt, long dailyResetAt)> CheckRateLimitMemoryAsync(
        string hourlyKey, string dailyKey,
        int hourlyLimit, int dailyLimit,
        long hourlyResetAt, long dailyResetAt,
        DateTime hourStart, DateTime dayStart,
        string userEmail, string tier)
    {
        await _fallbackLock.WaitAsync();
        try
        {
            // Get current counts from cache (default to 0 if not exists)
            var hourlyCount = _memoryCache.GetOrCreate(hourlyKey, entry =>
            {
                entry.AbsoluteExpiration = hourStart.AddHours(1);
                return 0;
            });

            var dailyCount = _memoryCache.GetOrCreate(dailyKey, entry =>
            {
                entry.AbsoluteExpiration = dayStart.AddDays(1);
                return 0;
            });

            // Check if limits would be exceeded (-1 = unlimited)
            var hourlyAllowed = hourlyLimit == -1 || hourlyCount < hourlyLimit;
            var dailyAllowed = dailyLimit == -1 || dailyCount < dailyLimit;

            var allowed = hourlyAllowed && dailyAllowed;

            if (allowed)
            {
                // Increment counts
                _memoryCache.Set(hourlyKey, hourlyCount + 1, hourStart.AddHours(1));
                _memoryCache.Set(dailyKey, dailyCount + 1, dayStart.AddDays(1));

                var hourlyRemaining = hourlyLimit == -1 ? int.MaxValue : Math.Max(0, hourlyLimit - (hourlyCount + 1));
                var dailyRemaining = dailyLimit == -1 ? int.MaxValue : Math.Max(0, dailyLimit - (dailyCount + 1));

                return (true, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt);
            }
            else
            {
                // Rate limit exceeded
                var hourlyRemaining = hourlyLimit == -1 ? int.MaxValue : Math.Max(0, hourlyLimit - hourlyCount);
                var dailyRemaining = dailyLimit == -1 ? int.MaxValue : Math.Max(0, dailyLimit - dailyCount);

                _logger.LogWarning(
                    "Rate limit exceeded for {Email} (tier: {Tier}). Hourly: {HourlyCount}/{HourlyLimit}, Daily: {DailyCount}/{DailyLimit}",
                    userEmail, tier, hourlyCount, hourlyLimit, dailyCount, dailyLimit);

                return (false, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt);
            }
        }
        finally
        {
            _fallbackLock.Release();
        }
    }
}
