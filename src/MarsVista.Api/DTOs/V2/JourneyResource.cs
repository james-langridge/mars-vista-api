using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Journey resource representing a rover's path over a sol range
/// </summary>
public record JourneyResource
{
    /// <summary>
    /// Resource type (always "journey")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "journey";

    /// <summary>
    /// Journey attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public JourneyAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Path waypoints
    /// </summary>
    [JsonPropertyName("path")]
    public List<JourneyWaypoint> Path { get; init; } = new();

    /// <summary>
    /// Related links
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JourneyLinks? Links { get; init; }
}

/// <summary>
/// Journey attributes and statistics
/// </summary>
public record JourneyAttributes
{
    /// <summary>
    /// Rover name
    /// </summary>
    [JsonPropertyName("rover")]
    public string Rover { get; init; } = string.Empty;

    /// <summary>
    /// Starting sol
    /// </summary>
    [JsonPropertyName("sol_start")]
    public int SolStart { get; init; }

    /// <summary>
    /// Ending sol
    /// </summary>
    [JsonPropertyName("sol_end")]
    public int SolEnd { get; init; }

    /// <summary>
    /// Approximate distance traveled in kilometers
    /// </summary>
    [JsonPropertyName("distance_traveled_km")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? DistanceTraveledKm { get; init; }

    /// <summary>
    /// Number of unique locations visited
    /// </summary>
    [JsonPropertyName("locations_visited")]
    public int LocationsVisited { get; init; }

    /// <summary>
    /// Elevation change in meters (if calculable)
    /// </summary>
    [JsonPropertyName("elevation_change_m")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? ElevationChangeM { get; init; }

    /// <summary>
    /// Total photos taken during journey
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }
}

/// <summary>
/// A waypoint on the rover's journey
/// </summary>
public record JourneyWaypoint
{
    /// <summary>
    /// Sol at this waypoint
    /// </summary>
    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    /// <summary>
    /// Earth date at this waypoint
    /// </summary>
    [JsonPropertyName("earth_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EarthDate { get; init; }

    /// <summary>
    /// Site number
    /// </summary>
    [JsonPropertyName("site")]
    public int Site { get; init; }

    /// <summary>
    /// Drive number
    /// </summary>
    [JsonPropertyName("drive")]
    public int Drive { get; init; }

    /// <summary>
    /// 3D coordinates
    /// </summary>
    [JsonPropertyName("coordinates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoCoordinates? Coordinates { get; init; }

    /// <summary>
    /// Number of photos taken at this waypoint
    /// </summary>
    [JsonPropertyName("photos_taken")]
    public int PhotosTaken { get; init; }
}

/// <summary>
/// Links related to the journey
/// </summary>
public record JourneyLinks
{
    /// <summary>
    /// Link to map visualization (future feature)
    /// </summary>
    [JsonPropertyName("map_visualization")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MapVisualization { get; init; }

    /// <summary>
    /// Link to KML export (future feature)
    /// </summary>
    [JsonPropertyName("kml_export")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KmlExport { get; init; }
}
