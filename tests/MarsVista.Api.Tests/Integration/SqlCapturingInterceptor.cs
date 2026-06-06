using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// EF Core DbCommandInterceptor that records every SQL command text the
/// DbContext emits, so tests can assert on what the production code path
/// actually produced (rather than asserting on a separately-constructed
/// IQueryable that mirrors what BuildQuery is *supposed* to do).
///
/// Register on the DbContext via .AddInterceptors(...). Read captured SQL
/// from <see cref="ExecutedSql"/>. Call <see cref="Clear"/> between scenarios.
/// </summary>
public class SqlCapturingInterceptor : DbCommandInterceptor
{
    private readonly object _gate = new();
    private readonly List<string> _executed = new();

    public IReadOnlyList<string> ExecutedSql
    {
        get
        {
            lock (_gate) return _executed.ToArray();
        }
    }

    public void Clear()
    {
        lock (_gate) _executed.Clear();
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return new ValueTask<InterceptionResult<object>>(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void Record(DbCommand command)
    {
        lock (_gate) _executed.Add(command.CommandText);
    }
}
