using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Photo resource in v2 API format with clear separation of attributes and relationships
/// </summary>
public record PhotoResource
{
    /// <summary>
    /// Unique photo identifier
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>
    /// Resource type (always "photo")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "photo";

    /// <summary>
    /// Photo attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public PhotoAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Related resources (rover, camera)
    /// Only included when requested via ?include= parameter
    /// </summary>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoRelationships? Relationships { get; init; }
}

/// <summary>
/// Photo attributes (data fields)
/// </summary>
public record PhotoAttributes
{
    /// <summary>
    /// URL to the photo image
    /// </summary>
    [JsonPropertyName("img_src")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrc { get; init; }

    /// <summary>
    /// Mars sol (day) when photo was taken
    /// </summary>
    [JsonPropertyName("sol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Sol { get; init; }

    /// <summary>
    /// Earth date when photo was taken (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("earth_date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EarthDate { get; init; }

    /// <summary>
    /// UTC timestamp when photo was taken
    /// </summary>
    [JsonPropertyName("date_taken_utc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? DateTakenUtc { get; init; }

    /// <summary>
    /// Mars local time when photo was taken
    /// </summary>
    [JsonPropertyName("date_taken_mars")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DateTakenMars { get; init; }

    /// <summary>
    /// Image dimensions
    /// </summary>
    [JsonPropertyName("width")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Width { get; init; }

    [JsonPropertyName("height")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Height { get; init; }

    /// <summary>
    /// Sample type (e.g., "Full", "Thumbnail")
    /// </summary>
    [JsonPropertyName("sample_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SampleType { get; init; }

    /// <summary>
    /// Multiple image sizes when available
    /// </summary>
    [JsonPropertyName("img_src_small")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrcSmall { get; init; }

    [JsonPropertyName("img_src_medium")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrcMedium { get; init; }

    [JsonPropertyName("img_src_large")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrcLarge { get; init; }

    [JsonPropertyName("img_src_full")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrcFull { get; init; }

    /// <summary>
    /// Location data
    /// </summary>
    [JsonPropertyName("site")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Site { get; init; }

    [JsonPropertyName("drive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Drive { get; init; }

    [JsonPropertyName("xyz")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Xyz { get; init; }

    /// <summary>
    /// Camera telemetry
    /// </summary>
    [JsonPropertyName("mast_az")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MastAz { get; init; }

    [JsonPropertyName("mast_el")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MastEl { get; init; }

    /// <summary>
    /// Photo metadata
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    [JsonPropertyName("caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; init; }

    /// <summary>
    /// When this photo was added to our database
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; init; }
}

/// <summary>
/// Photo relationships (related resources)
/// </summary>
public record PhotoRelationships
{
    /// <summary>
    /// The rover that took this photo
    /// </summary>
    [JsonPropertyName("rover")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResourceReference? Rover { get; init; }

    /// <summary>
    /// The camera that took this photo
    /// </summary>
    [JsonPropertyName("camera")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CameraReference? Camera { get; init; }
}

/// <summary>
/// Reference to a related resource
/// </summary>
public record ResourceReference
{
    /// <summary>
    /// Resource identifier (slug for rovers, id for others)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Attributes of the related resource (when included)
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Attributes { get; init; }
}

/// <summary>
/// Camera reference with attributes
/// </summary>
public record CameraReference
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
    /// Camera attributes (when included)
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CameraAttributes? Attributes { get; init; }
}

/// <summary>
/// Camera attributes
/// </summary>
public record CameraAttributes
{
    /// <summary>
    /// Full camera name
    /// </summary>
    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Number of photos from this camera (when included in statistics)
    /// </summary>
    [JsonPropertyName("photo_count")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? PhotoCount { get; init; }
}
