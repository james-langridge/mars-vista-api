# Story 016: Query Performance Optimization

## Status
Not Started

## Context

The manifest endpoint performance issue revealed that EF Core's `GroupBy` operation was processing 675K photos in-memory, causing 1.5-1.9s response times. After replacing it with raw SQL, performance improved 3x (1800ms → 600ms).

This raises the question: **are there other queries with similar performance issues?**

## Goal

Systematically audit all API endpoints for query performance issues and optimize any bottlenecks found.

## Success Criteria

1. All public query endpoints respond in <500ms for typical datasets
2. Database queries use indexes efficiently (no sequential scans on large tables)
3. Complex aggregations use raw SQL instead of in-memory processing
4. Query execution plans documented for critical endpoints

## Implementation Steps

### 1. Audit Current Endpoints

**Identify all query endpoints:**
- `GET /api/v1/rovers` - Get all rovers with stats
- `GET /api/v1/rovers/{name}` - Get specific rover with stats
- `GET /api/v1/rovers/{name}/photos` - Query photos (with filters)
- `GET /api/v1/rovers/{name}/latest` - Get latest photos
- `GET /api/v1/photos/{id}` - Get photo by ID
- `GET /api/v1/manifests/{name}` - Get manifest (OPTIMIZED)
- `GET /api/scraper/{rover}/progress` - Scraper progress

**For each endpoint, measure:**
- Response time with realistic dataset size (local database with 675K photos)
- Database query execution time (check EF Core logs)
- Number of database round trips (N+1 query problem?)

### 2. Analyze Query Patterns

**Check for common performance issues:**

**Problem: N+1 Queries**
- Loading collections without `Include()` causes multiple queries
- Example: Loading rover cameras in a loop

**Problem: In-Memory Operations**
- EF Core operations that can't translate to SQL execute in-memory
- Look for: `GroupBy`, complex `Select`, `OrderBy` with computed properties
- Check: `.ToList()` followed by LINQ operations

**Problem: Missing Indexes**
- Sequential scans on filtered columns (rover_id, sol, earth_date, camera_id)
- Check current indexes: `SELECT * FROM pg_indexes WHERE tablename IN ('photos', 'rovers', 'cameras');`

**Problem: Over-Fetching**
- Loading entire `raw_data` JSONB column when not needed
- Loading more rows than necessary

### 3. Create Performance Test Suite

Create a script to benchmark all endpoints:

```bash
#!/bin/bash
# File: ./test-query-performance.sh

echo "=== Query Performance Test Suite ==="
echo "Database: $(psql ... -t -c 'SELECT COUNT(*) FROM photos') photos"
echo ""

endpoints=(
  "GET /api/v1/rovers"
  "GET /api/v1/rovers/curiosity"
  "GET /api/v1/rovers/curiosity/photos?sol=1000"
  "GET /api/v1/rovers/curiosity/photos?earth_date=2015-01-01"
  "GET /api/v1/rovers/curiosity/photos?sol=1000&camera=MAST"
  "GET /api/v1/rovers/curiosity/photos?page=1&per_page=100"
  "GET /api/v1/rovers/curiosity/latest"
  "GET /api/v1/manifests/curiosity"
  "GET /api/scraper/curiosity/progress"
)

for endpoint in "${endpoints[@]}"; do
  echo "Testing: $endpoint"
  time curl -s "http://localhost:5127${endpoint#GET }" > /dev/null
  echo ""
done
```

### 4. Identify Problem Queries

**Method 1: Enable EF Core Query Logging**

Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Method 2: Check PostgreSQL Slow Queries**

```sql
-- Enable slow query logging (if not already enabled)
ALTER DATABASE marsvista_dev SET log_min_duration_statement = 100;

-- View slow queries
SELECT * FROM pg_stat_statements
WHERE mean_exec_time > 100
ORDER BY mean_exec_time DESC;
```

**Method 3: Use EXPLAIN ANALYZE**

For each query found in EF Core logs:
```sql
EXPLAIN ANALYZE SELECT ...;
```

Look for:
- Seq Scan (sequential scan - bad for large tables)
- High execution time
- High row count estimates

### 5. Common Optimizations to Apply

**Optimization 1: Add Missing Indexes**

Check if these indexes exist and add if missing:
```sql
-- Photo queries by rover and sol
CREATE INDEX IF NOT EXISTS idx_photos_rover_sol ON photos(rover_id, sol);

-- Photo queries by rover and earth_date
CREATE INDEX IF NOT EXISTS idx_photos_rover_earth_date ON photos(rover_id, earth_date);

-- Photo queries by rover and camera
CREATE INDEX IF NOT EXISTS idx_photos_rover_camera ON photos(rover_id, camera_id);

-- Combined index for common query patterns
CREATE INDEX IF NOT EXISTS idx_photos_rover_sol_camera ON photos(rover_id, sol, camera_id);
```

