using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MarsVista.Api.Middleware;

/// <summary>
/// The global rate limiter, which runs before authentication. Anonymous
/// requests share a fixed window of <see cref="AnonymousPermitLimit"/> per
/// minute per client IP. Requests carrying an API key on a path the key
/// middleware governs are exempt: their limit is the key's hourly/daily quota.
/// </summary>
internal static class GlobalRateLimitPolicy
{
    internal const int AnonymousPermitLimit = 100;

    internal static void Configure(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(PartitionFor);

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too Many Requests",
                message = $"Rate limit exceeded. Maximum {AnonymousPermitLimit} requests per minute per IP address without an API key.",
                retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? retryAfter.TotalSeconds
                    : 60
            }, cancellationToken);
        };
    }

    internal static RateLimitPartition<string> PartitionFor(HttpContext context)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(apiKey) && UserApiKeyAuthenticationMiddleware.RequiresApiKey(context.Request.Path))
        {
            // One shared no-op limiter; the key is a label, not an identity.
            return RateLimitPartition.GetNoLimiter("keyed");
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = AnonymousPermitLimit,
                Window = TimeSpan.FromMinutes(1)
            });
    }
}
