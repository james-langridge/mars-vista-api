using System.Diagnostics;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware that measures server-side processing time and response size.
/// Adds X-Response-Time and X-Response-Size headers for performance monitoring.
/// This allows clients to distinguish between network latency and server processing time,
/// and identify bloated responses that could benefit from optimization.
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

        // Capture the original response body stream
        var originalBodyStream = context.Response.Body;

        // Create a new memory stream to capture the response
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Execute the rest of the pipeline
            await _next(context);

            // Stop timing
            stopwatch.Stop();

            // Get response size in bytes
            var responseSize = responseBody.Length;

            // Add performance headers
            var responseTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            context.Response.Headers.Append("X-Response-Time", responseTimeSeconds.ToString("F6"));
            context.Response.Headers.Append("X-Response-Size", responseSize.ToString());

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Restore the original stream
            context.Response.Body = originalBodyStream;
        }
    }
}
