# NASA Mars Rover Image APIs - Complete Documentation

## Overview

This Rails API doesn't store the actual images - it stores **metadata and URLs** pointing to images hosted on NASA's servers. The application scrapes three different NASA data sources to collect this information, then serves it through a unified API.

## Data Flow Architecture

```
NASA's Mars Rovers (on Mars)
    ↓ (transmit images to Earth)
NASA/JPL receives images
    ↓ (processes and hosts on servers)
NASA Public APIs & Websites
    ↓ (scraped by this Rails app)
Mars Photo API Database (PostgreSQL)
    - Stores image URLs (not actual images)
    - Stores metadata (sol, camera, earth_date)
    ↓ (served via REST API)
Your API Consumers
    → Fetch actual images directly from NASA URLs
```

## NASA Data Sources

### 1. Perseverance Rover - RSS/JSON API

**Active Status**: ✅ Currently Active (tested October 2025)

**API Endpoints**:
- Latest Sol Info: `https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true`
- Sol-Specific Images: `https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol={sol_number}`

**API Structure**:

```json
// Latest Sol Response
{
  "latest": "2025-10-07T14:26:56Z",
  "new_count": 1032,
  "sol_count": 164,
  "latest_sols": [1646, 1645],
  "total": 887260,
  "type": "mars2020-latest-images-1.1",
  "latest_sol": 1646
}

// Sol-Specific Response
{
  "images": [
    {
      "sol": 1646,
      "image_files": {
        "small": "https://mars.nasa.gov/.../image_320.jpg",
        "medium": "https://mars.nasa.gov/.../image_800.jpg",
        "large": "https://mars.nasa.gov/.../image_1200.jpg",
        "full_res": "https://mars.nasa.gov/.../image.png"
      },
      "camera": {
        "instrument": "NAVCAM_RIGHT"  // Camera abbreviation
      },
      "sample_type": "Full",  // Filter for full-resolution only
      "date_taken_utc": "2025-10-07T02:08:38.572",
      "extended": {
        "mastAz": "-156.098",
        "mastEl": "-10.1652",
        // Additional telemetry data
      }
    }
  ]
}
```

**Key Details**:
- This is an **unofficial RSS feed API** (not documented publicly)
- Returns JSON despite being called "RSS API"
- Includes multiple image sizes (small, medium, large, full_res)
- The scraper only collects `sample_type == 'Full'` images
- Uses the `large` image URL (1200px wide) for storage

### 2. Curiosity Rover - Raw Image Items API

**Active Status**: ✅ Currently Active (tested October 2025)

**API Endpoint**:
- Base: `https://mars.nasa.gov/api/v1/raw_image_items/`
- With parameters: `?order=sol%20desc&per_page=200&condition_1=msl:mission&condition_2={sol}:sol:in`

**API Structure**:

```json
{
  "items": [
    {
      "id": 1523343,
      "sol": 4681,
      "instrument": "CHEMCAM_RMI",  // Camera name
      "url": "https://mars.nasa.gov/msl-raw-images/.../image.PNG",
      "https_url": "https://mars.nasa.gov/msl-raw-images/.../image.PNG",
      "extended": {
        "sample_type": "chemcam prc",  // Filter for "full" only
        "lmst": "Sol-04681M10:40:32.355",
        "mast_az": "346.566",
        "mast_el": "-44.9492"
      },
      "date_taken": "2025-10-06T17:16:46.000Z",
      "site": 119,
      "drive": 1344
    }
  ],
  "more": true,
  "total": 1388223,
  "page": 0,
  "per_page": 200
}
```

**Key Details**:
- This is a **semi-public API** (not officially documented but stable)
- Paginated results (200 per page)
- Filters for `sample_type == 'full'` in extended properties
- Auto-creates new cameras when discovered
- Uses `https_url` field for image storage

### 3. Opportunity & Spirit Rovers - HTML Gallery Scraping

**Active Status**: ❌ DEPRECATED (as of October 2025)

**Original Approach**:
- Base URL: `https://mars.nasa.gov/mer/gallery/all/`
- Scraped HTML pages: `opportunity.html` and `spirit.html`
- Parsed dropdown menus to find sol/camera combinations
- Followed links to individual photo pages

**Current Status**:
- **These URLs now redirect** to `https://science.nasa.gov/mars/resources/`
- The original MER (Mars Exploration Rover) galleries have been removed
- Historical data already scraped remains in the database
- No new photos available (rovers inactive since 2010/2018)

**Original Data Structure** (for reference):
```
Base Gallery → Sol/Camera Dropdowns → Photo List Pages → Individual Images
Example path: /mer/gallery/all/1/f/001/1F128284889EFF00E1P1111L0M1.HTML
```

## Important Notes About These APIs

### 1. These Are NOT Official Public APIs

- **No official documentation exists** for the RSS/JSON endpoints
- NASA doesn't guarantee stability or availability
- The APIs could change structure or disappear without notice
- They appear to be internal feeds made publicly accessible

