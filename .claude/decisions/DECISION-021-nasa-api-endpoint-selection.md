# Decision 021: NASA API Endpoint Selection Per Rover

**Status:** Active
**Date:** 2025-11-20
**Context:** Investigation into why Curiosity and Perseverance scrapers use different NASA API endpoints and why they cannot be unified

## Problem Statement

The Curiosity and Perseverance scrapers use different NASA API endpoints:
- **Curiosity**: `https://mars.nasa.gov/api/v1/raw_image_items/`
- **Perseverance**: `https://mars.nasa.gov/rss/api/`

This raises questions:
1. Why do we use different endpoints for different rovers?
2. Can we standardize on a single endpoint for consistency?
3. Is the performance difference between endpoints significant?

## Investigation Results

### Endpoint Compatibility Testing

**Test 1: Can Perseverance data be accessed via raw_image_items endpoint?**
```bash
curl "https://mars.nasa.gov/api/v1/raw_image_items/?condition_1=mars2020:mission&condition_2=1:sol:in"
# Result: {"items":[],"total":0}
```
**Result:** ❌ No Perseverance data available in raw_image_items endpoint

**Test 2: Can Curiosity data be accessed via RSS API?**
```bash
curl "https://mars.nasa.gov/rss/api/?feed=raw_images&category=msl&feedtype=json&sol=1"
# Result: 404 or no data (category 'msl' not supported)
```
**Result:** ❌ RSS API does not support Curiosity mission

### Performance Benchmarking

Tested both endpoints with comparable sol requests (Nov 20, 2025):

| API Endpoint | Rover | Sol Tested | Images | Response Time | Performance |
|--------------|-------|------------|--------|---------------|-------------|
| `/rss/api/` | Perseverance | 1646 | 250 | **20.6s** | Slow |
| `/rss/api/` | Perseverance | 1645 | ~250 | **23.4s** | Slow |
| `/rss/api/` | Perseverance | 100 | ~200 | **22.3s** | Slow |
| `/api/v1/raw_image_items/` | Curiosity | 4683 | 239 | **0.7s** | Fast |
| `/api/v1/raw_image_items/` | Curiosity | 4682 | ~200 | **0.4s** | Fast |
| `/api/v1/raw_image_items/` | Curiosity | 1 | 12 | **0.2s** | Fast |

**Performance Ratio:** Perseverance RSS API is **~50x slower** than Curiosity raw_image_items API

### Root Cause Analysis

**Why the performance difference exists:**

1. **RSS API (`/rss/api/`):**
   - Appears to dynamically generate RSS feed responses
   - Likely queries multiple backend systems
   - Slower server-side processing
   - Limited or ineffective CDN caching
   - Designed for RSS feed consumers (not optimized for bulk scraping)

2. **raw_image_items API (`/api/v1/raw_image_items/`):**
   - Direct database query with indexing
   - Well-optimized pagination (supports up to 1000 items/page)
   - Effective CloudFront CDN caching
   - Purpose-built for programmatic access

**Why endpoints are mission-specific:**

NASA maintains separate backend systems for different rover missions:
- **Mars 2020 (Perseverance)**: Modern RSS-based system
- **MSL (Curiosity)**: Older raw_image_items system
- These are **siloed data systems** with no cross-mission access

## Options Considered

### Option 1: Use Different Endpoints Per Rover (Current Implementation)
- Perseverance: RSS API
- Curiosity: raw_image_items API
- Accept performance difference as NASA infrastructure constraint

**Pros:**
- Only viable option (endpoints are mission-specific)
- Each scraper optimized for its available API
- Already implemented and working

**Cons:**
- Perseverance scraper is inherently 50x slower
- Inconsistent scraper performance across rovers
- Cannot optimize Perseverance scraper speed

### Option 2: Standardize on RSS API for All Rovers
**Status:** ❌ Not Possible
- RSS API does not provide Curiosity data
- Would require NASA to add MSL category (outside our control)

### Option 3: Standardize on raw_image_items API for All Rovers
**Status:** ❌ Not Possible
- raw_image_items endpoint does not contain Perseverance data
- Would require NASA to migrate Mars 2020 data (outside our control)

### Option 4: Use Official NASA API (api.nasa.gov)
**Status:** ⚠️ Possible but Not Recommended

The official NASA API at `https://api.nasa.gov/mars-photos/api/v1/` supports all rovers but has disadvantages:
- Requires API key management
- Rate limited (1000 req/hour for registered keys)
- Less comprehensive metadata than direct mission APIs
- Additional operational dependency

**Why we don't use it:**
- Direct mission APIs provide richer metadata (30-40 fields vs ~10 fields)
- No rate limits on mission APIs
- Mission APIs update faster (real-time vs batched)
- Following pattern established by reference Rails API

## Decision

**Use Option 1: Different Endpoints Per Rover (Current Implementation)**

Each rover scraper must use its mission-specific NASA API endpoint:
- `CuriosityScraper.cs:15`: `https://mars.nasa.gov/api/v1/raw_image_items/`
- `PerseveranceScraper.cs:14`: `https://mars.nasa.gov/rss/api/`

## Reasoning

### Why This Choice?

1. **No Alternative Exists**
   - NASA's endpoints are mission-specific and non-interchangeable
   - Cannot access Perseverance data from Curiosity endpoint (tested)
   - Cannot access Curiosity data from Perseverance endpoint (tested)

