# NASA API Data Analysis: Available vs Stored Fields

## Executive Summary

The Rails API stores only **5-10% of the available NASA data**, focusing on essential fields needed for photo discovery. This minimalist approach prioritizes simplicity and performance over comprehensive data storage.

## 1. Perseverance API Data Analysis

### Available Fields from NASA (mars.nasa.gov/rss/api/)

```json
{
  "sol": 1646,                              // ✅ STORED
  "imageid": "NRF_1646_0813073669_097ECM...", // ❌ NOT STORED
  "image_files": {
    "small": "https://...320.jpg",          // ❌ NOT STORED
    "medium": "https://...800.jpg",         // ❌ NOT STORED
    "large": "https://...1200.jpg",         // ✅ STORED (as img_src)
    "full_res": "https://....png"           // ❌ NOT STORED
  },
  "camera": {
    "instrument": "NAVCAM_RIGHT",           // ✅ STORED (as camera relation)
    "filter_name": "UNK",                   // ❌ NOT STORED
    "camera_vector": "(-0.684,-0.718...)",  // ❌ NOT STORED
    "camera_position": "(0.766,0.311...)",  // ❌ NOT STORED
    "camera_model_type": "CAHVORE",         // ❌ NOT STORED
    "camera_model_component_list": "..."     // ❌ NOT STORED
  },
  "attitude": "(0.953,-0.077,0.090...)",    // ❌ NOT STORED
  "sample_type": "Full",                    // ❌ NOT STORED (used for filtering only)
  "date_taken_utc": "2025-10-07T02:08:38",  // ❌ NOT STORED
  "date_taken_mars": "Sol-01646M15:18:15",  // ❌ NOT STORED
  "date_received": "2025-10-07T13:35:54Z",  // ❌ NOT STORED
  "drive": "1204",                          // ❌ NOT STORED
  "site": 79,                                // ❌ NOT STORED
  "title": "Mars Perseverance Sol 1646...",  // ❌ NOT STORED
  "caption": "NASA's Mars Perseverance...",  // ❌ NOT STORED
  "credit": "NASA/JPL-Caltech",             // ❌ NOT STORED
  "json_link": "https://mars.nasa.gov/...", // ❌ NOT STORED
  "link": "https://mars.nasa.gov/...",      // ❌ NOT STORED
  "extended": {
    "mastAz": "-156.098",                   // ❌ NOT STORED - Mast azimuth
    "mastEl": "-10.1652",                   // ❌ NOT STORED - Mast elevation
    "sclk": "813073669.716",                // ❌ NOT STORED - Spacecraft clock
    "scaleFactor": "2",                     // ❌ NOT STORED
    "xyz": "(35.4362,22.5714,-9.46445)",    // ❌ NOT STORED - Rover position
    "subframeRect": "(2545,1,2576,1936)",   // ❌ NOT STORED
    "dimension": "(1288,968)"               // ❌ NOT STORED - Image dimensions
  }
}
```

### Perseverance Data Summary
- **Total Available Fields**: 30+
- **Fields Stored**: 3 (sol, img_src from large, camera.instrument)
- **Storage Ratio**: ~10%

## 2. Curiosity API Data Analysis

### Available Fields from NASA (mars.nasa.gov/api/v1/raw_image_items/)

