using System.Data;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;

namespace MarsVista.Core.Services;

/// <summary>
/// Data-quality tripwire for the sol/earth_date ordering invariant: within one
/// rover, a later sol must never carry an earlier earth_date. The API's
/// sol-first sort optimization for earth_date sorts is order-preserving only
/// while this holds (see PhotoQueryServiceV2.ApplySorting). Legacy rows carry
/// NASA's sol-aligned attribution, but rows ingested by the current scrapers
/// derive earth_date from date_taken_utc - so a NASA timestamp anomaly
/// crossing UTC midnight would break the invariant silently. The daily scrape
/// runs this check so a break is loud instead.
/// </summary>
/// <summary>
/// Result of the invariant check. <paramref name="OrderingViolations"/> counts
/// consecutive-sol pairs (per rover) where the earlier sol's latest earth_date
/// exceeds the later sol's earliest. <paramref name="NullEarthDates"/> counts
/// photos with no earth_date at all - counted separately because SQL NULL
/// comparisons silently drop out of the ordering check, so NULLs would
/// otherwise blind it (an all-NULL sol even masks a real violation across it).
/// The invariant holds only when both are zero.
/// </summary>
public record EarthDateInvariantResult(int OrderingViolations, int NullEarthDates);

public interface IEarthDateMonotonicityCheck
{
    /// <summary>
    /// Checks the sol/earth_date ordering invariant across all photos.
    /// </summary>
    Task<EarthDateInvariantResult> CheckAsync(CancellationToken cancellationToken = default);
}

public class EarthDateMonotonicityCheck : IEarthDateMonotonicityCheck
{
    private const string InvariantCheckSql = @"
WITH per_sol AS (
    SELECT rover_id, sol, MIN(earth_date) AS min_date, MAX(earth_date) AS max_date
    FROM photos
    GROUP BY rover_id, sol
),
paired AS (
    SELECT max_date,
           LEAD(min_date) OVER (PARTITION BY rover_id ORDER BY sol) AS next_sol_min_date
    FROM per_sol
)
SELECT
    (SELECT COUNT(*) FROM paired WHERE max_date > next_sol_min_date) AS ordering_violations,
    (SELECT COUNT(*) FROM photos WHERE earth_date IS NULL) AS null_earth_dates";

    // One aggregate pass over ~1.5M rows (seq scan or index-only scan on the
    // rover/sol covering index; ~100 ms class either way) - the timeout is
    // generous headroom because the scrape runs unattended.
    private const int CommandTimeoutSeconds = 120;

    private readonly MarsVistaDbContext _context;

    public EarthDateMonotonicityCheck(MarsVistaDbContext context)
    {
        _context = context;
    }

    public async Task<EarthDateInvariantResult> CheckAsync(CancellationToken cancellationToken = default)
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
            command.CommandText = InvariantCheckSql;
            command.CommandTimeout = CommandTimeoutSeconds;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            return new EarthDateInvariantResult(
                Convert.ToInt32(reader.GetValue(0)),
                Convert.ToInt32(reader.GetValue(1)));
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
