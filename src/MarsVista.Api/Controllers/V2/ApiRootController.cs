using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V2;

/// <summary>
/// v2 API Root - Discovery endpoint for self-documentation
/// </summary>
[ApiController]
[Route("api/v2")]
[Tags("V2 - Discovery")]
public class ApiRootController : ControllerBase
{
    /// <summary>
    /// Get API information and available resources
    /// </summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult GetApiRoot()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var response = new
        {
            version = "2.0.0",
            description = "Mars Vista API v2 - Modern REST API for NASA Mars Rover photos",
            features = new[]
            {
                "Unified photo endpoint with cross-rover queries",
                "Advanced filtering (rovers, cameras, date ranges, sol ranges)",
                "Field selection for bandwidth optimization",
                "Always-on pagination with page and cursor support",
                "RFC 7807 error responses with detailed validation",
                "HTTP caching with ETag support",
                "Slug-based rover routing"
            },
            resources = new
            {
                photos = new
                {
                    href = $"{baseUrl}/api/v2/photos",
                    description = "Query Mars rover photos with powerful filtering",
                    methods = new[] { "GET" },
                    filters = new
                    {
                        rovers = new
                        {
                            type = "array",
                            description = "Filter by one or more rovers",
                            values = new[] { "curiosity", "perseverance", "opportunity", "spirit" },
                            example = "?rovers=curiosity,perseverance"
                        },
                        cameras = new
                        {
                            type = "array",
                            description = "Filter by one or more cameras",
                            values = new[] { "FHAZ", "RHAZ", "MAST", "CHEMCAM", "MAHLI", "MARDI", "NAVCAM", "PANCAM", "MINITES" },
                            example = "?cameras=FHAZ,NAVCAM"
                        },
                        sol_min = new
                        {
                            type = "integer",
                            description = "Minimum sol (inclusive)",
                            min = 0,
                            example = "?sol_min=100"
                        },
                        sol_max = new
                        {
                            type = "integer",
                            description = "Maximum sol (inclusive)",
                            min = 0,
                            example = "?sol_max=200"
                        },
                        sol = new
                        {
                            type = "integer",
                            description = "Exact sol (shorthand for sol_min=X&sol_max=X)",
                            min = 0,
                            example = "?sol=1000"
                        },
                        date_min = new
                        {
                            type = "date",
                            description = "Minimum earth date (inclusive)",
                            format = "YYYY-MM-DD",
                            example = "?date_min=2023-01-01"
                        },
                        date_max = new
                        {
                            type = "date",
                            description = "Maximum earth date (inclusive)",
                            format = "YYYY-MM-DD",
                            example = "?date_max=2023-12-31"
                        },
                        earth_date = new
                        {
                            type = "date",
                            description = "Exact earth date (shorthand)",
                            format = "YYYY-MM-DD",
                            example = "?earth_date=2023-06-15"
                        },
                        sort = new
                        {
                            type = "string",
                            description = "Sort fields (comma-separated, prefix with - for descending)",
                            values = new[] { "id", "sol", "earth_date", "date_taken_utc", "camera", "created_at" },
                            example = "?sort=-earth_date,camera"
                        },
                        fields = new
                        {
                            type = "string",
                            description = "Field selection for sparse fieldsets (comma-separated)",
                            values = new[] { "id", "sol", "earth_date", "img_src", "width", "height", "site", "drive", "xyz", "mast_az", "mast_el" },
                            example = "?fields=id,img_src,sol,earth_date"
                        },
                        include = new
                        {
                            type = "string",
                            description = "Include related resources (comma-separated)",
                            values = new[] { "rover", "camera" },
                            example = "?include=rover,camera"
                        },
                        page = new
                        {
                            type = "integer",
                            description = "Page number (1-indexed, default: 1)",
                            min = 1,
                            example = "?page=2"
                        },
                        per_page = new
                        {
                            type = "integer",
                            description = "Items per page (default: 25, max: 100)",
                            min = 1,
                            max = 100,
                            example = "?per_page=50"
                        }
                    },
                    examples = new[]
                    {
                        new { description = "Latest photos from Curiosity", url = $"{baseUrl}/api/v2/photos?rovers=curiosity&sort=-earth_date&per_page=10" },
                        new { description = "Navigation camera photos from sol 1000-2000", url = $"{baseUrl}/api/v2/photos?rovers=curiosity&cameras=NAVCAM&sol_min=1000&sol_max=2000" },
                        new { description = "Cross-rover query with field selection", url = $"{baseUrl}/api/v2/photos?rovers=curiosity,perseverance&fields=id,img_src,sol&include=camera" }
                    }
                },
                rovers = new
                {
                    href = $"{baseUrl}/api/v2/rovers",
                    description = "Get rover information and statistics",
                    methods = new[] { "GET" },
                    endpoints = new
                    {
                        list = $"{baseUrl}/api/v2/rovers",
                        bySlug = $"{baseUrl}/api/v2/rovers/{{slug}}",
                        manifest = $"{baseUrl}/api/v2/rovers/{{slug}}/manifest",
                        cameras = $"{baseUrl}/api/v2/rovers/{{slug}}/cameras"
                    },
                    examples = new[]
                    {
                        new { description = "Get all rovers", url = $"{baseUrl}/api/v2/rovers" },
                        new { description = "Get Curiosity details", url = $"{baseUrl}/api/v2/rovers/curiosity" },
                        new { description = "Get Perseverance manifest", url = $"{baseUrl}/api/v2/rovers/perseverance/manifest" }
                    }
                },
                cameras = new
                {
                    href = $"{baseUrl}/api/v2/cameras",
                    description = "Get camera information across all rovers",
                    methods = new[] { "GET" },
                    endpoints = new
                    {
                        list = $"{baseUrl}/api/v2/cameras",
                        byId = $"{baseUrl}/api/v2/cameras/{{id}}"
                    },
                    examples = new[]
                    {
                        new { description = "Get all cameras", url = $"{baseUrl}/api/v2/cameras" },
                        new { description = "Get MAST camera", url = $"{baseUrl}/api/v2/cameras/MAST" }
                    }
                }
            },
            links = new
            {
                self = $"{baseUrl}/api/v2",
                photos = $"{baseUrl}/api/v2/photos",
                rovers = $"{baseUrl}/api/v2/rovers",
                cameras = $"{baseUrl}/api/v2/cameras",
                documentation = $"{baseUrl}/docs"
            }
        };

        return Ok(response);
    }
}
