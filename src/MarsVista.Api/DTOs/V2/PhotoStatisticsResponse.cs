namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Response containing photo statistics and analytics
/// </summary>
public class PhotoStatisticsResponse
{
    /// <summary>
    /// Time period for the statistics
    /// </summary>
    public PeriodInfo Period { get; set; } = new();

    /// <summary>
    /// Total count of photos in the period
    /// </summary>
    public int TotalPhotos { get; set; }

    /// <summary>
    /// Statistics grouped by camera (if requested)
    /// </summary>
    public List<CameraStatistics>? ByCamera { get; set; }

    /// <summary>
    /// Statistics grouped by rover (if requested)
    /// </summary>
    public List<RoverStatistics>? ByRover { get; set; }

    /// <summary>
    /// Statistics grouped by sol (if requested)
    /// </summary>
    public List<SolStatistics>? BySol { get; set; }
}

/// <summary>
/// Time period information
/// </summary>
public class PeriodInfo
{
    /// <summary>
    /// Start date of the period
    /// </summary>
    public string? From { get; set; }

    /// <summary>
    /// End date of the period
    /// </summary>
    public string? To { get; set; }
}

/// <summary>
/// Camera-level statistics
/// </summary>
public class CameraStatistics
{
    /// <summary>
    /// Camera name
    /// </summary>
    public string Camera { get; set; } = string.Empty;

    /// <summary>
    /// Number of photos
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total photos
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// Rover-level statistics
/// </summary>
public class RoverStatistics
{
    /// <summary>
    /// Rover name
    /// </summary>
    public string Rover { get; set; } = string.Empty;

    /// <summary>
    /// Number of photos
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total photos
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Average photos per sol
    /// </summary>
    public double AvgPerSol { get; set; }
}

/// <summary>
/// Sol-level statistics
/// </summary>
public class SolStatistics
{
    /// <summary>
    /// Sol number
    /// </summary>
    public int Sol { get; set; }

    /// <summary>
    /// Number of photos taken on this sol
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Earth date for this sol
    /// </summary>
    public string? EarthDate { get; set; }
}
