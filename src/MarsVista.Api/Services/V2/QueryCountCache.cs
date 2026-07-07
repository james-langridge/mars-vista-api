using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Entities;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Serves pagination total counts from the two-level cache instead of running
/// COUNT(*) per request. A rover-filtered count scans ~500K index entries
/// (~184 MB of buffer reads) on production, and gallery browsing repeats the
/// same filter shapes constantly, so counts are cached for up to an hour.
/// Photos only change via the daily 2 AM scraper, so a stale count is at most
/// one sol's worth of photos behind.
/// </summary>
public interface IQueryCountCache
{
    /// <summary>
    /// Returns the row count of <paramref name="query"/>, served from cache
    /// when an identical query shape was counted within the TTL.
    /// </summary>
    Task<int> GetOrSetCountAsync(IQueryable<Photo> query, CancellationToken cancellationToken = default);
}

public class QueryCountCache : IQueryCountCache
{
    private readonly ICachingServiceV2 _cachingService;

    public QueryCountCache(ICachingServiceV2 cachingService)
    {
        _cachingService = cachingService;
    }

    public async Task<int> GetOrSetCountAsync(IQueryable<Photo> query, CancellationToken cancellationToken = default)
    {
        // ToQueryString() renders the query's SQL with its parameter values, so the
        // key automatically incorporates every filter BuildQuery applied - no
        // hand-maintained parameter list to drift when a filter is added. The SQL
        // text changes across EF/Npgsql upgrades, which merely rotates keys (cold
        // misses for one TTL window).
        var key = _cachingService.GenerateCacheKey("photocount", HashQuery(query.ToQueryString()));

        var cached = await _cachingService.GetOrSetAsync(
            key,
            async () => new CachedCount(await query.CountAsync(cancellationToken)));

        return cached?.Value ?? await query.CountAsync(cancellationToken);
    }

    private static string HashQuery(string sql)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sql));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    /// <summary>
    /// Wrapper because <see cref="ICachingServiceV2.GetOrSetAsync{T}"/> requires a
    /// reference type.
    /// </summary>
    private sealed record CachedCount(int Value);
}
