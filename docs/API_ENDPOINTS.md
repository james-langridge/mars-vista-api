# API Endpoints Documentation

Complete reference for all Mars Vista API endpoints.

## Base URLs

- **Production**: `https://api.marsvista.dev`
- **Local Development**: `http://localhost:5127`

All examples below use `localhost` for local development. For production, use `https://api.marsvista.dev`.

---

## Authentication

All query API endpoints require authentication using an API key. Scraper endpoints use a separate admin API key.

### Getting an API Key

1. **Sign in** at [marsvista.dev/signin](https://marsvista.dev/signin)
2. **Generate an API key** from your dashboard at [marsvista.dev/dashboard](https://marsvista.dev/dashboard)
3. **Copy your key** - it will look like: `mv_live_a1b2c3d4e5f6789012345678901234567890abcd`
4. **Use the key** in the `X-API-Key` header for all requests

### Using Your API Key

Include the `X-API-Key` header in all API requests:

```bash
curl -H "X-API-Key: mv_live_a1b2c3d4e5f6789012345678901234567890abcd" \
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"
```

### Rate Limits

Rate limits are enforced per API key based on your tier:

**Free Tier** (default):
- 1,000 requests per hour (matches NASA's API Gateway)
- 10,000 requests per day
- 5 concurrent requests

**Pro Tier** ($20/month):
- 10,000 requests per hour (10x NASA's limit)
- 100,000 requests per day
- 25 concurrent requests
- Usage analytics dashboard
- Priority support

### Rate Limit Headers

All API responses include rate limit information:

```http
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 987
X-RateLimit-Reset: 1731859200
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

### Error Responses

**401 Unauthorized** - Missing or invalid API key:
```json
{
  "error": "Unauthorized",
  "message": "Invalid or missing API key. Sign in at https://marsvista.dev to get your API key."
}
```

**429 Too Many Requests** - Rate limit exceeded:
```json
{
  "error": "Rate limit exceeded",
  "message": "You have exceeded the 60 requests per hour limit for the free tier.",
  "tier": "free",
  "limit": 60,
  "resetAt": "2025-11-18T15:00:00Z",
  "upgradeUrl": "https://marsvista.dev/pricing"
}
```

### Health Check Endpoint

The `/health` endpoint does NOT require authentication and can be used to verify API availability:

```bash
curl "http://localhost:5127/health"
```

---

## Table of Contents

- [Query API (Public)](#query-api-public)
  - [Rovers](#rovers)
  - [Photos](#photos)
  - [Manifests](#manifests)
- [Scraper API (Admin)](#scraper-api-admin)
  - [Single Sol](#single-sol-scraping)
  - [Bulk Scraping](#bulk-scraping)
  - [Progress Monitoring](#progress-monitoring)
  - [Smart Resume](#smart-resume)
- [Health Check](#health-check)

---

## Query API (Public)

NASA-compatible API for querying Mars rover photo data.

### Response Format

All API endpoints support two response formats via the optional `?format` query parameter:

**Default (snake_case)** - 100% compatible with original NASA Mars Photo API:
```bash
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1000"
# Returns: { "img_src": "...", "earth_date": "...", "full_name": "..." }
```

**Modern JavaScript (camelCase)** - Add `?format=camelCase`:
```bash
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase"
# Returns: { "imgSrc": "...", "earthDate": "...", "fullName": "..." }
```

**Migration from NASA API:** Simply replace the base URL - no code changes required! See [NASA_API_MIGRATION_GUIDE.md](NASA_API_MIGRATION_GUIDE.md) for details.

### Rovers

#### Get All Rovers

```http
GET /api/v1/rovers
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers"
```

**Response:**
```json
{
  "rovers": [
    {
      "id": 2,
      "name": "Curiosity",
      "landingDate": "2012-08-06",
      "launchDate": "2011-11-26",
      "status": "active",
      "maxSol": 4683,
      "maxDate": "2025-11-14",
      "totalPhotos": 368,
      "cameras": [
        {
          "id": 20,
          "name": "MAST",
          "fullName": "Mast Camera"
        },
        {
          "id": 21,
          "name": "NAVCAM",
          "fullName": "Navigation Camera"
        }
      ]
    }
  ]
}
```

#### Get Specific Rover

```http
GET /api/v1/rovers/{name}
```

**Parameters:**
- `name` (path) - Rover name: `curiosity`, `perseverance`, `opportunity`, `spirit`

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity"
```

**Response:**
```json
{
  "rover": {
    "id": 2,
    "name": "Curiosity",
    "landingDate": "2012-08-06",
    "launchDate": "2011-11-26",
    "status": "active",
    "maxSol": 4683,
    "maxDate": "2025-11-14",
    "totalPhotos": 368,
    "cameras": [...]
  }
}
```

**Error Response (404):**
```json
{
  "error": "Rover 'invalid' not found"
}
```

---

### Photos

#### Query Photos

```http
GET /api/v1/rovers/{name}/photos
```

**Parameters:**
- `name` (path, required) - Rover name
- `sol` (query, optional) - Martian sol (integer)
- `earth_date` (query, optional) - Earth date in YYYY-MM-DD format
- `camera` (query, optional) - Camera name (e.g., `MAST`, `NAVCAM`)
- `page` (query, optional) - Page number (default: 1)
- `per_page` (query, optional) - Results per page (default: 25, max: 100)

**Examples:**

**Get photos from sol 1:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1"
```

**Get photos from specific Earth date:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/photos?earth_date=2012-08-06"
```

**Filter by camera:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=100&camera=MAST"
```

**Pagination:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1&page=2&per_page=50"
```

**Response:**
```json
{
  "photos": [
    {
      "id": 451603,
      "sol": 1,
      "camera": {
        "id": 20,
        "name": "MAST",
        "fullName": "Mast Camera"
      },
      "imgSrc": "https://mars.jpl.nasa.gov/msl-raw-images/msss/00001/mcam/0001MR0000000010100001C00_DXXX.jpg",
      "earthDate": "2012-08-06",
      "rover": {
        "id": 2,
        "name": "Curiosity",
        "landingDate": "2012-08-06",
        "launchDate": "2011-11-26",
        "status": "active"
      }
    }
  ],
  "pagination": {
    "total_count": 20,
    "page": 1,
    "per_page": 25,
    "total_pages": 1
  }
}
```

**Error Response (400):**
```json
{
  "error": "Invalid earth_date format. Use YYYY-MM-DD."
}
```

#### Get Latest Photos

Get the most recent photos from a rover (highest sol number).

```http
GET /api/v1/rovers/{name}/latest
GET /api/v1/rovers/{name}/latest_photos  (NASA API compatible alias)
```

**Parameters:**
- `name` (path, required) - Rover name
- `page` (query, optional) - Page number (default: 1)
- `per_page` (query, optional) - Results per page (default: 25, max: 100)
- `format` (query, optional) - Response format: `snake_case` (default) or `camelCase`

**Examples:**
```bash
# Modern endpoint
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/latest?per_page=10"

# NASA API compatible endpoint
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/latest_photos?per_page=10"
```

**Response:**
```json
{
  "photos": [...],
  "pagination": {
    "total_count": 156,
    "page": 1,
    "per_page": 10,
    "total_pages": 16
  }
}
```

#### Get Photo by ID

```http
GET /api/v1/photos/{id}
```

**Parameters:**
- `id` (path, required) - Photo ID (integer)

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/photos/451603"
```

**Response:**
```json
{
  "photo": {
    "id": 451603,
    "sol": 1,
    "camera": {...},
    "imgSrc": "https://...",
    "earthDate": "2012-08-06",
    "rover": {...}
  }
}
```

**Error Response (404):**
```json
{
  "error": "Photo not found"
}
```

---

### Manifests

#### Get Rover Manifest

Get a summary of available photos organized by sol.

```http
GET /api/v1/manifests/{name}
```

**Parameters:**
- `name` (path, required) - Rover name

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/manifests/curiosity"
```

**Response:**
```json
{
  "manifest": {
    "name": "Curiosity",
    "landingDate": "2012-08-06",
    "launchDate": "2011-11-26",
    "status": "active",
    "maxSol": 4683,
    "maxDate": "2025-11-14",
    "totalPhotos": 368,
    "photos": [
      {
        "sol": 1,
        "earthDate": "2012-08-06",
        "totalPhotos": 20,
        "cameras": ["MAST", "MAHLI", "MARDI", "NAVCAM"]
      },
      {
        "sol": 2,
        "earthDate": "2012-08-07",
        "totalPhotos": 148,
        "cameras": ["MAST", "NAVCAM", "FHAZ", "RHAZ"]
      }
    ]
  }
}
```

---

## Scraper API (Admin)

Administrative endpoints for scraping NASA data.

### Single Sol Scraping

Scrape photos for a specific Martian sol.

```http
POST /api/scraper/{rover}/sol/{sol}
```

**Parameters:**
- `rover` (path, required) - Rover name: `curiosity`, `perseverance`
- `sol` (path, required) - Sol number (integer)

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/sol/100"
```

**Response:**
```json
{
  "rover": "curiosity",
  "sol": 100,
  "photosScraped": 156
}
```

**Error Response (500):**
```json
{
  "error": "HTTP error scraping sol 100: NotFound"
}
```

---

### Bulk Scraping

Scrape a range of sols in sequence.

```http
POST /api/scraper/{rover}/bulk
```

**Parameters:**
- `rover` (path, required) - Rover name
- `startSol` (query, required) - Starting sol number (integer)
- `endSol` (query, required) - Ending sol number (integer)

**Examples:**

**Small test (10 sols):**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=10"
```

**Full scrape (all Curiosity data):**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=4683"
```

**Resume from specific sol:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=2000&endSol=4683"
```

**Response:**
```json
{
  "rover": "curiosity",
  "startSol": 1,
  "endSol": 100,
  "totalSols": 100,
  "successfulSols": 98,
  "skippedSols": 2,
  "failedSols": null,
  "totalPhotosScraped": 15234,
  "durationSeconds": 342,
  "timestamp": "2025-11-14T23:12:10.232Z"
}
```

**Notes:**
- Each sol includes a 1-second delay to avoid rate limiting
- Already-scraped sols are automatically skipped (idempotent)
- Failed sols can be retried with a dedicated retry script

---

### Progress Monitoring

Get real-time scraping progress and statistics.

```http
GET /api/scraper/{rover}/progress
```

**Parameters:**
- `rover` (path, required) - Rover name

**Example:**
```bash
curl "http://localhost:5127/api/scraper/curiosity/progress"
```

**Response:**
```json
{
  "rover": "curiosity",
  "totalPhotos": 368,
  "solsScraped": 3,
  "expectedTotalSols": 4683,
  "percentComplete": 0.06,
  "oldestSol": 1,
  "latestSol": 3,
  "lastPhotoScraped": "2025-11-14T23:12:10.122439Z",
  "minutesSinceLastUpdate": 0.1,
  "status": "active",
  "statusMessage": "Scraping in progress",
  "timestamp": "2025-11-14T23:12:16.740280Z"
}
```

**Status Values:**
- `active` - Photos scraped in last 5 minutes
- `slow` - Photos scraped 5-30 minutes ago
- `stalled` - No photos in 30-60 minutes
- `stopped` - No photos in over 60 minutes
- `complete` - All sols scraped

**Visual Monitoring:**

Use the included CLI monitor for a real-time dashboard:
```bash
./scrape-monitor.sh curiosity
```

---

### Smart Resume

Automatically resume scraping from the latest scraped sol.

```http
POST /api/scraper/{rover}/resume
```

**Parameters:**
- `rover` (path, required) - Rover name

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/resume"
```

**Response:**
```json
{
  "rover": "curiosity",
  "resumedFromSol": 2458,
  "endSol": 4683,
  "totalSols": 2225,
  "totalPhotosScraped": 345678,
  "durationSeconds": 8932,
  "timestamp": "2025-11-14T23:45:00.000Z"
}
```

**Notes:**
- Determines latest sol from database
- Automatically scrapes from (latest + 1) to rover's max sol
- Useful after interruptions or for catching up with new data

---

### Opportunity Rover (PDS Volume Scraping)

Opportunity uses a different scraping approach - parsing PDS (Planetary Data System) index files rather than querying JSON APIs, since NASA's traditional APIs don't support MER rovers.

#### Scrape Single Volume

Scrape a specific camera volume from PDS archives.

```http
POST /api/scraper/opportunity/volume/{volumeName}
```

**Parameters:**
- `volumeName` (path, required) - PDS volume name

**Available Volumes:**
- `mer1po_0xxx` - PANCAM (366,510 photos)
- `mer1no_0xxx` - NAVCAM (~500,000 photos)
- `mer1ho_0xxx` - HAZCAM (~100,000 photos)
- `mer1mo_0xxx` - Microscopic Imager
- `mer1do_0xxx` - Descent Camera

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/opportunity/volume/mer1po_0xxx"
```

**Response:**
```json
{
  "rover": "Opportunity",
  "volume": "mer1po_0xxx",
  "photosScraped": 366510,
  "timestamp": "2025-11-16T00:00:00.000Z"
}
```

#### Scrape All Volumes

Scrape all Opportunity volumes sequentially.

```http
POST /api/scraper/opportunity/all
```

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/opportunity/all"
```

**Response:**
```json
{
  "rover": "Opportunity",
  "message": "All volumes scraped",
  "totalPhotosScraped": 1000000,
  "timestamp": "2025-11-16T00:00:00.000Z"
}
```

**Notes:**
- Scraping is volume-based (not sol-based like other rovers)
- Processes large index files (342 MB for PANCAM)
- Expected time: ~90 minutes for all volumes
- See [Opportunity Scraper Guide](OPPORTUNITY_SCRAPER_GUIDE.md) for details

---

### Spirit Rover (PDS Volume Scraping)

Spirit uses the same PDS scraping approach as Opportunity. Spirit (MER-2) was Opportunity's twin rover with identical hardware but a shorter mission (6 years vs 14.5 years).

#### Scrape Single Volume

Scrape a specific camera volume from PDS archives.

```http
POST /api/scraper/spirit/volume/{volumeName}
```

**Parameters:**
- `volumeName` (path, required) - PDS volume name

**Available Volumes:**
- `mer2po_0xxx` - PANCAM (~150,000 photos)
- `mer2no_0xxx` - NAVCAM (~50,000 photos)
- `mer2ho_0xxx` - HAZCAM (~15,000 photos)
- `mer2mo_0xxx` - Microscopic Imager (~10,000 photos)
- `mer2do_0xxx` - Descent Camera (9 photos)

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/spirit/volume/mer2po_0xxx"
```

**Response:**
```json
{
  "rover": "Spirit",
  "volume": "mer2po_0xxx",
  "photosScraped": 150000,
  "timestamp": "2025-11-16T00:00:00.000Z"
}
```

#### Scrape All Volumes

Scrape all Spirit volumes sequentially.

```http
POST /api/scraper/spirit/all
```

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/spirit/all"
```

**Response:**
```json
{
  "rover": "Spirit",
  "message": "All volumes scraped",
  "totalPhotosScraped": 225000,
  "timestamp": "2025-11-16T00:00:00.000Z"
}
```

**Notes:**
- Scraping is volume-based (not sol-based like other rovers)
- Spirit's shorter mission means fewer photos (~225K vs Opportunity's ~548K)
- Expected time: ~20-30 minutes for all volumes
- See [Spirit Scraper Guide](SPIRIT_SCRAPER_GUIDE.md) for details

---

## Health Check

Simple health check endpoint.

```http
GET /health
```

**Example:**
```bash
curl "http://localhost:5127/health"
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-14T23:12:00.000Z"
}
```

---

## Rate Limiting and Resilience

### Built-in Protection

The scraper includes automatic resilience policies:

**Retry Policy:**
- Exponential backoff: 2s, 4s, 8s delays
- Handles transient failures (500, 502, 503, 504)
- Maximum 3 retry attempts per request

**Circuit Breaker:**
- Opens after 5 consecutive failures
- Stays open for 30 seconds
- Prevents cascading failures

**Delay Between Sols:**
- 1 second delay between each sol request
- Prevents overwhelming NASA's servers

### Best Practices

1. **Test with small ranges first:**
   ```bash
   curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=10"
   ```

2. **Monitor progress during long runs:**
   ```bash
   ./scrape-monitor.sh curiosity
   ```

3. **Use resume endpoint for interruptions:**
   ```bash
   curl -X POST "http://localhost:5127/api/scraper/curiosity/resume"
   ```

4. **Check for failures:**
   ```bash
   curl "http://localhost:5127/api/scraper/curiosity/progress" | jq '.failedSols'
   ```

---

## Error Handling

### Common Error Codes

**400 Bad Request**
- Invalid parameters (date format, negative sol, etc.)
- Missing required parameters

**404 Not Found**
- Rover not found
- Photo ID doesn't exist
- No photos for given criteria

**500 Internal Server Error**
- Database connection issues
- Scraper failures
- NASA API errors

### Error Response Format

```json
{
  "error": "Descriptive error message",
  "details": "Optional additional context"
}
```

---

## Camera Reference

### Curiosity Cameras

| Code | Full Name | Description |
|------|-----------|-------------|
| MAST | Mast Camera (Mastcam) | Color imaging, stereo pair |
| NAVCAM | Navigation Camera | Black and white, wide angle |
| FHAZ | Front Hazard Avoidance Camera | Obstacle detection |
| RHAZ | Rear Hazard Avoidance Camera | Rear obstacle detection |
| CHEMCAM | Chemistry and Camera Complex | Remote micro-imager |
| MAHLI | Mars Hand Lens Imager | Close-up imaging |
| MARDI | Mars Descent Imager | Landing sequence |

### Perseverance Cameras

| Code | Full Name | Description |
|------|-----------|-------------|
| EDL_RUCAM | Rover Up-Look Camera | Landing sequence |
| EDL_RDCAM | Rover Down-Look Camera | Landing sequence |
| EDL_DDCAM | Descent Stage Down-Look Camera | Landing sequence |
| EDL_PUCAM1 | Parachute Up-Look Camera A | Parachute deployment |
| EDL_PUCAM2 | Parachute Up-Look Camera B | Parachute deployment |
| NAVCAM_LEFT | Navigation Camera - Left | Stereo navigation |
| NAVCAM_RIGHT | Navigation Camera - Right | Stereo navigation |
| MCZ_LEFT | Mast Camera Zoom - Left | High-res color, zoom |
| MCZ_RIGHT | Mast Camera Zoom - Right | High-res color, zoom |
| FRONT_HAZCAM_LEFT_A | Front Hazard Camera - Left | Obstacle avoidance |
| FRONT_HAZCAM_RIGHT_A | Front Hazard Camera - Right | Obstacle avoidance |
| REAR_HAZCAM_LEFT | Rear Hazard Camera - Left | Rear obstacle detection |
| REAR_HAZCAM_RIGHT | Rear Hazard Camera - Right | Rear obstacle detection |
| SKYCAM | MEDA Skycam | Atmospheric imaging |
| SHERLOC_WATSON | WATSON Camera | Close-up texture analysis |

---

## Performance Benchmarks

### Typical Response Times

- `GET /api/v1/rovers` - 50-100ms
- `GET /api/v1/rovers/{name}/photos?sol={sol}` - 100-200ms
- `POST /api/scraper/{rover}/sol/{sol}` - 2-5 seconds
- `POST /api/scraper/{rover}/bulk?startSol=1&endSol=100` - 3-5 minutes

### Bulk Scraping Performance

**Curiosity (4,683 sols):**
- Average: 500 photos per sol
- Speed: ~25 photos/second
- Total time: **9-10 hours**

**Perseverance (1,682 sols):**
- Average: 500 photos per sol
- Speed: ~25 photos/second
- Total time: **3-4 hours**

---

## Examples

### Complete Workflow

**1. Check available rovers:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers" | jq '.rovers[].name'
```

**2. Start bulk scrape:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=100"
```

**3. Monitor progress:**
```bash
# In another terminal
./scrape-monitor.sh curiosity
```

**4. Check progress via API:**
```bash
watch -n 5 'curl -s "http://localhost:5127/api/scraper/curiosity/progress" | jq'
```

**5. Query scraped photos:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=50" | jq '.photos[0]'
```

**6. Resume if interrupted:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/resume"
```

---

## API v2 - Modern REST API

**Version 2.0** provides a modern, resource-oriented API with powerful filtering, HTTP caching, field selection, and comprehensive error handling. It addresses all the limitations of v1 while maintaining NASA data compatibility.

### Key Improvements Over v1

**Why v2?**

- ✅ **Unified Photos Endpoint** - Query across multiple rovers/cameras in a single request
- ✅ **HTTP Caching** - ETags, conditional requests, different TTLs for active/inactive rovers
- ✅ **Field Selection** - Return only the fields you need
- ✅ **Always-On Pagination** - Never unbounded results, cursor support
- ✅ **RFC 7807 Error Responses** - Structured, helpful error messages
- ✅ **Comprehensive Validation** - Clear error messages with examples
- ✅ **Sorting & Range Queries** - Flexible data retrieval
- ✅ **Batch Operations** - Retrieve multiple photos by ID efficiently
- ✅ **Statistics Endpoints** - Aggregate photo data for analytics
- ✅ **Self-Documenting** - Discovery endpoints show available filters

### Base Path

All v2 endpoints are under `/api/v2/`:

```
https://api.marsvista.dev/api/v2/...
http://localhost:5127/api/v2/...
```

### Authentication

Same as v1 - include your API key in the `X-API-Key` header:

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000"
```

---

### Response Format

All v2 endpoints return a consistent envelope structure:

```json
{
  "data": [...],           // The actual data
  "meta": {                // Metadata about the request/response
    "total_count": 15234,
    "returned_count": 25,
    "query": {             // Echo of query parameters
      "rovers": ["curiosity"],
      "sol_min": 1000
    }
  },
  "pagination": {          // Pagination information
    "page": 1,
    "per_page": 25,
    "total_pages": 610,
    "cursor": {
      "current": "eyJpZCI6MTIzNDU2fQ==",
      "next": "eyJpZCI6MTIzNDgxfQ=="
    }
  },
  "links": {               // Navigation links
    "self": "https://api.marsvista.dev/api/v2/photos?page=1",
    "next": "https://api.marsvista.dev/api/v2/photos?page=2",
    "first": "https://api.marsvista.dev/api/v2/photos?page=1",
    "last": "https://api.marsvista.dev/api/v2/photos?page=610"
  }
}
```

---

### Photos Endpoint

**Unified endpoint for all photo queries** - the core improvement of v2.

```http
GET /api/v2/photos
```

#### Basic Examples

**Single rover:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=10"
```

**Multiple rovers:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&per_page=10"
```

**Multiple cameras:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=FHAZ,NAVCAM,MAST&per_page=10"
```

#### Advanced Filtering

**Sol range:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=100&sol_max=200"
```

**Date range:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&date_min=2023-01-01&date_max=2023-12-31"
```

**Combined filters:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=MAST,CHEMCAM&sol_min=1000&sol_max=2000&sort=-earth_date"
```

#### Field Selection

**Return only specific fields:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&fields=id,img_src,sol,earth_date&per_page=5"
```

Response includes only requested fields:
```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "img_src": "https://mars.nasa.gov/msl/123456.jpg",
        "sol": 1000,
        "earth_date": "2015-05-30"
      }
    }
  ]
}
```

#### Include Relationships

**Include rover and camera details:**
```bash
curl -H "X-API-Key": YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&include=rover,camera&per_page=5"
```

Response includes relationship data:
```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "img_src": "https://mars.nasa.gov/msl/123456.jpg",
        "sol": 1000,
        "earth_date": "2015-05-30"
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "type": "rover",
          "attributes": {
            "name": "Curiosity",
            "status": "active"
          }
        },
        "camera": {
          "id": "mast",
          "type": "camera",
          "attributes": {
            "full_name": "Mast Camera"
          }
        }
      }
    }
  ]
}
```

#### Sorting

**Sort by field (ascending):**
```
?sort=earth_date
```

**Sort descending (- prefix):**
```
?sort=-earth_date
```

**Multiple sort fields:**
```
?sort=-earth_date,camera
```

#### Pagination

**Page-based pagination (default):**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=2&per_page=50"
```

