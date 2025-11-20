using System.Text.Json;
using System.Text.RegularExpressions;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

/// <summary>
/// Scraper for NASA's Perseverance rover API
/// </summary>
public class PerseveranceScraper : IScraperService
{
    private const string ApiLatestMetadataUrl = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true";
    private const string ApiSolUrl = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol={0}";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PerseveranceScraper> _logger;

    public string RoverName => "Perseverance";

    public PerseveranceScraper(
        IHttpClientFactory httpClientFactory,
        MarsVistaDbContext context,
        ILogger<PerseveranceScraper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scrape for {RoverName} (latest photos)", RoverName);

        var httpClient = _httpClientFactory.CreateClient("NASA");

        // Get latest sol number
        var metadataResponse = await httpClient.GetStringAsync(ApiLatestMetadataUrl, cancellationToken);
        var metadata = JsonDocument.Parse(metadataResponse);
        var latestSol = metadata.RootElement.GetProperty("latest_sol").GetInt32();

        _logger.LogInformation("Latest sol for {RoverName}: {Sol}", RoverName, latestSol);

        // Fetch images for latest sol
        return await ScrapeSolAsync(latestSol, cancellationToken);
    }

    public async Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scrape for {RoverName} sol {Sol}", RoverName, sol);

        var httpClient = _httpClientFactory.CreateClient("NASA");
        var url = string.Format(ApiSolUrl, sol);
        var response = await httpClient.GetStringAsync(url, cancellationToken);

