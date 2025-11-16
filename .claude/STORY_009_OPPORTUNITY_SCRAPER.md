# Story 009: Opportunity Rover Scraper (PDS Index Parser)

**Status**: In Progress
**Created**: 2025-11-15
**Related**: MER_DATA_SOURCE_INVESTIGATION.md

## Overview

Implement scraper for Opportunity rover (MER1) using NASA's PDS (Planetary Data System) tab-delimited index files. This approach is necessary because traditional NASA APIs do not support MER rovers (see investigation document).

## Background

- **Data Source**: PDS Data Volumes at `planetarydata.jpl.nasa.gov`
- **Format**: Tab-delimited index files (edrindex.tab) with 55 metadata fields per photo
- **Volume Count**: 5 camera volumes (PANCAM, NAVCAM, HAZCAM, MI, DESCENT)
- **Total Photos**: ~1 million estimated
- **Key Innovation**: Parse index files instead of making API calls

## Requirements

### Functional

1. **Index File Parser**
   - Stream large tab-delimited files (326 MB for PANCAM alone)
   - Parse 55 fields per row
   - Handle malformed rows gracefully
   - Extract required Photo model fields

2. **Camera Name Mapping**
   - Map PDS camera names (PANCAM_LEFT, PANCAM_RIGHT) to database names (PANCAM)
   - Support all MER cameras: PANCAM, NAVCAM, FHAZ, RHAZ, MINITES, ENTRY

3. **URL Construction**
   - Convert data paths to browse JPG URLs
   - Pattern: `/data/sol{N}/edr/` → `/browse/sol{NNNN}/edr/`
   - Append `.jpg` extension to filenames

4. **Photo Entity Creation**
   - Populate all Photo model fields from index data
   - Store complete metadata in RawData JSONB column
   - Ensure idempotent inserts (duplicate detection by NasaId)

5. **Scraper Endpoints**
   - `POST /api/scraper/opportunity/volume/{volumeName}` - Scrape single volume
   - `POST /api/scraper/opportunity/all` - Scrape all volumes
   - `GET /api/scraper/opportunity/progress` - Monitor progress

### Non-Functional

1. **Performance**
   - Stream parsing (low memory footprint)
   - Batched database commits (1000 rows)
   - ~15-20 minutes per volume
   - Total scrape: 4-5 hours for all volumes

2. **Reliability**
   - Resume capability from last successful batch
   - Error logging for malformed rows
   - HTTP retry with exponential backoff (existing Polly policies)

3. **Data Quality**
   - Preserve all 55 metadata fields in RawData
   - No data loss from original PDS records
   - 100% NASA data preservation

## Implementation Steps

### Step 1: Create PDS Index Parser Service

**File**: `Services/Scrapers/PdsIndexParser.cs`

```csharp
public class PdsIndexParser
{
    public async Task<List<PdsIndexRow>> ParseIndexFileAsync(
        Stream stream,
        IProgress<int> progress,
        CancellationToken cancellationToken);

    public PdsIndexRow ParseRow(string line);
}

public record PdsIndexRow
{
    // 55 fields from index file
    public string ProductId { get; init; }
    public int Sol { get; init; }
    public DateTime StartTime { get; init; }
    public string InstrumentId { get; init; }
    public string PathName { get; init; }
    public string FileName { get; init; }
    public string FilterName { get; init; }
    public int Lines { get; init; }
    public int LineSamples { get; init; }
    public float? SiteInstrumentAzimuth { get; init; }
    public float? SiteInstrumentElevation { get; init; }
    // ... all other fields
}
```

### Step 2: Create Camera Name Mapper

**File**: `Services/Scrapers/MerCameraMapper.cs`

```csharp
public static class MerCameraMapper
{
    private static readonly Dictionary<string, string> CameraMapping = new()
    {
        { "PANCAM_LEFT", "PANCAM" },
        { "PANCAM_RIGHT", "PANCAM" },
        { "NAVCAM_LEFT", "NAVCAM" },
        { "NAVCAM_RIGHT", "NAVCAM" },
        { "FHAZ_LEFT", "FHAZ" },
        { "FHAZ_RIGHT", "FHAZ" },
        { "RHAZ_LEFT", "RHAZ" },
        { "RHAZ_RIGHT", "RHAZ" },
        { "MI", "MINITES" },
        { "DESCENT", "ENTRY" }
    };

    public static string MapToDbName(string pdsName);
}
```

### Step 3: Create URL Constructor

**File**: `Services/Scrapers/PdsBrowseUrlBuilder.cs`

```csharp
public static class PdsBrowseUrlBuilder
{
    private const string BaseUrl = "https://planetarydata.jpl.nasa.gov/img/data/mer";

    public static string BuildBrowseUrl(
        string rover,      // "opportunity"
        string path,       // "/mer1po_0xxx/data/sol0001/edr/"
        string filename,   // "1p128287181eff0000p2303l2m1.img"
        int sol)
    {
        var browsePath = path
            .Replace("/data/", "/browse/")
            .Replace($"sol{sol}", $"sol{sol:D4}");

        return $"{BaseUrl}/{rover.ToLower()}{browsePath}{filename}.jpg";
    }
}
```

