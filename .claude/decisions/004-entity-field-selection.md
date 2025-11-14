# Decision 004: Entity Field Selection Strategy

**Status:** Active
**Date:** 2025-11-13
**Context:** Story 004 - Define Core Domain Entities

## Context

NASA's Mars rover APIs provide 30-50 fields per photo depending on the rover (Perseverance has the most fields, Spirit/Opportunity the least). The Rails Mars Photo API only stores ~5-10% of this data in columns.

We need to decide:
1. Which fields should be promoted to database columns?
2. Should we store complete NASA responses?
3. How do we balance query performance with data completeness?

## Requirements

- **Query performance:** Common queries (by rover, camera, sol, date) must be fast
- **Data completeness:** Preserve all NASA data for future features
- **Flexibility:** Enable advanced queries without schema changes
- **Maintainability:** Schema shouldn't change every time NASA adds fields

## Alternatives

### Alternative 1: Minimal Columns Only (Rails API approach)

**Implementation:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public int Sol { get; set; }
    public DateTime EarthDate { get; set; }
    public string ImgSrc { get; set; }
    public int RoverId { get; set; }
    public int CameraId { get; set; }
}
```

**Pros:**
- Simple schema
- Fast queries on all fields (all indexed)
- Small storage footprint

**Cons:**
- **Loses 90% of NASA data** (telemetry, location, metadata)
- **No future features** (panoramas, stereo pairs, location search, 3D reconstruction)
- **Limited API value** - users could just call NASA directly
- Schema changes required for any new queryable field

**Example limitations:**
- Can't answer: "Show me photos with attitude data"
- Can't answer: "Find photos taken at this location"
- Can't answer: "Detect panorama sequences"

### Alternative 2: All Fields as Columns

**Implementation:**
```csharp
public class Photo
{
    // 30+ column properties
    public int Id { get; set; }
    public int Sol { get; set; }
    public DateTime EarthDate { get; set; }
    public string ImgSrc { get; set; }
    // ... 25 more columns
    public string? ExtendedMetadataField23 { get; set; }
}
```

**Pros:**
- All fields indexed and queryable
- Fast queries on any field
- Complete data preservation

**Cons:**
- **Wide tables** (30+ columns) hurt query performance
- **Memory overhead** - reading one photo loads all columns
- **Index bloat** - too many indexes slow down inserts
- **Schema brittleness** - NASA adds field = migration required
- **Different rovers have different fields** - nullable columns everywhere

**Performance impact:**
- Reading 30 columns vs 10 columns = ~3x slower on simple queries
- 15+ indexes on one table = slow inserts, high storage cost

### Alternative 3: Pure JSONB Storage

**Implementation:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public JsonDocument Data { get; set; }  // Everything in JSON
}
```

**Pros:**
- **Ultimate flexibility** - store any structure
- **No schema changes** when NASA adds fields
- **Minimal code** - just serialize/deserialize JSON

**Cons:**
- **Slow queries** - can't use indexes effectively
- **JSONB queries are 10-100x slower** than column queries
- **Type safety lost** - everything is dynamic JSON
- **Harder to enforce constraints** (nullability, lengths)

**Performance comparison:**
```sql
-- Column query (fast: ~1ms)
SELECT * FROM photos WHERE rover_id = 1 AND sol = 1000;

-- JSONB query (slow: ~50-500ms without GIN index)
SELECT * FROM photos WHERE data->>'rover_id' = '1' AND data->>'sol' = '1000';
```

Even with GIN index, JSONB queries are 10x slower.

### Alternative 4: Hybrid Columns + JSONB (RECOMMENDED)

**Implementation:**
```csharp
public class Photo
{
    // Core queryable fields as columns (80% of queries)
    public int Sol { get; set; }
    public DateTime EarthDate { get; set; }
    public string ImgSrcFull { get; set; }
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Advanced queryable fields as columns (15% of queries)
    public int? Site { get; set; }
    public int? Drive { get; set; }
    public float? MastAz { get; set; }
    public float? MastEl { get; set; }

    // Complete NASA response in JSONB (5% of queries)
    public JsonDocument RawData { get; set; }
}
```

