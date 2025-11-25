namespace MarsVista.Api.Services.V2;

/// <summary>
/// Options for controlling cache TTL
/// </summary>
public class CacheOptions
{
    public TimeSpan? RedisDuration { get; set; }

    public static CacheOptions Default => new CacheOptions
    {
        RedisDuration = TimeSpan.FromHours(1)
    };
}

/// <summary>
/// Service for handling HTTP caching (ETags, Cache-Control headers) and
/// two-level caching with Memory (L1) + Redis (L2)
/// </summary>
public interface ICachingServiceV2
{
    // Two-level caching methods

    /// <summary>
    /// Get or set cached value with two-level caching (L1 Memory + L2 Redis)
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Set value in both cache levels
    /// </summary>
    Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Invalidate cache entry in both levels
    /// </summary>
    Task InvalidateAsync(string key);

    /// <summary>
    /// Generate cache key from parts
    /// </summary>
    string GenerateCacheKey(params object?[] parts);

    // ETag support (existing)

    /// <summary>
    /// Generate an ETag from response data
    /// </summary>
    string GenerateETag(object data);

    /// <summary>
    /// Check if the request ETag matches the current ETag (cache hit)
    /// </summary>
    bool CheckETag(string? requestETag, string currentETag);

    // Cache-Control headers (existing)

    /// <summary>
    /// Get cache control header value based on rover status
    /// </summary>
    /// <param name="isActiveRover">True if querying active rovers (Curiosity, Perseverance)</param>
    /// <param name="maxAgeSeconds">Optional custom max-age in seconds</param>
    string GetCacheControlHeader(bool isActiveRover, int? maxAgeSeconds = null);

    /// <summary>
    /// Get cache control header for static resources (rovers, cameras)
    /// </summary>
    string GetStaticResourceCacheHeader();

    // Cache option helpers

    /// <summary>
    /// Get cache options for manifest data
    /// </summary>
    CacheOptions GetManifestCacheOptions(bool isActiveRover);

    /// <summary>
    /// Get cache options for static resources (rovers, cameras)
    /// </summary>
    CacheOptions GetStaticResourceCacheOptions();

    /// <summary>
    /// Get cache options for statistics
    /// </summary>
    CacheOptions GetStatisticsCacheOptions();

    /// <summary>
    /// Get cache options for panoramas
    /// </summary>
    CacheOptions GetPanoramaCacheOptions();

    // Cache metrics

    /// <summary>
    /// Get current cache statistics
    /// </summary>
    CacheStats GetCacheStats();

    /// <summary>
    /// Check if Redis is connected
    /// </summary>
    bool IsRedisConnected { get; }
}