### Step 4: Create OpportunityScraper Service

**File**: `Services/Scrapers/OpportunityScraper.cs`

Implements `IScraperService` with:
- `ScrapeVolumeAsync(string volumeUrl)` - Scrapes single PDS volume
- `ScrapeAllVolumesAsync()` - Scrapes all 5 camera volumes
- `GetProgressAsync()` - Returns scraping progress

Key methods:
```csharp
private async Task<int> ProcessIndexFileAsync(
    string indexUrl,
    string rover,
    CancellationToken cancellationToken)
{
    // 1. Download index file via HTTP stream
    // 2. Parse line by line using PdsIndexParser
    // 3. For each row:
    //    a. Map camera name
    //    b. Build browse URL
    //    c. Create Photo entity
    //    d. Store complete row as RawData JSON
    //    e. Check for duplicates
    //    f. Insert to database
    // 4. Batch commit every 1000 photos
    // 5. Log progress every 10,000 rows
}

private Photo MapToPhoto(PdsIndexRow row, Rover rover, Camera camera)
{
    return new Photo
    {
        NasaId = row.ProductId,
        Sol = row.Sol,
        EarthDate = row.StartTime.Date,
        DateTakenUtc = row.StartTime,
        ImgSrcFull = PdsBrowseUrlBuilder.BuildBrowseUrl(...),
        Width = row.LineSamples,
        Height = row.Lines,
        FilterName = row.FilterName,
        MastAz = row.SiteInstrumentAzimuth,
        MastEl = row.SiteInstrumentElevation,
        SampleType = "full",
        RawData = SerializeToJson(row), // All 55 fields preserved
        RoverId = rover.Id,
        CameraId = camera.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
}
```

### Step 5: Add Scraper Endpoints

**File**: `Controllers/ScraperController.cs`

Add endpoints:
```csharp
[HttpPost("opportunity/volume/{volumeName}")]
public async Task<IActionResult> ScrapeOpportunityVolume(
    string volumeName,
    CancellationToken cancellationToken)
{
    // volumeName examples: "mer1po_0xxx", "mer1no_0xxx", etc.
}

[HttpPost("opportunity/all")]
public async Task<IActionResult> ScrapeAllOpportunityVolumes(
    CancellationToken cancellationToken)
{
    // Process all 5 camera volumes sequentially
}

[HttpGet("opportunity/progress")]
public async Task<IActionResult> GetOpportunityProgress()
{
    // Return scraping progress stats
}
```

### Step 6: Add Configuration

**File**: `appsettings.json`

```json
{
  "PdsVolumes": {
    "BaseUrl": "https://planetarydata.jpl.nasa.gov/img/data/mer",
    "Opportunity": {
      "Volumes": [
        "mer1po_0xxx",  // PANCAM
        "mer1no_0xxx",  // NAVCAM
        "mer1ho_0xxx",  // HAZCAM
        "mer1mo_0xxx",  // MI
        "mer1do_0xxx"   // DESCENT
      ]
    },
    "IndexFileName": "index/edrindex.tab",
    "BatchSize": 1000,
    "ProgressLogInterval": 10000
  }
}
```

### Step 7: Testing

1. **Unit Tests**
   - Test tab parsing with sample rows
   - Test camera name mapping
   - Test URL construction
   - Test Photo entity mapping

2. **Integration Test**
   - Download first 1000 rows from PANCAM index
   - Verify parsing accuracy
   - Verify browse URLs are accessible
   - Verify database inserts

3. **Volume Test**
   - Scrape complete PANCAM volume (366,510 photos)
   - Measure performance
   - Verify photo counts match index row count
   - Test query API with scraped data

4. **Full Scrape Test**
   - Run all 5 volumes
   - Monitor memory usage
   - Verify total photo count
   - Test edge cases (missing cameras, malformed rows)

### Step 8: Documentation

Create `docs/OPPORTUNITY_SCRAPER_GUIDE.md`:
- PDS index file structure
- Camera mapping logic
- URL construction patterns
- Scraping process flow
- Performance characteristics
- Troubleshooting common issues

Update `docs/API_ENDPOINTS.md`:
- Document new scraper endpoints
- Include examples
- Note differences from Curiosity/Perseverance scrapers

## Acceptance Criteria

