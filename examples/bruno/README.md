# Mars Vista API - Bruno Collection

Complete Bruno API collection for testing all Mars Vista API endpoints.

## Collection Structure

```
api/
├── bruno.json                      # Collection configuration
├── environments/                   # Environment variables
│   ├── local.bru                  # Local development (localhost:5127)
│   └── production.bru             # Production (Railway)
├── Health/                         # Health check
│   └── health-check.bru
├── Query API/                      # Public query endpoints (v1)
│   ├── Rovers/
│   │   ├── get-all-rovers.bru
│   │   └── get-rover-by-name.bru
│   ├── Photos/
│   │   ├── query-photos-by-sol.bru
│   │   ├── query-photos-by-earth-date.bru
│   │   ├── query-photos-with-camera-filter.bru
│   │   ├── query-photos-with-pagination.bru
│   │   ├── query-photos-camelcase-format.bru
│   │   ├── get-latest-photos.bru
│   │   ├── get-latest-photos-nasa-compatible.bru
│   │   └── get-photo-by-id.bru
│   ├── Manifests/
│   │   └── get-rover-manifest.bru
│   └── Statistics/
│       └── get-database-statistics.bru
├── Internal API/                   # Internal endpoints (Next.js only)
│   ├── generate-api-key.bru
│   ├── regenerate-api-key.bru
│   └── get-current-api-key.bru
├── Admin API/                      # Admin-only endpoints
│   ├── System/
│   │   ├── get-system-stats.bru
│   │   ├── get-users.bru
│   │   ├── get-activity.bru
│   │   └── get-rate-limit-violations.bru
│   └── Metrics/
│       ├── get-performance-metrics.bru
│       ├── get-endpoint-usage.bru
│       ├── get-errors.bru
│       └── get-performance-trends.bru
├── API v2/                         # Modern REST API (v2)
│   ├── api-discovery.bru
│   ├── Photos/
│   │   ├── query-photos.bru
│   │   ├── query-photos-multiple-rovers.bru
│   │   ├── query-photos-with-field-selection.bru
│   │   ├── query-photos-with-relationships.bru
│   │   ├── query-photos-with-sol-range.bru
│   │   ├── query-photos-with-date-range.bru
│   │   ├── query-photos-with-sorting.bru
│   │   ├── get-photo-by-id.bru
│   │   ├── batch-get-photos.bru
│   │   ├── photo-statistics.bru
│   │   └── photo-statistics-by-rover.bru
│   ├── Rovers/
│   │   ├── get-all-rovers.bru
│   │   ├── get-rover-by-slug.bru
│   │   ├── get-rover-manifest.bru
│   │   └── get-rover-cameras.bru
│   ├── Cameras/
│   │   ├── get-all-cameras.bru
│   │   ├── get-camera-by-id.bru
│   │   └── get-camera-with-rover-filter.bru
│   └── Advanced Features/
│       ├── panoramas-list.bru
│       ├── panoramas-get-by-id.bru
│       ├── locations-list.bru
│       └── locations-get-by-id.bru
└── Scraper API/                    # Admin scraper endpoints
    ├── Curiosity/
    │   ├── scrape-single-sol.bru
    │   ├── scrape-bulk-small.bru
    │   ├── scrape-bulk-full.bru
    │   ├── get-progress.bru
    │   └── resume-scraping.bru
    ├── Perseverance/
    │   ├── scrape-single-sol.bru
    │   ├── scrape-bulk-small.bru
    │   ├── get-progress.bru
    │   └── resume-scraping.bru
    ├── Opportunity/
    │   ├── scrape-single-volume.bru
    │   ├── scrape-all-volumes.bru
    │   └── get-progress.bru
    └── Spirit/
        ├── scrape-single-volume.bru
        ├── scrape-all-volumes.bru
        └── get-progress.bru
```

## Getting Started

1. **Install Bruno**: Download from https://www.usebruno.com/

