using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Stereo pair resource representing matched left/right camera pairs
/// </summary>
public record StereoPairResource
{
    /// <summary>
    /// Unique stereo pair identifier (e.g., "stereo_123456")
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "stereo_pair")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "stereo_pair";

    /// <summary>
    /// Left camera photo
    /// </summary>
    [JsonPropertyName("left_photo")]
    public PhotoResource? LeftPhoto { get; init; }

    /// <summary>
    /// Right camera photo
    /// </summary>
    [JsonPropertyName("right_photo")]
    public PhotoResource? RightPhoto { get; init; }

    /// <summary>
    /// Stereo pair attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StereoPairAttributes? Attributes { get; init; }

    /// <summary>
    /// Related links
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public StereoPairLinks? Links { get; init; }
}

/// <summary>
/// Stereo pair attributes
/// </summary>
public record StereoPairAttributes
{
    /// <summary>
    /// Time difference between left and right photos in seconds
    /// </summary>
    [JsonPropertyName("time_delta_seconds")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? TimeDeltaSeconds { get; init; }

    /// <summary>
    /// Baseline distance between cameras in meters
    /// </summary>
    [JsonPropertyName("baseline_meters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? BaselineMeters { get; init; }

    /// <summary>
    /// Rover name
    /// </summary>
    [JsonPropertyName("rover")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Rover { get; init; }

    /// <summary>
    /// Sol when stereo pair was captured
    /// </summary>
    [JsonPropertyName("sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Sol { get; init; }
}

/// <summary>
/// Links related to the stereo pair
/// </summary>
public record StereoPairLinks
{
    /// <summary>
    /// Link to anaglyph 3D image (future feature)
    /// </summary>
    [JsonPropertyName("anaglyph")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Anaglyph { get; init; }

    /// <summary>
    /// Link to depth map (future feature)
    /// </summary>
    [JsonPropertyName("depth_map")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DepthMap { get; init; }
}
