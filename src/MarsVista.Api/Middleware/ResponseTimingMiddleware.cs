using System.Diagnostics;

namespace MarsVista.Api.Middleware;

/// <summary>
/// Middleware that measures server-side processing time, database query time, and response size.
/// Adds performance monitoring headers:
/// - X-Response-Time: Total server processing time in seconds
/// - X-Response-Size: Response body size in bytes
/// - X-DB-Time: Total time spent in database queries (if any queries were executed)
/// - X-App-Time: Application processing time (Response-Time minus DB-Time)
/// - X-DB-Query-Count: Number of database queries executed
///
/// This allows clients to distinguish between network latency and server processing time,
/// identify slow database queries vs slow application logic, and detect bloated responses.
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

            // Get database timing if available
            var totalDbTime = context.Items["__TotalDbTime"] as TimeSpan?;
            var dbQueryCount = context.Items["__DbQueryCount"] as int?;

            // Add performance headers
            var responseTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            context.Response.Headers.Append("X-Response-Time", responseTimeSeconds.ToString("F6"));
            context.Response.Headers.Append("X-Response-Size", responseSize.ToString());

            // Add database timing headers if queries were executed
            if (totalDbTime.HasValue)
            {
                var dbTimeSeconds = totalDbTime.Value.TotalSeconds;
                context.Response.Headers.Append("X-DB-Time", dbTimeSeconds.ToString("F6"));

                // Calculate application time (total time minus database time)
                var appTimeSeconds = responseTimeSeconds - dbTimeSeconds;
                context.Response.Headers.Append("X-App-Time", appTimeSeconds.ToString("F6"));
            }

            if (dbQueryCount.HasValue)
            {
                context.Response.Headers.Append("X-DB-Query-Count", dbQueryCount.Value.ToString());
            }

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
