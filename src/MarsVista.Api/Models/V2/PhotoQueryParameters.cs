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
