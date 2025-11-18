namespace MarsVista.Api.Entities;

/// <summary>
/// Tracks API rate limit usage for persistent rate limiting.
/// Currently using in-memory tracking (MemoryCache), but this entity is
/// ready for future migration to database-backed rate limiting.
/// </summary>
public class RateLimit
{
    /// <summary>
    /// Unique identifier for the rate limit record
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// User's email address (matches ApiKey.UserEmail)
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Start of the rate limit window (e.g., 2025-01-15 14:00:00 for hourly window)
    /// </summary>
    public DateTime WindowStart { get; set; }

    /// <summary>
    /// Type of rate limit window: 'hour' or 'day'
    /// </summary>
    public string WindowType { get; set; } = string.Empty;

    /// <summary>
    /// Number of requests made in this window
    /// </summary>
    public int RequestCount { get; set; }
}
