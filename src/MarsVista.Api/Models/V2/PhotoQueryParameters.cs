using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Models.V2;

/// <summary>
/// Query parameters for the unified photos endpoint
/// Supports powerful filtering across multiple rovers and cameras
/// </summary>
public class PhotoQueryParameters
{
    /// <summary>
    /// Search by NASA ID (partial match, case-insensitive)
    /// Example: nasa_id=NLB_780234
    /// </summary>
    [FromQuery(Name = "nasa_id")]
    public string? NasaId { get; set; }

    /// <summary>
    /// Filter by one or more rovers (comma-separated)
    /// Example: rovers=curiosity,perseverance
    /// </summary>
    [FromQuery(Name = "rovers")]
    public string? Rovers { get; set; }

    /// <summary>
    /// Filter by one or more cameras (comma-separated)
    /// Example: cameras=FHAZ,NAVCAM,MAST
    /// </summary>
    [FromQuery(Name = "cameras")]
    public string? Cameras { get; set; }

    /// <summary>
    /// Minimum sol (inclusive)
    /// </summary>
    [FromQuery(Name = "sol_min")]
    [Range(0, int.MaxValue, ErrorMessage = "sol_min must be >= 0")]
    public int? SolMin { get; set; }

    /// <summary>
    /// Maximum sol (inclusive)
    /// </summary>
    [FromQuery(Name = "sol_max")]
    [Range(0, int.MaxValue, ErrorMessage = "sol_max must be >= 0")]
    public int? SolMax { get; set; }

    /// <summary>
    /// Exact sol (shorthand for sol_min=X&sol_max=X)
    /// </summary>
    [FromQuery(Name = "sol")]
    [Range(0, int.MaxValue, ErrorMessage = "sol must be >= 0")]
    public int? Sol { get; set; }

    /// <summary>
    /// Minimum earth date (inclusive, YYYY-MM-DD format)
    /// </summary>
    [FromQuery(Name = "date_min")]
    public string? DateMin { get; set; }

    /// <summary>
    /// Maximum earth date (inclusive, YYYY-MM-DD format)
    /// </summary>
    [FromQuery(Name = "date_max")]
    public string? DateMax { get; set; }

    /// <summary>
    /// Exact earth date (shorthand, YYYY-MM-DD format)
    /// </summary>
    [FromQuery(Name = "earth_date")]
    public string? EarthDate { get; set; }

    /// <summary>
    /// Sort fields (comma-separated, prefix with - for descending)
    /// Example: sort=-earth_date,camera
    /// </summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    /// <summary>
    /// Fields to include (comma-separated, for sparse fieldsets)
    /// Example: fields=id,img_src,sol,earth_date
    /// </summary>
    [FromQuery(Name = "fields")]
    public string? Fields { get; set; }

    /// <summary>
    /// Related resources to include (comma-separated)
    /// Example: include=rover,camera
    /// </summary>
    [FromQuery(Name = "include")]
    public string? Include { get; set; }

    /// <summary>
    /// Page number (1-indexed, default: 1)
    /// </summary>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "page must be >= 1")]
    public int? Page { get; set; }

    /// <summary>
    /// Items per page (default: 25, max: 100)
    /// </summary>
    [FromQuery(Name = "per_page")]
    [Range(1, 100, ErrorMessage = "per_page must be between 1 and 100")]
    public int? PerPage { get; set; }

    /// <summary>
    /// Cursor for cursor-based pagination (base64-encoded)
    /// </summary>
    [FromQuery(Name = "cursor")]
    public string? Cursor { get; set; }

    // Mars time filtering
    /// <summary>
    /// Minimum Mars local time (format: Mhh:mm:ss or hh:mm:ss)
    /// Example: M06:00:00 for 6 AM Mars time
    /// </summary>
    [FromQuery(Name = "mars_time_min")]
    public string? MarsTimeMin { get; set; }

    /// <summary>
    /// Maximum Mars local time (format: Mhh:mm:ss or hh:mm:ss)
    /// Example: M18:00:00 for 6 PM Mars time
    /// </summary>
    [FromQuery(Name = "mars_time_max")]
    public string? MarsTimeMax { get; set; }

