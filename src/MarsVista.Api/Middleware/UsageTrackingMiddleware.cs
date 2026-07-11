using System.Diagnostics;
using System.Text.Json;
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
    private readonly IServiceScopeFactory _scopeFactory;

    public UsageTrackingMiddleware(
        RequestDelegate next,
        ILogger<UsageTrackingMiddleware> logger,
        IServiceScopeFactory scopeFactory)
    {
        _next = next;
        _logger = logger;
        _scopeFactory = scopeFactory;
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

            // Read error detail from response body before copying back
            string? errorDetail = null;
            if (context.Response.StatusCode >= 400)
            {
                errorDetail = ExtractErrorDetail(responseBody);
            }

            // Copy response back to original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Build the usage event while the HttpContext is still alive, then
            // persist it fire-and-forget. The detached task must NOT touch the
            // HttpContext: once the response completes the framework recycles the
            // context and its IFeatureCollection, so any later access throws
            // ObjectDisposedException on the unobserved task.
            var usageEvent = BuildUsageEvent(context, stopwatch.ElapsedMilliseconds, errorDetail);
            if (usageEvent is not null)
            {
                _ = TrackUsageAsync(usageEvent);
            }
        }
    }

    /// <summary>
    /// Extracts a concise error description from the response body JSON.
    /// </summary>
    private string? ExtractErrorDetail(MemoryStream responseBody)
    {
        try
        {
            if (responseBody.Length == 0) return null;

            responseBody.Seek(0, SeekOrigin.Begin);
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // RFC 7807 format (custom validation errors): { "detail": "...", "errors": [...] }
            if (root.TryGetProperty("detail", out var detail) &&
                root.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array)
            {
                var parts = new List<string>();
                foreach (var err in errors.EnumerateArray())
                {
                    if (err.TryGetProperty("message", out var msg))
                        parts.Add(msg.GetString() ?? "");
                }
                return parts.Count > 0 ? string.Join("; ", parts) : detail.GetString();
            }

            // ASP.NET Core validation format: { "errors": { "field": ["msg"] } }
            if (root.TryGetProperty("errors", out var aspErrors) &&
                aspErrors.ValueKind == JsonValueKind.Object)
            {
                var parts = new List<string>();
                foreach (var prop in aspErrors.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var msg in prop.Value.EnumerateArray())
                            parts.Add($"{prop.Name}: {msg.GetString()}");
                    }
                }
                return parts.Count > 0 ? string.Join("; ", parts) : null;
            }

            // Simple { "detail": "..." } or { "message": "..." }
            if (root.TryGetProperty("detail", out var d)) return d.GetString();
            if (root.TryGetProperty("message", out var m)) return m.GetString();

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the usage event from the request/response while the HttpContext is
    /// still alive. Returns null for unauthenticated requests (nothing to track).
    /// </summary>
    private static UsageEvent? BuildUsageEvent(HttpContext context, long responseTimeMs, string? errorDetail)
    {
        // Extract user context (set by authentication middleware)
        var userEmail = context.Items["UserEmail"] as string;
        if (string.IsNullOrEmpty(userEmail))
        {
            // No authenticated user - nothing to track
            return null;
        }

        var userTier = context.Items["UserTier"] as string;

        // Count photos returned (if applicable)
        var photosReturned = context.Items.TryGetValue("PhotosReturned", out var value) && value is int count
            ? count
            : 0;

        // Capture query string for error requests
        var queryString = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value
            : null;

        return new UsageEvent
        {
            UserEmail = userEmail,
            Tier = userTier ?? "free",
            Endpoint = context.Request.Path,
            StatusCode = context.Response.StatusCode,
            ResponseTimeMs = (int)responseTimeMs,
            PhotosReturned = photosReturned,
            QueryString = queryString,
            ErrorDetail = errorDetail,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Persists the usage event fire-and-forget, without blocking the response.
    /// Detached from the request: it works only with the pre-built
    /// <paramref name="usageEvent"/> and never dereferences the HttpContext, which
    /// may already be disposed by the time this runs.
    /// </summary>
    private async Task TrackUsageAsync(UsageEvent usageEvent)
    {
        try
        {
            await PersistAsync(usageEvent);
        }
        catch (Exception ex)
        {
            // Log with the captured endpoint - touching the HttpContext here would
            // throw a second, unobserved ObjectDisposedException on this task.
            _logger.LogError(ex, "Failed to track usage event for {Path}", usageEvent.Endpoint);
        }
    }

    /// <summary>
    /// Writes the usage event to the database using a fresh DI scope from the
    /// application root, independent of the request scope (which ends with the
    /// response). Virtual so tests can simulate persistence failures.
    /// </summary>
    protected virtual async Task PersistAsync(UsageEvent usageEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        dbContext.UsageEvents.Add(usageEvent);
        await dbContext.SaveChangesAsync();
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
