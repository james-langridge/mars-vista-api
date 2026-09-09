using System.Threading.RateLimiting;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Partition policy for the global (pre-authentication) rate limiter.
/// Anonymous requests share a fixed window of 100 per minute per client IP.
/// Requests carrying an API key are exempt here: their limit is the key's
/// hourly/daily quota enforced in <see cref="UserApiKeyAuthenticationMiddleware"/>.
/// Without the exemption every visitor of a first-party frontend, which proxies
/// all traffic through one server IP, shared a single 100/min bucket.
/// </summary>
public static class GlobalRateLimitPartition
{
    public const int AnonymousPermitLimit = 100;

    public static RateLimitPartition<string> Resolve(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-API-Key"))
        {
            return RateLimitPartition.GetNoLimiter("keyed");
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AnonymousPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }
}
