namespace MarsVista.Api.DTOs;

public record RoverDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MaxSol { get; init; }
    public string MaxDate { get; init; } = string.Empty;
    public int TotalPhotos { get; init; }
    public List<CameraDto> Cameras { get; init; } = new();
}

public record CameraDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public record PhotoDto
{
    public int Id { get; init; }
    public int Sol { get; init; }
    public CameraDto Camera { get; init; } = new();
    public string ImgSrc { get; init; } = string.Empty;
    public string EarthDate { get; init; } = string.Empty;
    public RoverSummaryDto Rover { get; init; } = new();
}

public record RoverSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record PhotoManifestDto
{
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MaxSol { get; init; }
    public string MaxDate { get; init; } = string.Empty;
    public int TotalPhotos { get; init; }
    public List<PhotosBySolDto> Photos { get; init; } = new();
}

public record PhotosBySolDto
{
    public int Sol { get; init; }
    public int TotalPhotos { get; init; }
    public List<string> Cameras { get; init; } = new();
}
