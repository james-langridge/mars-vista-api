# Story 006: NASA API Scraper Service (Perseverance)

## Story
As a system, I need to automatically scrape photos from NASA's Perseverance API and store them in the database so that users can browse the latest Mars rover photos.

## Acceptance Criteria
- [ ] HTTP client configured with retry and circuit breaker patterns
- [ ] Perseverance scraper service created and registered
- [ ] Scraper extracts all 30+ fields from NASA JSON response
- [ ] Photos stored with hybrid approach (indexed columns + JSONB)
- [ ] Scraper is idempotent (duplicate photos skipped)
- [ ] Unknown cameras auto-created and logged
- [ ] Earth date calculated from sol and landing date
- [ ] Bulk insert for performance (batch processing)
- [ ] Scraper can be triggered manually via CLI/API
- [ ] Comprehensive error handling and logging
- [ ] Successfully scrapes at least 50 photos from Perseverance

## Context

NASA provides an unofficial JSON API for Perseverance photos:
- **Latest photos:** `https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true`
- **Sol-specific:** `https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol={sol}`

Each response contains 30+ fields per photo including:
- Core identifiers (imageid, sol, camera)
- Dates (date_taken_utc, date_taken_mars, date_received)
- Image URLs (small, medium, large, full_res)
- Location (site, drive, xyz coordinates)
- Camera telemetry (mastAz, mastEl, camera_vector, camera_position)
- Metadata (title, caption, credit, attitude)

We need to:
1. Fetch JSON from NASA
2. Extract and store ALL fields (hybrid storage)
3. Handle new cameras gracefully
4. Process efficiently (batch inserts)
5. Be resilient (retry, circuit breaker)

## Implementation Steps

### 1. Add Required NuGet Packages

We need HTTP client with resilience patterns (retry, circuit breaker):

```bash
cd src/MarsVista.Api
dotnet add package Microsoft.Extensions.Http.Polly
dotnet add package Polly
```

**Why Polly?**
- Industry-standard resilience library for .NET
- Retry policies (exponential backoff)
- Circuit breaker (fail fast when NASA API is down)
- Timeout policies
- Integrates with HttpClient

