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

    /// <summary>
    /// Photo-specific computed metadata
    /// </summary>
    [JsonPropertyName("meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoMeta? Meta { get; init; }
}

/// <summary>
/// Photo-specific computed metadata
/// </summary>
public record PhotoMeta
{
    /// <summary>
    /// Whether this photo is part of a panorama sequence
    /// </summary>
    [JsonPropertyName("is_panorama_part")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsPanoramaPart { get; init; }

    /// <summary>
    /// Panorama sequence identifier if part of a panorama
    /// </summary>
    [JsonPropertyName("panorama_sequence_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PanoramaSequenceId { get; init; }

    /// <summary>
    /// Whether this photo has a stereo pair
    /// </summary>
    [JsonPropertyName("has_stereo_pair")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasStereoPair { get; init; }

    /// <summary>
    /// Stereo pair photo ID if available
    /// </summary>
    [JsonPropertyName("stereo_pair_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StereoPairId { get; init; }

    /// <summary>
    /// Lighting conditions (e.g., "golden_hour", "midday", "evening")
    /// </summary>
    [JsonPropertyName("lighting_conditions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LightingConditions { get; init; }

    /// <summary>
    /// Number of times rover visited this location
    /// </summary>
    [JsonPropertyName("location_visits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LocationVisits { get; init; }
}

/// <summary>
/// Photo attributes (data fields)
/// </summary>
public record PhotoAttributes
{
    // Basic fields
    /// <summary>
    /// NASA's unique identifier for this photo
    /// </summary>
    [JsonPropertyName("nasa_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NasaId { get; init; }

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
    /// Mars local time when photo was taken (e.g., "Sol-1000M14:23:45")
    /// </summary>
    [JsonPropertyName("date_taken_mars")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DateTakenMars { get; init; }

    // Multiple image sizes (NEW - nested structure)
    /// <summary>
    /// Multiple image URLs for different sizes
    /// </summary>
    [JsonPropertyName("images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoImages? Images { get; init; }

    /// <summary>
    /// Image dimensions
    /// </summary>
    [JsonPropertyName("dimensions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoDimensions? Dimensions { get; init; }

    /// <summary>
    /// Sample type (e.g., "Full", "Thumbnail", "Subframe")
    /// </summary>
    [JsonPropertyName("sample_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SampleType { get; init; }

    // Location data (NEW - nested structure)
    /// <summary>
    /// Location where photo was taken
    /// </summary>
    [JsonPropertyName("location")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoLocation? Location { get; init; }

    // Camera telemetry (NEW - nested structure)
    /// <summary>
    /// Camera telemetry data for panorama detection and stitching
    /// </summary>
    [JsonPropertyName("telemetry")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoTelemetry? Telemetry { get; init; }

    // Metadata
    /// <summary>
    /// Photo title
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; init; }

    /// <summary>
    /// Photo caption/description
    /// </summary>
    [JsonPropertyName("caption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; init; }

    /// <summary>
    /// Photo credit (e.g., "NASA/JPL-Caltech")
    /// </summary>
    [JsonPropertyName("credit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Credit { get; init; }

    /// <summary>
    /// When this photo was added to our database
    /// </summary>
    [JsonPropertyName("created_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? CreatedAt { get; init; }

    // Legacy field for backwards compatibility
    /// <summary>
    /// URL to the photo image (legacy field, use images.medium instead)
    /// </summary>
    [JsonPropertyName("img_src")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ImgSrc { get; init; }
}

/// <summary>
/// Multiple image URLs for different sizes
/// </summary>
public record PhotoImages
{
    /// <summary>
    /// Small size (320px wide) - for thumbnails
    /// </summary>
    [JsonPropertyName("small")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Small { get; init; }

    /// <summary>
    /// Medium size (800px wide) - for galleries
    /// </summary>
    [JsonPropertyName("medium")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Medium { get; init; }

    /// <summary>
    /// Large size (1200px wide) - for detailed viewing
    /// </summary>
    [JsonPropertyName("large")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Large { get; init; }

    /// <summary>
    /// Full resolution - for download and analysis
    /// </summary>
    [JsonPropertyName("full")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Full { get; init; }
}

/// <summary>
/// Image dimensions
/// </summary>
public record PhotoDimensions
{
    /// <summary>
    /// Image width in pixels
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }
}

/// <summary>
/// Location where photo was taken
/// </summary>
public record PhotoLocation
{
    /// <summary>
    /// Site number (geological location marker)
    /// </summary>
    [JsonPropertyName("site")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Site { get; init; }

    /// <summary>
    /// Drive number (rover's drive sequence)
    /// </summary>
    [JsonPropertyName("drive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Drive { get; init; }

    /// <summary>
    /// 3D coordinates of rover position
    /// </summary>
    [JsonPropertyName("coordinates")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PhotoCoordinates? Coordinates { get; init; }
}

/// <summary>
/// 3D coordinates
/// </summary>
public record PhotoCoordinates
{
    /// <summary>
    /// X coordinate
    /// </summary>
    [JsonPropertyName("x")]
    public float X { get; init; }

    /// <summary>
    /// Y coordinate
    /// </summary>
    [JsonPropertyName("y")]
    public float Y { get; init; }

    /// <summary>
    /// Z coordinate
    /// </summary>
    [JsonPropertyName("z")]
    public float Z { get; init; }
}

/// <summary>
/// Camera telemetry data
/// </summary>
public record PhotoTelemetry
{
    /// <summary>
    /// Mast azimuth angle (horizontal rotation in degrees)
    /// </summary>
    [JsonPropertyName("mast_azimuth")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MastAzimuth { get; init; }

    /// <summary>
    /// Mast elevation angle (vertical tilt in degrees)
    /// </summary>
    [JsonPropertyName("mast_elevation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? MastElevation { get; init; }

    /// <summary>
    /// Spacecraft clock at time of capture
    /// </summary>
    [JsonPropertyName("spacecraft_clock")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? SpacecraftClock { get; init; }
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
