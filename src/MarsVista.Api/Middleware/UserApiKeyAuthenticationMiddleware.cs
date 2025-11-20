using MarsVista.Api.Data;
using MarsVista.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware to authenticate user API requests using X-API-Key header.
/// Validates API key, checks if active, and enforces rate limits.
/// Applies to public query endpoints (/api/v1/rovers/*, /api/v1/manifests/*).
/// </summary>
public class UserApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserApiKeyAuthenticationMiddleware> _logger;

    public UserApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<UserApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        MarsVistaDbContext dbContext,
        IApiKeyService apiKeyService,
        IRateLimitService rateLimitService)
    {
        // Skip authentication for these paths
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Extract API key from header
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning(
                "API request without API key from {IP} to {Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key required. Provide via X-API-Key header. Sign up at https://marsvista.dev to get your API key."
            });
            return;
        }

        // Validate API key format
        if (!apiKeyService.ValidateApiKeyFormat(apiKey))
        {
            _logger.LogWarning(
                "Invalid API key format from {IP}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid API key format"
            });
            return;
        }

        // Hash API key and look up in database
        var apiKeyHash = apiKeyService.HashApiKey(apiKey);
        var apiKeyRecord = await dbContext.ApiKeys
            .Where(k => k.ApiKeyHash == apiKeyHash)
            .FirstOrDefaultAsync();

        if (apiKeyRecord == null)
        {
            _logger.LogWarning(
                "Unknown API key attempt from {IP}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "Invalid API key"
            });
            return;
        }

        // Check if API key is active
        if (!apiKeyRecord.IsActive)
        {
            _logger.LogWarning(
                "Inactive API key attempt for {Email} from {IP}",
                apiKeyRecord.UserEmail,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Unauthorized",
                message = "API key has been deactivated. Please regenerate your API key at https://marsvista.dev/dashboard"
            });
            return;
        }

        // Check rate limits
        var (allowed, hourlyRemaining, dailyRemaining, hourlyResetAt, dailyResetAt) =
            await rateLimitService.CheckRateLimitAsync(apiKeyRecord.UserEmail, apiKeyRecord.Tier);

        // Add rate limit headers to response (even if request is allowed)
        var (hourlyLimit, dailyLimit) = rateLimitService.GetLimitsForTier(apiKeyRecord.Tier);
        context.Response.Headers.Append("X-RateLimit-Limit-Hour", hourlyLimit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining-Hour", hourlyRemaining.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset-Hour", hourlyResetAt.ToString());
        context.Response.Headers.Append("X-RateLimit-Limit-Day", dailyLimit == -1 ? "unlimited" : dailyLimit.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining-Day", dailyRemaining == int.MaxValue ? "unlimited" : dailyRemaining.ToString());
        context.Response.Headers.Append("X-RateLimit-Reset-Day", dailyResetAt.ToString());
        context.Response.Headers.Append("X-RateLimit-Tier", apiKeyRecord.Tier);

        if (!allowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Email} (tier: {Tier}) from {IP}",
                apiKeyRecord.UserEmail,
                apiKeyRecord.Tier,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = $"Rate limit exceeded. Free tier: {hourlyLimit} requests/hour, {dailyLimit} requests/day. Upgrade at https://marsvista.dev/pricing",
                hourlyLimit,
                hourlyRemaining,
                dailyLimit = dailyLimit == -1 ? (object)"unlimited" : dailyLimit,
                dailyRemaining = dailyRemaining == int.MaxValue ? (object)"unlimited" : dailyRemaining,
                hourlyResetAt,
                dailyResetAt,
                tier = apiKeyRecord.Tier,
                upgradeUrl = "https://marsvista.dev/pricing"
            });
            return;
        }

        // Store user context for downstream middleware/controllers
        context.Items["UserEmail"] = apiKeyRecord.UserEmail;
        context.Items["UserTier"] = apiKeyRecord.Tier;
        context.Items["UserRole"] = apiKeyRecord.Role;
        context.Items["ApiKeyId"] = apiKeyRecord.Id;

        // Update last_used_at timestamp (fire and forget - don't block request)
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
                var key = await db.ApiKeys.FindAsync(apiKeyRecord.Id);
                if (key != null)
                {
                    key.LastUsedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update last_used_at for API key {Id}", apiKeyRecord.Id);
            }
        });

        // Valid API key - continue to next middleware
        await _next(context);
    }

    /// <summary>
    /// Determines if authentication should be skipped for a given path.
    /// </summary>
    private static bool ShouldSkipAuthentication(PathString path)
    {
        // Skip authentication for these paths
        var skippedPaths = new[]
        {
            "/health",
            "/api/v1/internal",  // Internal API uses different auth (X-Internal-Secret)
            "/api/scraper",      // Scraper endpoints use simple API_KEY auth
            "/api/v1/statistics" // Public statistics for landing page
        };

        return skippedPaths.Any(p => path.StartsWithSegments(p));
    }
}
