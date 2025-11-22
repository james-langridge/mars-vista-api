# Mars Vista API

A C#/.NET API for Mars rover photo data, designed as a modern alternative to NASA's Mars Photo API. Provides access to all four major Mars rovers (Perseverance, Curiosity, Opportunity, and Spirit) with complete metadata preservation and advanced querying capabilities.

## Production API

The Mars Vista API is live and publicly accessible at:

**Base URL**: `https://api.marsvista.dev`

### Getting Started

**1. Get Your API Key**

Sign in at [marsvista.dev/signin](https://marsvista.dev/signin) using magic link authentication (no password needed):
- Enter your email address
- Click the magic link sent to your email
- Generate your API key from the dashboard

**2. Make Your First Request**

Use your API key in the `X-API-Key` header:

```bash
# Get all available rovers
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers"

# Query Perseverance photos from Sol 1000
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1000&per_page=10"
```

**3. Check API Health (no auth required)**

```bash
curl "https://api.marsvista.dev/health"
```

### Rate Limits

The Mars Vista API is free for all users with generous rate limits:

- 10,000 requests per hour (10x NASA's API Gateway limit)
- 100,000 requests per day
- 50 concurrent requests

These generous limits are suitable for most applications, from personal projects to production services.

Complete API documentation available in [API_ENDPOINTS.md](docs/API_ENDPOINTS.md).

## Features

- **Modern API v2 with Phase 1 Enhancements**: Enhanced REST API with nested resources, Mars time filtering, location-based queries, image quality filters, camera angle searches, and field set control
- **Interactive Documentation**: Swagger UI with OpenAPI specification for easy API exploration
- **API Key Authentication**: Secure per-user API keys with generous rate limiting
- **Multi-Rover Support**: All four major Mars rovers (Perseverance, Curiosity, Opportunity, Spirit) with automatic data source adaptation
- **Complete Data Preservation**: Stores all 30-55 metadata fields using hybrid PostgreSQL storage (indexed columns + JSONB)
- **Dual Scraper Architecture**: NASA JSON API for active rovers (Perseverance, Curiosity) and PDS index files for historic MER rovers (Opportunity, Spirit)
- **High Performance**: Processes 500-1000+ photos per second with batch insert optimization and in-memory duplicate checking
- **Resilient HTTP Client**: Polly-based retry policies and circuit breakers for reliable NASA API communication
- **Idempotent Operations**: Duplicate detection prevents re-scraping already stored photos
- **Progress Monitoring**: Real-time CLI dashboard for tracking long-running scrapes
- **Rich Metadata**: PDS scrapers preserve 55 metadata fields including mast telemetry, solar position, and Mars local time - data unavailable from JSON APIs

## Tech Stack

- **.NET 9.0**: Latest framework with minimal API pattern
- **PostgreSQL 15**: Relational database with JSONB support
- **Entity Framework Core**: ORM with code-first migrations
- **Polly**: Resilience policies for HTTP communication
- **Docker Compose**: Containerized PostgreSQL for local development

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- PostgreSQL client (optional, for direct database access)

### Setup

1. Start the PostgreSQL database:
```bash
docker compose up -d
```

2. Run database migrations:
```bash
dotnet ef database update --project src/MarsVista.Api
```

3. Start the API:
```bash
dotnet run --project src/MarsVista.Api
```

The API will be available at `http://localhost:5127`

## API Endpoints

### API v2 (Recommended)

**Interactive Documentation**: Access Swagger UI at `/swagger` for complete v2 API documentation

**Query photos (multi-rover support):**
```bash
GET /api/v2/photos?rovers=curiosity,perseverance&sol_min=1000&cameras=NAVCAM,FHAZ&per_page=25
```

Key v2 features:
- **Multi-rover queries**: Query multiple rovers in a single request (`rovers=curiosity,perseverance`)
- **Advanced filtering**: Sol ranges (`sol_min`, `sol_max`), date ranges (`date_min`, `date_max`), multiple cameras
- **Field selection**: Request only needed fields (`fields=id,img_src,sol`)
- **Relationships**: Include related resources (`include=rover,camera`)
- **HTTP caching**: ETags and Cache-Control for optimal performance
- **JSON:API format**: Standardized response structure with `data`, `meta`, `pagination`, and `links`

**Phase 1 Enhanced Capabilities** (NEW):
- **Nested resource structure**: Rich response format with organized `images`, `dimensions`, `location`, `telemetry`, and `meta` objects
- **Mars time filtering**: Query by local solar time (`mars_time_min`, `mars_time_max`) and find golden hour photos (`mars_time_golden_hour=true`)
- **Location-based queries**: Search by site/drive coordinates with proximity radius (`site=79&drive=1204&location_radius=5`)
- **Image quality filters**: Filter by dimensions (`min_width`, `max_width`), aspect ratio (`aspect_ratio=16:9`), and sample type
- **Camera angle queries**: Find photos by mast elevation/azimuth angles for directional searches
- **Field set presets**: Control response verbosity with presets (`field_set=minimal|standard|extended|scientific|complete`)
- **Multiple image sizes**: Access 4 image URLs (small/medium/large/full) for progressive loading
- **Complete NASA metadata**: Access 100% of NASA's metadata fields vs original API's 5%

See [V2_PHASE_1_ENHANCEMENTS.md](docs/V2_PHASE_1_ENHANCEMENTS.md) for complete documentation with examples.

Example v2 requests:
```bash
# Query multiple rovers with date range
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&date_min=2024-01-01&per_page=10"

# Get specific fields only
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?fields=id,img_src,sol&include=rover"

# HTTP caching with ETags
curl -H "X-API-Key: YOUR_API_KEY" -H "If-None-Match: \"etag-value\"" \
  "https://api.marsvista.dev/api/v2/photos?sol=1000"

# Phase 1 Enhanced Examples:

# Find golden hour photos (sunrise/sunset lighting on Mars)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mars_time_golden_hour=true&rovers=curiosity&per_page=10"

# Location-based search with proximity radius
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204&location_radius=5&rovers=curiosity"

# Filter by image quality and aspect ratio
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?min_width=1024&aspect_ratio=16:9&sample_type=Full&rovers=perseverance"

# Use field set presets for minimal response
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=minimal&rovers=curiosity&sol=1000"
```

**List all rovers:**
```bash
GET /api/v2/rovers
```

**Get specific rover:**
```bash
GET /api/v2/rovers/{slug}
```

**Get rover manifest:**
```bash
GET /api/v2/rovers/{slug}/manifest
```

**Get photo statistics:**
```bash
GET /api/v2/statistics?rovers=curiosity&group_by=camera
```

Complete v2 documentation: [API_ENDPOINTS.md](docs/API_ENDPOINTS.md) (lines 932-1648)

### API v1 (Legacy)

**Query photos with filters:**
```bash
GET /api/v1/rovers/{name}/photos?sol=1000&camera=NAVCAM_LEFT&page=1&per_page=25
```

Query parameters:
- `sol`: Martian sol number (e.g., `sol=1000`)
- `earth_date`: Earth date in YYYY-MM-DD format (e.g., `earth_date=2024-06-15`)
- `camera`: Camera name (e.g., `camera=NAVCAM_LEFT`)
- `page`: Page number (default: 1)
- `per_page`: Results per page (default: 25, max: 100)

Example:
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1000&per_page=10"
```

Note: For local development, you can skip authentication by not configuring API key middleware, or use a test API key.

Response:
```json
{
  "photos": [
    {
      "id": 194,
      "sol": 1000,
      "camera": {
        "id": 5,
        "name": "NAVCAM_LEFT",
        "fullName": "Navigation Camera - Left"
      },
      "imgSrc": "https://mars.nasa.gov/mars2020-raw-images/...",
      "earthDate": "2024-06-15",
      "rover": {
        "id": 1,
        "name": "Perseverance",
        "landingDate": "2021-02-18",
        "launchDate": "2020-07-30",
        "status": "active"
      }
    }
  ],
  "pagination": {
    "total_count": 385,
    "page": 1,
    "per_page": 10,
    "total_pages": 39
  }
}
```

**Get latest photos:**
```bash
GET /api/v1/rovers/{name}/latest?per_page=25
```

**Get specific photo by ID:**
```bash
GET /api/v1/photos/{id}
```

**Get rover photo manifest:**
```bash
GET /api/v1/manifests/{name}
```

Returns photos grouped by sol with camera counts.

### Scraper Endpoints

**Bulk scrape (recommended for full data collection):**
```bash
POST /api/scraper/{rover}/bulk?startSol=1&endSol=1682&delayMs=1000
```

Example - scrape all Perseverance data:
```bash
curl -X POST "http://localhost:5127/api/scraper/perseverance/bulk?startSol=1&endSol=1682&delayMs=1000"
```

Response:
```json
{
  "rover": "perseverance",
  "startSol": 1,
  "endSol": 1682,
  "totalSols": 1682,
  "successfulSols": 1650,
  "skippedSols": 32,
  "failedSols": null,
  "totalPhotosScraped": 887593,
  "durationSeconds": 32400,
  "timestamp": "2025-11-13T21:00:00Z"
}
```

**Scrape specific sol:**
```bash
POST /api/scraper/{rover}/sol/{sol}
```

Example:
```bash
curl -X POST "http://localhost:5127/api/scraper/perseverance/sol/1000"
```

**Scrape latest photos:**
```bash
POST /api/scraper/{rover}
```

**Get scraping progress:**
```bash
GET /api/scraper/{rover}/progress
```

Response:
```json
{
  "rover": "perseverance",
  "totalPhotos": 450000,
  "solsScraped": 850,
  "expectedTotalSols": 1682,
  "percentComplete": 50.53,
  "oldestSol": 1,
  "latestSol": 1200,
  "lastPhotoScraped": "2025-11-13T21:00:00Z"
}
```

## Database Schema

The API uses a three-table relational schema:

- **rovers**: Rover metadata (name, landing date, mission status)
- **cameras**: Camera specifications per rover (name, full name, rover association)
- **photos**: Image records with indexed queryable columns + JSONB for complete NASA data

Key features:
- Unique constraint on `nasa_id` prevents duplicates
- Composite indexes for efficient queries (rover + camera + sol, site + drive)
- JSONB storage preserves all NASA fields for future feature development
- UTC timestamps for consistent timezone handling

## Monitoring Scraper Progress

For long-running bulk scrapes, use the included monitoring CLI:

```bash
./scrape-monitor.sh perseverance
```

Displays real-time progress:
- Total photos scraped
- Sols completed (e.g., 850/1682 = 50.53%)
- Visual progress bar
- Current speed (photos/second)
- Estimated time remaining
- Last update timestamp

The monitor refreshes every 2 seconds and provides a clean, colorful interface for tracking multi-hour scraping sessions.

## Database Management

### Backup and Restore

**Create a local backup:**
```bash
./db-backup.sh [backup_name]
```

Creates a compressed PostgreSQL dump in `./backups/` directory. If no name is provided, uses timestamp (e.g., `marsvista_20251115_153000.dump`).

Example:
```bash
# Auto-named backup with timestamp
./db-backup.sh

# Named backup
./db-backup.sh curiosity_complete
```

**Restore to Railway (or other remote database):**
```bash
./db-restore-to-railway.sh [backup_file]
```

Automatically uses the latest backup from `./backups/` if no file is specified. Replaces the remote database with the backup contents.

Example:
```bash
# Restore latest backup
./db-restore-to-railway.sh

# Restore specific backup
./db-restore-to-railway.sh ./backups/curiosity_complete.dump
```

**Advanced sync (upsert mode):**
```bash
./db-sync-to-railway.sh [--dry-run]
```

Syncs local database to Railway using upserts (INSERT ... ON CONFLICT DO UPDATE). Useful when you want to merge data rather than replace. Use `--dry-run` to preview changes without modifying the remote database.

### Utility Scripts

Additional helper scripts for scraper operations:

**Monitor scraper progress:**
```bash
./scrape-monitor.sh {rover}
```

**Resume scraping from a specific sol:**
```bash
./scrape-resume.sh {rover} {start_sol}
```

**Retry failed sols:**
```bash
./scrape-retry-failed.sh "489,888,929,931" curiosity
```

Can also accept full JSON response from bulk scrape to auto-extract failed sols.

## Deployment

The API is designed to be deployed on Railway alongside the PostgreSQL database.

### Prerequisites
- Railway account (Pro plan recommended)
- Railway CLI installed: `npm install -g @railway/cli`
- Production database already deployed on Railway

### Quick Deploy

```bash
# Link to your Railway project
railway link

# Set production environment
railway variables set ASPNETCORE_ENVIRONMENT=Production

# Link to PostgreSQL service (auto-injects DATABASE_URL)
railway service link Postgres

# Deploy
railway up

# Get your public URL
railway domain
```

After deployment, update the Production API section above with your actual api.marsvista.dev.

For complete deployment instructions, troubleshooting, and monitoring, see [DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Performance

Tested with Perseverance rover data:

- **Sol 10**: 11 photos in 28 seconds
- **Sol 1000**: 385 photos in 19.8 seconds
- **Sol 1368**: 580 photos in 19.4 seconds
- **Re-scrape (idempotent)**: 0 photos in 0.08 seconds
- **Bulk scrape estimate**: ~9-10 hours for all 1,682 sols (with 1s delay)

## Documentation

Comprehensive guides available in the `docs/` directory:

- **[API Endpoints](docs/API_ENDPOINTS.md)** - Complete API reference with examples for all endpoints
- **[Database Access](docs/DATABASE_ACCESS.md)** - Database credentials, useful queries, and management commands
- **[Deployment Guide](docs/DEPLOYMENT.md)** - Complete Railway deployment instructions and troubleshooting
- **[Curiosity Scraper Guide](docs/CURIOSITY_SCRAPER_GUIDE.md)** - Curiosity-specific scraper documentation
- **[Opportunity Scraper Guide](docs/OPPORTUNITY_SCRAPER_GUIDE.md)** - Opportunity PDS scraper documentation
- **[Spirit Scraper Guide](docs/SPIRIT_SCRAPER_GUIDE.md)** - Spirit PDS scraper documentation

## Development Status

Currently implemented:
- ✅ **API v2 Phase 1 Enhancements**: Nested resources, Mars time filtering, location queries, image quality filters, camera angles, field sets
- ✅ **API v2 with modern features**: Multi-rover queries, field selection, HTTP caching, JSON:API format
- ✅ **Interactive Swagger UI**: OpenAPI documentation with try-it-now interface
- ✅ **Comprehensive testing**: 40 unit and integration tests for v2 API
- ✅ API key authentication with per-user rate limiting
- ✅ Next.js dashboard for API key management
- ✅ Auth.js magic link authentication (passwordless)
- ✅ PostgreSQL database with migrations
- ✅ Rover and camera seed data for all 4 rovers
- ✅ Perseverance and Curiosity NASA API scrapers
- ✅ Opportunity and Spirit PDS index file scrapers
- ✅ Bulk scraper endpoint for efficient multi-sol ingestion
- ✅ Volume-based scraping for MER rovers (Opportunity, Spirit)
- ✅ Progress monitoring endpoint and CLI tool
- ✅ Hybrid storage (indexed columns + JSONB)
- ✅ Idempotent operations with duplicate detection
- ✅ In-memory duplicate checking for 1000x performance improvement
- ✅ Public query API (v1) with filtering and pagination
- ✅ Photo manifest endpoint
- ✅ Multi-rover support with strategy pattern
- ✅ Complete metadata preservation (55 fields for MER rovers)

Planned:
- 🔄 Advanced features (panoramas, stereo pairs, location search)
- 🔄 Automated background scraping
- 🔄 Redis caching layer for query performance
- 🔄 Photo download and local storage

## License

MIT License - See LICENSE file for details

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