**Cursor-based pagination:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cursor=eyJpZCI6MTIzNDgxfQ=="
```

Limits:
- Default: 25 results per page
- Maximum: 100 results per page

#### HTTP Caching

v2 implements proper HTTP caching with ETags:

**Initial request:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" -i \
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&per_page=10"
```

Response includes caching headers:
```http
HTTP/1.1 200 OK
ETag: "abc123def456"
Cache-Control: public, max-age=31536000, must-revalidate

{
  "data": [...]
}
```

**Subsequent request with ETag:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
     -H "If-None-Match: \"abc123def456\"" -i \
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&per_page=10"
```

If data hasn't changed:
```http
HTTP/1.1 304 Not Modified
ETag: "abc123def456"
```

**Cache TTL by rover status:**
- **Inactive rovers** (Opportunity, Spirit): 1 year (`max-age=31536000`)
- **Active rovers** (Curiosity, Perseverance): 1 hour (`max-age=3600`)

---

### Get Photo by ID

Retrieve a specific photo.

```http
GET /api/v2/photos/{id}
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos/123456?include=rover,camera"
```

**Response:**
```json
{
  "data": {
    "id": 123456,
    "type": "photo",
    "attributes": {
      "img_src": "https://mars.nasa.gov/msl/123456.jpg",
      "sol": 1000,
      "earth_date": "2015-05-30",
      "created_at": "2015-05-30T20:12:34Z"
    },
    "relationships": {
      "rover": {
        "id": "curiosity",
        "type": "rover"
      },
      "camera": {
        "id": "mast",
        "type": "camera",
        "attributes": {
          "full_name": "Mast Camera"
        }
      }
    }
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/photos/123456"
  }
}
```

---

### Batch Get Photos

Retrieve multiple photos by ID in a single request.

```http
POST /api/v2/photos/batch
```

**Request body:**
```json
{
  "ids": [123456, 123457, 123458, 123459, 123460]
}
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
     -H "Content-Type: application/json" \
     -d '{"ids": [123456, 123457, 123458]}' \
  "https://api.marsvista.dev/api/v2/photos/batch"