        return await ProcessResponseAsync(response, cancellationToken);
    }

    private async Task<int> ProcessResponseAsync(string jsonResponse, CancellationToken cancellationToken)
    {
        var jsonDoc = JsonDocument.Parse(jsonResponse);
        var images = jsonDoc.RootElement.GetProperty("images").EnumerateArray().ToList();

        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstAsync(r => r.Name == RoverName, cancellationToken);

        // PERFORMANCE: Batch fetch existing nasa_ids to avoid N+1 queries
        // Extract all nasa_ids from the response
        var nasaIdsInResponse = images
            .Where(img => img.GetProperty("sample_type").GetString() == "Full")
            .Select(img => img.GetProperty("imageid").GetString()!)
            .ToHashSet();

        // Fetch existing nasa_ids in a single query
        var existingNasaIds = await _context.Photos
            .Where(p => nasaIdsInResponse.Contains(p.NasaId))
            .Select(p => p.NasaId)
            .ToHashSetAsync(cancellationToken);

        _logger.LogDebug(
            "Batch duplicate check: {Total} photos in response, {Existing} already exist",
            nasaIdsInResponse.Count, existingNasaIds.Count);

        var newPhotos = new List<Photo>();

        foreach (var imageElement in images)
        {
            try
            {
                // Only process full-resolution images
                var sampleType = imageElement.GetProperty("sample_type").GetString();
                if (sampleType != "Full")
                {
                    _logger.LogDebug("Skipping non-full image: {SampleType}", sampleType);
                    continue;
                }

                var nasaId = imageElement.GetProperty("imageid").GetString()!;

                // Check if photo already exists (O(1) in-memory lookup)
                if (existingNasaIds.Contains(nasaId))
                {
                    _logger.LogDebug("Photo {NasaId} already exists, skipping", nasaId);
                    continue;
                }

                // Extract photo data
                var photo = await ExtractPhotoDataAsync(imageElement, rover, cancellationToken);
                if (photo != null)
                {
                    newPhotos.Add(photo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing photo from NASA response");
            }
        }

        // Bulk insert new photos
        if (newPhotos.Any())
        {
            await _context.Photos.AddRangeAsync(newPhotos, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Scraped {Count} new photos for {RoverName}", newPhotos.Count, RoverName);
        }
        else
        {
            _logger.LogInformation("No new photos found for {RoverName}", RoverName);
        }

        return newPhotos.Count;
    }

    private async Task<Photo?> ExtractPhotoDataAsync(
        JsonElement imageElement,
        Rover rover,
        CancellationToken cancellationToken)
    {
        try
        {
            var nasaId = imageElement.GetProperty("imageid").GetString()!;
            var sol = imageElement.GetProperty("sol").GetInt32();

            // Get or create camera
            var cameraName = imageElement.GetProperty("camera")
                .GetProperty("instrument").GetString()!;
            var camera = await GetOrCreateCameraAsync(cameraName, rover, cancellationToken);

            // Extract image URLs
            var imageFiles = imageElement.GetProperty("image_files");

            // Extract dates (NASA provides UTC timestamps, specify kind for PostgreSQL)
            var dateTakenUtc = DateTime.SpecifyKind(
                DateTime.Parse(imageElement.GetProperty("date_taken_utc").GetString()!),
                DateTimeKind.Utc);
            var dateTakenMars = imageElement.GetProperty("date_taken_mars").GetString();

            // Calculate earth date from sol (if landing date is available)
            DateTime? earthDate = rover.LandingDate.HasValue
                ? CalculateEarthDate(sol, rover.LandingDate.Value)
                : null;

            // Extract extended telemetry (may not exist for all photos)
            imageElement.TryGetProperty("extended", out var extended);

            // Extract all fields using pure functions
            var (width, height) = ExtractDimensions(extended);

            // Create photo entity with ALL data
            var photo = new Photo
            {
                NasaId = nasaId,
                Sol = sol,
                EarthDate = earthDate,
                DateTakenUtc = dateTakenUtc,
                DateTakenMars = dateTakenMars,
                DateReceived = TryGetDateTime(imageElement, "date_received"),

                // Image URLs
                ImgSrcSmall = TryGetString(imageFiles, "small"),
                ImgSrcMedium = TryGetString(imageFiles, "medium"),
                ImgSrcLarge = TryGetString(imageFiles, "large"),
                ImgSrcFull = TryGetString(imageFiles, "full_res"),
                Width = width,
                Height = height,
                SampleType = imageElement.GetProperty("sample_type").GetString(),

                // Location
                Site = TryGetInt(imageElement, "site"),
                Drive = TryGetInt(imageElement, "drive"),
                Xyz = TryGetString(extended, "xyz"),

                // Camera telemetry
                MastAz = TryGetFloat(extended, "mastAz"),
                MastEl = TryGetFloat(extended, "mastEl"),
                CameraVector = TryGetString(imageElement.GetProperty("camera"), "camera_vector"),
                CameraPosition = TryGetString(imageElement.GetProperty("camera"), "camera_position"),
                CameraModelType = TryGetString(imageElement.GetProperty("camera"), "camera_model_type"),
                FilterName = TryGetString(imageElement.GetProperty("camera"), "filter_name"),

                // Rover telemetry
                Attitude = TryGetString(imageElement, "attitude"),
                SpacecraftClock = TryGetFloat(extended, "sclk"),

                // Metadata
                Title = TryGetString(imageElement, "title"),
                Caption = TryGetString(imageElement, "caption"),
                Credit = TryGetString(imageElement, "credit"),

                // Relationships
                RoverId = rover.Id,
                CameraId = camera.Id,

                // Store complete NASA response in JSONB
                RawData = JsonDocument.Parse(imageElement.GetRawText()),

                // Timestamps set automatically by database defaults
            };

            return photo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract photo data from JSON element");
            return null;
        }
    }

    private async Task<Camera> GetOrCreateCameraAsync(
        string cameraName,
        Rover rover,
        CancellationToken cancellationToken)
    {
        // Check if camera exists in rover's camera collection
        var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

        if (camera == null)
        {
            // Auto-create new camera (happens when NASA adds new instruments)
            _logger.LogWarning(
                "Unknown camera discovered: {CameraName} for {RoverName}. Auto-creating.",
                cameraName, rover.Name);

            camera = new Camera
            {
                Name = cameraName,
                FullName = cameraName, // Will be updated manually later
                RoverId = rover.Id
            };

            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync(cancellationToken);

            // Add to rover's collection so subsequent photos find it
            rover.Cameras.Add(camera);

            _logger.LogInformation("Created new camera: {CameraName} (ID: {CameraId})",
                cameraName, camera.Id);
        }

        return camera;
    }

    // ============================================================================
    // PURE CALCULATION FUNCTIONS (No side effects, always return same output for same input)
    // ============================================================================

    /// <summary>
    /// Calculate Earth date from Mars sol and rover landing date
    /// Formula: earthDate = landingDate + (sol * secondsPerSol / secondsPerDay)
    /// </summary>
    private static DateTime CalculateEarthDate(int sol, DateTime landingDate)
    {
        const double SecondsPerSol = 88775.244;
        const double SecondsPerDay = 86400;

        var earthDaysSinceLanding = sol * SecondsPerSol / SecondsPerDay;
        return landingDate.AddDays(earthDaysSinceLanding);
    }

    /// <summary>
    /// Extract image dimensions from extended data
    /// Format: "(width,height)" -> (width, height)
    /// </summary>
    private static (int? Width, int? Height) ExtractDimensions(JsonElement extended)
    {
        var dimensionStr = TryGetString(extended, "dimension");
        if (string.IsNullOrEmpty(dimensionStr))
            return (null, null);

        var match = Regex.Match(dimensionStr, @"\((\d+),(\d+)\)");
        if (!match.Success)
            return (null, null);

        var width = int.Parse(match.Groups[1].Value);
        var height = int.Parse(match.Groups[2].Value);
        return (width, height);
    }

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
