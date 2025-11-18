namespace MarsVista.Api.Services;

/// <summary>
/// Service for checking and tracking API rate limits.
/// Uses in-memory caching for single-instance deployment.
/// Migrate to Redis for multi-instance/distributed deployments.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Checks if a request from a user should be allowed based on rate limits.
    /// Increments request count if allowed.
    /// </summary>
    /// <param name="userEmail">User's email (from API key)</param>
    /// <param name="tier">User's tier (free, pro, enterprise)</param>
    /// <returns>
    /// Tuple containing:
    /// - allowed: Whether the request should be allowed
    /// - hourlyRemaining: Remaining requests in current hour
    /// - dailyRemaining: Remaining requests in current day
    /// - hourlyResetAt: When hourly limit resets (Unix timestamp)
    /// - dailyResetAt: When daily limit resets (Unix timestamp)
    /// </returns>
    Task<(bool allowed, int hourlyRemaining, int dailyRemaining, long hourlyResetAt, long dailyResetAt)> CheckRateLimitAsync(
        string userEmail,
        string tier);

    /// <summary>
    /// Gets the rate limit configuration for a given tier.
    /// </summary>
    /// <param name="tier">The subscription tier (free, pro, enterprise)</param>
    /// <returns>Tuple of (hourly limit, daily limit)</returns>
    (int hourlyLimit, int dailyLimit) GetLimitsForTier(string tier);
}
