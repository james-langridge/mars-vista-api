# Architecture Overview

This document describes the system architecture of Mars Vista API.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Clients                                   │
│   (Web Apps, Mobile Apps, AI Agents, Scripts)                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Mars Vista API                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   v1 API    │  │   v2 API    │  │     Admin API           │  │
│  │  (Legacy)   │  │ (Enhanced)  │  │  (Scraper Control)      │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│         │                │                      │                │
│         └────────────────┼──────────────────────┘                │
│                          │                                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                   Service Layer                              ││
│  │  PhotoService, RoverService, CacheService, ScraperService   ││
│  └─────────────────────────────────────────────────────────────┘│
│                          │                                       │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │                 Repository Layer                             ││
│  │              Entity Framework Core                           ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────┐              ┌─────────────────────┐
│   PostgreSQL    │              │       Redis         │
│   (Photo Data)  │              │  (Cache + Limits)   │
└─────────────────┘              └─────────────────────┘
         ▲
         │
┌─────────────────────────────────────────────────────────────────┐
│                      Scraper Service                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │ Perseverance│  │  Curiosity  │  │   Opportunity/Spirit    │  │
│  │   Scraper   │  │   Scraper   │  │       Scrapers          │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      NASA Raw Image APIs                         │
│              (Perseverance, Curiosity, MER)                      │
└─────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
mars-vista-api/
├── src/
│   ├── MarsVista.Api/           # REST API service
│   │   ├── Controllers/         # API endpoints (v1, v2, admin)
│   │   ├── Services/            # Business logic
│   │   ├── DTOs/                # Data transfer objects
│   │   ├── Middleware/          # Auth, rate limiting, error handling
│   │   └── Program.cs           # Application entry point
│   │
│   ├── MarsVista.Core/          # Shared library
│   │   ├── Entities/            # Database entities
│   │   ├── Data/                # DbContext, migrations
│   │   ├── Repositories/        # Data access layer
│   │   └── Helpers/             # Utilities
│   │
│   └── MarsVista.Scraper/       # Data ingestion service
│       ├── Scrapers/            # Per-rover scraper implementations
│       ├── Services/            # Scraping orchestration
│       └── Program.cs           # Console app entry point
│
├── tests/
│   ├── MarsVista.Api.Tests/     # API unit/integration tests
│   └── MarsVista.Scraper.Tests/ # Scraper unit tests
│
├── docs/                        # Documentation
├── examples/                    # Example API collections
└── scripts/                     # Utility scripts
```

## Database Schema

### Core Tables

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│     rovers      │     │    cameras      │     │     photos      │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ id              │◄────│ rover_id        │     │ id              │
│ name            │     │ id              │◄────│ camera_id       │
│ landing_date    │     │ name            │     │ sol             │
│ launch_date     │     │ full_name       │     │ earth_date      │
│ status          │     │ is_active       │     │ image_url       │
│ max_sol         │     └─────────────────┘     │ raw_data (JSONB)│
└─────────────────┘                             │ created_at      │
                                                └─────────────────┘
```

### JSONB Storage Strategy

Photos store the complete NASA response in a `raw_data` JSONB column:

```sql
-- Indexed columns for fast queries
SELECT sol, earth_date, camera_id FROM photos WHERE rover_id = 1;

-- JSONB for full metadata access
SELECT raw_data->'extended'->'mastAz' as mast_azimuth FROM photos;
```

**Benefits:**
- 100% NASA data preservation (vs ~5% in traditional schemas)
- Flexible queries without schema changes
- Future-proof for new NASA fields

### Indexes

Key indexes for query performance:

```sql
-- Primary lookup patterns
CREATE INDEX ix_photos_rover_sol ON photos(rover_id, sol);
CREATE INDEX ix_photos_earth_date ON photos(earth_date);

-- Partial indexes for common filters
CREATE INDEX ix_photos_active_rovers ON photos(rover_id)
  WHERE rover_id IN (1, 2);  -- Perseverance, Curiosity

-- JSONB for location queries
CREATE INDEX ix_photos_site ON photos((raw_data->>'site'));
```

