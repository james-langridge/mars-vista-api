# Spirit Rover Scraper Guide

## Overview

The Spirit scraper uses the same approach as Opportunity - it parses PDS (Planetary Data System) tab-delimited index files instead of querying JSON APIs, because NASA's traditional APIs don't support the MER rovers.

Spirit (MER-2) was Opportunity's twin rover that landed on Mars on January 3, 2004. Spirit operated until March 2010 when it became stuck in soft sand and eventually lost communication.

**Mission Statistics:**
- Landing: January 3, 2004
- Last Communication: March 22, 2010
- Mission Duration: ~6 years
- Total Sols: ~2,208
- Status: Mission ended (stuck in sand, communication lost)

## Key Differences from Other Scrapers

- **Data Source**: PDS Data Volumes at `planetarydata.jpl.nasa.gov`
- **Format**: Tab-delimited index files (edrindex.tab) with 55 metadata fields per photo
- **Volume-based**: Scrapes by camera volume (PANCAM, NAVCAM, HAZCAM, MI, DESCENT) rather than by sol
- **Total Photos**: ~225,000 estimated across all volumes (41% of Opportunity's total due to shorter mission)

## API Endpoints

### Scrape Single Volume

```bash
POST /api/scraper/spirit/volume/{volumeName}
```

**Available Volumes:**
- `mer2po_0xxx` - PANCAM (~150,000 photos estimated)
- `mer2no_0xxx` - NAVCAM (~50,000 photos estimated)
- `mer2ho_0xxx` - HAZCAM (~15,000 photos estimated)
- `mer2mo_0xxx` - Microscopic Imager (~10,000 photos estimated)
- `mer2do_0xxx` - Descent Camera (9 photos)

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/spirit/volume/mer2po_0xxx"
```

### Scrape All Volumes

```bash
POST /api/scraper/spirit/all
```

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/spirit/all"
```

### Monitor Progress

```bash
GET /api/scraper/spirit/progress
```

**Example:**
```bash
curl "http://localhost:5127/api/scraper/spirit/progress" | jq
```

Or use the monitoring script:
```bash
./scrape-monitor.sh spirit
```

## How It Works

### 1. PDS Index File Structure

Each camera volume contains an `index/edrindex.tab` file with metadata for all photos:

```
Volume: mer2po_0xxx/index/edrindex.tab (PANCAM)
Format: Tab-delimited ASCII

Note: Field counts vary by camera:
- PANCAM, NAVCAM, HAZCAM, MI: 59 fields
- DESCENT: 52 fields (missing PathName and FileName fields)
```

### 2. Scraping Process

1. **Download Index File**: HTTP stream from PDS server
2. **Parse Line-by-Line**: Streaming parser (low memory)
3. **Extract Metadata**: 55 fields including sol, date, camera, filter, telemetry
4. **Map Camera Names**: Convert PDS names (PANCAM_LEFT) to DB names (PANCAM)
5. **Build Browse URLs**: Convert data paths to browse JPG paths (mer2 prefix)
6. **Store Photos**: Batch insert (1000 photos per commit)
7. **Store Raw Data**: Complete 55-field record in JSONB column

### 3. Camera Name Mapping

PDS uses LEFT/RIGHT camera variants and full descriptive names (identical to Opportunity):

| PDS Instrument ID | Database Name | Notes |
|------------------|---------------|-------|
| PANCAM_LEFT | PANCAM | Panoramic Camera |
| PANCAM_RIGHT | PANCAM | |
| NAVCAM_LEFT | NAVCAM | Navigation Camera |
| NAVCAM_RIGHT | NAVCAM | |
| FRONT_HAZCAM_LEFT | FHAZ | Front Hazard Avoidance |
| FRONT_HAZCAM_RIGHT | FHAZ | |
| REAR_HAZCAM_LEFT | RHAZ | Rear Hazard Avoidance |
| REAR_HAZCAM_RIGHT | RHAZ | |
| MI | MINITES | Microscopic Imager |
| DESCAM | ENTRY | Descent Camera |

### 4. URL Construction

Browse JPG URLs are constructed from index file paths:

```
Data path:   /mer2po_0xxx/data/sol0001/edr/2p128287181eff0000p2303l2m1.img
Browse path: /mer2po_0xxx/browse/sol0001/edr/2p128287181eff0000p2303l2m1.img.jpg

Full URL: https://planetarydata.jpl.nasa.gov/img/data/mer/spirit/mer2po_0xxx/browse/sol0001/edr/2p128287181eff0000p2303l2m1.img.jpg
```

**Note**: Spirit uses `mer2` prefix (MER-2) vs Opportunity's `mer1` (MER-1).

## Performance

### Expected Times

- **PANCAM volume**: ~10 minutes (~150,000 photos)
- **NAVCAM volume**: ~5 minutes (~50,000 photos)
- **HAZCAM volume**: ~2 minutes (~15,000 photos)
- **MI volume**: ~2 minutes (~10,000 photos)
- **DESCENT volume**: <1 minute (~9 photos)
- **All volumes**: ~20-30 minutes total

### Memory Usage

- Stream parsing keeps memory usage low (<500 MB)
- Batched commits (1000 photos) prevent memory bloat
- In-memory HashSet for duplicate checking

### Processing Rate

- **Parsing**: ~1,000 rows/second
- **Database inserts**: ~1,000 photos/second (with in-memory duplicate checking)

## Data Quality

### Rich Metadata

PDS index files provide **55 metadata fields** vs ~20 for active rovers:

**Time Data:**
- Sol number
- Earth date/time (UTC)
- Mars local time
- Received date/time
- Spacecraft clock

**Image Metadata:**
- Dimensions (width/height)
- Filter name
- Exposure duration
- Compression info

**Camera Telemetry:**
- Mast azimuth/elevation
- Camera orientation
- Field of view
- Sun position

**Location/Navigation:**
- Rover motion counter (odometry)
- Solar longitude (seasonal marker)
- Coordinate frame

### 100% Data Preservation

All 55 fields stored in `raw_data` JSONB column - enables future features like panorama reconstruction, stereo imaging, sun angle analysis, etc.

## Troubleshooting

### Scraper Appears Stuck

The scraper may appear unresponsive when:
- Downloading large index file (can take 30-60 seconds)
- Processing first batch (parsing 1000 rows before first commit)
- Database commit (batched inserts can take 10-20 seconds)

**Solution**: Wait 1-2 minutes, then check progress endpoint.

### Camera Not Found Error

If you see "Camera not found" warnings:
- Ensure Spirit rover seed data is present (rover_id = 4)
- Check camera names in database match MerCameraMapper
- Verify rover ID is 4 (Spirit)

```sql
SELECT id, name FROM rovers WHERE name = 'Spirit';
-- Expected: id = 4

SELECT id, name FROM cameras WHERE rover_id = 4;
-- Expected: 6 cameras (FHAZ, RHAZ, NAVCAM, PANCAM, MINITES, ENTRY)
```

### Photos Skipped

Photos are skipped if:
- Already exist in database (idempotent by NasaId)
- Camera not found in database
- Missing required fields (StartTime, ProductId)

Check logs for skip reasons.

## Implementation Details

### Services Used

Spirit scraper reuses all services created for Opportunity:

1. **PdsIndexParser** - Parses tab-delimited index files
2. **PdsIndexRow** - Record with 55 fields
3. **MerCameraMapper** - Camera name mapping (shared with Opportunity)
4. **PdsBrowseUrlBuilder** - URL construction (supports mer2 prefix)
5. **SpiritScraper** - Main scraper service (Spirit-specific)

### Code Location

```
src/MarsVista.Api/Services/
├── SpiritScraper.cs (NEW)
├── OpportunityScraper.cs
├── PdsIndexParser.cs (shared)
├── PdsIndexRow.cs (shared)
├── MerCameraMapper.cs (shared)
└── PdsBrowseUrlBuilder.cs (shared)
```

### Configuration

Scraper uses existing NASA HTTP client with Polly resilience policies:
- 3 retries with exponential backoff
- Circuit breaker (5 failures → 1 minute break)
- 30 second timeout

## Example Workflow

### Test with DESCENT Volume (Smallest)

```bash
# Start with smallest volume to verify scraper works
curl -X POST "http://localhost:5127/api/scraper/spirit/volume/mer2do_0xxx"

# Should complete in <1 minute with 9 photos
```

### Scrape Single Volume

```bash
# Start PANCAM scrape
curl -X POST "http://localhost:5127/api/scraper/spirit/volume/mer2po_0xxx"

# Monitor progress (in another terminal)
watch -n 5 'curl -s "http://localhost:5127/api/scraper/spirit/progress" | jq'

# Or use the monitor script
./scrape-monitor.sh spirit
```

### Scrape All Volumes

```bash
# Start all volumes scrape (runs sequentially)
curl -X POST "http://localhost:5127/api/scraper/spirit/all"

# This will take ~20-30 minutes total
# Monitor in real-time
./scrape-monitor.sh spirit
```

### Query Results

```bash
# Get Spirit photos from sol 1
curl "http://localhost:5127/api/v1/rovers/spirit/photos?sol=1" | jq

# Get PANCAM photos
curl "http://localhost:5127/api/v1/rovers/spirit/photos?camera=pancam&sol=1" | jq
```

### Check Status

```bash
# Use the status script for quick database check
./scrape-spirit-status.sh
```

## Differences from Opportunity

| Aspect | Opportunity (MER-1) | Spirit (MER-2) |
|--------|---------------------|----------------|
| Mission Duration | 14.5 years | 6 years |
| Total Sols | 5,111 | 2,208 |
| Volume Prefix | mer1 | mer2 |
| Rover ID | 3 | 4 |
| Estimated Photos | 548,817 | ~225,000 |
| Mission End | 2019 (dust storm) | 2010 (stuck in sand) |

## Lessons from Opportunity Implementation

All issues discovered during Opportunity scraper development were resolved before Spirit implementation:

### ✅ Camera Name Mapping
- Full PDS instrument names already in MerCameraMapper
- HAZCAM photos will be mapped correctly from the start

### ✅ Variable Field Counts
- Parser already handles 52-field (DESCENT) and 59-field (standard) formats
- DESCENT volume should work without issues

### ✅ Performance Optimization
- In-memory HashSet for duplicate checking (1000x faster)
- No database query per photo check
- Consistent ~1000 photos/second throughput

### ✅ Batch Error Handling
- Graceful handling of duplicate key constraints
- Entity detachment to prevent tracking issues
- Continue processing after batch failures

## Related Documentation

- `.claude/STORY_010_SPIRIT_SCRAPER.md` - Implementation story
- `.claude/MER_DATA_SOURCE_INVESTIGATION.md` - Data source research
- `docs/OPPORTUNITY_SCRAPER_GUIDE.md` - Sister rover implementation
- `docs/API_ENDPOINTS.md` - All API endpoints
- `docs/DATABASE_ACCESS.md` - Database queries