**Pros:**
- **Fast common queries** - use indexed columns
- **Complete data preservation** - everything in JSONB
- **Flexible advanced queries** - use JSONB for rare fields
- **No data loss** - 100% of NASA data available
- **Future-proof** - new NASA fields automatically stored

**Cons:**
- Data duplication (column fields also in JSONB) - adds ~20% storage
- Slightly more complex query logic (column vs JSONB)

**Performance:**
```sql
-- Fast query (1ms) - uses column indexes
SELECT * FROM photos WHERE rover_id = 1 AND sol = 1000;

-- Advanced query (10-20ms) - uses GIN index on JSONB
SELECT * FROM photos
WHERE rover_id = 1
  AND raw_data->>'attitude' IS NOT NULL;

-- Complex hybrid query (20-50ms)
SELECT * FROM photos
WHERE rover_id = 1
  AND sol BETWEEN 1000 AND 2000
  AND raw_data->>'camera_model_type' = 'ECAM';
```

## Decision

**Use Hybrid Columns + JSONB approach (Alternative 4)**

### Column Selection Criteria

Promote field to column if:
1. **Used in 80%+ of queries** (rover, camera, sol, date, img_src)
2. **Needs fast sorting** (date, sol)
3. **Needs complex filtering** (date ranges, numeric comparisons)
4. **Enables core features** (image URLs for display)

Keep in JSONB only if:
1. **Rarely queried** (spacecraft_clock, quaternions)
2. **Advanced features only** (stereo pair metadata)
3. **Rover-specific** (not all rovers have this field)

### Selected Columns

**Core (used in 90%+ of queries):**
- `nasa_id` - Unique identifier
- `sol` - Sol number (most common filter)
- `earth_date` - Calendar date (second most common filter)
- `date_taken_utc` - Precise timestamp
- `img_src_*` - Image URLs (small/medium/large/full)
- `rover_id` - Rover filter
- `camera_id` - Camera filter

**Advanced (enables future features):**
- `site`, `drive` - Location-based search
- `mast_az`, `mast_el` - Panorama detection
- `xyz` - Rover position for proximity search
- `width`, `height` - Image dimensions for layout
- `sample_type` - Filter full-res vs thumbnails

**Metadata (useful but not query-critical):**
- `title`, `caption`, `credit` - Display information
- `date_received` - Data pipeline tracking

**Everything in JSONB:**
- All of the above fields PLUS:
- Camera vectors, quaternions, spacecraft clock
- Extended metadata NASA might add in the future
- Rover-specific telemetry

## Trade-offs

**Accepted:**
- ~20% storage overhead (duplication of column fields in JSONB)
- Slightly more complex queries (need to know column vs JSONB)

**Gained:**
- **100% data preservation** vs Rails API's 10%
- **Fast common queries** (1-10ms) via column indexes
- **Flexible advanced queries** (10-100ms) via JSONB GIN indexes
- **Future-proof** - no schema changes when NASA adds fields
- **Enables advanced features:**
  - Panorama detection (mast angles)
  - Location-based search (site/drive/xyz)
  - 3D path reconstruction (camera position)
  - Stereo pair matching (camera vectors)
  - Time-series analytics (spacecraft clock)

## Implementation

### Entity Definition

```csharp
public class Photo
{
    // Columns for fast queries
    public int Id { get; set; }
    public string NasaId { get; set; }
    public int Sol { get; set; }
    public DateTime? EarthDate { get; set; }
    public DateTime DateTakenUtc { get; set; }
    public string ImgSrcFull { get; set; }
    public int? Site { get; set; }
    public int? Drive { get; set; }
    public float? MastAz { get; set; }
    public float? MastEl { get; set; }
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Complete NASA data in JSONB
    public JsonDocument RawData { get; set; }
}
```

