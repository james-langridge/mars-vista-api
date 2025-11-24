using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MarsVista.Api.Data;

/// <summary>
/// EF Core interceptor that tracks database query execution time.
/// Accumulates total time spent in database queries for the current HTTP request.
/// Used to calculate X-DB-Time header for performance monitoring.
/// </summary>
public class QueryTimingInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryTimingInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        StartTiming();
        return result;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        StopTiming(eventData);
        return result;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming();
        return result;
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return result;
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        StartTiming();
        return result;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        StopTiming(eventData);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming();
        return result;
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return result;
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        StartTiming();
        return result;
    }

    public override object ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result)
    {
        StopTiming(eventData);
        return result;
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming();
        return result;
    }

    public override async ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return result;
    }

    private void StartTiming()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Store a stopwatch in HttpContext.Items to track this query
        var stopwatch = Stopwatch.StartNew();
        httpContext.Items["__CurrentQueryStopwatch"] = stopwatch;
    }

    private void StopTiming(CommandExecutedEventData eventData)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // Get the stopwatch for this query
        if (httpContext.Items["__CurrentQueryStopwatch"] is Stopwatch queryStopwatch)
        {
            queryStopwatch.Stop();

            // Accumulate total database time for this request
            var totalDbTime = httpContext.Items["__TotalDbTime"] as TimeSpan? ?? TimeSpan.Zero;
            totalDbTime += queryStopwatch.Elapsed;
            httpContext.Items["__TotalDbTime"] = totalDbTime;

            // Also track query count
            var queryCount = httpContext.Items["__DbQueryCount"] as int? ?? 0;
            httpContext.Items["__DbQueryCount"] = queryCount + 1;

            // Clean up
            httpContext.Items.Remove("__CurrentQueryStopwatch");
        }
    }
}