## API Versions

### v1 API (NASA-Compatible)

Mirrors NASA's original API structure for easy migration:

```
GET /api/v1/rovers
GET /api/v1/rovers/{name}/photos?sol=1000
GET /api/v1/manifests/{rover}
```

### v2 API (Enhanced)

Modern REST design with advanced features:

```
GET /api/v2/rovers
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100
GET /api/v2/photos?mars_time_min=M12:00:00&mars_time_max=M14:00:00
```

**v2 Features:**
- Multi-rover queries
- Mars time filtering (golden hour detection)
- Location-based search (site, drive, proximity)
- Nested response structure with relationships
- Field selection and includes

## Caching Architecture

Two-level caching for optimal performance:

```
┌─────────────┐    miss    ┌─────────────┐    miss    ┌─────────────┐
│  L1 Cache   │ ────────► │  L2 Cache   │ ────────► │  Database   │
│  (Memory)   │           │   (Redis)   │           │ (PostgreSQL)│
│   15 min    │ ◄──────── │  1hr-24hr   │ ◄──────── │             │
└─────────────┘    fill    └─────────────┘    fill    └─────────────┘
```

**Cache Key Strategy:**
```
rovers:all                    # All rovers
manifests:{rover}:{count}     # Per-rover, invalidated on new photos
photos:{hash}                 # Query-specific caching
```

## Authentication & Rate Limiting

### API Key Authentication

```
X-API-Key: mv_live_xxxxx...
```

Keys are:
- Generated per-user
- Stored as SHA-256 hashes
- Validated on each request

### Rate Limiting

Redis-backed sliding window:

```
Rate limit: 10,000 req/hour, 100,000 req/day
Key pattern: ratelimit:{keyHash}:{window}
```

Headers returned:
```
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9847
X-RateLimit-Reset: 1701432000
```

## Scraper Architecture

### Strategy Pattern

Each rover has a dedicated scraper implementing `IPhotoScraper`:

```csharp
public interface IPhotoScraper
{
    string RoverName { get; }
    Task<int> ScrapeSolAsync(int sol);
    Task<int> GetCurrentSolAsync();
}
```

### Resilience

HTTP client with Polly policies:

```csharp
// Retry with exponential backoff
RetryPolicy: 3 attempts, 2s/4s/8s delays

// Circuit breaker
CircuitBreaker: Opens after 5 failures, 30s recovery
```

### Incremental Updates

Daily scraper workflow:

1. Query NASA for current mission sol
2. Scrape last 7 sols (handles transmission delays)
3. Idempotent insert (skip existing photos)
4. Update rover's `max_sol`

## Error Handling

### Global Exception Handler

```csharp
// Structured error responses
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Try again in 3600 seconds.",
    "details": {
      "limit": 10000,
      "reset": 1701432000
    }
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| VALIDATION_ERROR | 400 | Invalid request parameters |
| UNAUTHORIZED | 401 | Missing or invalid API key |
| RATE_LIMIT_EXCEEDED | 429 | Too many requests |
| INTERNAL_ERROR | 500 | Server error (logged to Sentry) |

## Performance Optimizations

1. **Response Compression** - Brotli/Gzip for 30-50% payload reduction
2. **Computed Columns** - Pre-calculated aspect ratios
3. **Partial Indexes** - Optimized queries for active rovers
4. **Query Splitting** - EF Core batch optimization
5. **Connection Pooling** - Npgsql default pooling

## Security Considerations

1. **SQL Injection** - Parameterized queries via EF Core
2. **Rate Limiting** - Per-key limits prevent abuse
3. **Input Validation** - DTO validation on all inputs
4. **Secret Management** - Environment variables, never committed
5. **HTTPS Only** - TLS required in production

## Deployment Topology

### Single Instance (Development)

```
[Browser] → [API:5127] → [PostgreSQL] + [Redis]
```

### Production (Railway)

```
[CDN] → [API Service] → [PostgreSQL (Managed)]
                      → [Redis (Managed)]
        [Scraper Cron] ↗
```