```

**Limits:**
- Maximum 100 IDs per request

**Response:**
```json
{
  "data": [
    {"id": 123456, ...},
    {"id": 123457, ...},
    {"id": 123458, ...}
  ],
  "meta": {
    "total_count": 3,
    "returned_count": 3,
    "query": {
      "ids_requested": 3,
      "ids_found": 3
    }
  }
}
```

---

### Photo Statistics

Get aggregated statistics about photos.

```http
GET /api/v2/photos/stats
```

**Parameters:**
- `group_by` (required) - Grouping field: `camera`, `rover`, or `sol`
- All photo filters apply (rovers, cameras, date ranges, etc.)

**Group by camera:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera"
```

**Response:**
```json
{
  "data": {
    "total_photos": 710000,
    "period": {
      "from": "2012-08-06",
      "to": "2024-11-20"
    },
    "by_camera": [
      {
        "camera": "MAST",
        "count": 242800,
        "percentage": 34.2,
        "avg_per_sol": 58.7
      },
      {
        "camera": "NAVCAM",
        "count": 195300,
        "percentage": 27.5,
        "avg_per_sol": 47.2
      }
    ]
  },
  "meta": {
    "query": {
      "group_by": "camera",
      "total_photos": 710000
    }
  }
}
```

**With date filtering:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2023-01-01&date_max=2024-01-01"
```

---

### Rovers Endpoint

List all rovers with their details.

```http
GET /api/v2/rovers
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers"
```

**Response:**
```json
{
  "data": [
    {
      "id": "curiosity",
      "type": "rover",
      "attributes": {
        "name": "Curiosity",
        "landing_date": "2012-08-06",
        "launch_date": "2011-11-26",
        "status": "active",
        "max_sol": 4102,
        "max_date": "2024-11-20",
        "total_photos": 710000
      }
    },
    {
      "id": "perseverance",
      "type": "rover",
      "attributes": {
        "name": "Perseverance",
        "landing_date": "2021-02-18",
        "launch_date": "2020-07-30",
        "status": "active",
        "max_sol": 1682,
        "max_date": "2024-11-20",
        "total_photos": 485000
      }
    }
  ],
  "meta": {
    "returned_count": 4
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/rovers"
  }
}
```

---

### Get Rover by Slug

Get details for a specific rover.

```http
GET /api/v2/rovers/{slug}
```

**Parameters:**
- `slug` (path) - Rover slug: `curiosity`, `perseverance`, `opportunity`, `spirit`

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/curiosity"
```

