# API Endpoints Documentation

Complete reference for all Mars Vista API endpoints.

## Base URLs

- **Production**: `https://[your-railway-domain].up.railway.app` (update after deployment)
- **Local Development**: `http://localhost:5127`

All examples below use `localhost` for local development. For production, replace with your Railway domain.

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
curl "http://localhost:5127/api/v1/rovers"
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
curl "http://localhost:5127/api/v1/rovers/curiosity"
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
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1"
```

**Get photos from specific Earth date:**
```bash
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?earth_date=2012-08-06"
```

**Filter by camera:**
```bash
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=100&camera=MAST"
```

**Pagination:**
```bash
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1&page=2&per_page=50"
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
curl "http://localhost:5127/api/v1/rovers/curiosity/latest?per_page=10"

# NASA API compatible endpoint
curl "http://localhost:5127/api/v1/rovers/curiosity/latest_photos?per_page=10"
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
curl "http://localhost:5127/api/v1/photos/451603"
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
curl "http://localhost:5127/api/v1/manifests/curiosity"
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
curl "http://localhost:5127/api/v1/rovers" | jq '.rovers[].name'
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
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=50" | jq '.photos[0]'
```

**6. Resume if interrupted:**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/resume"
```

---

## See Also

- [Database Access Guide](DATABASE_ACCESS.md)
- [Curiosity Scraper Guide](CURIOSITY_SCRAPER_GUIDE.md)
- [Opportunity Scraper Guide](OPPORTUNITY_SCRAPER_GUIDE.md)
- [Spirit Scraper Guide](SPIRIT_SCRAPER_GUIDE.md)
- [Main README](../README.md)