**Optimization 2: Fix N+1 Queries**

Before:
```csharp
var rovers = await _context.Rovers.ToListAsync();
foreach (var rover in rovers)
{
    var stats = await GetRoverStatsAsync(rover.Id); // N+1!
}
```

After:
```csharp
var rovers = await _context.Rovers
    .Include(r => r.Cameras)
    .ToListAsync();

// Batch stats query
var roverIds = rovers.Select(r => r.Id).ToList();
var stats = await GetBatchRoverStatsAsync(roverIds);
```

**Optimization 3: Use Projections to Reduce Data Transfer**

Before:
```csharp
var photos = await _context.Photos
    .Include(p => p.Camera)
    .Include(p => p.Rover)
    .ToListAsync(); // Loads ALL columns including raw_data JSONB
```

After:
```csharp
var photos = await _context.Photos
    .Select(p => new PhotoDto
    {
        Id = p.Id,
        Sol = p.Sol,
        // Only select needed columns, skip raw_data
    })
    .ToListAsync();
```

**Optimization 4: Replace Complex LINQ with Raw SQL**

Candidates for raw SQL (similar to manifest fix):
- Any query with `GroupBy`
- Queries with complex aggregations
- Queries with window functions needs

### 6. Specific Endpoints to Check

**High Priority:**

**`GET /api/v1/rovers` - GetAllRoversAsync()**
- Current: Loops through rovers calling `GetRoverStatsAsync` for each (potential N+1)
- Optimization: Batch stats query or use single query with LEFT JOIN

**`GET /api/v1/rovers/{name}/photos` - Photo Query**
- Current: Uses EF Core with filters
- Check: Are indexes being used? Any sequential scans?
- Optimization: Ensure proper indexes, consider pagination limits

**`GET /api/scraper/{rover}/progress` - ScraperProgressAsync()**
- Current: Multiple separate queries (COUNT, MAX, etc.)
- Optimization: Combine into single query with aggregations

**Medium Priority:**

**`GET /api/v1/rovers/curiosity/latest`**
- Check: How is "latest sol" determined? Is it efficient?

**Low Priority:**
- `GET /api/v1/photos/{id}` - Single row lookup (should be fast)

### 7. Document Optimizations

For each optimization made, document:

**File: `.claude/decisions/019-query-performance-optimizations.md`**

```markdown
# Query Performance Optimizations

## Problem: [Endpoint Name]
**Query Time Before**: X ms
**Database Operation**: [Description of what was slow]
**Root Cause**: [Why it was slow]

## Solution
**Query Time After**: Y ms
**Improvement**: Zx faster
**Approach**: [What was changed]

## SQL Changes
[Before/after SQL if applicable]

## Indexes Added
[Any new indexes created]
```

### 8. Create Performance Monitoring

**Add performance middleware** (optional, for ongoing monitoring):

```csharp
public class QueryPerformanceMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Slow query: {Method} {Path} took {Duration}ms",
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds);
        }
    }
}
```

## Testing

1. **Benchmark before optimization**: Run performance test suite, record baseline
2. **Apply optimizations**: Make changes one endpoint at a time
3. **Benchmark after optimization**: Verify improvement
4. **Test correctness**: Ensure results are identical to before
5. **Test with production data**: If possible, test against Railway database

## Acceptance Criteria

- [ ] All public query endpoints respond in <500ms (with 675K photos locally)
- [ ] No N+1 query patterns in codebase
- [ ] Database indexes added for common query patterns
- [ ] Complex aggregations use raw SQL instead of in-memory processing
- [ ] Performance test suite created and documented
- [ ] All optimizations documented with before/after metrics
- [ ] Code compiles and all existing tests pass
- [ ] Deployed to production and verified improvement

## Expected Outcomes

**Conservative estimate:**
- 2-3 endpoints will have optimization opportunities
- Average improvement: 2-3x faster
- No breaking changes to API contracts

**Best case scenario:**
- All endpoints <200ms
- Database CPU usage reduced by 30-50%
- Improved user experience for high-traffic endpoints

## Notes

- This is a performance optimization story, not a new feature
- Changes should be transparent to API consumers (no breaking changes)
- Focus on "hot path" queries (most frequently called endpoints)
- Use the manifest endpoint fix as a reference pattern

## Related Stories

- Story 007: Public query API (initial implementation)
- Story 015: Manifest query optimization (template for this work)

## Resources

- EF Core Performance: https://learn.microsoft.com/en-us/ef/core/performance/
- PostgreSQL EXPLAIN: https://www.postgresql.org/docs/current/sql-explain.html
- Indexing Best Practices: https://www.postgresql.org/docs/current/indexes.html