    /// <summary>
    /// Filter for golden hour photos (sunrise/sunset lighting)
    /// </summary>
    [FromQuery(Name = "mars_time_golden_hour")]
    public bool? MarsTimeGoldenHour { get; set; }

    // Location-based queries
    /// <summary>
    /// Exact site number
    /// </summary>
    [FromQuery(Name = "site")]
    [Range(0, int.MaxValue, ErrorMessage = "site must be >= 0")]
    public int? Site { get; set; }

    /// <summary>
    /// Minimum site number (inclusive)
    /// </summary>
    [FromQuery(Name = "site_min")]
    [Range(0, int.MaxValue, ErrorMessage = "site_min must be >= 0")]
    public int? SiteMin { get; set; }

    /// <summary>
    /// Maximum site number (inclusive)
    /// </summary>
    [FromQuery(Name = "site_max")]
    [Range(0, int.MaxValue, ErrorMessage = "site_max must be >= 0")]
    public int? SiteMax { get; set; }

    /// <summary>
    /// Exact drive number
    /// </summary>
    [FromQuery(Name = "drive")]
    [Range(0, int.MaxValue, ErrorMessage = "drive must be >= 0")]
    public int? Drive { get; set; }

    /// <summary>
    /// Minimum drive number (inclusive)
    /// </summary>
    [FromQuery(Name = "drive_min")]
    [Range(0, int.MaxValue, ErrorMessage = "drive_min must be >= 0")]
    public int? DriveMin { get; set; }

    /// <summary>
    /// Maximum drive number (inclusive)
    /// </summary>
    [FromQuery(Name = "drive_max")]
    [Range(0, int.MaxValue, ErrorMessage = "drive_max must be >= 0")]
    public int? DriveMax { get; set; }

    /// <summary>
    /// Location proximity radius in drives (used with site and drive)
    /// Example: site=79&drive=1204&location_radius=5
    /// </summary>
    [FromQuery(Name = "location_radius")]
    [Range(0, 1000, ErrorMessage = "location_radius must be between 0 and 1000")]
    public int? LocationRadius { get; set; }

    // Image quality filters
    /// <summary>
    /// Minimum image width in pixels
    /// </summary>
    [FromQuery(Name = "min_width")]
    [Range(1, 10000, ErrorMessage = "min_width must be between 1 and 10000")]
    public int? MinWidth { get; set; }

    /// <summary>
    /// Minimum image height in pixels
    /// </summary>
    [FromQuery(Name = "min_height")]
    [Range(1, 10000, ErrorMessage = "min_height must be between 1 and 10000")]
    public int? MinHeight { get; set; }

    /// <summary>
    /// Maximum image width in pixels
    /// </summary>
    [FromQuery(Name = "max_width")]
    [Range(1, 10000, ErrorMessage = "max_width must be between 1 and 10000")]
    public int? MaxWidth { get; set; }

    /// <summary>
    /// Maximum image height in pixels
    /// </summary>
    [FromQuery(Name = "max_height")]
    [Range(1, 10000, ErrorMessage = "max_height must be between 1 and 10000")]
    public int? MaxHeight { get; set; }

    /// <summary>
    /// Filter by sample type (e.g., "Full", "Thumbnail", "Subframe")
    /// Comma-separated for multiple types
    /// </summary>
    [FromQuery(Name = "sample_type")]
    public string? SampleType { get; set; }

    /// <summary>
    /// Filter by aspect ratio (e.g., "16:9", "4:3", "1:1")
    /// </summary>
    [FromQuery(Name = "aspect_ratio")]
    public string? AspectRatio { get; set; }

    // Camera angle queries
    /// <summary>
    /// Minimum mast elevation angle in degrees
    /// Negative = looking down, Positive = looking up
    /// </summary>
    [FromQuery(Name = "mast_elevation_min")]
    [Range(-90, 90, ErrorMessage = "mast_elevation_min must be between -90 and 90")]
    public float? MastElevationMin { get; set; }

    /// <summary>
    /// Maximum mast elevation angle in degrees
    /// </summary>
    [FromQuery(Name = "mast_elevation_max")]
    [Range(-90, 90, ErrorMessage = "mast_elevation_max must be between -90 and 90")]
    public float? MastElevationMax { get; set; }