---

### Rover Manifest

Get photo history by sol for a specific rover.

```http
GET /api/v2/rovers/{slug}/manifest
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/curiosity/manifest"
```

**Response:**
```json
{
  "data": {
    "name": "Curiosity",
    "landing_date": "2012-08-06",
    "launch_date": "2011-11-26",
    "status": "active",
    "max_sol": 4102,
    "max_date": "2024-11-20",
    "total_photos": 710000,
    "photos": [
      {
        "sol": 0,
        "earth_date": "2012-08-06",
        "total_photos": 3702,
        "cameras": ["MAHLI", "MARDI", "NAVCAM"]
      },
      {
        "sol": 1,
        "earth_date": "2012-08-07",
        "total_photos": 16,
        "cameras": ["NAVCAM"]
      }
    ]
  }
}
```

---

### Rover Cameras

Get all cameras for a specific rover with photo counts.

```http
GET /api/v2/rovers/{slug}/cameras
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/curiosity/cameras"
```

**Response:**
```json
{
  "data": [
    {
      "id": "mast",
      "type": "camera",
      "attributes": {
        "name": "MAST",
        "full_name": "Mast Camera",
        "photo_count": 242800,
        "first_photo_sol": 0,
        "last_photo_sol": 4102
      }
    },
    {
      "id": "navcam",
      "type": "camera",
      "attributes": {
        "name": "NAVCAM",
        "full_name": "Navigation Camera",
        "photo_count": 195300,
        "first_photo_sol": 0,
        "last_photo_sol": 4102
      }
    }
  ],
  "meta": {
    "returned_count": 7
  }
}
```

