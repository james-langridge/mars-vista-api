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
/// Thread safety: an EF Core interceptor's Reader/NonQuery/Scalar callbacks
/// can overlap within a single HTTP request whenever the application produces
/// a continuation that runs on a thread-pool thread distinct from the one
/// that issued the command (e.g. an unawaited <c>Task</c> path that lets the
/// next callback fire before the previous one returns). The previous
/// implementation stored a single stopwatch in <c>HttpContext.Items</c> under
/// the key <c>"__CurrentQueryStopwatch"</c> and called <c>Items.Remove(...)</c>
/// on it in StopTiming. <c>HttpContext.Items</c> is backed by a plain
/// <see cref="Dictionary{TKey,TValue}"/>, not <see cref="ConcurrentDictionary{TKey,TValue}"/>,
/// so two overlapping callbacks could race the dictionary's bucket structure
/// and throw
///
///   System.InvalidOperationException: Operations that change non-concurrent
///   collections must have exclusive access...
///
/// at <c>Dictionary.Remove</c> - reproduced in production via Sentry.
///
/// This implementation tracks per-command start timestamps in a
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// <see cref="CommandEventData.CommandId"/>, accumulates the running totals
/// with <see cref="Interlocked"/>, and snapshots them into
/// <c>HttpContext.Items</c> under a per-instance lock. Because the interceptor
/// is registered as Scoped, the lock and the instance fields are per-request,
/// so contention is bounded to a single request's overlapping DB callbacks.
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
        RecordStart(eventData.CommandId);
        return result;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RecordStop(eventData.CommandId);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        RecordStart(eventData.CommandId);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RecordStop(eventData.CommandId);
        return new ValueTask<DbDataReader>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        RecordStart(eventData.CommandId);
        return result;
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        RecordStop(eventData.CommandId);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        RecordStart(eventData.CommandId);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RecordStop(eventData.CommandId);
        return new ValueTask<int>(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        RecordStart(eventData.CommandId);
        return result;
    }

    public override object ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result)
    {
        RecordStop(eventData.CommandId);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        RecordStart(eventData.CommandId);
        return new ValueTask<InterceptionResult<object>>(result);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        RecordStop(eventData.CommandId);
        return new ValueTask<object>(result);
    }

    /// <summary>
    /// Record the start of a database command. Internal to allow concurrent
    /// regression tests to drive the timing logic without constructing EF's
    /// <see cref="CommandEventData"/>.
    /// </summary>
    internal void RecordStart(Guid commandId)
    {
        // Each concurrent command gets its own dictionary slot keyed by its
        // unique CommandId, so concurrent commands never contend on the same
        // slot.
        _startTimestamps[commandId] = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Record the end of a database command. See <see cref="RecordStart(Guid)"/>.
    /// </summary>
    internal void RecordStop(Guid commandId)
    {
        if (!_startTimestamps.TryRemove(commandId, out var startTimestamp))
        {
            // No matching Start - e.g. the interceptor pipeline produced
            // an Executed event without an Executing one. Ignore.
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
        Interlocked.Add(ref _totalElapsedTicks, elapsed.Ticks);
        Interlocked.Increment(ref _queryCount);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // HttpContext.Items is a Dictionary - concurrent writes from
        // overlapping callbacks within the same request would corrupt the
        // dictionary. Guard the snapshot writes with the per-instance lock,
        // and read the current totals *inside* the lock so the last lock
        // holder always publishes the most recent state (the Interlocked
        // ordering of Add/Increment is independent of lock acquisition
        // order, so capturing the return values outside the lock would let
        // an earlier holder overwrite a later one's snapshot).
        lock (_itemsWriteLock)
        {
            httpContext.Items["__TotalDbTime"] = new TimeSpan(Interlocked.Read(ref _totalElapsedTicks));
            httpContext.Items["__DbQueryCount"] = Volatile.Read(ref _queryCount);
        }
    }
}
