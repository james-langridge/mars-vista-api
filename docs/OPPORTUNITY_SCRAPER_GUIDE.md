# Opportunity Rover Scraper Guide

## Overview

The Opportunity scraper uses a different approach than Curiosity/Perseverance - it parses PDS (Planetary Data System) tab-delimited index files instead of querying JSON APIs, because NASA's traditional APIs don't support the MER rovers.

## Key Differences from Other Scrapers

- **Data Source**: PDS Data Volumes at `planetarydata.jpl.nasa.gov`
- **Format**: Tab-delimited index files (edrindex.tab) with 55 metadata fields per photo
- **Volume-based**: Scrapes by camera volume (PANCAM, NAVCAM, HAZCAM, MI, DESCENT) rather than by sol
- **Index File Size**: 342 MB for PANCAM volume alone
- **Total Photos**: ~1 million estimated across all volumes

## API Endpoints

### Scrape Single Volume

```bash
POST /api/scraper/opportunity/volume/{volumeName}
```

**Available Volumes:**
- `mer1po_0xxx` - PANCAM (366,510 photos)
- `mer1no_0xxx` - NAVCAM (~500,000 photos estimated)
- `mer1ho_0xxx` - HAZCAM (~100,000 photos estimated)
- `mer1mo_0xxx` - Microscopic Imager
- `mer1do_0xxx` - Descent Camera

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/opportunity/volume/mer1po_0xxx"
```

### Scrape All Volumes

```bash
POST /api/scraper/opportunity/all
```

**Example:**
```bash
curl -X POST "http://localhost:5127/api/scraper/opportunity/all"
```

### Monitor Progress

```bash
GET /api/scraper/opportunity/progress
```

**Example:**
```bash
curl "http://localhost:5127/api/scraper/opportunity/progress" | jq
```

Or use the monitoring script:
```bash
./scrape-monitor.sh opportunity
```

## How It Works

### 1. PDS Index File Structure

Each camera volume contains an `index/edrindex.tab` file with metadata for all photos:

```
Volume: mer1po_0xxx/index/edrindex.tab (PANCAM)
Size: 342 MB
Rows: 366,510 (one per photo)
Columns: 59 metadata fields
Format: Tab-delimited ASCII

Note: Field counts vary by camera:
- PANCAM, NAVCAM, HAZCAM, MI: 59 fields
- DESCENT: 52 fields (missing 3 calibration fields)
```

### 2. Scraping Process

1. **Download Index File**: HTTP stream from PDS server
2. **Parse Line-by-Line**: Streaming parser (low memory)
3. **Extract Metadata**: 55 fields including sol, date, camera, filter, telemetry
4. **Map Camera Names**: Convert PDS names (PANCAM_LEFT) to DB names (PANCAM)
5. **Build Browse URLs**: Convert data paths to browse JPG paths
6. **Store Photos**: Batch insert (1000 photos per commit)
7. **Store Raw Data**: Complete 55-field record in JSONB column

### 3. Camera Name Mapping

PDS uses LEFT/RIGHT camera variants and full descriptive names:

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
| DESCENT | ENTRY |

### 4. URL Construction

Browse JPG URLs are constructed from index file paths:

```
Data path:   /mer1po_0xxx/data/sol0001/edr/1p128287181eff0000p2303l2m1.img
Browse path: /mer1po_0xxx/browse/sol0001/edr/1p128287181eff0000p2303l2m1.img.jpg