---

### API Discovery

Get API capabilities and available filter values.

```http
GET /api/v2
```

**Example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2"
```

**Response:**
```json
{
  "version": "2.0.0",
  "resources": {
    "photos": {
      "href": "/api/v2/photos",
      "methods": ["GET"],
      "filters": {
        "rovers": {
          "type": "array",
          "values": ["curiosity", "perseverance", "opportunity", "spirit"]
        },
        "cameras": {
          "type": "array",
          "values": ["FHAZ", "RHAZ", "MAST", "CHEMCAM", "MAHLI", "MARDI", "NAVCAM", ...]
        },
        "sol_min": { "type": "integer", "min": 0 },
        "sol_max": { "type": "integer", "min": 0 },
        "date_min": { "type": "date", "format": "YYYY-MM-DD" },
        "date_max": { "type": "date", "format": "YYYY-MM-DD" }
      }
    },
    "rovers": {
      "href": "/api/v2/rovers",
      "methods": ["GET"]
    }
  }
}
```

---

### Error Handling

v2 uses **RFC 7807 Problem Details** for consistent error responses.

**Validation error example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=invalid_rover"
```

**Response (400):**
```json
{
  "type": "/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request contains invalid parameters",
  "instance": "/api/v2/photos?rovers=invalid_rover",
  "errors": [
    {
      "field": "rovers",
      "value": "invalid_rover",
      "message": "Unknown rover name. Must be one of: curiosity, perseverance, opportunity, spirit",
      "example": "curiosity"
    }
  ]
}
```

