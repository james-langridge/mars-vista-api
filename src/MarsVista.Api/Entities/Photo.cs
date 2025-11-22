using System.Text.Json;

namespace MarsVista.Api.Entities;

public class Photo : ITimestamped
{
    // Primary identifiers
    public int Id { get; set; }
    public string NasaId { get; set; } = string.Empty;  // NASA's unique identifier

    // Core queryable fields (indexed columns)
    public int Sol { get; set; }
    public DateTime? EarthDate { get; set; }
    public DateTime DateTakenUtc { get; set; }
    public string? DateTakenMars { get; set; }  // "Sol-01646M15:18:15.866"

    // Image URLs (NASA provides multiple sizes)
    public string ImgSrcSmall { get; set; } = string.Empty;   // 320px wide (for thumbnails)
    public string ImgSrcMedium { get; set; } = string.Empty;  // 800px wide (for galleries)
    public string ImgSrcLarge { get; set; } = string.Empty;   // 1200px wide (for viewing)
    public string ImgSrcFull { get; set; } = string.Empty;    // Full resolution (for download)

    // Image properties
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string SampleType { get; set; } = string.Empty;  // "Full", "Thumbnail", "Subframe"

    // Location data (enables proximity search, panorama detection)
    public int? Site { get; set; }
    public int? Drive { get; set; }
    public string? Xyz { get; set; }  // "(35.4362,22.5714,-9.46445)" - rover position

    // Camera telemetry (enables panorama stitching)
    public float? MastAz { get; set; }        // Mast azimuth (horizontal angle)
    public float? MastEl { get; set; }        // Mast elevation (vertical angle)
    public string? CameraVector { get; set; }
    public string? CameraPosition { get; set; }
    public string? CameraModelType { get; set; }

    // Rover orientation
    public string? Attitude { get; set; }      // Quaternion orientation
    public float? SpacecraftClock { get; set; }

    // Metadata
    public string? Title { get; set; }
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public DateTime? DateReceived { get; set; }
    public string? FilterName { get; set; }

    // Foreign keys
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Navigation properties
    public virtual Rover Rover { get; set; } = null!;
    public virtual Camera Camera { get; set; } = null!;

    // JSONB storage for complete NASA response (30+ fields)
    // This stores the raw NASA JSON with all fields they provide
    // Enables future features without schema changes
    public JsonDocument? RawData { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
