using System.Text.Json;
using System.Text.RegularExpressions;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class CuriosityScraper : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<CuriosityScraper> _logger;

    private const string BaseUrl = "https://mars.nasa.gov/api/v1/raw_image_items/";
    private const int CuriosityRoverId = 2;

    public CuriosityScraper(
        IHttpClientFactory httpClientFactory,
        MarsVistaDbContext context,
        ILogger<CuriosityScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public string RoverName => "Curiosity";

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ScrapeAsync not implemented for Curiosity (use ScrapeSolAsync instead)");
        return 0;
    }

    /// <summary>
    /// Maps NASA instrument names to database camera names
    /// NASA uses variations like MAST_LEFT, MAST_RIGHT, NAV_LEFT_A, etc.
    /// Database uses generic names like MAST, NAVCAM, etc.
    /// </summary>
    private string MapInstrumentToCamera(string instrument)
    {
        return instrument.ToUpper() switch
        {
            // Mast Camera (Mastcam)
            var s when s.StartsWith("MAST_") => "MAST",
            "MASTCAM" => "MAST",

            // Navigation Camera
            var s when s.StartsWith("NAV_") => "NAVCAM",
            "NAVCAM" => "NAVCAM",

            // Front Hazard Avoidance Camera
            var s when s.StartsWith("FHAZ_") => "FHAZ",
            "FHAZ" => "FHAZ",

            // Rear Hazard Avoidance Camera
            var s when s.StartsWith("RHAZ_") => "RHAZ",
            "RHAZ" => "RHAZ",

            // Chemistry and Camera Complex
            var s when s.StartsWith("CHEMCAM_") => "CHEMCAM",
            "CHEMCAM" => "CHEMCAM",

            // Mars Hand Lens Imager
            "MAHLI" => "MAHLI",

            // Mars Descent Imager
            "MARDI" => "MARDI",

            _ => instrument // Return as-is if no mapping found
        };
    }

    public async Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping Curiosity photos for sol {Sol}", sol);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("NASA");

            var url = $"{BaseUrl}?order=sol%20desc&per_page=200&condition_1=msl:mission&condition_2={sol}:sol:in";

            _logger.LogDebug("Fetching {Url}", url);

            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No photos found for sol {Sol}", sol);
                return 0;
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Received JSON response of length {Length}", json.Length);

            // Parse JSON directly (Perseverance approach)
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("items", out var items))
            {
                _logger.LogWarning("No 'items' array in response for sol {Sol}", sol);
                return 0;
            }

            var itemsArray = items.EnumerateArray().ToList();

            if (itemsArray.Count == 0)
            {
                _logger.LogInformation("No photos found for sol {Sol}", sol);
                return 0;
            }

            _logger.LogInformation("Found {Count} photos for sol {Sol}", itemsArray.Count, sol);

            var (inserted, skipped) = await ProcessPhotosAsync(itemsArray, sol, cancellationToken);

            return inserted;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error scraping sol {Sol}: {Message}", sol, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping sol {Sol}", sol);
            throw;
        }
    }

    private async Task<(int inserted, int skipped)> ProcessPhotosAsync(
        List<JsonElement> photos,
        int sol,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var skipped = 0;

        // Build list of NASA IDs for duplicate checking
        var nasaIds = photos
            .Select(p => TryGetInt(p, "id")?.ToString() ?? "")
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var existingIds = await _context.Photos
            .Where(p => nasaIds.Contains(p.NasaId))
            .Select(p => p.NasaId)
            .ToHashSetAsync(cancellationToken);

        foreach (var photo in photos)
        {
            try
            {
                var nasaId = TryGetInt(photo, "id")?.ToString() ?? "";

                if (string.IsNullOrEmpty(nasaId))
                {
                    _logger.LogWarning("Photo missing ID, skipping");
                    skipped++;
                    continue;
                }

                if (existingIds.Contains(nasaId))
                {
                    skipped++;
                    continue;
                }

                // Extract instrument and map to camera
                var instrument = TryGetString(photo, "instrument") ?? "";
                var cameraName = MapInstrumentToCamera(instrument);

                var camera = await _context.Cameras
                    .FirstOrDefaultAsync(
                        c => c.RoverId == CuriosityRoverId &&
                             c.Name.ToLower() == cameraName.ToLower(),
                        cancellationToken);

                if (camera == null)
                {
                    _logger.LogWarning(
                        "Camera not found for Curiosity: {Instrument} (mapped to {CameraName}). Skipping photo {PhotoId}",
                        instrument, cameraName, nasaId);
                    skipped++;
                    continue;
                }

                // Extract and parse date_taken
                var dateTakenStr = TryGetString(photo, "date_taken");
                if (string.IsNullOrEmpty(dateTakenStr) || !DateTime.TryParse(dateTakenStr, out var dateTaken))
                {
                    _logger.LogWarning("Invalid date_taken: {Date}. Skipping photo {PhotoId}",
                        dateTakenStr, nasaId);
                    skipped++;
                    continue;
                }

                // Ensure dates are UTC for PostgreSQL
                var earthDate = DateTime.SpecifyKind(dateTaken.Date, DateTimeKind.Utc);
                var dateTakenUtc = DateTime.SpecifyKind(dateTaken, DateTimeKind.Utc);

                // Extract extended object
                JsonElement extended = default;
                photo.TryGetProperty("extended", out extended);

                // Extract mast orientation as floats
                var mastAz = TryGetFloatFromString(extended, "mast_az");
                var mastEl = TryGetFloatFromString(extended, "mast_el");

                var photoEntity = new Photo
                {
                    NasaId = nasaId,
                    Sol = TryGetInt(photo, "sol") ?? sol,
                    EarthDate = earthDate,
                    DateTakenUtc = dateTakenUtc,
                    RoverId = CuriosityRoverId,
                    CameraId = camera.Id,

                    // Image URLs - NASA provides full and thumbnail
                    ImgSrcFull = TryGetString(photo, "https_url") ?? "",
                    ImgSrcSmall = TryGetString(extended, "url_list"),

                    // Sample type and metadata
                    SampleType = TryGetString(extended, "sample_type") ?? "unknown",
                    Title = TryGetString(photo, "title"),
                    Caption = TryGetString(photo, "description"),
                    Credit = TryGetString(photo, "image_credit"),

                    // Location and telemetry
                    Site = TryGetInt(photo, "site"),
                    Drive = TryGetInt(photo, "drive"),
                    Xyz = TryGetString(photo, "xyz"),

                    // Camera orientation
                    MastAz = mastAz,
                    MastEl = mastEl,
                    CameraVector = TryGetString(photo, "camera_vector"),
                    CameraPosition = TryGetString(photo, "camera_position"),
                    CameraModelType = TryGetString(photo, "camera_model_type"),

                    // Mars local time and spacecraft data
                    DateTakenMars = TryGetString(extended, "lmst") ?? "",
                    MarsTimeHour = Helpers.MarsTimeHelper.ExtractHour(TryGetString(extended, "lmst")),
                    Attitude = TryGetString(photo, "attitude"),
                    SpacecraftClock = TryGetFloat(photo, "spacecraft_clock"),

                    // Processing metadata
                    DateReceived = TryGetDateTime(photo, "date_received"),
                    FilterName = TryGetString(extended, "filter_name"),

                    // Store complete NASA response in JSONB (Perseverance approach)
                    RawData = JsonDocument.Parse(photo.GetRawText()),

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Photos.Add(photoEntity);
                inserted++;
            }
            catch (Exception ex)
            {
                var photoId = TryGetInt(photo, "id")?.ToString() ?? "unknown";
                _logger.LogError(ex, "Error processing photo {PhotoId}", photoId);
                skipped++;
            }
        }

        if (inserted > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Inserted {Count} new photos for sol {Sol}", inserted, sol);
        }

        return (inserted, skipped);
    }

    // ============================================================================
    // HELPER METHODS (copied from PerseveranceScraper for consistency)
    // ============================================================================

    /// <summary>
    /// Safely extract string from JSON element
    /// </summary>
    private static string? TryGetString(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value) &&
            value.ValueKind != JsonValueKind.Null &&
            value.ValueKind != JsonValueKind.Undefined)
        {
            return value.GetString();
        }

        return null;
    }

    /// <summary>
    /// Safely extract integer from JSON element
    /// </summary>
    private static int? TryGetInt(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value) &&
            value.ValueKind == JsonValueKind.Number)
        {
            return value.GetInt32();
        }

        return null;
    }

    /// <summary>
    /// Safely extract float from JSON element
    /// Handles both numeric and string representations
    /// </summary>
    private static float? TryGetFloat(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return (float)value.GetDouble();

            // Some numeric fields come as strings
            if (value.ValueKind == JsonValueKind.String &&
                float.TryParse(value.GetString(), out var floatValue))
            {
                return floatValue;
            }
        }

        return null;
    }

    /// <summary>
    /// Safely extract float from string property (for mast_az, mast_el which come as strings)
    /// </summary>
    private static float? TryGetFloatFromString(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && float.TryParse(str, out var floatValue))
        {
            return floatValue;
        }
        return null;
    }

    /// <summary>
    /// Safely extract DateTime from JSON element (assumes UTC timestamps from NASA)
    /// </summary>
    private static DateTime? TryGetDateTime(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && DateTime.TryParse(str, out var dateTime))
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
        return null;
    }
}