```json
{
  "id": 1523343,                            // ❌ NOT STORED - NASA's internal ID
  "sol": 4681,                              // ✅ STORED
  "imageid": "CR0_813038955PRC_F119...",    // ❌ NOT STORED
  "instrument": "CHEMCAM_RMI",              // ✅ STORED (as camera relation)
  "url": "https://mars.nasa.gov/...",       // ❌ NOT STORED (uses https_url)
  "https_url": "https://mars.nasa.gov/...", // ✅ STORED (as img_src)
  "site": 119,                              // ❌ NOT STORED
  "drive": 1344,                             // ❌ NOT STORED
  "spacecraft_clock": 813038955.646,        // ❌ NOT STORED
  "subframe_rect": "(1,1,1024,1024)",      // ❌ NOT STORED
  "scale_factor": null,                     // ❌ NOT STORED
  "camera_vector": null,                    // ❌ NOT STORED
  "camera_position": null,                  // ❌ NOT STORED
  "camera_model_type": null,                // ❌ NOT STORED
  "camera_model_component_list": null,      // ❌ NOT STORED
  "attitude": "(0.940,0.056,-0.053...)",    // ❌ NOT STORED - Rover orientation
  "xyz": "(-14.4449,44.2653,2.908)",        // ❌ NOT STORED - Rover position
  "date_taken": "2025-10-06T17:16:46",      // ❌ NOT STORED
  "date_received": "2025-10-07T11:12:08",   // ❌ NOT STORED
  "created_at": "2025-10-07T11:13:30",      // ❌ NOT STORED
  "updated_at": "2025-10-07T11:35:06",      // ❌ NOT STORED
  "mission": "msl",                         // ❌ NOT STORED
  "instrument_sort": 4,                     // ❌ NOT STORED
  "sample_type_sort": 999,                  // ❌ NOT STORED
  "is_thumbnail": false,                    // ❌ NOT STORED
  "title": "Sol 4681: Chemistry & Camera",  // ❌ NOT STORED
  "description": "This image was taken...",  // ❌ NOT STORED
  "link": "/raw_images/1523343",            // ❌ NOT STORED
  "image_credit": "NASA/JPL-Caltech/LANL",  // ❌ NOT STORED
  "extended": {
    "lmst": "Sol-04681M10:40:32.355",       // ❌ NOT STORED - Local Mars Solar Time
    "bucket": "msl-raws",                   // ❌ NOT STORED
    "mast_az": "346.566",                   // ❌ NOT STORED - Mast azimuth
    "mast_el": "-44.9492",                  // ❌ NOT STORED - Mast elevation
    "url_list": "https://...",              // ❌ NOT STORED
    "contributor": "Team MSLICE",           // ❌ NOT STORED
    "filter_name": null,                    // ❌ NOT STORED
    "sample_type": "chemcam prc"            // ❌ NOT STORED (used for filtering only)
  }
}
```

### Curiosity Data Summary
- **Total Available Fields**: 38+
- **Fields Stored**: 3 (sol, https_url as img_src, instrument)
- **Storage Ratio**: ~8%

## 3. Rails Database Schema

### What Rails Actually Stores

```ruby
# photos table (6 fields total)
- id           # Auto-generated
- img_src      # URL to large/full image
- sol          # Martian day
- earth_date   # Calculated from sol
- rover_id     # Foreign key
- camera_id    # Foreign key
- old_camera   # Legacy field (unused)

# cameras table (3 fields)
- id           # Auto-generated
- name         # Abbreviation (NAVCAM, FHAZ, etc.)
- full_name    # Human-readable name
- rover_id     # Foreign key

# rovers table (4 fields)
- id           # Auto-generated
- name         # Perseverance, Curiosity, etc.
- landing_date # Mars landing date
- launch_date  # Earth launch date
- status       # active/inactive
```

### What Rails API Serves

```json
{
  "id": 123,
  "sol": 1646,
  "camera": {
    "name": "NAVCAM_RIGHT",
    "full_name": "Navigation Camera - Right"
  },
  "img_src": "https://mars.nasa.gov/.../image_1200.jpg",
  "earth_date": "2025-10-07",
  "rover": {
    "id": 1,
    "name": "Perseverance",
    "landing_date": "2021-02-18",
    "launch_date": "2020-07-30",
    "status": "active",
    "max_sol": 1646,
    "max_date": "2025-10-07",
    "total_photos": 887260
  }
}
```

## 4. Why These Fields Were Chosen

### Fields Stored (The Essentials)

1. **img_src** - The core purpose: linking to the actual photo
2. **sol** - Primary temporal identifier on Mars
3. **earth_date** - Human-friendly date (calculated, not scraped)
4. **camera** - Essential for filtering by instrument type
5. **rover** - Essential for multi-rover support

### Fields Ignored (And Why)

#### Position/Navigation Data
- **xyz coordinates** - Rover's position on Mars
- **drive/site** - Location identifiers
- **attitude** - Rover orientation quaternion
- **Why ignored**: Not useful for photo discovery; specialized users needing this data would use NASA's raw feeds

