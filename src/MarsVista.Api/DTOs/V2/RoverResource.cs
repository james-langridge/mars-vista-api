using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs.V2;

/// <summary>
/// Rover resource in v2 API format
/// </summary>
public record RoverResource
{
    /// <summary>
    /// Rover identifier (slug: curiosity, perseverance, opportunity, spirit)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "rover")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "rover";

    /// <summary>
    /// Rover attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public RoverAttributes Attributes { get; init; } = new();

    /// <summary>
    /// Related resources
    /// </summary>
    [JsonPropertyName("relationships")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RoverRelationships? Relationships { get; init; }
}

/// <summary>
/// Rover attributes
/// </summary>
public record RoverAttributes
{
    /// <summary>
    /// Rover name (capitalized)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Landing date on Mars (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("landing_date")]
    public string LandingDate { get; init; } = string.Empty;

    /// <summary>
    /// Launch date from Earth (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("launch_date")]
    public string LaunchDate { get; init; } = string.Empty;

    /// <summary>
    /// Mission status (active, complete)
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Maximum sol reached by this rover
    /// </summary>
    [JsonPropertyName("max_sol")]
    public int MaxSol { get; init; }

    /// <summary>
    /// Most recent photo date (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("max_date")]
    public string MaxDate { get; init; } = string.Empty;

    /// <summary>
    /// Total number of photos in our database
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }
}

/// <summary>
/// Rover relationships
/// </summary>
public record RoverRelationships
{
    /// <summary>
    /// Cameras on this rover
    /// </summary>
    [JsonPropertyName("cameras")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CameraResource>? Cameras { get; init; }
}

/// <summary>
/// Rover manifest (photo history by sol)
/// </summary>
public record RoverManifest
{
    /// <summary>
    /// Rover identifier (slug)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Resource type (always "manifest")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "manifest";

    /// <summary>
    /// Manifest attributes
    /// </summary>
    [JsonPropertyName("attributes")]
    public ManifestAttributes Attributes { get; init; } = new();
}

/// <summary>
/// Manifest attributes
/// </summary>
public record ManifestAttributes
{
    /// <summary>
    /// Rover name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Landing date (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("landing_date")]
    public string LandingDate { get; init; } = string.Empty;

    /// <summary>
    /// Launch date (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("launch_date")]
    public string LaunchDate { get; init; } = string.Empty;

    /// <summary>
    /// Mission status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Maximum sol
    /// </summary>
    [JsonPropertyName("max_sol")]
    public int MaxSol { get; init; }

    /// <summary>
    /// Most recent photo date
    /// </summary>
    [JsonPropertyName("max_date")]
    public string MaxDate { get; init; } = string.Empty;

    /// <summary>
    /// Total photos
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    /// <summary>
    /// Photo counts by sol
    /// </summary>
    [JsonPropertyName("photos")]
    public List<PhotosBySol> Photos { get; init; } = new();
}

/// <summary>
/// Photos taken on a specific sol
/// </summary>
public record PhotosBySol
{
    /// <summary>
    /// Mars sol number
    /// </summary>
    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    /// <summary>
    /// Earth date (YYYY-MM-DD)
    /// </summary>
    [JsonPropertyName("earth_date")]
    public string EarthDate { get; init; } = string.Empty;

    /// <summary>
    /// Total photos taken on this sol
    /// </summary>
    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    /// <summary>
    /// Cameras that took photos on this sol
    /// </summary>
    [JsonPropertyName("cameras")]
    public List<string> Cameras { get; init; } = new();
}
