namespace MarsVista.Api.Services.V2;

/// <summary>
/// Service for handling HTTP caching (ETags, Cache-Control headers)
/// </summary>
public interface ICachingServiceV2
{
    /// <summary>
    /// Generate an ETag from response data
    /// </summary>
    string GenerateETag(object data);

    /// <summary>
    /// Check if the request ETag matches the current ETag (cache hit)
    /// </summary>
    bool CheckETag(string? requestETag, string currentETag);

    /// <summary>
    /// Get cache control header value based on rover status
    /// </summary>
    /// <param name="isActiveRover">True if querying active rovers (Curiosity, Perseverance)</param>
    /// <param name="maxAgeSeconds">Optional custom max-age in seconds</param>
    string GetCacheControlHeader(bool isActiveRover, int? maxAgeSeconds = null);
}
