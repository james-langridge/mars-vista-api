# Story 008: Curiosity Rover Scraper

## Story
As a Mars photo enthusiast, I need to access Curiosity rover photos through the API so that I can explore the longest-running Mars mission (2012-present, 4000+ sols).

## Acceptance Criteria
- [ ] Implement CuriosityScraper service using the strategy pattern
- [ ] Parse NASA's Curiosity JSON API format (different from Perseverance)
- [ ] Handle Curiosity's 7 cameras (FHAZ, RHAZ, MAST, CHEMCAM, MAHLI, MARDI, NAVCAM)
- [ ] Map NASA photo data to database entities
- [ ] Store complete NASA response in raw_data JSONB column
- [ ] Idempotent ingestion (skip duplicate nasa_photo_id)
- [ ] Support single-sol scraping: POST /api/scraper/curiosity?sol=1000
- [ ] Support bulk scraping: POST /api/scraper/curiosity/bulk?startSol=1&endSol=4000
- [ ] Progress monitoring works for Curiosity
- [ ] Handle API errors gracefully with retry logic
- [ ] Validate data before inserting into database

## Context

### Why Curiosity Next?

1. **Still Active**: Curiosity has been operating since 2012 (longest Mars mission)
2. **Massive Dataset**: 4000+ sols of photos (estimated 700K+ photos)
3. **Different API Format**: NASA uses a different JSON structure than Perseverance
4. **Scientific Value**: Gale Crater exploration, different terrain than Jezero
5. **Popular Rover**: Second most searched rover after Perseverance

### Curiosity API Endpoint

