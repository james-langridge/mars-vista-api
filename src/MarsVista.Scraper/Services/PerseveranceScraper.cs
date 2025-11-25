using System.Text.Json;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Core.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarsVista.Scraper.Services;

public class PerseveranceScraper : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PerseveranceScraper> _logger;

    private const string BaseUrl = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json";
    private const int PerPage = 100;  // NASA API max per page
    private const int PerseveranceRoverId = 1;  // Database rover ID

    public PerseveranceScraper(
        IHttpClientFactory httpClientFactory,
        MarsVistaDbContext context,
        ILogger<PerseveranceScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public string RoverName => "Perseverance";

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ScrapeAsync not implemented for Perseverance (use ScrapeSolAsync instead)");
        return 0;
    }

    public async Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping Perseverance photos for sol {Sol}", sol);

        try
        {
            var httpClient = _httpClientFactory.CreateClient("NASA");

            // Fetch all pages for this sol
            var allPhotos = new List<JsonElement>();
            var page = 0;
            var hasMorePages = true;

            while (hasMorePages)
            {
                var url = $"{BaseUrl}&sol={sol}&num={PerPage}&page={page}";
                _logger.LogDebug("Fetching page {Page} from {Url}", page, url);

                var response = await httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("No photos found for sol {Sol} (404)", sol);
                    return 0;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                // Parse JSON directly to preserve all fields
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (!root.TryGetProperty("images", out var images))
                {
                    _logger.LogWarning("No 'images' array in response for sol {Sol} page {Page}", sol, page);
                    break;
                }

                var imagesArray = images.EnumerateArray().ToList();

                if (imagesArray.Count == 0)
                {
                    hasMorePages = false;
                    continue;
                }

                // Clone elements since we're disposing the document
                foreach (var img in imagesArray)
                {
                    allPhotos.Add(JsonDocument.Parse(img.GetRawText()).RootElement);
                }

                _logger.LogDebug("Page {Page}: {Count} images", page, imagesArray.Count);

                hasMorePages = imagesArray.Count == PerPage;
                page++;
            }

            if (allPhotos.Count == 0)
            {
                _logger.LogInformation("No photos found for sol {Sol}", sol);
                return 0;
            }

            _logger.LogInformation("Found {Count} photos for sol {Sol}", allPhotos.Count, sol);

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
            .Select(p => TryGetInt(p, "imageid")?.ToString() ?? "")
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
                var nasaId = TryGetInt(photo, "imageid")?.ToString() ?? "";

                if (string.IsNullOrEmpty(nasaId))
                {
                    _logger.LogWarning("Photo missing imageid, skipping");
                    skipped++;
                    continue;
                }

                if (existingIds.Contains(nasaId))
                {
                    skipped++;
                    continue;
                }

                // Extract camera info
                var cameraName = TryGetString(photo, "camera", "instrument") ?? "";

                var camera = await _context.Cameras
                    .FirstOrDefaultAsync(
                        c => c.RoverId == PerseveranceRoverId &&
                             c.Name.ToLower() == cameraName.ToLower(),
                        cancellationToken);

                if (camera == null)
                {
                    _logger.LogWarning(
                        "Camera not found for Perseverance: {Camera}. Skipping photo {PhotoId}",
                        cameraName, nasaId);
                    skipped++;
                    continue;
                }

                // Extract dates with proper UTC handling
                var dateTaken = TryGetDateTime(photo, "date_taken");
                if (!dateTaken.HasValue)
                {
                    _logger.LogWarning("Photo {PhotoId} has no date_taken, skipping", nasaId);
                    skipped++;
                    continue;
                }

                var earthDate = DateTime.SpecifyKind(dateTaken.Value.Date, DateTimeKind.Utc);

                // Extract extended object for additional metadata
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
                    DateTakenUtc = DateTime.SpecifyKind(dateTaken.Value, DateTimeKind.Utc),
                    RoverId = PerseveranceRoverId,
                    CameraId = camera.Id,

                    // Image URLs - multiple sizes from NASA
                    ImgSrcFull = TryGetString(photo, "image_files", "full_res") ?? "",
                    ImgSrcSmall = TryGetString(photo, "image_files", "small") ?? "",
                    ImgSrcMedium = TryGetString(photo, "image_files", "medium") ?? "",
                    ImgSrcLarge = TryGetString(photo, "image_files", "large") ?? "",

                    // Image metadata
                    Width = TryGetInt(extended, "scaleFactor"),
                    Height = TryGetInt(extended, "subframeRect"),
                    SampleType = TryGetString(photo, "sample_type") ?? "unknown",
                    Title = TryGetString(photo, "title"),
                    Caption = TryGetString(photo, "caption"),
                    Credit = TryGetString(photo, "credit"),

                    // Location data
                    Site = TryGetInt(photo, "site"),
                    Drive = TryGetInt(photo, "drive"),
                    Xyz = TryGetString(photo, "xyz"),

                    // Camera telemetry
                    MastAz = mastAz,
                    MastEl = mastEl,
                    CameraVector = TryGetString(photo, "camera_vector"),
                    CameraPosition = TryGetString(photo, "camera_position"),
                    CameraModelType = TryGetString(photo, "camera_model_type"),

                    // Mars time
                    DateTakenMars = TryGetString(extended, "lmst") ?? "",
                    MarsTimeHour = MarsTimeHelper.ExtractHour(TryGetString(extended, "lmst")),
                    Attitude = TryGetString(photo, "attitude"),
                    SpacecraftClock = TryGetFloat(photo, "spacecraft_clock"),

                    // Receiving metadata
                    DateReceived = TryGetDateTime(photo, "date_received"),
                    FilterName = TryGetString(extended, "filter_name"),

                    // Store complete NASA response in JSONB
                    RawData = JsonDocument.Parse(photo.GetRawText()),

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Photos.Add(photoEntity);
                inserted++;
            }
            catch (Exception ex)
            {
                var photoId = TryGetInt(photo, "imageid")?.ToString() ?? "unknown";
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
    // HELPER METHODS
    // ============================================================================

    private static string? TryGetString(JsonElement element, string property, string? nestedProperty = null)
    {
        if (element.ValueKind == JsonValueKind.Undefined)
            return null;

        if (!element.TryGetProperty(property, out var value))
            return null;

        if (nestedProperty != null)
        {
            if (value.ValueKind != JsonValueKind.Object)
                return null;
            return TryGetString(value, nestedProperty);
        }

        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;

        return value.GetString();
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