2. **Open Collection**:
   - Open Bruno
   - File > Open Collection
   - Navigate to `/home/james/git/mars-vista-api/api`

3. **Select Environment**:
   - Click the environment dropdown (top right)
   - Choose "local" for local development or "production" for Railway

4. **Run Requests**:
   - Navigate through folders in the left sidebar
   - Click any request to view it
   - Click "Send" to execute

## Environments

### Local Development
- **Base URL**: `http://localhost:5127`
- **API Key**: Auto-configured (inherited from environment)

### Production
- **Base URL**: `https://api.marsvista.dev`
- **API Key**: Auto-configured (inherited from environment)

### Environment Variables

Configure these in your environment files (`local.bru` or `production.bru`):

- `baseUrl` - API base URL
- `apiKey` - Your user API key (for public endpoints)
- `adminApiKey` - Admin API key (for admin endpoints)
- `internalSecret` - Internal secret (for Next.js backend endpoints)

## Quick Test Workflow

### API v2 (Recommended)

1. **Health Check**:
   - Health > Health Check

2. **Discover API**:
   - API v2 > API Discovery

3. **Query Rovers**:
   - API v2 > Rovers > Get All Rovers
   - API v2 > Rovers > Get Rover by Slug

4. **Query Photos**:
   - API v2 > Photos > Query Photos (Unified Endpoint)
   - API v2 > Photos > Query Photos - Multiple Rovers
   - API v2 > Photos > Query Photos - Field Selection
   - API v2 > Photos > Query Photos - Include Relationships

5. **Statistics**:
   - API v2 > Photos > Photo Statistics
   - API v2 > Photos > Photo Statistics - By Rover

6. **Advanced Queries**:
   - API v2 > Photos > Query Photos - Sol Range
   - API v2 > Photos > Query Photos - Date Range
   - API v2 > Photos > Query Photos - Sorting
   - API v2 > Photos > Batch Get Photos

7. **Cameras**:
   - API v2 > Cameras > Get All Cameras
   - API v2 > Cameras > Get Camera by ID

8. **Advanced Features**:
   - API v2 > Advanced Features > Time Machine Query
   - API v2 > Advanced Features > Panoramas - List All
   - API v2 > Advanced Features > Locations - List All

### API v1 (NASA-Compatible)

1. **Query Data**:
   - Query API > Rovers > Get All Rovers
   - Query API > Photos > Query Photos by Sol

2. **Test Scraping** (local only):
   - Scraper API > Curiosity > Scrape Bulk (Small Test)
   - Scraper API > Curiosity > Get Scraping Progress

3. **Statistics**:
   - Query API > Statistics > Get Database Statistics (no auth required)

## API Versions

### API v2 - Modern REST API

**Full-featured** version with:
- **Core:** Photos, rovers, cameras with comprehensive filtering
- **Advanced Features:** Time machine, panoramas, locations
- **Capabilities:** Multi-rover queries, field selection, HTTP caching, batch operations
- **Response Format:** Consistent JSON:API envelope with RFC 7807 errors

### API v1 - NASA-Compatible

**Legacy-compatible** version:
- 100% compatible with NASA Mars Photo API
- Single-rover queries only
- Simple response format (matches NASA exactly)
- Statistics endpoint for public data

### Internal API

**Backend-only** endpoints for Next.js frontend:
- API key generation and regeneration
- User key management
- Protected by internal secret (not for public use)

### Admin API

**Admin-only** endpoints for system management:
- System statistics and monitoring
- User management
- Performance metrics and analytics
- Activity logs and error tracking
- Requires admin API key

## Notes

- All requests include the API key header automatically via environment variables
- Query parameters are pre-configured but can be modified as needed
- Scraper endpoints should be used carefully in production
- The collection is organized to match the API documentation structure
- Use API v2 for new applications (more features, better performance)
- Use API v1 for NASA API migration (drop-in replacement)

## See Also

- [API Endpoints Documentation](../docs/API_ENDPOINTS.md)
- [Main README](../README.md)
