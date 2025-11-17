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
├── Query API/                      # Public query endpoints
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
│   └── Manifests/
│       └── get-rover-manifest.bru
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

## Quick Test Workflow

1. **Health Check**:
   - Health > Health Check

2. **Query Data**:
   - Query API > Rovers > Get All Rovers
   - Query API > Photos > Query Photos by Sol

3. **Test Scraping** (local only):
   - Scraper API > Curiosity > Scrape Bulk (Small Test)
   - Scraper API > Curiosity > Get Scraping Progress

## Notes

- All requests include the API key header automatically via environment variables
- Query parameters are pre-configured but can be modified as needed
- Scraper endpoints should be used carefully in production
- The collection is organized to match the API documentation structure

## See Also

- [API Endpoints Documentation](../docs/API_ENDPOINTS.md)
- [Main README](../README.md)