#### Camera Technical Data
- **camera_vector/position** - 3D camera orientation
- **camera_model_type/component_list** - CAHVORE camera model parameters
- **filter_name** - Optical filter used
- **Why ignored**: Too technical for general users; scientists needing this would use raw data

#### Metadata
- **NASA's internal IDs** - imageid, id
- **Timestamps** - date_taken_utc, date_received, created_at
- **Links** - json_link, link (to NASA pages)
- **Why ignored**: Redundant or not useful for photo discovery

#### Telemetry
- **spacecraft_clock** - Precise timing
- **mast_az/mast_el** - Mast position angles
- **lmst** - Local Mars Solar Time
- **Why ignored**: Scientific data not needed for photo browsing

#### Image Variants
- **small/medium/full_res** - Alternative sizes
- **Why ignored**: Chose single size (large) to simplify; users can derive other sizes from URL patterns

## 5. Discarded Data Analysis

### High-Value Fields Being Discarded

1. **Image Dimensions** (`dimension: "(1288,968)"`)
   - Could enable filtering by resolution
   - Useful for layout planning in UIs

2. **Mars Time** (`date_taken_mars: "Sol-01646M15:18:15"`)
   - Precise time of day on Mars
   - Useful for shadow analysis, lighting conditions

3. **Multiple Image Sizes**
   ```json
   "small": "...320.jpg",
   "medium": "...800.jpg",
   "large": "...1200.jpg",
   "full_res": "....png"
   ```
   - Could offer bandwidth options
   - Progressive loading capabilities

4. **Location Data** (`site`, `drive`)
   - Track rover journey
   - Group photos by location

5. **NASA's Internal ID**
   - Would enable cross-referencing with NASA's systems
   - Stable identifier for deduplication

6. **Sample Type**
   - Currently used for filtering but not stored
   - Could enable quality filtering in API

## 6. Recommendations for C# Implementation

### Minimal Approach (Match Rails)
Store exactly what Rails stores for compatibility:
- ✅ Simple, proven approach
- ✅ Minimal storage requirements
- ❌ Misses valuable metadata

### Enhanced Approach (Recommended)
Add these high-value fields:

```csharp
public class Photo
{
    // Current Rails fields
    public int Id { get; set; }
    public string ImgSrc { get; set; }
    public int Sol { get; set; }
    public DateTime? EarthDate { get; set; }
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Recommended additions
    public string NasaId { get; set; }        // NASA's imageid for cross-reference
    public string SampleType { get; set; }    // "Full", "Thumbnail", etc.
    public DateTime DateTakenUtc { get; set; } // Precise UTC timestamp
    public string DateTakenMars { get; set; }  // Mars local time
    public int? Width { get; set; }            // Image dimensions
    public int? Height { get; set; }
    public int? Site { get; set; }             // Location identifiers
    public int? Drive { get; set; }
    public string SmallUrl { get; set; }       // Alternative sizes
    public string MediumUrl { get; set; }
    public string FullResUrl { get; set; }
}
```

### Comprehensive Approach (Store Everything)
Use PostgreSQL JSONB column for complete NASA response:

```csharp
public class Photo
{
    // Core fields as columns for querying
    public int Id { get; set; }
    public string ImgSrc { get; set; }
    public int Sol { get; set; }
    public DateTime? EarthDate { get; set; }
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Complete NASA response as JSONB
    public JsonDocument RawNasaData { get; set; }  // PostgreSQL JSONB column
}
```

Benefits:
- ✅ Never lose data
- ✅ Can extract new fields later without re-scraping
- ✅ Supports scientific users
- ❌ Larger storage requirements

### Recommended Architecture

