using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MarsVista.Api.Data;

/// <summary>
/// EF Core interceptor that tracks database query execution time per HTTP
/// request. Accumulated totals are published to <c>HttpContext.Items</c> so
/// <see cref="Middleware.ResponseTimingMiddleware"/> can emit
/// <c>X-DB-Time</c> and <c>X-DB-Query-Count</c> response headers.
///
/// Thread safety: a single request can issue multiple concurrent EF callbacks
/// (e.g. split-query <c>Include()</c> loading, or any fire-and-forget save
/// running while the main pipeline is still active). The previous
/// implementation stored a single stopwatch in <c>HttpContext.Items</c> and
/// called <c>Items.Remove(...)</c> on it, which raced under that load and
/// corrupted the underlying <see cref="Dictionary{TKey,TValue}"/>, producing
/// <see cref="InvalidOperationException"/> ("Operations that change
/// non-concurrent collections must have exclusive access...").
///
/// This implementation tracks per-command start timestamps in a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="CommandEventData.CommandId"/>, accumulates the running totals
/// with <see cref="Interlocked"/>, and snapshots them into
/// <c>HttpContext.Items</c> under a per-instance lock. Because the
/// interceptor is registered as Scoped, the lock and the instance fields are
/// per-request, so contention is bounded to a single request's overlapping
/// DB callbacks (typically 1-3).
/// </summary>
public class QueryTimingInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConcurrentDictionary<Guid, long> _startTimestamps = new();
    private readonly object _itemsWriteLock = new();
    private long _totalElapsedTicks;
    private int _queryCount;

    public QueryTimingInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        StartTiming(eventData);
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

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(eventData);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return new ValueTask<DbDataReader>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        StartTiming(eventData);
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

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(eventData);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return new ValueTask<int>(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        StartTiming(eventData);
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

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        StartTiming(eventData);
        return new ValueTask<InterceptionResult<object>>(result);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        StopTiming(eventData);
        return new ValueTask<object>(result);
    }

    private void StartTiming(CommandEventData eventData)
    {
        // Record the start timestamp under the command's unique id. Concurrent
        // commands within one request use distinct CommandId values, so they
        // do not contend on the same dictionary slot.
        _startTimestamps[eventData.CommandId] = Stopwatch.GetTimestamp();
    }

    private void StopTiming(CommandExecutedEventData eventData)
    {
        if (!_startTimestamps.TryRemove(eventData.CommandId, out var startTicks))
        {
            // No matching Start - e.g. the interceptor pipeline produced
            // an Executed event without an Executing one. Ignore.
            return;
        }

        var rawElapsed = Stopwatch.GetTimestamp() - startTicks;
        var elapsedTimeSpanTicks = rawElapsed * TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        var newTotalTicks = Interlocked.Add(ref _totalElapsedTicks, elapsedTimeSpanTicks);
        var newQueryCount = Interlocked.Increment(ref _queryCount);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // HttpContext.Items is a Dictionary - concurrent writes to it from
        // overlapping callbacks within the same request would corrupt the
        // dictionary. Guard the snapshot writes with the per-instance lock.
        // Because the interceptor is Scoped (one instance per request) and a
        // single request rarely has more than a handful of concurrent
        // commands, this lock has negligible contention.
        lock (_itemsWriteLock)
        {
            httpContext.Items["__TotalDbTime"] = new TimeSpan(newTotalTicks);
            httpContext.Items["__DbQueryCount"] = newQueryCount;
        }
    }
}