NASA provides a JSON API for Curiosity (unlike Perseverance's RSS):

```
https://mars.nasa.gov/msl-raw-images/msss/{{sol_padded}}/images.json
```

Example: `https://mars.nasa.gov/msl-raw-images/msss/00001/images.json`

**Key Differences from Perseverance:**
- Uses JSON instead of RSS/JSON hybrid
- Sol numbers are zero-padded to 5 digits (00001, 01000, 04000)
- Different field names and structure
- Simpler metadata (fewer extended fields than Perseverance)
- Images stored in different directory structure

### Sample Curiosity API Response

```json
{
  "images": [
    {
      "id": "NLA_397674890EDR_F0050104NCAM00354M1",
      "sol": 1,
      "camera": {
        "name": "NAVCAM_LEFT_B",
        "full_name": "Navigation Camera - Left B"
      },
      "date_taken": "2012-08-06T12:20:03.000Z",
      "earth_date": "2012-08-06",
      "img_src": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/00001/opgs/edr/ncam/NLA_397674890EDR_F0050104NCAM00354M1.JPG",
      "sample_type": "full",
      "url_list": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/00001/soas/rdr/thumbnails/NLA_397674890EDR_F0050104NCAM00354M1_DXXX.jpg"
    }
  ]
}
```

**Field Mapping:**
- `id` → `nasa_photo_id`
- `sol` → `sol`
- `date_taken` → `date_taken_utc`
- `earth_date` → `earth_date`
- `img_src` → `img_src_full`
- `sample_type` → `sample_type`
- `url_list` → thumbnail URL (if available)

### Curiosity Cameras (Already Seeded)

The database already has Curiosity's 7 camera systems:
- FHAZ (Front Hazard Avoidance Camera)
- RHAZ (Rear Hazard Avoidance Camera)
- MAST (Mast Camera - Left/Right)
- CHEMCAM (Chemistry and Camera Complex)
- MAHLI (Mars Hand Lens Imager)
- MARDI (Mars Descent Imager)
- NAVCAM (Navigation Camera - Left/Right)

## Implementation Steps

### 1. Create Curiosity Scraper Service

**File:** `src/MarsVista.Api/Services/CuriosityScraper.cs`

```csharp
using System.Text.Json;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class CuriosityScraper : IScraperService
{
    private readonly HttpClient _httpClient;
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<CuriosityScraper> _logger;

    private const string BaseUrl = "https://mars.nasa.gov/msl-raw-images/msss";
    private const int CuriosityRoverId = 2; // From seed data

    public CuriosityScraper(
        HttpClient httpClient,
        MarsVistaDbContext context,
        ILogger<CuriosityScraper> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _logger = logger;
    }

    public string RoverName => "Curiosity";

    public async Task<ScraperResult> ScrapePhotosAsync(
        int sol,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping Curiosity photos for sol {Sol}", sol);

        try
        {
            // Format sol with zero-padding (5 digits)
            var solPadded = sol.ToString("D5");
            var url = $"{BaseUrl}/{solPadded}/images.json";

            _logger.LogDebug("Fetching {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            // 404 means no photos for this sol (not an error)
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("No photos found for sol {Sol}", sol);
                return ScraperResult.Success(sol, 0, 0);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<CuriosityApiResponse>(json);

            if (data?.Images == null || data.Images.Count == 0)
            {
                _logger.LogInformation("No photos in response for sol {Sol}", sol);
                return ScraperResult.Success(sol, 0, 0);
            }

            _logger.LogInformation("Found {Count} photos for sol {Sol}", data.Images.Count, sol);

            // Process photos and insert into database
            var (inserted, skipped) = await ProcessPhotosAsync(data.Images, sol, cancellationToken);

            return ScraperResult.Success(sol, inserted, skipped);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error scraping sol {Sol}: {Message}", sol, ex.Message);
            return ScraperResult.Failure(sol, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping sol {Sol}", sol);
            return ScraperResult.Failure(sol, ex.Message);
        }
    }

    private async Task<(int inserted, int skipped)> ProcessPhotosAsync(
        List<CuriosityPhoto> photos,
        int sol,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var skipped = 0;

        // Get existing NASA photo IDs to avoid duplicates
        var nasaIds = photos.Select(p => p.Id).ToList();
        var existingIds = await _context.Photos
            .Where(p => nasaIds.Contains(p.NasaPhotoId))
            .Select(p => p.NasaPhotoId)
            .ToHashSetAsync(cancellationToken);

        foreach (var photo in photos)
        {
            try
            {
                // Skip if already exists
                if (existingIds.Contains(photo.Id))
                {
                    skipped++;
                    continue;
                }

                // Map camera name to database camera ID
                var camera = await _context.Cameras
                    .FirstOrDefaultAsync(
                        c => c.RoverId == CuriosityRoverId &&
                             c.Name.ToLower() == photo.Camera.Name.ToLower(),
                        cancellationToken);

                if (camera == null)
                {
                    _logger.LogWarning(
                        "Camera not found for Curiosity: {CameraName}. Skipping photo {PhotoId}",
                        photo.Camera.Name, photo.Id);
                    skipped++;
                    continue;
                }

                // Parse earth date
                if (!DateOnly.TryParse(photo.EarthDate, out var earthDate))
                {
                    _logger.LogWarning("Invalid earth_date: {Date}. Skipping photo {PhotoId}",
                        photo.EarthDate, photo.Id);
                    skipped++;
                    continue;
                }

                // Create photo entity
                var photoEntity = new Photo
                {
                    NasaPhotoId = photo.Id,
                    Sol = photo.Sol,
                    EarthDate = earthDate,
                    DateTakenUtc = photo.DateTaken,
                    RoverId = CuriosityRoverId,
                    CameraId = camera.Id,
                    ImgSrcFull = photo.ImgSrc,
                    SampleType = photo.SampleType,
                    RawData = JsonDocument.Parse(JsonSerializer.Serialize(photo)),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Extract thumbnail URL from url_list if available
                if (!string.IsNullOrWhiteSpace(photo.UrlList))
                {
                    photoEntity.ImgSrcSmall = photo.UrlList;
                }

                _context.Photos.Add(photoEntity);
                inserted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing photo {PhotoId}", photo.Id);
                skipped++;
            }
        }

        // Save changes in batch
        if (inserted > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Inserted {Count} new photos for sol {Sol}", inserted, sol);
        }

        return (inserted, skipped);
    }

    public async Task<BulkScraperResult> BulkScrapeAsync(
        int startSol,
        int endSol,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting bulk scrape for Curiosity: sols {Start}-{End}",
            startSol, endSol);

        var totalInserted = 0;
        var totalSkipped = 0;
        var successfulSols = 0;
        var failedSols = 0;

        for (var sol = startSol; sol <= endSol; sol++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Bulk scrape cancelled at sol {Sol}", sol);
                break;
            }

            var result = await ScrapePhotosAsync(sol, cancellationToken);

            if (result.Success)
            {
                totalInserted += result.PhotosInserted;
                totalSkipped += result.PhotosSkipped;
                successfulSols++;
            }
            else
            {
                failedSols++;
                _logger.LogWarning("Failed to scrape sol {Sol}: {Error}", sol, result.ErrorMessage);
            }

            // Small delay to be respectful to NASA servers
            await Task.Delay(100, cancellationToken);
        }

        _logger.LogInformation(
            "Bulk scrape complete. Successful: {Success}, Failed: {Failed}, Inserted: {Inserted}, Skipped: {Skipped}",
            successfulSols, failedSols, totalInserted, totalSkipped);

        return new BulkScraperResult
        {
            StartSol = startSol,
            EndSol = endSol,
            SuccessfulSols = successfulSols,
            FailedSols = failedSols,
            TotalPhotosInserted = totalInserted,
            TotalPhotosSkipped = totalSkipped
        };
    }
}

// DTOs for Curiosity API response
public class CuriosityApiResponse
{
    public List<CuriosityPhoto> Images { get; set; } = new();
}

public class CuriosityPhoto
{
    public string Id { get; set; } = string.Empty;
    public int Sol { get; set; }
    public CuriosityCamera Camera { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("date_taken")]
    public DateTime DateTaken { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("earth_date")]
    public string EarthDate { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("img_src")]
    public string ImgSrc { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("sample_type")]
    public string SampleType { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("url_list")]
    public string? UrlList { get; set; }
}

public class CuriosityCamera
{
    public string Name { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
}
```

### 2. Register Curiosity Scraper in Dependency Injection

**File:** `src/MarsVista.Api/Program.cs`

Update the scraper registration to support multiple rovers:

```csharp
// Register scrapers by rover name
builder.Services.AddKeyedScoped<IScraperService, PerseveranceScraper>("perseverance");
builder.Services.AddKeyedScoped<IScraperService, CuriosityScraper>("curiosity");
```

### 3. Update ScraperController to Support Multiple Rovers

**File:** `src/MarsVista.Api/Controllers/ScraperController.cs`

Update the controller to resolve the correct scraper based on rover name:

```csharp
[HttpPost("{rover}")]
public async Task<IActionResult> ScrapeSol(
    string rover,
    [FromQuery] int sol,
    CancellationToken cancellationToken)
{
    try
    {
        var scraper = HttpContext.RequestServices
            .GetKeyedService<IScraperService>(rover.ToLower());

        if (scraper == null)
        {
            return BadRequest(new { error = $"No scraper available for rover: {rover}" });
        }

        var result = await scraper.ScrapePhotosAsync(sol, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(500, new { error = result.ErrorMessage });
        }

        return Ok(new
        {
            rover = scraper.RoverName,
            sol,
            photos_inserted = result.PhotosInserted,
            photos_skipped = result.PhotosSkipped
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error scraping {Rover} sol {Sol}", rover, sol);
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpPost("{rover}/bulk")]
public async Task<IActionResult> BulkScrape(
    string rover,
    [FromQuery] int startSol,
    [FromQuery] int endSol,
    CancellationToken cancellationToken)
{
    try
    {
        var scraper = HttpContext.RequestServices
            .GetKeyedService<IScraperService>(rover.ToLower());

        if (scraper == null)
        {
            return BadRequest(new { error = $"No scraper available for rover: {rover}" });
        }

        if (startSol < 0 || endSol < startSol)
        {
            return BadRequest(new { error = "Invalid sol range" });
        }

        var result = await scraper.BulkScrapeAsync(startSol, endSol, cancellationToken);

        return Ok(new
        {
            rover = scraper.RoverName,
            start_sol = result.StartSol,
            end_sol = result.EndSol,
            successful_sols = result.SuccessfulSols,
            failed_sols = result.FailedSols,
            total_photos_inserted = result.TotalPhotosInserted,
            total_photos_skipped = result.TotalPhotosSkipped
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error bulk scraping {Rover}", rover);
        return StatusCode(500, new { error = ex.Message });
    }
}
```

### 4. Test the Curiosity Scraper

Start the API:
```bash
dotnet run --project src/MarsVista.Api
```

Test single sol scraping:
```bash
# Scrape Curiosity sol 1 (landing day - should have ~100 photos)
curl -X POST "http://localhost:5000/api/scraper/curiosity?sol=1"

# Scrape Curiosity sol 1000 (should have 200-400 photos)
curl -X POST "http://localhost:5000/api/scraper/curiosity?sol=1000"
```

Test bulk scraping (small range first):
```bash
# Scrape first 10 sols
curl -X POST "http://localhost:5000/api/scraper/curiosity/bulk?startSol=1&endSol=10"
```

Verify in database:
```bash
psql -h localhost -U marsvista -d marsvista_dev -c \
  "SELECT COUNT(*), MIN(sol), MAX(sol) FROM photos WHERE rover_id = 2;"
```

Query via API:
```bash
# Get Curiosity photos via query API
curl "http://localhost:5000/api/v1/rovers/curiosity/photos?sol=1" | jq
```

## Testing Checklist

- [ ] CuriosityScraper compiles without errors
- [ ] Service registered with keyed DI
- [ ] ScraperController updated to support multiple rovers
- [ ] POST /api/scraper/curiosity?sol=1 works
- [ ] Photos are inserted into database with correct rover_id (2)
- [ ] Camera mapping works for all Curiosity cameras
- [ ] Duplicate photos are skipped (idempotent)
- [ ] POST /api/scraper/curiosity/bulk works
- [ ] Progress monitoring shows Curiosity stats
- [ ] Query API returns Curiosity photos correctly
- [ ] RawData JSONB column contains complete NASA response
- [ ] 404 responses handled gracefully (no photos for sol)
- [ ] HTTP errors trigger retry via Polly policies

## Success Criteria

✅ Curiosity scraper implemented using strategy pattern
✅ NASA JSON API parsed correctly
✅ All 7 Curiosity cameras mapped to database
✅ Complete NASA response stored in raw_data JSONB
✅ Idempotent ingestion prevents duplicates
✅ Single-sol and bulk scraping both work
✅ Progress monitoring includes Curiosity
✅ Photos queryable via /api/v1/rovers/curiosity/photos
✅ Error handling with retry logic
✅ Successfully scraped at least 100 photos

## Performance Expectations

- **Sol 1** (landing): ~100 photos, ~2 seconds
- **Sol 1000** (typical): ~300 photos, ~5 seconds
- **Bulk 1-100**: ~10,000 photos, ~3 minutes
- **Full mission (1-4000)**: ~700K photos, ~10-12 hours

## Next Steps

After completing this story:
- **Story 009:** Opportunity and Spirit scrapers (legacy rovers, HTML scraping)
- **Story 010:** Redis caching layer for query performance
- **Story 011:** Extended photo details endpoint (all raw_data fields)
- **Story 012:** Background job scheduler for automatic daily scraping

## Notes

### Curiosity vs Perseverance Differences

| Feature | Perseverance | Curiosity |
|---------|-------------|-----------|
| API Format | RSS/JSON hybrid | Pure JSON |
| Sol Padding | No padding | 5 digits (00001) |
| Extended Data | 30+ fields | Minimal fields |
| Image Sizes | Multiple (small/medium/large/full) | Full + thumbnail only |
| Camera Telemetry | Full (mast_az, mast_el, xyz, etc.) | Limited |
| API Reliability | Very reliable | Occasional 404s |

### Why Separate Scrapers?

We're using the strategy pattern (different scraper per rover) because:
- Each rover has different NASA API endpoints and formats
- Perseverance: RSS/JSON with extensive metadata
- Curiosity: JSON with basic metadata
- Opportunity/Spirit: Legacy HTML scraping (no API)
- Easier to test and maintain separately
- Can optimize each scraper for its specific API

### Camera Name Variations

Curiosity cameras may have different naming conventions:
- `NAVCAM_LEFT_B` vs `NAVCAM_LEFT`
- `MAST_LEFT` vs `MASTCAM_LEFT`
- Use case-insensitive matching in camera lookup

### Curiosity Mission Context

- **Launch:** November 26, 2011
- **Landing:** August 6, 2012 (Gale Crater)
- **Mission Duration:** 12+ years (still active)
- **Mission:** Search for ancient habitable environments
- **Notable Achievement:** Confirmed Mars once had conditions suitable for life
- **Current Status:** Climbing Mount Sharp, studying rock layers
