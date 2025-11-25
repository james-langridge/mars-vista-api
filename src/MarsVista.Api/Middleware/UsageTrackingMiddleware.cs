using System.Diagnostics;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware to track API usage events for analytics and monitoring.
/// Logs request details, response time, and status code for admin dashboard visibility.
/// Uses fire-and-forget pattern to avoid blocking requests.
/// </summary>
public class UsageTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsageTrackingMiddleware> _logger;

    public UsageTrackingMiddleware(
        RequestDelegate next,
        ILogger<UsageTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tracking for these paths
        if (ShouldSkipTracking(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        // Capture the response for analysis
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Track usage (fire-and-forget)
            _ = TrackUsageAsync(context, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Asynchronously tracks the usage event without blocking the response.
    /// </summary>
    private async Task TrackUsageAsync(HttpContext context, long responseTimeMs)
    {
        try
        {
            // Extract user context (set by authentication middleware)
            var userEmail = context.Items["UserEmail"] as string;
            var userTier = context.Items["UserTier"] as string;

            if (string.IsNullOrEmpty(userEmail))
            {
                // No authenticated user - skip tracking
                return;
            }

            // Count photos returned (if applicable)
            var photosReturned = 0;
            if (context.Items.ContainsKey("PhotosReturned"))
            {
                photosReturned = (int)(context.Items["PhotosReturned"] ?? 0);
            }

            // Create usage event
            var usageEvent = new UsageEvent
            {
                UserEmail = userEmail,
                Tier = userTier ?? "free",
                Endpoint = context.Request.Path,
                StatusCode = context.Response.StatusCode,
                ResponseTimeMs = (int)responseTimeMs,
                PhotosReturned = photosReturned,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database (use a new scope to avoid issues with disposed contexts)
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

            dbContext.UsageEvents.Add(usageEvent);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            _logger.LogError(ex, "Failed to track usage event for {Path}", context.Request.Path);
        }
    }

    /// <summary>
    /// Determines if tracking should be skipped for a given path.
    /// </summary>
    private static bool ShouldSkipTracking(PathString path)
    {
        // Skip tracking for these paths
        var skippedPaths = new[]
        {
            "/health",
            "/api/v1/admin",    // Don't track admin dashboard requests
            "/api/v1/internal", // Don't track internal API calls
            "/api/scraper"      // Don't track scraper endpoints
        };

        return skippedPaths.Any(p => path.StartsWithSegments(p));
    }
}
