using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Camera resource in v2 API format
/// </summary>
public record CameraResource
{
    /// <summary>
    /// Camera identifier (name like "FHAZ", "MAST")
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "camera")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "camera";

    /// <summary>
    /// Camera attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public CameraResourceAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Related resources
    /// </summary>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CameraRelationships? Relationships { get; init; }
}

/// <summary>
/// Camera attributes
/// </summary>
public record CameraResourceAttributes
{
    /// <summary>
    /// Camera name (e.g., "FHAZ", "MAST")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Full camera name (e.g., "Front Hazard Avoidance Camera")
    /// </summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Total number of photos from this camera
    /// </summary>
    [JsonPropertyName("photo_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PhotoCount { get; init; }

    /// <summary>
    /// First sol with photos from this camera
    /// </summary>
    [JsonPropertyName("first_photo_sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FirstPhotoSol { get; init; }

    /// <summary>
    /// Last sol with photos from this camera
    /// </summary>
    [JsonPropertyName("last_photo_sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LastPhotoSol { get; init; }
}

/// <summary>
/// Camera relationships
/// </summary>
public record CameraRelationships
{
    /// <summary>
    /// The rover this camera is mounted on
    /// </summary>
    [JsonPropertyName("rover")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResourceReference? Rover { get; init; }
}