### EF Core Configuration

```csharp
modelBuilder.Entity<Photo>(entity =>
{
    // Column indexes
    entity.HasIndex(e => e.NasaId).IsUnique();
    entity.HasIndex(e => new { e.RoverId, e.Sol });
    entity.HasIndex(e => new { e.Site, e.Drive });

    // JSONB column
    entity.Property(e => e.RawData)
        .HasColumnType("jsonb");
});
```

### Query Examples

```csharp
// Fast column query (1-5ms)
var photos = await context.Photos
    .Where(p => p.RoverId == 1 && p.Sol == 1000)
    .ToListAsync();

// Hybrid query: column filter + JSONB projection (10-20ms)
var photosWithAttitude = await context.Photos
    .Where(p => p.RoverId == 1 && p.Sol == 1000)
    .Where(p => EF.Functions.JsonContains(
        p.RawData,
        JsonDocument.Parse(@"{""attitude"": """"}").RootElement
    ))
    .ToListAsync();

// Pure JSONB query (20-50ms with GIN index)
var photosWithCamera = await context.Photos
    .FromSqlRaw(@"
        SELECT * FROM photos
        WHERE rover_id = 1
          AND raw_data->>'camera_model_type' = 'ECAM'
    ")
    .ToListAsync();
```

## Validation Criteria

Success metrics:
- Common queries (rover, sol, camera) respond in < 10ms
- 100% of NASA data accessible via JSONB
- Zero data loss during import
- Schema unchanged when NASA adds new fields

Performance benchmarks:
- Column-only query: 1-5ms
- Hybrid query: 10-20ms
- Pure JSONB query: 20-50ms (with GIN index)

## References

- [PostgreSQL JSONB Performance](https://www.postgresql.org/docs/current/datatype-json.html)
- [Npgsql JSON Mapping](https://www.npgsql.org/efcore/mapping/json.html)
- [JSONB Indexing Strategies](https://www.postgresql.org/docs/current/datatype-json.html#JSON-INDEXING)
- [NASA Data Analysis (.claude/NASA_DATA_ANALYSIS.md)](../NASA_DATA_ANALYSIS.md)
- [Architecture Analysis (.claude/ARCHITECTURE_ANALYSIS.md)](../ARCHITECTURE_ANALYSIS.md)

## Related Decisions

- **Decision 002:** PostgreSQL with JSONB support (foundation for this decision)
- **Decision 003:** Entity Framework Core (provides JSONB mapping)
- **Future:** GIN indexing strategy for JSONB queries (Story 006)

## Notes

### Storage Overhead Calculation

Example photo record:
- Column data: ~500 bytes (30 fields)
- JSONB data: ~2KB (complete NASA response)
- Duplication penalty: 500 bytes / 2500 bytes = 20%

For 1 million photos:
- Pure columns: 500MB
- Pure JSONB: 2GB
- Hybrid: 2.5GB (20% overhead)

**Trade-off:** 500MB extra storage to preserve 100% of NASA data and enable advanced features.

### Why Not Normalize Further?

Could we normalize more? Yes, but:
- Creating tables for every NASA structure is over-engineering
- JSONB is designed for semi-structured data like this
- Flexibility > normalization for metadata fields

Good candidates for normalization:
- ✅ Rovers (static reference data)
- ✅ Cameras (static reference data)
- ❌ Photo telemetry (varies by rover, changes over time)
- ❌ Extended metadata (NASA adds fields regularly)

### Performance Testing

Before going to production, benchmark:
1. Common query patterns with realistic data (1M+ photos)
2. JSONB query performance with and without GIN indexes
3. Insert performance with multiple indexes
4. Storage requirements with production data volume

Expected production data:
- Curiosity: 700K photos
- Perseverance: 200K photos (growing)
- Spirit/Opportunity: 300K photos
- Total: ~1.2M photos × 2.5KB = 3GB database
