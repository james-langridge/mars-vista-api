using System.Diagnostics;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware that measures server-side processing time and adds X-Response-Time header.
/// This allows clients to distinguish between network latency and server processing time.
/// Response time is measured in seconds with 6 decimal places for compatibility with benchmark tools.
/// </summary>
public class ResponseTimingMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseTimingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Execute the rest of the pipeline
        await _next(context);

        // Stop timing
        stopwatch.Stop();

        // Add response time header in seconds (matches curl's time_total format)
        var responseTimeSeconds = stopwatch.Elapsed.TotalSeconds;
        context.Response.Headers.Append("X-Response-Time", responseTimeSeconds.ToString("F6"));
    }
}