- [ ] PDS index parser successfully parses all 55 fields
- [ ] Camera name mapping works for all MER cameras
- [ ] Browse URLs are correctly constructed and accessible
- [ ] Photos are correctly inserted into database with all metadata
- [ ] RawData JSONB stores complete 55-field records
- [ ] Duplicate photos are skipped (idempotent)
- [ ] Scraper handles malformed rows without crashing
- [ ] Progress logging shows every 10,000 rows
- [ ] Batch commits work (1000 photos per commit)
- [ ] Memory usage remains stable during large scrapes
- [ ] Complete PANCAM volume scrapes in ~20 minutes
- [ ] All 5 volumes can be scraped successfully
- [ ] Query API returns Opportunity photos correctly
- [ ] Unit tests cover all parsing logic
- [ ] Integration test validates end-to-end scraping
- [ ] Documentation is complete and accurate

## Technical Decisions

### Decision 1: Stream Parsing vs Bulk Download

**Chosen**: Stream parsing

**Rationale**:
- Index files are 326 MB (PANCAM) to potentially 500 MB (NAVCAM)
- Loading entire file into memory would consume ~1-2 GB RAM
- Stream parsing keeps memory usage constant
- Slightly slower but much more scalable

**Trade-offs**:
- Cannot easily seek/skip to specific positions
- Must process linearly from start to finish
- But: memory efficiency is more important for production deployment

### Decision 2: Store All 55 Fields in RawData

**Chosen**: Store complete index row as JSON in RawData column

**Rationale**:
- PDS provides 55 metadata fields vs ~20 from active rover APIs
- Many fields could enable future features (panoramas, telemetry, calibration)
- Disk space is cheap (~100 bytes per photo in JSON)
- Enables 100% data preservation philosophy
- Aligns with hybrid storage approach from Story 002

**Trade-offs**:
- Slightly larger database size
- But: enables rich metadata queries without re-scraping
- Future-proofs for advanced features (see ADVANCED_FEATURES_POSSIBILITIES.md)

### Decision 3: Sequential vs Parallel Volume Processing

**Chosen**: Sequential (for initial implementation)

**Rationale**:
- Simpler error handling and progress tracking
- Lower risk of database connection exhaustion
- Easier to debug and monitor
- One-time scrape (rovers are inactive)
- Can parallelize in future if needed

**Trade-offs**:
- Takes 4-5 hours vs potentially 1-2 hours parallelized
- But: reliability and simplicity are more important
- Can run overnight without supervision

## Performance Estimates

**Per Volume**:
- Index download: ~30 seconds
- Parsing: ~1,000 rows/second
- Database inserts: ~500 photos/second (batched)
- Total: 15-20 minutes per volume

**All Volumes** (5 cameras):
- PANCAM: 20 minutes (366,510 photos)
- NAVCAM: 30 minutes (est. 500,000 photos)
- HAZCAM: 10 minutes (est. 100,000 photos)
- MI: 5 minutes (est. 50,000 photos)
- DESCENT: 2 minutes (est. 20,000 photos)
- **Total**: ~90 minutes (1.5 hours)

**Memory Usage**: <500 MB (stream parsing)

## Risks & Mitigations

### Risk 1: Index File Format Changes

**Likelihood**: Low (PDS archives are stable)
**Impact**: High (scraper breaks)
**Mitigation**: Validate column count, log warnings for unexpected formats

### Risk 2: Missing Browse JPGs

**Likelihood**: Medium (some sols may lack browse images)
**Impact**: Low (raw IMG files exist as fallback)
**Mitigation**: Log missing images, continue processing

### Risk 3: Camera Not Found in Database

**Likelihood**: Low (seed data complete)
**Impact**: Medium (photos skipped)
**Mitigation**: Auto-create missing cameras, or fail loudly with clear error

### Risk 4: Database Connection Timeout

**Likelihood**: Low (connection pooling)
**Impact**: Medium (scrape interruption)
**Mitigation**: Batched commits, resume capability from last successful batch

## Future Enhancements

1. **Progress Persistence**: Store progress in database table for resume capability
2. **Parallel Volume Processing**: Process multiple volumes concurrently
3. **Differential Updates**: Check if volume has new data (unlikely for inactive rovers)
4. **Browse Image Validation**: HEAD request to verify JPG exists before storing URL
5. **RDR (Calibrated) Images**: Support Radiometrically-calibrated images from RDR index

## Related Files

- `.claude/MER_DATA_SOURCE_INVESTIGATION.md` - Data source research
- `Services/Scrapers/CuriosityScraper.cs` - Reference implementation pattern
- `Services/Scrapers/PerseveranceScraper.cs` - Reference implementation pattern
- `docs/API_ENDPOINTS.md` - Endpoint documentation

## References

- [PDS Imaging Node - MER Mission](https://pds-imaging.jpl.nasa.gov/portal/mer_mission.html)
- [Opportunity Data Volumes](https://planetarydata.jpl.nasa.gov/img/data/mer/opportunity/)
- [PDS3 Standards Reference](https://pds.nasa.gov/datastandards/pds3/)

---

**Story Points**: 8 (Large - New parsing approach, complex data mapping)
**Estimated Hours**: 6-7 hours development + 2 hours testing
**Priority**: High (Required for complete MER support)