    /// <summary>
    /// Minimum mast azimuth angle in degrees (0-360)
    /// </summary>
    [FromQuery(Name = "mast_azimuth_min")]
    [Range(0, 360, ErrorMessage = "mast_azimuth_min must be between 0 and 360")]
    public float? MastAzimuthMin { get; set; }

    /// <summary>
    /// Maximum mast azimuth angle in degrees (0-360)
    /// </summary>
    [FromQuery(Name = "mast_azimuth_max")]
    [Range(0, 360, ErrorMessage = "mast_azimuth_max must be between 0 and 360")]
    public float? MastAzimuthMax { get; set; }

    // Field selection control
    /// <summary>
    /// Field set preset (minimal, standard, extended, scientific, complete)
    /// Can also be comma-separated field names for custom selection
    /// </summary>
    [FromQuery(Name = "field_set")]
    public string? FieldSet { get; set; }

    /// <summary>
    /// Specific image sizes to include (comma-separated: small, medium, large, full)
    /// Default: all sizes
    /// </summary>
    [FromQuery(Name = "image_sizes")]
    public string? ImageSizes { get; set; }

    /// <summary>
    /// Exclude images entirely (metadata only)
    /// </summary>
    [FromQuery(Name = "exclude_images")]
    public bool? ExcludeImages { get; set; }

    // Parsed values (populated during validation)

    /// <summary>
    /// Parsed rover names (lowercase)
    /// </summary>
    public List<string> RoverList { get; set; } = new();

    /// <summary>
    /// Parsed camera names (uppercase)
    /// </summary>
    public List<string> CameraList { get; set; } = new();

    /// <summary>
    /// Parsed sort fields with direction
    /// </summary>
    public List<SortField> SortFields { get; set; } = new();

    /// <summary>
    /// Parsed field names for sparse fieldsets
    /// </summary>
    public List<string> FieldList { get; set; } = new();

    /// <summary>
    /// Parsed include resource names
    /// </summary>
    public List<string> IncludeList { get; set; } = new();

    /// <summary>
    /// Parsed minimum date
    /// </summary>
    public DateTime? DateMinParsed { get; set; }

    /// <summary>
    /// Parsed maximum date
    /// </summary>
    public DateTime? DateMaxParsed { get; set; }

    /// <summary>
    /// Effective page number (with default)
    /// </summary>
    public int PageNumber => Page ?? 1;

    /// <summary>
    /// Effective page size (with default, capped at 100)
    /// </summary>
    public int PageSize => Math.Min(PerPage ?? 25, 100);

    /// <summary>
    /// Parsed Mars time minimum (TimeSpan from midnight)
    /// </summary>
    public TimeSpan? MarsTimeMinParsed { get; set; }

    /// <summary>
    /// Parsed Mars time maximum (TimeSpan from midnight)
    /// </summary>
    public TimeSpan? MarsTimeMaxParsed { get; set; }

    /// <summary>
    /// Parsed sample types list
    /// </summary>
    public List<string> SampleTypeList { get; set; } = new();

    /// <summary>
    /// Parsed aspect ratio (width:height)
    /// </summary>
    public (int Width, int Height)? AspectRatioParsed { get; set; }

    /// <summary>
    /// Parsed field set enum value
    /// </summary>
    public FieldSetType? FieldSetParsed { get; set; }

    /// <summary>
    /// Parsed image sizes list
    /// </summary>
    public List<string> ImageSizesList { get; set; } = new();
}

/// <summary>
/// Field set types for controlling response data
/// </summary>
public enum FieldSetType
{
    /// <summary>
    /// Minimal fields: id, sol, images.medium
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard fields: minimal + earth_date, camera, rover
    /// </summary>
    Standard,

    /// <summary>
    /// Extended fields: standard + location, dimensions, mars_time
    /// </summary>
    Extended,

    /// <summary>
    /// Scientific fields: extended + telemetry, coordinates
    /// </summary>
    Scientific,

    /// <summary>
    /// Complete: everything including raw_data
    /// </summary>
    Complete
}

/// <summary>
/// Sort field with direction
/// </summary>
public class SortField
{
    public string Field { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}

/// <summary>
/// Sort direction
/// </summary>
public enum SortDirection
{
    Ascending,
    Descending
}
