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
public interface IEarthDateMonotonicityCheck
{
    /// <summary>
    /// Counts pairs of consecutive sols (per rover) where the earlier sol's
    /// latest earth_date exceeds the later sol's earliest. Zero means the
    /// invariant holds.
    /// </summary>
    Task<int> CountViolationsAsync(CancellationToken cancellationToken = default);
}

public class EarthDateMonotonicityCheck : IEarthDateMonotonicityCheck
{
    private const string ViolationCountSql = @"
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
SELECT COUNT(*) FROM paired WHERE max_date > next_sol_min_date";

    // Aggregates ~1.5M rows via the rover/sol covering index; well under this,
    // but the scrape runs unattended so give it headroom.
    private const int CommandTimeoutSeconds = 120;

    private readonly MarsVistaDbContext _context;

    public EarthDateMonotonicityCheck(MarsVistaDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountViolationsAsync(CancellationToken cancellationToken = default)
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
            command.CommandText = ViolationCountSql;
            command.CommandTimeout = CommandTimeoutSeconds;

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
