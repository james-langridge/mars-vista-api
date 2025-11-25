using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Two-level caching service with Memory (L1) and Redis (L2)
/// Provides fast in-memory cache with distributed persistence via Redis
/// </summary>
public class CachingServiceV2 : ICachingServiceV2
{
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<CachingServiceV2> _logger;

    // Cache durations by data type
    private static readonly TimeSpan ActiveRoverCacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan InactiveRoverCacheDuration = TimeSpan.FromDays(365);
    private static readonly TimeSpan StaticResourceCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan StatisticsCacheDuration = TimeSpan.FromHours(1);
    private static readonly TimeSpan PanoramaCacheDuration = TimeSpan.FromHours(4);

    // L1 (memory) cache is shorter for memory efficiency
    private static readonly TimeSpan L1CacheDuration = TimeSpan.FromMinutes(15);

    public CachingServiceV2(
        IMemoryCache memoryCache,
        IConnectionMultiplexer? redis,
        ILogger<CachingServiceV2> logger)
    {
        _memoryCache = memoryCache;
        _redis = redis;
        _logger = logger;

        if (redis == null)
        {
            _logger.LogWarning("Redis not configured - falling back to memory-only caching");
        }
        else
        {
            _logger.LogInformation("Two-level caching enabled: L1 (Memory) + L2 (Redis)");
        }
    }

    /// <summary>
    /// Get or set cached value with two-level caching
    /// </summary>
    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;

        // L1: Check memory cache first (fastest)
        if (_memoryCache.TryGetValue(key, out T? cached))
        {
            _logger.LogDebug("L1 cache hit: {Key}", key);
            return cached;
        }

        // L2: Check Redis if available
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                var redisValue = await db.StringGetAsync(key);

                if (!redisValue.IsNullOrEmpty)
                {
                    _logger.LogDebug("L2 cache hit: {Key}", key);
                    var deserialized = JsonSerializer.Deserialize<T>(redisValue!);

                    // Populate L1 cache
                    _memoryCache.Set(key, deserialized, L1CacheDuration);

                    return deserialized;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis get failed for key {Key}, falling through to factory", key);
            }
        }

        // Cache miss - generate value
        _logger.LogDebug("Cache miss: {Key}", key);
        var value = await factory();

        if (value != null)
        {
            // Set in both caches
            await SetAsync(key, value, options);
        }

        return value;
    }

    /// <summary>
    /// Set value in both cache levels
    /// </summary>
    public async Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;

        // Set in L1 (memory)
        _memoryCache.Set(key, value, L1CacheDuration);

        // Set in L2 (Redis) if available
        if (_redis != null && _redis.IsConnected && options.RedisDuration.HasValue)
        {
            try
            {
                var db = _redis.GetDatabase();
                var json = JsonSerializer.Serialize(value);
                await db.StringSetAsync(key, json, options.RedisDuration);

                _logger.LogDebug("Set L1 + L2 cache: {Key} (Redis TTL: {TTL})", key, options.RedisDuration);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis set failed for key {Key}", key);
            }
        }
        else
        {
            _logger.LogDebug("Set L1 cache only: {Key}", key);
        }
    }

    /// <summary>
    /// Invalidate cache entry in both levels
    /// </summary>
    public async Task InvalidateAsync(string key)
    {
        // Remove from L1
        _memoryCache.Remove(key);

        // Remove from L2
        if (_redis != null && _redis.IsConnected)
        {
            try
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(key);
                _logger.LogDebug("Invalidated cache: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis delete failed for key {Key}", key);
            }
        }
    }

    /// <summary>
    /// Generate cache key from parts
    /// </summary>
    public string GenerateCacheKey(params object?[] parts)
    {
        var sanitizedParts = parts.Select(p =>
            p?.ToString()?.Replace(":", "_").Replace(" ", "_") ?? "null");

        return $"marsvista:{string.Join(":", sanitizedParts)}";
    }

    /// <summary>
    /// Generate an ETag from response data using SHA256 hash
    /// </summary>
    public string GenerateETag(object data)
    {
        try
        {
            // Serialize the data to JSON
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            // Generate SHA256 hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

            // Convert to base64 and take first 16 characters for efficiency
            var base64Hash = Convert.ToBase64String(hashBytes);
            return base64Hash.Substring(0, Math.Min(16, base64Hash.Length));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate ETag, using fallback");
            // Fallback to timestamp-based ETag
            return Convert.ToBase64String(BitConverter.GetBytes(DateTime.UtcNow.Ticks));
        }
    }

    /// <summary>
    /// Check if the request ETag matches the current ETag
    /// </summary>
    public bool CheckETag(string? requestETag, string currentETag)
    {
        if (string.IsNullOrWhiteSpace(requestETag))
            return false;

        // Remove quotes if present (ETags are typically quoted)
        requestETag = requestETag.Trim('"', ' ');
        currentETag = currentETag.Trim('"', ' ');

        return string.Equals(requestETag, currentETag, StringComparison.Ordinal);
    }

    /// <summary>
    /// Get appropriate Cache-Control header based on rover status
    /// </summary>
    public string GetCacheControlHeader(bool isActiveRover, int? maxAgeSeconds = null)
    {
        var maxAge = maxAgeSeconds ?? (isActiveRover
            ? (int)ActiveRoverCacheDuration.TotalSeconds
            : (int)InactiveRoverCacheDuration.TotalSeconds);

        // public: can be cached by any cache (CDN, proxy, browser)
        // max-age: how long the response can be cached
        // must-revalidate: caches must verify with origin server when stale
        return $"public, max-age={maxAge}, must-revalidate";
    }

    /// <summary>
    /// Get cache duration for static resources (rovers, cameras)
    /// </summary>
    public string GetStaticResourceCacheHeader()
    {
        return $"public, max-age={(int)StaticResourceCacheDuration.TotalSeconds}, must-revalidate";
    }

    /// <summary>
    /// Get cache options for manifest
    /// </summary>
    public CacheOptions GetManifestCacheOptions(bool isActiveRover)
    {
        return new CacheOptions
        {
            RedisDuration = isActiveRover ? ActiveRoverCacheDuration : InactiveRoverCacheDuration
        };
    }

    /// <summary>
    /// Get cache options for static resources (rovers, cameras)
    /// </summary>
    public CacheOptions GetStaticResourceCacheOptions()
    {
        return new CacheOptions
        {
            RedisDuration = StaticResourceCacheDuration
        };
    }

    /// <summary>
    /// Get cache options for statistics
    /// </summary>
    public CacheOptions GetStatisticsCacheOptions()
    {
        return new CacheOptions
        {
            RedisDuration = StatisticsCacheDuration
        };
    }

    /// <summary>
    /// Get cache options for panoramas
    /// </summary>
    public CacheOptions GetPanoramaCacheOptions()
    {
        return new CacheOptions
        {
            RedisDuration = PanoramaCacheDuration
        };
    }
}