**Documentation:**
- [Polly Documentation](https://github.com/App-vNext/Polly)
- [HTTP Client Factory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)

### 2. Create Scraper Service Interface

**File:** `src/MarsVista.Api/Services/IScraperService.cs`

```csharp
namespace MarsVista.Api.Services;

/// <summary>
/// Interface for NASA API scraper services
/// </summary>
public interface IScraperService
{
    /// <summary>
    /// Scrapes photos from NASA API and stores them in the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for stopping the scrape</param>
    /// <returns>Number of new photos scraped</returns>
    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes photos for a specific sol
    /// </summary>
    /// <param name="sol">Mars sol to scrape</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of new photos scraped</returns>
    Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rover name this scraper is for
    /// </summary>
    string RoverName { get; }
}
```

**Why an interface?**
- Multiple rovers need different scrapers (Perseverance, Curiosity)
- Testable (mock for unit tests)
- Dependency injection
- Consistent API across scrapers

### 3. Create Perseverance Scraper Implementation

**File:** `src/MarsVista.Api/Services/PerseveranceScraper.cs`

```csharp
using System.Text.Json;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

/// <summary>
/// Scraper for NASA's Perseverance rover API
/// </summary>
public class PerseveranceScraper : IScraperService
{
    private const string ApiLatestUrl = "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true";
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
        var response = await httpClient.GetStringAsync(ApiLatestUrl, cancellationToken);

        return await ProcessResponseAsync(response, cancellationToken);
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
        var images = jsonDoc.RootElement.GetProperty("images").EnumerateArray();

        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstAsync(r => r.Name == RoverName, cancellationToken);

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

                // Check if photo already exists (idempotency)
                var exists = await _context.Photos
                    .AnyAsync(p => p.NasaId == nasaId, cancellationToken);

                if (exists)
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
            var imgSrcSmall = TryGetString(imageFiles, "small");
            var imgSrcMedium = TryGetString(imageFiles, "medium");
            var imgSrcLarge = TryGetString(imageFiles, "large");
            var imgSrcFull = TryGetString(imageFiles, "full_res");

            // Extract dates
            var dateTakenUtc = DateTime.Parse(imageElement.GetProperty("date_taken_utc").GetString()!);
            var dateTakenMars = imageElement.GetProperty("date_taken_mars").GetString();
            var dateReceived = TryGetDateTime(imageElement, "date_received");

            // Calculate earth date from sol
            var earthDate = CalculateEarthDate(sol, rover.LandingDate);

            // Extract extended telemetry (may not exist for all photos)
            JsonElement extended = default;
            imageElement.TryGetProperty("extended", out extended);

            // Extract location data
            var site = TryGetInt(imageElement, "site");
            var drive = TryGetInt(imageElement, "drive");
            var xyz = TryGetString(extended, "xyz");

            // Extract camera telemetry
            var mastAz = TryGetFloat(extended, "mastAz");
            var mastEl = TryGetFloat(extended, "mastEl");
            var cameraElement = imageElement.GetProperty("camera");
            var cameraVector = TryGetString(cameraElement, "camera_vector");
            var cameraPosition = TryGetString(cameraElement, "camera_position");
            var cameraModelType = TryGetString(cameraElement, "camera_model_type");
            var filterName = TryGetString(cameraElement, "filter_name");

            // Extract rover telemetry
            var attitude = TryGetString(imageElement, "attitude");
            var spacecraftClock = TryGetFloat(extended, "sclk");

            // Extract metadata
            var title = TryGetString(imageElement, "title");
            var caption = TryGetString(imageElement, "caption");
            var credit = TryGetString(imageElement, "credit");
            var sampleType = imageElement.GetProperty("sample_type").GetString();

            // Extract dimensions (stored in "dimension" as "(width,height)")
            int? width = null;
            int? height = null;
            var dimensionStr = TryGetString(extended, "dimension");
            if (!string.IsNullOrEmpty(dimensionStr))
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    dimensionStr, @"\((\d+),(\d+)\)");
                if (match.Success)
                {
                    width = int.Parse(match.Groups[1].Value);
                    height = int.Parse(match.Groups[2].Value);
                }
            }

            // Create photo entity with ALL data
            var photo = new Photo
            {
                NasaId = nasaId,
                Sol = sol,
                EarthDate = earthDate,
                DateTakenUtc = dateTakenUtc,
                DateTakenMars = dateTakenMars,
                DateReceived = dateReceived,

                // Image URLs
                ImgSrcSmall = imgSrcSmall,
                ImgSrcMedium = imgSrcMedium,
                ImgSrcLarge = imgSrcLarge,
                ImgSrcFull = imgSrcFull,
                Width = width,
                Height = height,
                SampleType = sampleType,

                // Location
                Site = site,
                Drive = drive,
                Xyz = xyz,

                // Camera telemetry
                MastAz = mastAz,
                MastEl = mastEl,
                CameraVector = cameraVector,
                CameraPosition = cameraPosition,
                CameraModelType = cameraModelType,
                FilterName = filterName,

                // Rover telemetry
                Attitude = attitude,
                SpacecraftClock = spacecraftClock,

                // Metadata
                Title = title,
                Caption = caption,
                Credit = credit,

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

    // Helper methods for safe JSON extraction
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

            // Some numeric fields come as strings
            if (value.ValueKind == JsonValueKind.String &&
                float.TryParse(value.GetString(), out var floatValue))
            {
                return floatValue;
            }
        }

        return null;
    }

    private static DateTime? TryGetDateTime(JsonElement element, string property)
    {
        var str = TryGetString(element, property);
        if (str != null && DateTime.TryParse(str, out var dateTime))
        {
            return dateTime;
        }
        return null;
    }
}
```

**Key Design Choices:**

1. **Idempotency**: Check if photo exists before inserting
   - Uses `nasa_id` unique constraint
   - Safe to run scraper multiple times
   - Won't create duplicates

2. **Auto-Create Cameras**: Unknown cameras logged and created automatically
   - NASA occasionally adds new instruments
   - Scraper doesn't crash, just warns
   - Can update camera full name later

3. **Bulk Processing**: AddRangeAsync for performance
   - Single transaction for all photos
   - Much faster than individual inserts
   - All-or-nothing (transaction rollback on error)

4. **Complete Data Storage**: Extract ALL fields
   - 30+ structured columns
   - Complete JSON in `raw_data` JSONB field
   - 100% data preservation

5. **Safe JSON Extraction**: Helper methods with null checks
   - NASA data is inconsistent (some fields missing)
   - Prevents crashes on missing/null fields
   - Graceful degradation

### 4. Register HTTP Client with Resilience Policies

**File:** `src/MarsVista.Api/Program.cs`

Update to add HTTP client configuration:

```csharp
using MarsVista.Api.Data;
using MarsVista.Api.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Database context
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    )
    .UseSnakeCaseNamingConvention());

// HTTP client for NASA API with resilience policies
builder.Services.AddHttpClient("NASA", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaAPI/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Scraper services
builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed database on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Retry policy with exponential backoff
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, network failures
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Request failed. Waiting {timespan} before retry {retryCount}...");
            });
}

// Circuit breaker - stop hitting NASA API if it's down
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1));
}
```

**What these policies do:**

1. **Retry Policy** (Exponential Backoff):
   - Retry on transient errors (500, 502, 503, 504, network failures)
   - Wait 2s, 4s, 8s between retries
   - Max 3 retries
   - Logs retry attempts

2. **Circuit Breaker**:
   - After 5 consecutive failures, stop trying for 1 minute
   - Prevents overwhelming NASA's API when it's down
   - Fail fast instead of waiting for timeouts
   - Automatically resets after break duration

3. **Timeout**: 30 seconds per request

**Documentation:**
- [Polly Retry Policy](https://github.com/App-vNext/Polly#retry)
- [Polly Circuit Breaker](https://github.com/App-vNext/Polly#circuit-breaker)
- [HTTP Resilience in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)

### 5. Create Manual Scraper Trigger Endpoint

For testing and manual control, add an API endpoint to trigger scraping:

**File:** `src/MarsVista.Api/Controllers/ScraperController.cs`

```csharp
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly IEnumerable<IScraperService> _scrapers;
    private readonly ILogger<ScraperController> _logger;

    public ScraperController(
        IEnumerable<IScraperService> scrapers,
        ILogger<ScraperController> logger)
    {
        _scrapers = scrapers;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger scraping for a specific rover
    /// </summary>
    /// <param name="roverName">Rover name (e.g., "Perseverance")</param>
    /// <returns>Number of photos scraped</returns>
    [HttpPost("{roverName}")]
    public async Task<IActionResult> ScrapeRover(string roverName)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        _logger.LogInformation("Manual scrape triggered for {RoverName}", roverName);

        try
        {
            var count = await scraper.ScrapeAsync();
            return Ok(new
            {
                rover = roverName,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape failed for {RoverName}", roverName);
            return StatusCode(500, new { error = "Scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Scrape a specific sol for a rover
    /// </summary>
    /// <param name="roverName">Rover name</param>
    /// <param name="sol">Mars sol number</param>
    [HttpPost("{roverName}/sol/{sol}")]
    public async Task<IActionResult> ScrapeSol(string roverName, int sol)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
        {
            return NotFound(new { error = $"No scraper found for rover: {roverName}" });
        }

        _logger.LogInformation("Manual scrape triggered for {RoverName} sol {Sol}", roverName, sol);

        try
        {
            var count = await scraper.ScrapeSolAsync(sol);
            return Ok(new
            {
                rover = roverName,
                sol,
                photosScraped = count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scrape failed for {RoverName} sol {Sol}", roverName, sol);
            return StatusCode(500, new { error = "Scrape failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Get available scrapers
    /// </summary>
    [HttpGet]
    public IActionResult GetScrapers()
    {
        var scraperInfo = _scrapers.Select(s => new
        {
            rover = s.RoverName,
            scrapeUrl = $"/api/scraper/{s.RoverName.ToLower()}",
            scrapeSolUrl = $"/api/scraper/{s.RoverName.ToLower()}/sol/{{sol}}"
        });

        return Ok(scraperInfo);
    }
}
```

**Usage:**
```bash
# Scrape latest photos
curl -X POST http://localhost:5000/api/scraper/perseverance

# Scrape specific sol
curl -X POST http://localhost:5000/api/scraper/perseverance/sol/1000

# List available scrapers
curl http://localhost:5000/api/scraper
```

### 6. Run and Test the Scraper

Start the application:

```bash
cd src/MarsVista.Api
dotnet run
```

Trigger a scrape:

```bash
# In another terminal
curl -X POST http://localhost:5000/api/scraper/perseverance
```

Expected response:
```json
{
  "rover": "Perseverance",
  "photosScraped": 127,
  "timestamp": "2025-11-13T20:30:00.000Z"
}
```

Check the database:

```bash
docker exec marsvista-postgres psql -U marsvista -d marsvista_dev -c "
SELECT
  COUNT(*) as total_photos,
  COUNT(DISTINCT camera_id) as unique_cameras,
  MIN(sol) as min_sol,
  MAX(sol) as max_sol
FROM photos;"
```

Expected output:
```
 total_photos | unique_cameras | min_sol | max_sol
--------------+----------------+---------+---------
          127 |              8 |    1234 |    1245
```

View sample photos:

```bash
docker exec marsvista-postgres psql -U marsvista -d marsvista_dev -c "
SELECT
  p.nasa_id,
  p.sol,
  p.earth_date,
  c.name as camera,
  p.img_src_medium IS NOT NULL as has_medium_image
FROM photos p
JOIN cameras c ON p.camera_id = c.id
LIMIT 10;"
```

### 7. Verify JSONB Storage

Check that raw NASA data is stored:

```bash
docker exec marsvista-postgres psql -U marsvista -d marsvista_dev -c "
SELECT
  nasa_id,
  raw_data->>'imageid' as raw_id,
  raw_data->'camera'->>'instrument' as raw_camera,
  jsonb_typeof(raw_data) as data_type
FROM photos
LIMIT 3;"
```

You should see the complete JSON structure stored in `raw_data`.

## Technical Decisions

This story requires several technical decisions to be documented:

### Decision 006: Scraper Service Pattern
**File:** `.claude/decisions/006-scraper-service-pattern.md`

**Context:** How should scrapers be structured and registered?

**Options:**
1. One scraper per rover (recommended)
2. Single scraper with rover parameter
3. Hosted background service

**Recommendation:** One scraper service per rover implementing `IScraperService`

**Reasoning:**
- Each rover has different API format (Perseverance vs Curiosity JSON structure differs)
- Clean separation of concerns
- Easy to test individually
- Can register all scrapers and inject `IEnumerable<IScraperService>`
- Supports different scraping strategies per rover

### Decision 006A: HTTP Resilience Strategy
**File:** `.claude/decisions/006a-http-resilience.md`

**Context:** How to handle transient HTTP failures when calling NASA API?

**Recommendation:** Polly with retry + circuit breaker

**Reasoning:**
- NASA API occasionally times out or returns 5xx errors
- Exponential backoff prevents overwhelming their servers
- Circuit breaker stops wasting resources when API is down
- Industry-standard approach

### Decision 006B: Duplicate Photo Detection
**File:** `.claude/decisions/006b-duplicate-detection.md`

**Context:** How to prevent duplicate photos when running scraper multiple times?

**Recommendation:** Check database by `nasa_id` before inserting

**Reasoning:**
- Simple and reliable
- Leverages unique index on `nasa_id`
- No extra state management needed
- Works across multiple scraper instances

### Decision 006C: Unknown Camera Handling
**File:** `.claude/decisions/006c-unknown-camera-handling.md`

**Context:** What should scraper do when NASA adds a new camera/instrument?

**Recommendation:** Auto-create camera record and log warning

**Reasoning:**
- Scraper doesn't crash on new cameras
- Photo data not lost
- Warning alerts developers
- Can manually update camera full name later
- Resilient to NASA changes

### Decision 006D: Bulk vs Individual Inserts
**File:** `.claude/decisions/006d-bulk-insert-strategy.md`

**Context:** Should photos be inserted one at a time or in batches?

**Recommendation:** Batch insert with `AddRangeAsync` + single `SaveChangesAsync`

**Reasoning:**
- 10-100x faster than individual inserts
- Single transaction (atomic)
- Reduced database round trips
- EF Core optimization
- For 1000 photos: 30s vs 5 minutes

## Testing Checklist

- [ ] Polly package installed
- [ ] `IScraperService` interface created
- [ ] `PerseveranceScraper` implemented
- [ ] HTTP client registered with retry and circuit breaker
- [ ] Scraper registered in DI container
- [ ] `ScraperController` created with endpoints
- [ ] Application builds without errors
- [ ] Scraper endpoint returns 200 OK
- [ ] At least 50 photos successfully scraped
- [ ] Photos visible in database
- [ ] All 30+ fields populated correctly
- [ ] JSONB `raw_data` contains complete NASA response
- [ ] Duplicate photos skipped on re-run (idempotency)
- [ ] Unknown cameras auto-created and logged
- [ ] Earth date calculated correctly from sol
- [ ] Retry policy triggers on transient failures
- [ ] Circuit breaker prevents repeated failures

## Key Documentation Links

1. [Polly Documentation](https://github.com/App-vNext/Polly)
2. [HTTP Client Factory](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
3. [System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/how-to)
4. [EF Core Bulk Operations](https://learn.microsoft.com/en-us/ef/core/performance/efficient-updating)
5. [Resilient HTTP Requests](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)

## Success Criteria

✅ HTTP client configured with Polly resilience policies
✅ Perseverance scraper service implemented and registered
✅ All 30+ NASA data fields extracted and stored
✅ Hybrid storage working (indexed columns + JSONB)
✅ Scraper is idempotent (duplicates skipped)
✅ Unknown cameras handled gracefully
✅ Bulk insert for performance
✅ Manual trigger endpoint working
✅ At least 50 photos successfully scraped
✅ Data verified in PostgreSQL

## Next Steps

After completing this story, you'll be ready for:
- **Story 007:** Implement Curiosity scraper (different JSON structure)
- **Story 008:** Build REST API endpoints for querying photos
- **Story 009:** Add photo filtering, pagination, and search
- **Story 010:** Implement background job scheduler for automatic scraping

## Notes

### NASA API Quirks

**Inconsistent Data:**
- Some photos missing `extended` object
- Some fields are strings, others are numbers
- `dimension` field format: `"(1288,968)"` (string, not structured)
- Camera names vary by rover

**Rate Limiting:**
- No official rate limits documented
- Be respectful with requests
- Circuit breaker prevents abuse

**Data Quality:**
- Not all photos have all telemetry
- Older missions have less metadata
- `sample_type` filters out thumbnails/lower quality

### Sol vs Earth Date

Mars sol (solar day) is 24 hours, 39 minutes, 35.244 seconds.

Calculation:
```
earthDate = landingDate + (sol × 88775.244 / 86400 days)
```

This gives approximate Earth date when photo was taken.

### Image URL Sizes

NASA provides multiple sizes:
- `small`: ~320px wide (thumbnails)
- `medium`: ~800px wide (gallery view)
- `large`: ~1200px wide (detail view)
- `full_res`: Full resolution (2MB-20MB)

We store all URLs, letting client choose appropriate size.

### What is JSONB?

PostgreSQL's JSONB is **binary JSON**:
- Stored in optimized binary format (not text)
- Supports indexing with GIN indexes
- Query with operators: `->`, `->>`, `@>`, `?`, etc.
- Much faster than storing JSON as TEXT

Example query:
```sql
-- Find photos with specific extended field
SELECT * FROM photos
WHERE raw_data->'extended'->>'xyz' IS NOT NULL;

-- Query nested camera data
SELECT raw_data->'camera'->>'instrument'
FROM photos LIMIT 5;
```

This enables advanced queries without adding columns!
