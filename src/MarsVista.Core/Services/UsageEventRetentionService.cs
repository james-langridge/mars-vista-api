using System.Data;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;

namespace MarsVista.Core.Services;

/// <summary>
/// Deletes usage_events older than a retention window, aggregating the deleted
/// rows into the usage_events_monthly rollup in the same atomic statement so
/// lifetime statistics survive the purge.
/// </summary>
public interface IUsageEventRetentionService
{
    /// <summary>
    /// Purges events older than <paramref name="retentionDays"/> and rolls their
    /// statistics into usage_events_monthly. Returns the number of events deleted.
    /// </summary>
    Task<int> PurgeAndRollUpAsync(int retentionDays = 90, CancellationToken cancellationToken = default);
}

public class UsageEventRetentionService : IUsageEventRetentionService
{
    // Single atomic statement: the DELETE ... RETURNING feeds the rollup INSERT,
    // and the outer SELECT reports how many rows were purged. Data-modifying CTEs
    // always run to completion even when the primary query does not reference
    // them, so `rolled` executes whether or not any rows were deleted.
    private const string PurgeAndRollUpSql = @"
WITH deleted AS (
    DELETE FROM usage_events
    WHERE created_at < NOW() - make_interval(days => @retention_days)
    RETURNING user_email, endpoint, tier, response_time_ms, photos_returned, status_code, created_at
),
rolled AS (
    INSERT INTO usage_events_monthly
        (month, user_email, endpoint, tier, request_count,
         total_response_time_ms, total_photos_returned, error_count, created_at, updated_at)
    SELECT
        date_trunc('month', created_at),
        user_email, endpoint, tier,
        COUNT(*),
        SUM(response_time_ms),
        SUM(photos_returned),
        COUNT(*) FILTER (WHERE status_code >= 400),
        NOW(), NOW()
    FROM deleted
    GROUP BY date_trunc('month', created_at), user_email, endpoint, tier
    ON CONFLICT (month, user_email, endpoint, tier) DO UPDATE SET
        request_count = usage_events_monthly.request_count + EXCLUDED.request_count,
        total_response_time_ms = usage_events_monthly.total_response_time_ms + EXCLUDED.total_response_time_ms,
        total_photos_returned = usage_events_monthly.total_photos_returned + EXCLUDED.total_photos_returned,
        error_count = usage_events_monthly.error_count + EXCLUDED.error_count,
        updated_at = NOW()
)
SELECT COUNT(*) FROM deleted";

    // The first purge deletes the entire backlog in one transaction; give it
    // generous headroom over the default command timeout.
    private const int CommandTimeoutSeconds = 300;

    private readonly MarsVistaDbContext _context;

    public UsageEventRetentionService(MarsVistaDbContext context)
    {
        _context = context;
    }

    public async Task<int> PurgeAndRollUpAsync(int retentionDays = 90, CancellationToken cancellationToken = default)
    {
        var connection = _context.Database.GetDbConnection();

        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = PurgeAndRollUpSql;
            command.CommandTimeout = CommandTimeoutSeconds;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "retention_days";
            parameter.Value = retentionDays;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
