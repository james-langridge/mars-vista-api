using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Panorama resource representing an auto-detected panoramic sequence
/// </summary>
public record PanoramaResource
{
    /// <summary>
    /// Unique panorama identifier (e.g., "pano_curiosity_1000_14")
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "panorama")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "panorama";

    /// <summary>
    /// Panorama attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public PanoramaAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Photos that make up this panorama
    /// </summary>
    [JsonPropertyName("photos")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PhotoResource>? Photos { get; init; }

    /// <summary>
    /// Related links
    /// </summary>
    [JsonPropertyName("links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PanoramaLinks? Links { get; init; }
}

/// <summary>
/// Panorama attributes
/// </summary>
public record PanoramaAttributes
{
    /// <summary>
    /// Rover name
    /// </summary>
    [JsonPropertyName("rover")]
    public string Rover { get; init; } = string.Empty;

    /// <summary>
    /// Sol when panorama was taken
    /// </summary>
    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    /// <summary>
    /// Mars time when panorama sequence started
    /// </summary>
    [JsonPropertyName("mars_time_start")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarsTimeStart { get; init; }

    /// <summary>
    /// Mars time when panorama sequence ended
    /// </summary>
    [JsonPropertyName("mars_time_end")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarsTimeEnd { get; init; }

    /// <summary>
    /// Total number of photos in the panorama
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    /// <summary>
    /// Approximate angular coverage in degrees
    /// </summary>
    [JsonPropertyName("coverage_degrees")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? CoverageDegrees { get; init; }

    /// <summary>
    /// Location where panorama was taken
    /// </summary>
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoLocation? Location { get; init; }

    /// <summary>
    /// Camera used for the panorama
    /// </summary>
    [JsonPropertyName("camera")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Camera { get; init; }

    /// <summary>
    /// Average elevation angle
    /// </summary>
    [JsonPropertyName("avg_elevation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? AvgElevation { get; init; }
}

/// <summary>
/// Links related to the panorama
/// </summary>
public record PanoramaLinks
{
    /// <summary>
    /// Link to panorama preview/stitched image (future feature)
    /// </summary>
    [JsonPropertyName("stitched_preview")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StitchedPreview { get; init; }

    /// <summary>
    /// Link to download all photos as a set
    /// </summary>
    [JsonPropertyName("download_set")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DownloadSet { get; init; }
}