```sql
-- Core queryable fields as columns
CREATE TABLE photos (
    id SERIAL PRIMARY KEY,
    nasa_id VARCHAR(100) UNIQUE,      -- NASA's ID for dedup
    img_src_large TEXT NOT NULL,      -- Primary image URL
    sol INTEGER NOT NULL,
    earth_date DATE,
    rover_id INTEGER,
    camera_id INTEGER,
    site INTEGER,                     -- Location tracking
    drive INTEGER,
    sample_type VARCHAR(50),          -- Image quality
    width INTEGER,                    -- Dimensions
    height INTEGER,
    raw_data JSONB,                   -- Complete NASA response
    created_at TIMESTAMP,

    -- Indexes for performance
    INDEX idx_sol (sol),
    INDEX idx_earth_date (earth_date),
    INDEX idx_site_drive (site, drive),
    INDEX idx_sample_type (sample_type),
    UNIQUE INDEX idx_dedup (sol, camera_id, nasa_id, rover_id)
);

-- Virtual columns from JSONB (PostgreSQL 12+)
ALTER TABLE photos
ADD COLUMN date_taken_mars TEXT
GENERATED ALWAYS AS (raw_data->>'date_taken_mars') STORED;

ALTER TABLE photos
ADD COLUMN mast_az DECIMAL
GENERATED ALWAYS AS ((raw_data->'extended'->>'mast_az')::DECIMAL) STORED;
```

## 7. Storage Impact Analysis

### Current Rails Approach
- **Per photo**: ~200 bytes
- **1 million photos**: ~200 MB
- **Query performance**: Excellent

### Enhanced Approach
- **Per photo**: ~500 bytes
- **1 million photos**: ~500 MB
- **Query performance**: Excellent

### Comprehensive Approach (with JSONB)
- **Per photo**: ~3-5 KB
- **1 million photos**: ~3-5 GB
- **Query performance**: Good with proper indexes

## 8. Decision Framework

### Choose Minimal (Rails) Approach If:
- Building exact NASA API clone
- Storage is expensive
- Simple photo browsing is the goal
- Don't need scientific data

### Choose Enhanced Approach If:
- Want better UX (image dimensions, multiple sizes)
- Need location tracking
- Want Mars time information
- Building educational/scientific tool

### Choose Comprehensive Approach If:
- Building scientific platform
- Need all telemetry data
- Want future flexibility
- Storage cost is not a concern
- May add new features based on raw data

## 9. Implementation Recommendations

### For C#/.NET Implementation

1. **Start with Enhanced Approach**
   - Store NASA ID, dimensions, sample type, location data
   - Provides good balance of functionality and storage

2. **Add JSONB column for raw data**
   - Future-proofs the system
   - Allows extracting new fields without re-scraping

3. **Create materialized views for manifests**
   ```sql
   CREATE MATERIALIZED VIEW photo_manifests AS
   SELECT
     rover_id,
     sol,
     earth_date,
     COUNT(*) as photo_count,
     ARRAY_AGG(DISTINCT camera_id) as camera_ids,
     ARRAY_AGG(DISTINCT sample_type) as sample_types
   FROM photos
   GROUP BY rover_id, sol, earth_date;
   ```

4. **Implement smart caching**
   - Cache processed data (manifests) aggressively
   - Don't cache raw NASA responses (they don't change)

5. **Consider GraphQL**
   - Let clients request exactly the fields they need
   - Especially valuable with comprehensive data storage

## 10. NASA Data Insights

### Data Quality Notes

1. **Perseverance** - Richest metadata (position, telemetry, multiple sizes)
2. **Curiosity** - Good metadata, some nulls in camera data
3. **Opportunity/Spirit** - Limited metadata (scraping HTML, not API)

### URL Patterns Discovered

```
# Perseverance images follow predictable patterns:
Small:    .../image_320.jpg
Medium:   .../image_800.jpg
Large:    .../image_1200.jpg
Full:     .../image.png

# Can derive all sizes from single URL
```

### Missing Data Considerations

- Some Curiosity photos have null camera_position/vector
- Filter names not always available
- Opportunity/Spirit have minimal metadata due to HTML scraping

## Conclusion

The Rails API takes a **minimalist approach**, storing only essential fields for photo discovery (5-10% of available data). This is appropriate for a simple photo browsing API but leaves valuable data on the table.

For your C# implementation, I recommend the **Enhanced Approach**:
- Store core fields as columns for querying
- Add high-value fields like dimensions, NASA ID, location data
- Include JSONB column for complete NASA response
- This provides the best balance of functionality, performance, and future flexibility

The ~10x increase in storage (200 bytes → 2-3 KB per photo) is negligible compared to the value of having complete data for future features and scientific use cases.