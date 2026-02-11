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

            // Track usage (fire-and-forget)
            _ = TrackUsageAsync(context, stopwatch.ElapsedMilliseconds, errorDetail);
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
    /// Asynchronously tracks the usage event without blocking the response.
    /// </summary>
    private async Task TrackUsageAsync(HttpContext context, long responseTimeMs, string? errorDetail)
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

            // Capture query string for error requests
            var queryString = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value
                : null;

            // Create usage event
            var usageEvent = new UsageEvent
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
