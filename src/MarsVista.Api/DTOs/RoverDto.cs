using System.Text.Json.Serialization;

namespace MarsVista.Api.DTOs;

public record RoverDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("landing_date")]
    public string LandingDate { get; init; } = string.Empty;

    [JsonPropertyName("launch_date")]
    public string LaunchDate { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("max_sol")]
    public int MaxSol { get; init; }

    [JsonPropertyName("max_date")]
    public string MaxDate { get; init; } = string.Empty;

    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    [JsonPropertyName("cameras")]
    public List<CameraDto> Cameras { get; init; } = new();
}

public record CameraDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; init; } = string.Empty;
}

public record PhotoDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    [JsonPropertyName("camera")]
    public CameraDto Camera { get; init; } = new();

    [JsonPropertyName("img_src")]
    public string ImgSrc { get; init; } = string.Empty;

    [JsonPropertyName("earth_date")]
    public string EarthDate { get; init; } = string.Empty;

    [JsonPropertyName("rover")]
    public RoverSummaryDto Rover { get; init; } = new();
}

public record RoverSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("landing_date")]
    public string LandingDate { get; init; } = string.Empty;

    [JsonPropertyName("launch_date")]
    public string LaunchDate { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;
}

public record PhotoManifestDto
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("landing_date")]
    public string LandingDate { get; init; } = string.Empty;

    [JsonPropertyName("launch_date")]
    public string LaunchDate { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("max_sol")]
    public int MaxSol { get; init; }

    [JsonPropertyName("max_date")]
    public string MaxDate { get; init; } = string.Empty;

    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    [JsonPropertyName("photos")]
    public List<PhotosBySolDto> Photos { get; init; } = new();
}

public record PhotosBySolDto
{
    [JsonPropertyName("sol")]
    public int Sol { get; init; }

    [JsonPropertyName("total_photos")]
    public int TotalPhotos { get; init; }

    [JsonPropertyName("cameras")]
    public List<string> Cameras { get; init; } = new();
}
