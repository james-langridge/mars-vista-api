# Mars Vista API - Production Benchmark Report

**Generated:** 2025-11-22T09:58:51-08:00
**Duration:** 1m 37s
**Base URL:** https://api.marsvista.dev

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 133 |
| **Passed** | 131 |
| **Failed** | 2 |
| **Pass Rate** | 98.00% |
| **Total Time** | 90.190513s |
| **Average Response Time** | .6781s |
| **Min Response Time** | 0.158901s |
| **Max Response Time** | 16.319819s |

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
| P50 (median) | 0.333811s |
| P90 | 1.029683s |
| P95 | 2.422863s |
| P99 | 3.117005s |

---

## Failed Tests

The following tests failed:


---

## Detailed Results

Full test results are available in JSON format:
`./benchmark-results/benchmark_20251122_095714.json`

### Top 10 Slowest Endpoints

- **16.319819s** - GET /api/v2/panoramas?rovers=curiosity&sol_min=1000
- **3.214493s** - GET /api/v2/photos?rovers=curiosity&aspect_ratio=4:3
- **3.117005s** - GET /api/v2/photos?rovers=curiosity&min_height=1080
- **3.072445s** - GET /api/v2/photos?rovers=curiosity&min_width=1024
- **3.002013s** - GET /api/v2/photos?rovers=curiosity&sample_type=Full
- **2.931007s** - GET /api/v2/photos?rovers=curiosity&aspect_ratio=16:9
- **2.631099s** - GET /api/v2/panoramas?rovers=curiosity&min_photos=10
- **2.422863s** - GET /api/v2/panoramas?rovers=curiosity
- **2.136377s** - GET /api/v2/panoramas
- **2.007319s** - GET /api/v2/photos?rovers=curiosity&sol_max=100

### Top 10 Fastest Endpoints

- **0.158901s** - GET /api/v2/photos?rovers=perseverance&date_min=2024-01-01&cameras=MCZ_LEFT,MCZ_RIGHT&aspect_ratio=16:9&field_set=extended
- **0.160760s** - GET /api/v1/photos/999999999
- **0.161345s** - GET /api/v2/photos/stats?rovers=curiosity
- **0.166107s** - GET /api/v1/rovers/nonexistent
- **0.180189s** - GET /api/v2/time-machine
- **0.186107s** - GET /api/v2/photos?rovers=curiosity&sample_type=Thumbnail
- **0.189064s** - GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=2000&cameras=MAST&mast_elevation_min=0&mast_elevation_max=30&sample_type=Full&min_width=2048&field_set=scientific
- **0.190360s** - GET /api/v1/rovers/perseverance/photos?sol=500&camera=MCZ_LEFT
- **0.203005s** - GET /api/v2/rovers/perseverance
- **0.211372s** - GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=10&min_width=1920&min_height=1080

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

**Report Generated:** 2025-11-22T09:58:51-08:00
