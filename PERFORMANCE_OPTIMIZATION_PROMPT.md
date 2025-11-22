# Performance Optimization Task - Mars Vista API

## Context

I've just completed a comprehensive benchmark test of the Mars Vista API production instance. The API is performing well overall (96% pass rate, median response time 0.37s), but we have identified critical performance bottlenecks that need systematic optimization.

**Current Status:**
- Production API: https://api.marsvista.dev
- Database: Railway PostgreSQL with 1,988,601 photos across 4 rovers
- Overall Grade: B+ (good but needs optimization)

## Critical Performance Issues Identified

### 1. Panorama Queries (CRITICAL - 95 seconds)
**Endpoint:** `GET /api/v2/panoramas?rovers=curiosity&sol_min=1000`
- Current: 94.85 seconds
- Target: < 2 seconds
- **File:** `src/MarsVista.Api/Controllers/V2/PanoramasController.cs`
- **Service:** Check `IPanoramaService` implementation

### 2. Landing Day Photo Queries (CRITICAL - 44 seconds)
**Endpoint:** `GET /api/v1/rovers/curiosity/photos?earth_date=2012-08-06`
- Current: 44.03 seconds
- Target: < 1 second
- High-traffic endpoint (users love landing day photos)
- **File:** `src/MarsVista.Api/Controllers/V1/RoversController.cs` or photos endpoints

### 3. Sol Max Queries (CRITICAL - 36 seconds)
**Endpoint:** `GET /api/v2/photos?rovers=curiosity&sol_max=100`
- Current: 35.61 seconds
- Target: < 2 seconds
- Early mission queries are very slow

### 4. Complex Combined Filters (HIGH - 32 seconds)
**Endpoint:** `GET /api/v2/photos?mars_time_min=M14:00:00&mars_time_max=M16:00:00&mast_elevation_min=0&mast_elevation_max=45&cameras=MAST`
- Current: 31.80 seconds
- Target: < 5 seconds
- Multiple filter combination performance

### 5. Image Quality Filters (MEDIUM - 3-5 seconds)
Multiple endpoints with `min_width`, `min_height`, `sample_type` filters
- Current: 3-5 seconds
- Target: < 1 second

## Performance Metrics (Current Baseline)

```
P50 (median):  0.37s  ✅ Good
P90:           2.04s  ✅ Acceptable
P95:           3.44s  ⚠️  Slow
P99:          35.61s  ❌ Critical
Max:          94.85s  ❌ Critical
Average:       2.20s  ⚠️  Fair (skewed by outliers)
```

## Database Schema Overview

**Key Tables:**
- `photos` - 1.99M rows with indexed columns + JSONB `raw_data`
- `rovers` - 4 rows (Curiosity, Perseverance, Opportunity, Spirit)
- `cameras` - 39 cameras across rovers

**Existing Indexes:**
Check `.claude/CSHARP_IMPLEMENTATION_GUIDE_V2.md` for documented indexes, but likely need:
- Composite index on (rover_id, earth_date)
- Composite index on (rover_id, sol)
- Indexes on Mars time columns (if they exist)
- Indexes on location columns (site, drive)
- Indexes on image dimensions (width, height)
- JSONB GIN indexes for nested queries

## Tasks to Complete

### Phase 1: Investigation (Do First)

1. **Review Database Schema and Existing Indexes**
   - Connect to Railway database
   - Run `\d+ photos` to see table structure
   - Run query to list all existing indexes
   - Identify missing indexes

2. **Analyze Slow Query Patterns**
   - Review EF Core query generation for slow endpoints
   - Check if N+1 queries are happening
   - Look for missing `.Include()` statements
   - Identify full table scans

3. **Profile Specific Slow Queries**
   - Use PostgreSQL `EXPLAIN ANALYZE` on slowest queries
   - Check query plans for sequential scans
   - Identify which filters are causing slowdowns

### Phase 2: Database Optimization

4. **Add Missing Indexes**
   - Create composite indexes for common query patterns
   - Add indexes for Mars time filtering
   - Add indexes for location-based queries
   - Add indexes for image dimension filtering
   - Consider partial indexes for specific use cases

5. **Optimize JSONB Queries**
   - Check if panorama detection queries JSONB efficiently
   - Add GIN indexes if needed
   - Consider extracting frequently-queried JSONB fields to columns

