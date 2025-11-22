# Panorama Endpoint Optimization

## Context

The `/api/v2/panoramas` endpoint was experiencing severe performance issues, taking 2-3 minutes to respond to simple queries. Investigation revealed the root cause: the endpoint loads all candidate photos into memory (204,583 photos for Curiosity), detects panoramas in-memory, and only then applies pagination.

## Performance Issue Details

**Problem:** `PanoramaService.GetPanoramasAsync()` line 96
```csharp
var photos = await query
    .Include(p => p.Rover)
    .Include(p => p.Camera)
    .OrderBy(p => p.RoverId)
    .ThenBy(p => p.Sol)
    .ThenBy(p => p.Site)
    .ThenBy(p => p.Drive)
    .ThenBy(p => p.SpacecraftClock)
    .ToListAsync(cancellationToken); // ❌ Loads 204,583 photos into memory!
```

**Impact:**
- Query time: 155+ seconds (2 minutes 36 seconds)
- Memory usage: High (loading 200k+ entities with navigation properties)
- Database load: Excessive data transfer

**Production statistics:**
- Total photos in database: 1,988,601
- Photos with panorama telemetry (Curiosity): 204,583
- Photos with panorama telemetry (all rovers): 204,583+

## Implemented Solution (Short-term Fix)

**Approach:** Default sol range limit when no filters specified

**Implementation:**
- Added `DefaultSolRangeLimit = 500` constant
- When no `solMin` or `solMax` is specified, automatically limit to most recent 500 sols
- Users can still query full range with explicit `solMin`/`solMax` parameters
- Added logging to track when default limit is applied

**Code changes:** `PanoramaService.cs` lines 78-92

**Expected improvement:**
- Query time: ~2-3 seconds (vs 155+ seconds)
- Dataset reduced from 204,583 photos to ~few thousand photos
- 98%+ performance improvement for typical queries

**Trade-offs:**
- ✅ Simple implementation (5 lines of code)
- ✅ No schema changes required
- ✅ Backward compatible (users can override)
- ❌ Still processes photos in-memory
- ❌ Doesn't help queries for old sols
- ❌ Doesn't address fundamental architecture issue

## Recommended Long-term Solution

### Pre-computed Panoramas Table

**Approach:** Materialize panorama detection results in a dedicated table

#### Database Schema

```sql
CREATE TABLE panoramas (
    id SERIAL PRIMARY KEY,
    panorama_id VARCHAR(100) UNIQUE NOT NULL, -- e.g., "pano_curiosity_1000_0"
    rover_id INT NOT NULL REFERENCES rovers(id),
    sol INT NOT NULL,
    sequence_index INT NOT NULL, -- Index within sol

    -- Metadata
    camera_id INT NOT NULL REFERENCES cameras(id),
    mars_time_start VARCHAR(20),
    mars_time_end VARCHAR(20),
    total_photos INT NOT NULL,
    coverage_degrees REAL NOT NULL,
    avg_elevation REAL NOT NULL,

    -- Location
    site INT,
    drive INT,
    coordinate_x REAL,
    coordinate_y REAL,
    coordinate_z REAL,

    -- Photo references (array of photo IDs)
    photo_ids INT[] NOT NULL,

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Indexes
    CONSTRAINT unique_rover_sol_sequence UNIQUE(rover_id, sol, sequence_index)
);

CREATE INDEX idx_panoramas_rover_sol ON panoramas(rover_id, sol DESC);
CREATE INDEX idx_panoramas_camera ON panoramas(camera_id);
CREATE INDEX idx_panoramas_location ON panoramas(site, drive) WHERE site IS NOT NULL;
```

#### Background Processing

**One-time population:**
```bash
# Console app or migration script
dotnet run --project MarsVista.PanoramaBuilder
```

**Incremental updates:**
```csharp
// Add to daily scraper after new photos ingested
public class PanoramaBuilder
{
    public async Task ProcessNewSols(int roverId, List<int> newSols)
    {
        foreach (var sol in newSols)
        {
            var photos = await LoadPhotosForSol(roverId, sol);
            var panoramas = DetectPanoramas(photos);
            await SavePanoramas(panoramas);
        }
    }
}
```

**Integration with incremental scraper:**
- After daily scraper completes, run panorama detection for new sols
- Store results in `panoramas` table
- Idempotent: can re-run for same sol to update

#### API Service Changes

