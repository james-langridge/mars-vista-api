# Mars Vista API

A C#/.NET API for Mars rover photo data, designed as a modern alternative to NASA's Mars Photo API. Provides access to Perseverance rover images with complete metadata preservation and advanced querying capabilities.

## Features

- **Complete Data Preservation**: Stores all 30+ NASA metadata fields using hybrid PostgreSQL storage (indexed columns + JSONB)
- **NASA API Scraper**: Automated ingestion from NASA's Mars 2020 raw image feeds
- **High Performance**: Processes 500+ photos in under 20 seconds with bulk insert optimization
- **Resilient HTTP Client**: Polly-based retry policies and circuit breakers for reliable NASA API communication
- **Idempotent Operations**: Duplicate detection prevents re-scraping already stored photos
- **Future-Ready**: Architecture supports additional rovers (Curiosity, Opportunity, Spirit) and advanced features (panoramas, stereo pairs, location-based search)

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

### Scraper Endpoints

**Scrape specific sol for Perseverance:**
```bash
POST /api/scraper/perseverance/sol/{sol}
```

Example:
```bash
curl -X POST "http://localhost:5127/api/scraper/perseverance/sol/1000"
```

Response:
```json
{
  "rover": "perseverance",
  "sol": 1000,
  "photosScraped": 385,
  "timestamp": "2023-12-13T09:18:57.463Z"
}
```

**Scrape latest photos for Perseverance:**
```bash
POST /api/scraper/perseverance
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

## Performance

Tested with Perseverance rover data:

- **Sol 10**: 11 photos in 28 seconds
- **Sol 1000**: 385 photos in 19.8 seconds
- **Sol 1368**: 580 photos in 19.4 seconds
- **Re-scrape (idempotent)**: 0 photos in 0.08 seconds

## Development Status

Currently implemented:
- ✅ PostgreSQL database with migrations
- ✅ Rover and camera seed data
- ✅ Perseverance NASA API scraper with resilience policies
- ✅ Manual scraper endpoints
- ✅ Hybrid storage (indexed columns + JSONB)

Planned:
- 🔄 Additional rover scrapers (Curiosity, Opportunity, Spirit)
- 🔄 Public photo query endpoints (by sol, date, camera, rover)
- 🔄 Advanced features (panoramas, stereo pairs, location search)
- 🔄 Automated background scraping

## License

MIT License - See LICENSE file for details

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