### 2. Alternative: NASA's Official API

NASA does have an **official Mars Rover Photos API** at:
- Endpoint: `https://api.nasa.gov/mars-photos/api/v1/`
- Documentation: `https://api.nasa.gov/`
- Requires API key (free registration)

Example:
```
https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&api_key=YOUR_KEY
```

However, this Rails application predates the official API and uses the unofficial endpoints.

### 3. Why Use These Unofficial APIs?

- **More comprehensive data**: Includes telemetry, positions, extended metadata
- **Real-time updates**: RSS feeds update immediately when new photos arrive
- **No rate limiting**: No API key required
- **Historical precedence**: These feeds existed before the official API

## Image Storage Architecture

### What This API Stores

```ruby
# Photos table only stores:
- img_src: "https://mars.nasa.gov/mars2020-raw-images/pub/..."  # URL only
- sol: 1646                                                       # Martian day
- camera_id: 5                                                   # Foreign key
- rover_id: 1                                                    # Foreign key
- earth_date: "2025-10-07"                                      # Calculated
```

### What Happens When Users Request Images

1. **Your API returns metadata**:
```json
{
  "id": 123,
  "sol": 1646,
  "camera": {"name": "NAVCAM_RIGHT"},
  "img_src": "https://mars.nasa.gov/mars2020-raw-images/pub/...",
  "earth_date": "2025-10-07"
}
```

2. **Client applications fetch images directly from NASA**:
   - The `img_src` URL points to NASA's CDN
   - Images are served by NASA's infrastructure
   - Your API never handles actual image data

### Benefits of This Architecture

- **No storage costs**: Images remain on NASA's servers
- **No bandwidth costs**: Images served directly from NASA
- **Always up-to-date**: If NASA updates an image, users get the latest
- **Legal compliance**: No copyright concerns, linking is allowed
- **Scalability**: Your API only serves lightweight JSON

## Scraping Strategy

### Incremental Scraping Algorithm

```ruby
# All scrapers use this pattern:
latest_sol_available = fetch_from_nasa_api()
latest_sol_scraped = database.maximum(:sol)
sols_to_scrape = (latest_sol_scraped..latest_sol_available)

# Only fetch new data, never re-scrape old sols
```

### Deduplication Strategy

```ruby
# Database level: Unique composite index
add_index(:photos, [:sol, :camera_id, :img_src, :rover_id], unique: true)

# Application level: Find or initialize
Photo.find_or_initialize_by(sol: sol, camera: camera, img_src: url, rover: rover)
```

## Understanding NASA's Data

### Sol vs Earth Date

- **Sol**: A Martian day (24 hours, 39 minutes, 35.244 seconds)
- **Sol 0**: The day the rover landed on Mars
- **Calculation**: `earth_date = landing_date + (sol * 1.0275 days)`

### Camera Abbreviations

```
FHAZ    = Front Hazard Avoidance Camera
RHAZ    = Rear Hazard Avoidance Camera
NAVCAM  = Navigation Camera
MAST    = Mast Camera
CHEMCAM = Chemistry Camera
MAHLI   = Mars Hand Lens Imager
MARDI   = Mars Descent Imager
```

### Image Quality Types

- **Thumbnail**: ~64x64 pixels
- **Small**: 320px wide
- **Medium**: 800px wide
- **Large**: 1200px wide
- **Full**: Original resolution (varies, often 2000+ pixels)

## For Your C#/.NET Implementation

### Recommended Approach

1. **Use the Official NASA API**:
   - It's documented and stable
   - Has guaranteed support
   - Includes all active rovers

2. **If Using Unofficial APIs**:
   - Implement robust error handling
   - Cache aggressively
   - Monitor for structure changes
   - Have fallback to official API

3. **Architecture Considerations**:
   - Store only URLs and metadata (like this Rails app)
   - Use background jobs for scraping (Hangfire, etc.)
   - Implement circuit breakers for API calls
   - Add retry logic with exponential backoff

### Example C# HTTP Client Setup

```csharp
public class NasaApiClient
{
    private readonly HttpClient _httpClient;

    public NasaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://mars.nasa.gov/");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mars-Photo-Scraper/1.0");
    }

    public async Task<PerseveranceLatestSol> GetLatestSolAsync()
    {
        var response = await _httpClient.GetAsync(
            "rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true"
        );
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PerseveranceLatestSol>(json);
    }
}
```

## Summary

The NASA "RSS API" you're seeing isn't officially documented because it's an internal feed that happens to be publicly accessible. These are the actual endpoints NASA uses for their own Mars mission websites. The Rails application cleverly leverages these feeds to aggregate Mars rover photos without storing any actual image data - just metadata and URLs pointing to NASA's servers.

For your C#/.NET implementation, you have two choices:
1. Use NASA's official API (recommended for stability)
2. Replicate this approach using the unofficial feeds (more data, but less stable)

The key insight is that this entire system is just a **metadata aggregator** - the actual images always live on NASA's servers, and your API just helps users find them.