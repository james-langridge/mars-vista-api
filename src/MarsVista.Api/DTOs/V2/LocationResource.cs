using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Location resource representing a unique site/drive location visited by a rover
/// </summary>
public record LocationResource
{
    /// <summary>
    /// Unique location identifier (e.g., "curiosity_79_1204")
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "location")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "location";

    /// <summary>
    /// Location attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public LocationAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Related links
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LocationLinks? Links { get; init; }
}

/// <summary>
/// Location attributes
/// </summary>
public record LocationAttributes
{
    /// <summary>
    /// Rover name
    /// </summary>
    [JsonPropertyName("rover")]
    public string Rover { get; init; } = string.Empty;

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
    /// Location name (if known)
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// First date this location was visited
    /// </summary>
    [JsonPropertyName("first_visited")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FirstVisited { get; init; }

    /// <summary>
    /// Last date this location was visited
    /// </summary>
    [JsonPropertyName("last_visited")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastVisited { get; init; }

    /// <summary>
    /// First sol this location was visited
    /// </summary>
    [JsonPropertyName("first_sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FirstSol { get; init; }

    /// <summary>
    /// Last sol this location was visited
    /// </summary>
    [JsonPropertyName("last_sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LastSol { get; init; }

    /// <summary>
    /// Total number of photos taken at this location
    /// </summary>
    [JsonPropertyName("photo_count")]
    public int PhotoCount { get; init; }

    /// <summary>
    /// Number of unique visits to this location
    /// </summary>
    [JsonPropertyName("visit_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VisitCount { get; init; }

    /// <summary>
    /// 3D coordinates of this location
    /// </summary>
    [JsonPropertyName("coordinates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoCoordinates? Coordinates { get; init; }
}

/// <summary>
/// Links related to the location
/// </summary>
public record LocationLinks
{
    /// <summary>
    /// Link to all photos at this location
    /// </summary>
    [JsonPropertyName("photos")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Photos { get; init; }

    /// <summary>
    /// Link to 360-degree view (future feature)
    /// </summary>
    [JsonPropertyName("360_view")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? View360 { get; init; }

    /// <summary>
    /// Link to time-lapse of this location (future feature)
    /// </summary>
    [JsonPropertyName("time_lapse")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TimeLapse { get; init; }
}