6. **Review Entity Framework Queries**
   - Ensure proper use of `.AsNoTracking()` for read-only queries
   - Check for projection optimization
   - Verify Include/ThenInclude efficiency

### Phase 3: Code Optimization

7. **Optimize Panorama Service**
   - Review panorama detection algorithm
   - Consider pre-computing panorama metadata
   - Add caching for panorama queries
   - File: Look for `PanoramaService.cs` or similar

8. **Optimize Photo Query Service**
   - Review complex filter combinations
   - Ensure efficient WHERE clause generation
   - Consider query result caching for common queries

9. **Add Response Caching**
   - Implement HTTP caching (ETags) for inactive rovers
   - Add in-memory caching for frequently accessed data
   - Cache landing day queries (popular endpoint)

### Phase 4: Verification

10. **Re-run Benchmarks**
    - Use existing script: `./benchmark-production-api.sh`
    - Compare P95 and P99 metrics
    - Verify all critical endpoints < 5s

11. **Document Changes**
    - Update technical decisions
    - Document new indexes created
    - Record performance improvements

## Reference Files

**Essential Documentation:**
- `PRODUCTION_API_BENCHMARK_REPORT.md` - Full benchmark results with all 133 tests
- `benchmark-results/benchmark_20251122_090547.json` - Raw test data
- `PRODUCTION_DB_SNAPSHOT.md` - Current database state
- `.claude/CSHARP_IMPLEMENTATION_GUIDE_V2.md` - Architecture guide
- `docs/DATABASE_ACCESS.md` - Database credentials and queries

**Code Locations to Review:**
- `src/MarsVista.Api/Controllers/V2/PhotosController.cs` - Main v2 photos endpoint
- `src/MarsVista.Api/Controllers/V2/PanoramasController.cs` - Slow panorama queries
- `src/MarsVista.Api/Services/V2/` - Service layer implementations
- `src/MarsVista.Api/Data/MarsVistaDbContext.cs` - EF Core configuration

## Database Connection

**Railway Production Database:**
```
Host: maglev.proxy.rlwy.net
Port: 38340
User: postgres
Password: OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh
Database: railway
```

Connect with:
```bash
PGPASSWORD=OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh psql -h maglev.proxy.rlwy.net -U postgres -p 38340 -d railway
```

## Success Criteria

After optimization, we should achieve:

1. **P99 Response Time < 5 seconds** (currently 35.61s)
2. **P95 Response Time < 2 seconds** (currently 3.44s)
3. **All "critical" endpoints < 2 seconds:**
   - Panorama queries
   - Landing day queries
   - Sol max queries
4. **Complex filters < 5 seconds** (currently 31.8s)
5. **Image quality filters < 1 second** (currently 3-5s)
6. **Zero performance regressions** on fast endpoints

## Approach

Please approach this systematically and professionally:

1. **Start with investigation** - Don't guess, measure and analyze
2. **Use EXPLAIN ANALYZE** - Understand what PostgreSQL is actually doing
3. **Add indexes strategically** - Target the most impactful queries first
4. **Test incrementally** - Add one optimization, test, measure improvement
5. **Document everything** - Record what you tried and the results
6. **Create atomic commits** - One optimization per commit with clear message
7. **Re-benchmark** - Use the existing benchmark script to verify improvements

## Notes

- This is a **production database** - be careful with migrations
- Test index creation on local database first if possible
- Some queries may need query plan hints or restructuring
- Consider adding database query logging to identify all slow queries in production
- The panorama endpoint is the #1 priority (95 seconds is unacceptable)

## Expected Outcome

After this optimization work:
- Production API should achieve an **A grade** (< 1s median, < 5s P99)
- All critical user-facing endpoints respond in < 2 seconds
- Complex scientific queries respond in < 5 seconds
- API can handle higher traffic loads efficiently
- Database utilization optimized with proper indexes

## Additional Context

- The API uses **hybrid storage**: indexed columns + JSONB for complete NASA data
- **Field selection** (sparse fieldsets) is implemented but may need optimization
- **Pagination** works well but may benefit from cursor-based approach for large datasets
- **Mars time filtering** is a new feature and may lack proper indexes
- **Location-based queries** use site/drive numbers and radius calculations

---

**Ready to begin systematic performance optimization!**

Please investigate the issues in priority order (panoramas → landing day → sol max → combined filters), create a plan, implement optimizations, and verify improvements with benchmarks.
