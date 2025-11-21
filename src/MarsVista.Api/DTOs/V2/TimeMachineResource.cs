using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Time machine resource representing photos from the same location at different times
/// </summary>
public record TimeMachineResource
{
    /// <summary>
    /// Sol when photo was taken
    /// </summary>
    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    /// <summary>
    /// Earth date when photo was taken
    /// </summary>
    [JsonPropertyName("earth_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EarthDate { get; init; }

    /// <summary>
    /// Mars time when photo was taken
    /// </summary>
    [JsonPropertyName("mars_time")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarsTime { get; init; }

    /// <summary>
    /// The photo taken at this time
    /// </summary>
    [JsonPropertyName("photo")]
    public PhotoResource? Photo { get; init; }

    /// <summary>
    /// Lighting conditions at this time
    /// </summary>
    [JsonPropertyName("lighting_conditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LightingConditions { get; init; }
}

/// <summary>
/// Time machine query response
/// </summary>
public record TimeMachineResponse
{
    /// <summary>
    /// Location being viewed
    /// </summary>
    [JsonPropertyName("location")]
    public TimeMachineLocation Location { get; init; } = new();

    /// <summary>
    /// Photos from different times at this location
    /// </summary>
    [JsonPropertyName("data")]
    public List<TimeMachineResource> Data { get; init; } = new();

    /// <summary>
    /// Response metadata
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResponseMeta? Meta { get; init; }
}

/// <summary>
/// Location information for time machine query
/// </summary>
public record TimeMachineLocation
{
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
    /// Total visits to this location
    /// </summary>
    [JsonPropertyName("total_visits")]
    public int TotalVisits { get; init; }

    /// <summary>
    /// Total photos from this location
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }
}
