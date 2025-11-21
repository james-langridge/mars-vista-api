using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of HTTP caching service
/// Handles ETag generation and cache-control header configuration
/// </summary>
public class CachingServiceV2 : ICachingServiceV2
{
    private readonly ILogger<CachingServiceV2> _logger;

    // Cache durations
    private const int ActiveRoverCacheSeconds = 3600; // 1 hour for active rovers
    private const int InactiveRoverCacheSeconds = 31536000; // 1 year for inactive rovers
    private const int StaticResourceCacheSeconds = 86400; // 1 day for static resources (rovers list, cameras)

    public CachingServiceV2(ILogger<CachingServiceV2> logger)
    {
        _logger = logger;
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
        var maxAge = maxAgeSeconds ?? (isActiveRover ? ActiveRoverCacheSeconds : InactiveRoverCacheSeconds);

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
        return $"public, max-age={StaticResourceCacheSeconds}, must-revalidate";
    }
}