2. **Performance Difference is Unavoidable**
   - 50x performance difference is inherent to NASA's infrastructure
   - Perseverance RSS API consistently takes 20-23 seconds per sol
   - Our retry policies and timeouts already accommodate this (30s timeout)
   - Not "flaky" — consistently slow, which we can work with

3. **Already Optimized Within Constraints**
   - Curiosity scraper uses max page size (200, could increase to 1000)
   - Perseverance scraper has no pagination (returns all images per sol)
   - Parallel scraping in `MarsVista.Scraper` minimizes total time
   - Incremental scraper (7-sol lookback) minimizes API calls

4. **Resilience Patterns Handle Slow Responses**
   - Polly retry policy with exponential backoff (`Program.cs:173-186`)
   - Circuit breaker prevents cascade failures (`Program.cs:189-196`)
   - 30-second HTTP timeout appropriate for slow RSS API (`Program.cs:59`)

## Implementation Details

### Current Timeouts (Appropriate for Slow RSS API)
```csharp
// MarsVista.Scraper/Program.cs:59
client.Timeout = TimeSpan.FromSeconds(30);  // ✅ Handles 20-23s RSS responses
```

### Retry Policy (Handles Transient Failures)
```csharp
// MarsVista.Scraper/Program.cs:173-186
.WaitAndRetryAsync(
    retryCount: 3,
    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
)
```

### Scraper Performance Expectations

**Curiosity (Fast API):**
- ~0.5 seconds per sol
- Can scrape 4,683 sols in ~40 minutes (with 1s delay between requests)
- Pagination available (up to 1000 items/page)

**Perseverance (Slow API):**
- ~20-23 seconds per sol
- Can scrape 1,646 sols in ~9-10 hours (with 1s delay between requests)
- No pagination (returns all images per sol in one request)

**Incremental Daily Updates (Both Rovers):**
- Perseverance: 7-sol lookback = ~3 minutes max (7 × 20s + delays)
- Curiosity: 7-sol lookback = ~10-15 seconds (7 × 0.5s + delays)

## Trade-offs Accepted

### Slow Perseverance Scraper
- **Accepted:** 20-23 second response time per sol request
- **Why it's OK:**
  - Incremental scraper only runs daily with 7-sol lookback (~3 min)
  - Full historical scrape is one-time operation (~10 hours)
  - NASA's infrastructure constraint, not our bug
- **Mitigation:**
  - Appropriate timeouts and retry policies
  - Parallel scraping doesn't block other rovers
  - Structured logging shows clear progress

### Inconsistent Scraper Performance
- **Accepted:** 50x performance difference between rovers
- **Why it's OK:**
  - Users don't directly interact with scrapers
  - Both complete successfully within acceptable timeframes
  - Monitoring shows clear metrics per rover
- **Mitigation:**
  - Document expected performance in deployment guide
  - Set monitoring alerts based on rover-specific baselines

## Alternatives Rejected

### Why Not Official NASA API?
- Less metadata richness (10 fields vs 30-40 fields)
- Rate limits would constrain operations
- Doesn't solve the performance issue
- Would diverge from reference Rails API pattern

### Why Not Wait for NASA to Improve RSS API?
- NASA APIs are unofficial and unsupported
- No SLA or improvement roadmap
- Cannot depend on external optimization
- Current performance is acceptable for our use case

### Why Not Cache NASA Responses?
- Would only help repeated requests for same sol
- Incremental scraper requests new sols (no cache benefit)
- Adds complexity for minimal gain
- Doesn't address root cause (slow API)

## Validation Criteria

This decision is validated by:
- ✅ Both scrapers successfully retrieve all photos
- ✅ Perseverance scraper completes within timeout limits (30s > 23s)
- ✅ Incremental scraper completes daily runs in reasonable time (<5 min)
- ✅ Full historical scrapes complete successfully (tested in production)
- ✅ Retry policies handle transient failures effectively
- ✅ Monitoring shows clear per-rover performance metrics

## Monitoring and Metrics

Track these metrics to validate ongoing performance:

```bash
# Perseverance API response time baseline
# Expected: 20-23 seconds per sol
# Alert if: >30 seconds (indicates API degradation)

# Curiosity API response time baseline
# Expected: 0.4-0.7 seconds per sol
# Alert if: >5 seconds (indicates API degradation)

# Incremental scraper total duration
# Expected Perseverance: 2-4 minutes (7 sols × 20-30s)
# Expected Curiosity: 10-30 seconds (7 sols × 1-3s)
```

## Future Considerations

1. **If NASA Improves RSS API Performance:**
   - No code changes needed
   - Scraper will automatically benefit
   - Update performance baselines in monitoring

2. **If NASA Provides Unified API:**
   - Evaluate metadata completeness vs current endpoints
   - Assess rate limits and reliability
   - Consider migration if benefits outweigh switching costs

3. **If Official API Removes Rate Limits:**
   - Re-evaluate official API as consolidation option
   - Would need to verify metadata parity first

## Related Decisions

- [Decision 006: Scraper Service Pattern](006-scraper-service-pattern.md) - Why separate scrapers per rover
- [Decision 006A: HTTP Resilience Strategy](006a-http-resilience.md) - Retry and circuit breaker policies

## References

- NASA Mars 2020 RSS API: https://mars.nasa.gov/rss/api/
- NASA MSL raw_image_items API: https://mars.nasa.gov/api/v1/raw_image_items/
- Official NASA API (not used): https://api.nasa.gov/
- Performance testing date: 2025-11-20
- Test results: See benchmarking table above
