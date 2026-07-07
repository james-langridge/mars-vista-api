namespace MarsVista.Core.Entities;

/// <summary>
/// Monthly rollup of usage_events, populated by the retention job when raw
/// events older than the retention window are deleted. Lifetime statistics
/// remain queryable as SUM over this table plus the live usage_events window.
/// </summary>
public class UsageEventMonthly
{
    /// <summary>
    /// Unique identifier for the rollup row
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// First instant of the calendar month (UTC) this row aggregates
    /// </summary>
    public DateTime Month { get; set; }

    /// <summary>
    /// User's email address (from API key authentication)
    /// </summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint that was called
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// User's subscription tier at time of the requests
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Number of requests aggregated into this row
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Sum of response times in milliseconds (average = total / request_count)
    /// </summary>
    public long TotalResponseTimeMs { get; set; }

    /// <summary>
    /// Sum of photos returned across the aggregated requests
    /// </summary>
    public long TotalPhotosReturned { get; set; }

    /// <summary>
    /// Number of aggregated requests with status code >= 400
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// When this rollup row was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this rollup row last accumulated a new batch
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
