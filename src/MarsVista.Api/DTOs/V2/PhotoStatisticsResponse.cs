using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Response containing photo statistics and analytics
/// </summary>
public class PhotoStatisticsResponse
{
    /// <summary>
    /// Total count of photos in the period
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; set; }

    /// <summary>
    /// Time period for the statistics
    /// </summary>
    [JsonPropertyName("period")]
    public PeriodInfo Period { get; set; } = new();

    /// <summary>
    /// Statistics grouped by the requested dimension (camera, rover, or sol)
    /// </summary>
    [JsonPropertyName("groups")]
    public List<StatisticsGroup> Groups { get; set; } = new();
}

/// <summary>
/// Time period information
/// </summary>
public class PeriodInfo
{
    /// <summary>
    /// Start date of the period (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// End date of the period (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }
}

/// <summary>
/// A single group in the statistics response
/// </summary>
public class StatisticsGroup
{
    /// <summary>
    /// The grouping key (camera name, rover name, or sol number as string)
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Number of photos in this group
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total photos (0-100)
    /// </summary>
    [JsonPropertyName("percentage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Percentage { get; set; }

    /// <summary>
    /// Average photos per sol (only for rover grouping)
    /// </summary>
    [JsonPropertyName("avg_per_sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? AvgPerSol { get; set; }

    /// <summary>
    /// Earth date for this sol (only for sol grouping)
    /// </summary>
    [JsonPropertyName("earth_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EarthDate { get; set; }
}
