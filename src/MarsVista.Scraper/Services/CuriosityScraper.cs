using System.Text.Json;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Helpers;
using MarsVista.Scraper.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarsVista.Scraper.Services;

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

    private const int PerPage = 200;

    public async Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping Curiosity photos for sol {Sol}", sol);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("NASA");

            // Fetch all pages for this sol
            var allPhotos = new List<JsonElement>();
            var page = 0;
            var total = int.MaxValue;  // Will be set from first response

            while (page * PerPage < total)
            {
                var url = $"{BaseUrl}?order=sol%20desc&per_page={PerPage}&page={page}&condition_1=msl:mission&condition_2={sol}:sol:in";
                _logger.LogDebug("Fetching page {Page} from {Url}", page, url);

                var response = await httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    if (page == 0)
                    {
                        _logger.LogInformation("No photos found for sol {Sol} (404)", sol);
                        return 0;
                    }
                    break;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                // Parse JSON directly (Perseverance approach)
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                // Get total from first page
                if (page == 0 && root.TryGetProperty("total", out var totalProp) &&
                    totalProp.ValueKind == JsonValueKind.Number)
                {
                    total = totalProp.GetInt32();
                    _logger.LogInformation("Sol {Sol} has {Total} total photos from NASA (will filter thumbnails)", sol, total);
                }

                if (!root.TryGetProperty("items", out var items))
                {
                    _logger.LogWarning("No 'items' array in response for sol {Sol} page {Page}", sol, page);
                    break;
                }

                var itemsArray = items.EnumerateArray().ToList();

                if (itemsArray.Count == 0)
                {
                    break;
                }

                // Clone elements since we're disposing the document
                foreach (var item in itemsArray)
                {
                    allPhotos.Add(JsonDocument.Parse(item.GetRawText()).RootElement);
                }

                _logger.LogDebug("Page {Page}: {Count} photos", page, itemsArray.Count);
                page++;
            }

            if (allPhotos.Count == 0)
            {
                _logger.LogInformation("No photos found for sol {Sol}", sol);
                return 0;
            }

            _logger.LogInformation("Fetched {Count} photos for sol {Sol} across {Pages} pages",
                allPhotos.Count, sol, page);

            var (inserted, skipped) = await ProcessPhotosAsync(allPhotos, sol, cancellationToken);

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

                // Extract extended object early for sample_type check
                JsonElement extended = default;
                photo.TryGetProperty("extended", out extended);

                // Skip thumbnails - they're too small to be useful (160x144 or smaller)
                var sampleType = ScraperHelpers.TryGetString(extended, "sample_type") ?? "unknown";
                if (ScraperHelpers.IsThumbnail(sampleType))
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

                // Extract dimensions from subframe_rect or estimate from sample_type
                var (width, height) = ScraperHelpers.ExtractCuriosityDimensions(extended, sampleType);

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

                    // Image URL - NASA only provides one size per record for Curiosity
                    // (unlike Perseverance which has small/medium/large/full)
                    ImgSrcFull = TryGetString(photo, "https_url") ?? "",

                    // Image dimensions (extracted from subframe_rect or estimated)
                    Width = width,
                    Height = height,

                    // Sample type and metadata
                    SampleType = sampleType,
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
                    MarsTimeHour = MarsTimeHelper.ExtractHour(TryGetString(extended, "lmst")),
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
    // HELPER METHODS (Local JSON helpers - dimension parsing now in ScraperHelpers)
    // ============================================================================

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

    private static float? TryGetFloat(JsonElement element, string property)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (element.TryGetProperty(property, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return (float)value.GetDouble();

            if (value.ValueKind == JsonValueKind.String &&
                float.TryParse(value.GetString(), out var floatValue))
            {
                return floatValue;
            }
        }

        return null;
    }

    private static float? TryGetFloatFromString(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && float.TryParse(str, out var floatValue))
        {
            return floatValue;
        }
        return null;
    }

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
