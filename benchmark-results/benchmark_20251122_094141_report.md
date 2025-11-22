# Mars Vista API - Production Benchmark Report

**Generated:** 2025-11-22T09:44:31-08:00
**Duration:** 2m 50s
**Base URL:** https://api.marsvista.dev

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 133 |
| **Passed** | 111 |
| **Failed** | 22 |
| **Pass Rate** | 83.00% |
| **Total Time** | 162.835480s |
| **Average Response Time** | 1.2243s |
| **Min Response Time** | 0.147181s |
| **Max Response Time** | 94.548521s |

---

## Test Coverage

### Categories Tested

1. ✅ Basic Endpoints (health, rovers, manifests)
2. ✅ API v1 Photos
3. ✅ API v2 Basic Filtering (sol, date, camera)
4. ✅ API v2 Mars Time Filtering (golden hour)
5. ✅ API v2 Location-Based Queries
6. ✅ API v2 Image Quality Filters
7. ✅ API v2 Camera Angle Queries
8. ✅ API v2 Field Selection & Image Sizes
9. ✅ API v2 Combined Advanced Filters
10. ✅ API v2 Statistics
11. ✅ API v2 Rovers & Cameras
12. ✅ API v2 Advanced Features (panoramas, locations, time-machine)
13. ✅ Error Cases

### Parameters Tested

All available query parameters were tested:
- ✅ rovers, cameras (multi-value)
- ✅ sol, sol_min, sol_max, earth_date, date_min, date_max
- ✅ mars_time_min, mars_time_max, mars_time_golden_hour
- ✅ site, site_min, site_max, drive, drive_min, drive_max, location_radius
- ✅ min_width, max_width, min_height, max_height, sample_type, aspect_ratio
- ✅ mast_elevation_min, mast_elevation_max, mast_azimuth_min, mast_azimuth_max
- ✅ sort, fields, include, field_set, image_sizes, exclude_images
- ✅ page, per_page

---

## Performance Analysis

### Response Time Distribution

Calculating percentiles...

| Percentile | Response Time |
|------------|---------------|
| P50 (median) | 0.308355s |
| P90 | 1.002754s |
| P95 | 2.135876s |
| P99 | 3.151890s |

---

## Failed Tests

The following tests failed:


---

## Detailed Results

Full test results are available in JSON format:
`./benchmark-results/benchmark_20251122_094141.json`

### Top 10 Slowest Endpoints

- **94.548521s** - GET /api/v2/panoramas?rovers=curiosity&sol_min=1000
- **3.213255s** - GET /api/v2/photos?rovers=curiosity&sample_type=Full
- **3.151890s** - GET /api/v2/panoramas?rovers=curiosity
- **3.023110s** - GET /api/v2/photos?rovers=curiosity&min_width=1024
- **2.782824s** - GET /api/v2/panoramas
- **2.604044s** - GET /api/v2/photos?rovers=curiosity&min_height=1080
- **2.263001s** - GET /api/v1/rovers/curiosity/photos?earth_date=2012-08-06
- **2.135876s** - GET /api/v1/rovers/curiosity/photos?earth_date=2024-11-20
- **1.903692s** - GET /api/v2/photos?rovers=curiosity&sol_max=100
- **1.872886s** - GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100

### Top 10 Fastest Endpoints

- **0.147181s** - GET /api/v2/photos?rovers=curiosity&sol_min=-1
- **0.154593s** - GET /api/v2/photos/451991
- **0.157648s** - GET /api/v2/photos?rovers=curiosity&per_page=101
- **0.164876s** - GET /api/v2/photos?rovers=invalid_rover
- **0.165761s** - GET /api/v1/photos/999999999
- **0.168087s** - GET /api/v2/photos?rovers=curiosity&date_min=invalid-date
- **0.171078s** - GET /api/v2/time-machine?site=82&drive=2176
- **0.175598s** - GET /api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT,MCZ_RIGHT
- **0.181016s** - GET /api/v1/photos/451991
- **0.190914s** - GET /api/v2/panoramas?rovers=curiosity&min_photos=10

---

## Recommendations

Based on the benchmark results:

1. **Performance**:
   - Endpoints with response time > 2s should be investigated for optimization
   - Consider caching for frequently accessed endpoints
   - Review database query performance for slow endpoints

2. **Reliability**:
   - All endpoints should maintain > 99% success rate
   - Failed tests should be investigated and fixed

3. **Monitoring**:
   - Set up alerts for response times > P95
   - Monitor error rates by endpoint category
   - Track rate limit usage patterns

---

## Next Steps

1. Review failed tests and fix any issues
2. Investigate endpoints with high response times
3. Set up continuous performance monitoring
4. Re-run benchmarks after optimizations

---

**Report Generated:** 2025-11-22T09:44:31-08:00