```csharp
public async Task<ApiResponse<List<PanoramaResource>>> GetPanoramasAsync(
    string? rovers = null,
    int? solMin = null,
    int? solMax = null,
    int? minPhotos = null,
    int pageNumber = 1,
    int pageSize = 25,
    CancellationToken cancellationToken = default)
{
    // Simple query with database-level pagination
    var query = _context.Panoramas.AsQueryable();

    // Apply filters
    if (!string.IsNullOrWhiteSpace(rovers))
    {
        var roverList = rovers.Split(',').Select(r => r.Trim()).ToList();
        query = query.Where(p => roverList.Any(r =>
            EF.Functions.ILike(p.Rover.Name, r)));
    }

    if (solMin.HasValue)
        query = query.Where(p => p.Sol >= solMin.Value);

    if (solMax.HasValue)
        query = query.Where(p => p.Sol <= solMax.Value);

    if (minPhotos.HasValue)
        query = query.Where(p => p.TotalPhotos >= minPhotos.Value);

    // Count and paginate at database level
    var totalCount = await query.CountAsync(cancellationToken);

    var panoramas = await query
        .Include(p => p.Rover)
        .Include(p => p.Camera)
        .OrderBy(p => p.RoverId)
        .ThenByDescending(p => p.Sol)
        .ThenBy(p => p.SequenceIndex)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    // Convert to resources
    var resources = panoramas.Select(p => ToPanoramaResource(p)).ToList();

    return new ApiResponse<List<PanoramaResource>>(resources)
    {
        Meta = new ResponseMeta { TotalCount = totalCount, ReturnedCount = resources.Count },
        Pagination = new PaginationInfo { Page = pageNumber, PerPage = pageSize, TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
    };
}
```

### Benefits of Pre-computed Approach

**Performance:**
- ✅ Query time: <100ms (database-level pagination)
- ✅ No in-memory processing during queries
- ✅ Scalable to millions of photos
- ✅ Consistent performance regardless of sol range

**Architecture:**
- ✅ Separation of concerns (detection vs querying)
- ✅ Enables complex panorama queries (location, coverage, time filters)
- ✅ Can add additional computed fields without query impact
- ✅ Supports caching and CDN strategies

**Features enabled:**
- Advanced filtering (location-based, coverage range, time of day)
- Panorama quality scoring
- Cross-sol panorama sequences
- Analytics and statistics
- Download endpoints (already referenced in links)

### Implementation Plan

**Phase 1: Schema and Migration**
- Create `panoramas` table with EF Core migration
- Add indexes for common query patterns
- Test migration on staging database

**Phase 2: Panorama Builder**
- Create console app or background service
- Implement panorama detection (reuse existing logic)
- Add progress tracking and logging
- Run one-time population for existing photos

**Phase 3: API Service Migration**
- Create new `PanoramaQueryService` reading from table
- Update controllers to use new service
- Add feature flag to switch between implementations
- Test thoroughly with production data

**Phase 4: Integration with Scraper**
- Add panorama detection step to incremental scraper
- Ensure idempotency (can re-run for same sol)
- Add error handling and retry logic
- Monitor for detection quality

**Phase 5: Cleanup**
- Remove old in-memory detection code
- Remove feature flag
- Update documentation
- Remove sol range limit workaround

### Effort Estimate

- **Phase 1:** 2-3 hours (schema design, migration, testing)
- **Phase 2:** 4-6 hours (builder implementation, population script)
- **Phase 3:** 3-4 hours (service refactor, testing)
- **Phase 4:** 2-3 hours (scraper integration)
- **Phase 5:** 1-2 hours (cleanup, documentation)

**Total:** ~12-18 hours of development + testing

### Storage Impact

**Estimated storage per panorama:**
- Metadata: ~200 bytes
- Photo ID array: ~20 bytes per photo (avg 5 photos = 100 bytes)
- Total: ~300 bytes per panorama

**Expected panorama count:**
- Curiosity (4,683 sols): ~10,000-15,000 panoramas
- Perseverance (1,683 sols): ~5,000-7,000 panoramas
- Future rovers: ~5,000 per 1,000 sols

**Total storage:** ~20,000 panoramas × 300 bytes = ~6 MB
- Negligible compared to photos table (multiple GB)
- Indexes: ~2-3 MB
- Total impact: <10 MB

## Decision

**Current:** Implemented short-term fix (sol range limit) ✅
**Next:** Implement long-term solution (pre-computed table) when time permits

The short-term fix provides immediate relief (98%+ performance improvement) while we plan and implement the proper architectural solution.

## References

- Performance issue discovered: 2025-11-22
- Short-term fix implemented: 2025-11-22
- Code: `src/MarsVista.Api/Services/V2/PanoramaService.cs`
- Related: Story 012 Phase 1 (API v2 panoramas endpoint)
