namespace MarsVista.Api.Services.V2;

/// <summary>
/// Thread-safe cache statistics tracking using atomic operations
/// </summary>
public class CacheMetrics
{
    private long _l1Hits;
    private long _l2Hits;
    private long _misses;
    private long _sets;
    private long _invalidations;

    public void RecordL1Hit() => Interlocked.Increment(ref _l1Hits);
    public void RecordL2Hit() => Interlocked.Increment(ref _l2Hits);
    public void RecordMiss() => Interlocked.Increment(ref _misses);
    public void RecordSet() => Interlocked.Increment(ref _sets);
    public void RecordInvalidation() => Interlocked.Increment(ref _invalidations);

    /// <summary>
    /// Get current cache statistics snapshot
    /// </summary>
    public CacheStats GetStats()
    {
        var l1Hits = Interlocked.Read(ref _l1Hits);
        var l2Hits = Interlocked.Read(ref _l2Hits);
        var misses = Interlocked.Read(ref _misses);
        var total = l1Hits + l2Hits + misses;

        return new CacheStats
        {
            L1Hits = l1Hits,
            L2Hits = l2Hits,
            Misses = misses,
            Sets = Interlocked.Read(ref _sets),
            Invalidations = Interlocked.Read(ref _invalidations),
            HitRate = total > 0 ? (l1Hits + l2Hits) / (double)total : 0
        };
    }

    /// <summary>
    /// Reset all counters (useful for periodic reporting)
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _l1Hits, 0);
        Interlocked.Exchange(ref _l2Hits, 0);
        Interlocked.Exchange(ref _misses, 0);
        Interlocked.Exchange(ref _sets, 0);
        Interlocked.Exchange(ref _invalidations, 0);
    }
}

/// <summary>
/// Snapshot of cache statistics at a point in time
/// </summary>
public record CacheStats
{
    public long L1Hits { get; init; }
    public long L2Hits { get; init; }
    public long Misses { get; init; }
    public long Sets { get; init; }
    public long Invalidations { get; init; }
    public double HitRate { get; init; }

    public long TotalHits => L1Hits + L2Hits;
    public long TotalRequests => L1Hits + L2Hits + Misses;

    public string HitRateFormatted => $"{HitRate:P1}";
}