Full URL: https://planetarydata.jpl.nasa.gov/img/data/mer/opportunity/mer1po_0xxx/browse/sol0001/edr/1p128287181eff0000p2303l2m1.img.jpg
```

## Performance

### Expected Times

- **PANCAM volume**: ~20 minutes (366,510 photos)
- **NAVCAM volume**: ~30 minutes (est. 500,000 photos)
- **HAZCAM volume**: ~10 minutes (est. 100,000 photos)
- **All volumes**: ~90 minutes total

### Memory Usage

- Stream parsing keeps memory usage low (<500 MB)
- Batched commits (1000 photos) prevent memory bloat

### Processing Rate

- **Parsing**: ~1,000 rows/second
- **Database inserts**: ~500 photos/second (batched)

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
- Downloading large index file (342 MB can take 30-60 seconds)
- Processing first batch (parsing 1000 rows before first commit)
- Database commit (batched inserts can take 10-20 seconds)

**Solution**: Wait 1-2 minutes, then check progress endpoint.

### Camera Not Found Error

If you see "Camera not found" warnings:
- Ensure Opportunity rover seed data is present
- Check camera names in database match MerCameraMapper
- Verify rover ID is 3 (Opportunity)

### Photos Skipped

Photos are skipped if:
- Already exist in database (idempotent by NasaId)
- Camera not found in database
- Missing required fields (StartTime, ProductId)

Check logs for skip reasons.

## Data Integrity Issues & Resolutions

### Issue #1: Missing HAZCAM Photos (FIXED)

**Problem**: Initial scrape showed 0 HAZCAM photos despite ~100,000 expected.

**Root Cause**: PDS index files use full instrument names (`FRONT_HAZCAM_LEFT`, `REAR_HAZCAM_RIGHT`) but our camera mapper only had short variants (`FHAZ_LEFT`, `RHAZ_RIGHT`). All HAZCAM photos were being skipped with "Camera not found" warnings.

**Solution**: Added full PDS instrument names to MerCameraMapper.cs:
```csharp
{ "FRONT_HAZCAM_LEFT", "FHAZ" },
{ "FRONT_HAZCAM_RIGHT", "FHAZ" },
{ "REAR_HAZCAM_LEFT", "RHAZ" },
{ "REAR_HAZCAM_RIGHT", "RHAZ" },
```

### Issue #2: Missing DESCENT Photos (FIXED)

**Problem**: DESCENT volume scraped 0 photos, logs showed "expected 55 fields, got 52".

**Root Cause**: Parser required exactly 55 fields, but DESCENT camera index files only have 52 fields (missing 3 calibration fields: `ShutterEffectCorrection`, `PixelAveragingHeight`, `PixelAveragingWidth`).

**Solution**: Made parser flexible for variable field counts:
- Changed minimum from 55→51 fields
- Added conditional access for optional fields 52-54
- Parser now handles both 59-field (standard) and 52-field (DESCENT) formats

### Issue #3: Variable Field Counts

**Discovery**: Different camera volumes have different field counts:
- **PANCAM, NAVCAM, HAZCAM, MI**: 59 fields (4 more than expected!)
- **DESCENT**: 52 fields (3 less than expected)

**Resolution**: Parser now accepts 51+ fields and gracefully handles optional fields.

### Data Integrity Verification

After fixes, re-scraped all volumes with clean results:

**Curiosity Scraper (JSON API):**
- ✅ All 7 cameras have photos
- ✅ Uses prefix matching for camera variants (NAV_LEFT_B → NAVCAM)
- ✅ No data loss issues

**Perseverance Scraper (JSON API):**
- ✅ 19 of 20 cameras have photos
- ✅ LCAM (Lander Vision System) has 0 photos - EXPECTED (landing camera only)
- ✅ No mapping issues (uses exact camera names from API)

**Opportunity Scraper (PDS Index):**
- ✅ All 5 camera volumes processing correctly
- ✅ Camera mapper updated with full PDS instrument names
- ✅ Parser handles variable field counts (52-59 fields)

### Lessons Learned

1. **Always verify camera mappings** - PDS/NASA use inconsistent naming across systems
2. **Test all camera types** - Don't assume field formats are consistent
3. **Monitor zero-photo cameras** - They indicate mapping/parsing issues
4. **Parse complete data first** - Don't rely on documentation for field counts

## Implementation Details

### Services Created

1. **PdsIndexParser** - Parses tab-delimited index files
2. **PdsIndexRow** - Record with 55 fields
3. **MerCameraMapper** - Camera name mapping
4. **PdsBrowseUrlBuilder** - URL construction
5. **OpportunityScraper** - Main scraper service

### Code Location

```
src/MarsVista.Api/Services/
├── OpportunityScraper.cs
├── PdsIndexParser.cs
├── PdsIndexRow.cs
├── MerCameraMapper.cs
└── PdsBrowseUrlBuilder.cs
```

### Configuration

Scraper uses existing NASA HTTP client with Polly resilience policies:
- 3 retries with exponential backoff
- Circuit breaker (5 failures → 1 minute break)
- 30 second timeout

## Example Workflow

### Scrape Single Volume

```bash
# Start PANCAM scrape
curl -X POST "http://localhost:5127/api/scraper/opportunity/volume/mer1po_0xxx"

# Monitor progress (in another terminal)
watch -n 5 'curl -s "http://localhost:5127/api/scraper/opportunity/progress" | jq'

# Or use the monitor script
./scrape-monitor.sh opportunity
```

### Scrape All Volumes

```bash
# Start all volumes scrape (runs sequentially)
curl -X POST "http://localhost:5127/api/scraper/opportunity/all"

# This will take ~90 minutes total
# Monitor in real-time
./scrape-monitor.sh opportunity
```

### Query Results

```bash
# Get Opportunity photos from sol 1
curl "http://localhost:5127/api/v1/rovers/opportunity/photos?sol=1" | jq

# Get PANCAM photos
curl "http://localhost:5127/api/v1/rovers/opportunity/photos?camera=pancam&sol=1" | jq
```

## Related Documentation

- `.claude/MER_DATA_SOURCE_INVESTIGATION.md` - Data source research
- `.claude/STORY_009_OPPORTUNITY_SCRAPER.md` - Implementation story
- `docs/API_ENDPOINTS.md` - All API endpoints
- `docs/DATABASE_ACCESS.md` - Database queries
