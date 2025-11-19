namespace MarsVista.Api.Entities;

/// <summary>
/// Represents an API usage event for monitoring, analytics, and rate limit tracking.
/// Tracks all API requests for admin dashboard visibility.
/// </summary>
public class UsageEvent
{
    /// <summary>
    /// Unique identifier for the usage event
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// User's email address (from API key authentication)
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// User's subscription tier at time of request
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint that was called (e.g., "/api/v1/rovers/curiosity/photos")
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code returned (200, 400, 429, 500, etc.)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Number of photos returned in the response (0 if not applicable)
    /// </summary>
    public int PhotosReturned { get; set; }

    /// <summary>
    /// When the API request was made
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
