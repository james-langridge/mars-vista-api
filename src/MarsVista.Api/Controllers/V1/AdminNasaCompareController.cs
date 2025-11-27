using System.Text.Json;
using MarsVista.Api.Filters;
using MarsVista.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Admin API for comparing our database with NASA API responses.
/// Useful for data validation and identifying discrepancies.
/// Protected by AdminAuthorization filter - requires admin role.
/// </summary>
[ApiController]
[Route("api/v1/admin/nasa")]
[AdminAuthorization]
[ApiExplorerSettings(IgnoreApi = true)]
public class AdminNasaCompareController : ControllerBase
{
    private readonly MarsVistaDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AdminNasaCompareController> _logger;

    public AdminNasaCompareController(
        MarsVistaDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<AdminNasaCompareController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Compare our database with NASA API for a specific rover and sol.
    /// Returns photo counts and identifies missing/extra photos.
    /// </summary>
    [HttpGet("sol/{rover}/{sol:int}")]
    public async Task<IActionResult> CompareSol(string rover, int sol)
    {
        if (!IsValidRover(rover))
        {
            return BadRequest(new { error = $"Invalid rover: {rover}. Valid rovers: perseverance, curiosity" });
        }

        if (sol < 0)
        {
            return BadRequest(new { error = "Sol must be >= 0" });
        }

        try
        {
            // Fetch NASA data for this sol
            var nasaPhotos = await FetchNasaSolAsync(rover.ToLower(), sol);

            // Get our database photos for this sol
            var roverLower = rover.ToLower();
            var ourPhotos = await _context.Photos
                .AsNoTracking()
                .Where(p => p.Rover.Name.ToLower() == roverLower && p.Sol == sol)
                .Select(p => new
                {
                    p.Id,
                    p.NasaId,
                    p.Sol,
                    p.EarthDate,
                    CameraName = p.Camera.Name,
                    p.Width,
                    p.Height,
                    p.SampleType,
                    p.ImgSrcFull
                })
                .ToListAsync();

            // Extract NASA IDs from NASA response
            var nasaIds = nasaPhotos.Select(p => p.NasaId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var ourIds = ourPhotos.Select(p => p.NasaId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Find missing and extra photos
            var missingFromOurs = nasaIds.Except(ourIds).ToList();
            var extraInOurs = ourIds.Except(nasaIds).ToList();

            var matchCount = nasaIds.Intersect(ourIds).Count();
            var matchPercent = nasaIds.Count > 0 ? Math.Round((double)matchCount / nasaIds.Count * 100, 1) : 100;

            return Ok(new
            {
                rover = roverLower,
                sol = sol,
                comparison = new
                {
                    nasaPhotoCount = nasaPhotos.Count,
                    ourPhotoCount = ourPhotos.Count,
                    matchCount = matchCount,
                    matchPercent = matchPercent,
                    missingFromOurs = missingFromOurs.Count,
                    extraInOurs = extraInOurs.Count,
                    status = matchCount == nasaPhotos.Count && extraInOurs.Count == 0 ? "match" :
                             missingFromOurs.Count > 0 ? "missing" : "extra"
                },
                details = new
                {
                    missingNasaIds = missingFromOurs.Take(50).ToList(), // Limit to 50 for readability
                    extraNasaIds = extraInOurs.Take(50).ToList(),
                    truncatedMissing = missingFromOurs.Count > 50,
                    truncatedExtra = extraInOurs.Count > 50
                },
                nasaPhotos = nasaPhotos.Take(10).ToList(), // Sample of NASA photos for reference
                ourPhotos = ourPhotos.Take(10).ToList() // Sample of our photos for reference
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare NASA data for {Rover} sol {Sol}", rover, sol);
            return StatusCode(500, new { error = "Failed to compare with NASA API", message = ex.Message });
        }
    }

    /// <summary>
    /// Compare a specific photo by NASA ID.
    /// Returns our stored data vs NASA's current API response.
    /// </summary>
    [HttpGet("photo/{nasaId}")]
    public async Task<IActionResult> ComparePhoto(string nasaId)
    {
        if (string.IsNullOrWhiteSpace(nasaId))
        {
            return BadRequest(new { error = "NASA ID is required" });
        }

        try
        {
            // Get our stored photo
            var ourPhoto = await _context.Photos
                .AsNoTracking()
                .Include(p => p.Rover)
                .Include(p => p.Camera)
                .Where(p => p.NasaId == nasaId)
                .FirstOrDefaultAsync();

            if (ourPhoto == null)
            {
                return NotFound(new { error = $"Photo with NASA ID '{nasaId}' not found in our database" });
            }

            // Fetch NASA data for this photo's sol to find the same photo
            var nasaPhotos = await FetchNasaSolAsync(ourPhoto.Rover.Name.ToLower(), ourPhoto.Sol);
            var nasaPhoto = nasaPhotos.FirstOrDefault(p =>
                p.NasaId.Equals(nasaId, StringComparison.OrdinalIgnoreCase));

            // Compare fields
            var differences = new List<FieldDifference>();

            if (nasaPhoto != null)
            {
                // Compare common fields
                CompareField(differences, "sol", ourPhoto.Sol, nasaPhoto.Sol);
                CompareField(differences, "earth_date", ourPhoto.EarthDate?.ToString("yyyy-MM-dd"), nasaPhoto.EarthDate);
                CompareField(differences, "width", ourPhoto.Width, nasaPhoto.Width);
                CompareField(differences, "height", ourPhoto.Height, nasaPhoto.Height);
                CompareField(differences, "sample_type", ourPhoto.SampleType, nasaPhoto.SampleType);
                CompareField(differences, "img_src", ourPhoto.ImgSrcFull, nasaPhoto.ImgSrc);
            }

            return Ok(new
            {
                nasaId = nasaId,
                foundInNasa = nasaPhoto != null,
                foundInOurs = true,
                ourData = new
                {
                    id = ourPhoto.Id,
                    nasaId = ourPhoto.NasaId,
                    sol = ourPhoto.Sol,
                    earthDate = ourPhoto.EarthDate?.ToString("yyyy-MM-dd"),
                    camera = ourPhoto.Camera.Name,
                    rover = ourPhoto.Rover.Name,
                    width = ourPhoto.Width,
                    height = ourPhoto.Height,
                    sampleType = ourPhoto.SampleType,
                    imgSrc = ourPhoto.ImgSrcFull,
                    site = ourPhoto.Site,
                    drive = ourPhoto.Drive,
                    mastAz = ourPhoto.MastAz,
                    mastEl = ourPhoto.MastEl,
                    dateTakenUtc = ourPhoto.DateTakenUtc,
                    dateTakenMars = ourPhoto.DateTakenMars,
                    rawDataPreview = ourPhoto.RawData != null
                        ? ourPhoto.RawData.RootElement.ToString()[..Math.Min(500, ourPhoto.RawData.RootElement.ToString().Length)] + "..."
                        : null
                },
                nasaData = nasaPhoto != null ? new
                {
                    nasaId = nasaPhoto.NasaId,
                    sol = nasaPhoto.Sol,
                    earthDate = nasaPhoto.EarthDate,
                    camera = nasaPhoto.Camera,
                    width = nasaPhoto.Width,
                    height = nasaPhoto.Height,
                    sampleType = nasaPhoto.SampleType,
                    imgSrc = nasaPhoto.ImgSrc
                } : null,
                differences = differences
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare photo {NasaId}", nasaId);
            return StatusCode(500, new { error = "Failed to compare photo", message = ex.Message });
        }
    }

    /// <summary>
    /// Compare multiple sols for a rover (bulk validation).
    /// Returns summary of discrepancies across sol range.
    /// </summary>
    [HttpGet("range/{rover}")]
    public async Task<IActionResult> CompareRange(
        string rover,
        [FromQuery] int startSol,
        [FromQuery] int endSol)
    {
        if (!IsValidRover(rover))
        {
            return BadRequest(new { error = $"Invalid rover: {rover}. Valid rovers: perseverance, curiosity" });
        }

        if (startSol < 0 || endSol < startSol)
        {
            return BadRequest(new { error = "Invalid sol range. StartSol must be >= 0 and <= endSol" });
        }

        var solCount = endSol - startSol + 1;
        if (solCount > 50)
        {
            return BadRequest(new { error = "Sol range too large. Max 50 sols per request." });
        }

        try
        {
            var roverLower = rover.ToLower();
            var results = new List<object>();
            var totalNasaPhotos = 0;
            var totalOurPhotos = 0;
            var totalMissing = 0;
            var totalExtra = 0;

            for (var sol = startSol; sol <= endSol; sol++)
            {
                // Fetch NASA data for this sol
                var nasaPhotos = await FetchNasaSolAsync(roverLower, sol);

                // Get our database photos for this sol
                var ourPhotoCount = await _context.Photos
                    .AsNoTracking()
                    .Where(p => p.Rover.Name.ToLower() == roverLower && p.Sol == sol)
                    .CountAsync();

                var ourIds = await _context.Photos
                    .AsNoTracking()
                    .Where(p => p.Rover.Name.ToLower() == roverLower && p.Sol == sol)
                    .Select(p => p.NasaId)
                    .ToListAsync();

                var nasaIds = nasaPhotos.Select(p => p.NasaId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var ourIdSet = ourIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var missing = nasaIds.Except(ourIdSet).Count();
                var extra = ourIdSet.Except(nasaIds).Count();

                totalNasaPhotos += nasaPhotos.Count;
                totalOurPhotos += ourPhotoCount;
                totalMissing += missing;
                totalExtra += extra;

                if (nasaPhotos.Count > 0 || ourPhotoCount > 0)
                {
                    results.Add(new
                    {
                        sol = sol,
                        nasaCount = nasaPhotos.Count,
                        ourCount = ourPhotoCount,
                        missing = missing,
                        extra = extra,
                        status = missing == 0 && extra == 0 ? "match" :
                                 missing > 0 ? "missing" : "extra"
                    });
                }
            }

            return Ok(new
            {
                rover = roverLower,
                startSol = startSol,
                endSol = endSol,
                solsCompared = solCount,
                summary = new
                {
                    totalNasaPhotos = totalNasaPhotos,
                    totalOurPhotos = totalOurPhotos,
                    totalMissing = totalMissing,
                    totalExtra = totalExtra,
                    matchPercent = totalNasaPhotos > 0
                        ? Math.Round((double)(totalNasaPhotos - totalMissing) / totalNasaPhotos * 100, 1)
                        : 100
                },
                sols = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare range for {Rover}", rover);
            return StatusCode(500, new { error = "Failed to compare sol range", message = ex.Message });
        }
    }

    private async Task<List<NasaPhotoInfo>> FetchNasaSolAsync(string rover, int sol)
    {
        var httpClient = _httpClientFactory.CreateClient("NASA");
        var photos = new List<NasaPhotoInfo>();

        try
        {
            if (rover == "perseverance")
            {
                // Perseverance uses mars.nasa.gov RSS API
                var page = 1;
                var pageSize = 100;
                var hasMore = true;

                while (hasMore)
                {
                    var url = $"https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol={sol}&num={pageSize}&page={page}";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty("images", out var images))
                    {
                        foreach (var image in images.EnumerateArray())
                        {
                            photos.Add(new NasaPhotoInfo
                            {
                                NasaId = image.TryGetProperty("imageid", out var id) ? id.GetString() ?? "" : "",
                                Sol = image.TryGetProperty("sol", out var s) ? s.GetInt32() : 0,
                                EarthDate = image.TryGetProperty("date_taken_utc", out var d)
                                    ? DateTime.TryParse(d.GetString(), out var dt) ? dt.ToString("yyyy-MM-dd") : null
                                    : null,
                                Camera = image.TryGetProperty("camera", out var cam)
                                    ? cam.TryGetProperty("instrument", out var inst) ? inst.GetString() : null
                                    : null,
                                ImgSrc = image.TryGetProperty("image_files", out var files)
                                    ? files.TryGetProperty("full_res", out var full) ? full.GetString() : null
                                    : null,
                                Width = image.TryGetProperty("image_files", out var f2)
                                    ? f2.TryGetProperty("full_res_width", out var w) ? w.GetInt32() : null
                                    : null,
                                Height = image.TryGetProperty("image_files", out var f3)
                                    ? f3.TryGetProperty("full_res_height", out var h) ? h.GetInt32() : null
                                    : null,
                                SampleType = image.TryGetProperty("sample_type", out var st) ? st.GetString() : null
                            });
                        }

                        hasMore = images.GetArrayLength() == pageSize;
                        page++;
                    }
                    else
                    {
                        hasMore = false;
                    }

                    // Limit pages to prevent runaway
                    if (page > 50) break;
                }
            }
            else if (rover == "curiosity")
            {
                // Curiosity uses mars.nasa.gov API
                var page = 1;
                var pageSize = 100;
                var hasMore = true;

                while (hasMore)
                {
                    var url = $"https://mars.nasa.gov/api/v1/raw_image_items/?sol={sol}&order=sol%20asc,date_taken%20asc&per_page={pageSize}&page={page}&condition_1=msl:mission";
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty("items", out var items))
                    {
                        foreach (var item in items.EnumerateArray())
                        {
                            photos.Add(new NasaPhotoInfo
                            {
                                NasaId = item.TryGetProperty("imageid", out var id) ? id.GetString() ?? "" : "",
                                Sol = item.TryGetProperty("sol", out var s) ? s.GetInt32() : 0,
                                EarthDate = item.TryGetProperty("date_taken_utc", out var d)
                                    ? DateTime.TryParse(d.GetString(), out var dt) ? dt.ToString("yyyy-MM-dd") : null
                                    : null,
                                Camera = item.TryGetProperty("instrument", out var inst) ? inst.GetString() : null,
                                ImgSrc = item.TryGetProperty("https_url", out var url2) ? url2.GetString() : null,
                                Width = item.TryGetProperty("image_width", out var w) ? w.GetInt32() : null,
                                Height = item.TryGetProperty("image_height", out var h) ? h.GetInt32() : null,
                                SampleType = item.TryGetProperty("sample_type", out var st) ? st.GetString() : null
                            });
                        }

                        hasMore = items.GetArrayLength() == pageSize;
                        page++;
                    }
                    else
                    {
                        hasMore = false;
                    }

                    // Limit pages to prevent runaway
                    if (page > 50) break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching NASA photos for {Rover} sol {Sol}", rover, sol);
            throw;
        }

        return photos;
    }

    private void CompareField<T>(List<FieldDifference> differences, string fieldName, T? ourValue, T? nasaValue)
    {
        var ourStr = ourValue?.ToString() ?? "(null)";
        var nasaStr = nasaValue?.ToString() ?? "(null)";

        if (!ourStr.Equals(nasaStr, StringComparison.OrdinalIgnoreCase))
        {
            differences.Add(new FieldDifference
            {
                Field = fieldName,
                OurValue = ourStr,
                NasaValue = nasaStr
            });
        }
    }

    private static bool IsValidRover(string rover)
    {
        var validRovers = new[] { "perseverance", "curiosity" };
        return validRovers.Contains(rover.ToLower());
    }
}

// Helper classes
public class NasaPhotoInfo
{
    public string NasaId { get; set; } = string.Empty;
    public int Sol { get; set; }
    public string? EarthDate { get; set; }
    public string? Camera { get; set; }
    public string? ImgSrc { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? SampleType { get; set; }
}

public class FieldDifference
{
    public string Field { get; set; } = string.Empty;
    public string OurValue { get; set; } = string.Empty;
    public string NasaValue { get; set; } = string.Empty;
}
