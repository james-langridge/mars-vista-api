namespace MarsVista.Core.Entities;

/// <summary>
/// Represents an official NASA rover position waypoint from the PDS localization data.
/// Coordinates are relative to the rover's landing site.
/// Source: https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/best_tactical.csv
/// </summary>
public class RoverWaypoint : ITimestamped
{
    public int Id { get; set; }

    // Foreign key to rover
    public int RoverId { get; set; }

    // Waypoint identification
    public string Frame { get; set; } = string.Empty;  // "SITE" or "ROVER"
    public int Site { get; set; }       // Site number (major location)
    public int? Drive { get; set; }     // Drive number within site (null for SITE frames)
    public int? Sol { get; set; }       // Mars sol when this position was recorded

    // Landing-relative coordinates (meters)
    // These form a consistent global reference frame for the entire mission
    public float LandingX { get; set; }
    public float LandingY { get; set; }
    public float LandingZ { get; set; }

    // Geographic coordinates
    public double? Latitude { get; set; }   // Planetocentric latitude
    public double? Longitude { get; set; }
    public float? Elevation { get; set; }   // Meters relative to areoid

    // Navigation properties
    public virtual Rover Rover { get; set; } = null!;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