**Not found example:**
```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos/999999"
```

**Response (404):**
```json
{
  "type": "/errors/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Photo with ID 999999 not found",
  "instance": "/api/v2/photos/999999"
}
```

---

### Migration from v1

**v1 remains NASA-compatible** and will not change. v2 is a complete redesign.

**v1 query:**
```
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=FHAZ
```

**Equivalent v2 query:**
```
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000&cameras=FHAZ
```

**Enhanced v2 query (not possible in v1):**
```
GET /api/v2/photos?rovers=curiosity,perseverance&sol_min=1000&sol_max=1100&cameras=FHAZ,NAVCAM&fields=id,img_src,sol&sort=-sol
```

**Key Differences:**

| Feature | v1 | v2 |
|---------|----|----|
| Multiple rovers | ❌ | ✅ |
| Multiple cameras | ❌ | ✅ |
| Field selection | ❌ | ✅ |
| HTTP caching | ❌ | ✅ (ETags, conditional requests) |
| Sorting | ❌ | ✅ |
| Batch operations | ❌ | ✅ |
| Statistics | Limited | ✅ Comprehensive |
| Error format | Inconsistent | ✅ RFC 7807 |
| Pagination | Sometimes optional | ✅ Always enforced |
| Response format | NASA-compatible | ✅ Consistent envelope |

---

## See Also

- [Database Access Guide](DATABASE_ACCESS.md)
- [Curiosity Scraper Guide](CURIOSITY_SCRAPER_GUIDE.md)
- [Opportunity Scraper Guide](OPPORTUNITY_SCRAPER_GUIDE.md)
- [Spirit Scraper Guide](SPIRIT_SCRAPER_GUIDE.md)
- [Authentication Guide](AUTHENTICATION_GUIDE.md)
- [Main README](../README.md)
