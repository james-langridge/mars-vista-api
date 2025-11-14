# Mars Vista API

A C#/.NET API for Mars rover photo data, designed as a modern alternative to NASA's Mars Photo API. Provides access to Perseverance and Curiosity rover images with complete metadata preservation and advanced querying capabilities.

## Features

- **Multi-Rover Support**: Perseverance and Curiosity scrapers with automatic NASA API adaptation
- **Complete Data Preservation**: Stores all 30+ NASA metadata fields using hybrid PostgreSQL storage (indexed columns + JSONB)
- **NASA API Scrapers**: Automated ingestion from multiple NASA Mars rover image feeds
- **High Performance**: Processes 500+ photos in under 20 seconds with bulk insert optimization
- **Resilient HTTP Client**: Polly-based retry policies and circuit breakers for reliable NASA API communication
- **Idempotent Operations**: Duplicate detection prevents re-scraping already stored photos
- **Progress Monitoring**: Real-time CLI dashboard for tracking long-running scrapes
- **Future-Ready**: Architecture supports additional rovers (Opportunity, Spirit) and advanced features (panoramas, stereo pairs, location-based search)

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

### Query Endpoints (Public API)

**List all rovers:**
```bash
GET /api/v1/rovers
```

**Get specific rover:**
```bash
GET /api/v1/rovers/{name}
```

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
curl "http://localhost:5127/api/v1/rovers/perseverance/photos?sol=1000&per_page=10"
```

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
- **[Curiosity Scraper Guide](docs/CURIOSITY_SCRAPER_GUIDE.md)** - Curiosity-specific scraper documentation

## Development Status

Currently implemented:
- ✅ PostgreSQL database with migrations
- ✅ Rover and camera seed data for all 4 rovers
- ✅ Perseverance and Curiosity NASA API scrapers
- ✅ Bulk scraper endpoint for efficient multi-sol ingestion
- ✅ Progress monitoring endpoint and CLI tool
- ✅ Hybrid storage (indexed columns + JSONB)
- ✅ Idempotent operations with duplicate detection
- ✅ Public query API (v1) with filtering and pagination
- ✅ Photo manifest endpoint
- ✅ Multi-rover support with strategy pattern

Planned:
- 🔄 Additional rover scrapers (Opportunity, Spirit)
- 🔄 Retry script for failed sols
- 🔄 Advanced features (panoramas, stereo pairs, location search)
- 🔄 Automated background scraping
- 🔄 Redis caching layer for query performance

## License

MIT License - See LICENSE file for details

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
