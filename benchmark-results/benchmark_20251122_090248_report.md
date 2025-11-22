# Mars Vista API - Production Benchmark Report

**Generated:** 2025-11-22T09:03:15-08:00
**Duration:** 0m 27s
**Base URL:** https://api.marsvista.dev

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 133 |
| **Passed** | 11 |
| **Failed** | 122 |
| **Pass Rate** | 8.00% |
| **Total Time** | 19.910478s |
| **Average Response Time** | .1497s |
| **Min Response Time** | 0.102582s |
| **Max Response Time** | 0.448764s |

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
| P50 (median) | 0.130263s |
| P90 | 0.194767s |
| P95 | 0.244960s |
| P99 | 0.361754s |

---

## Failed Tests

The following tests failed:


---

## Detailed Results

Full test results are available in JSON format:
`./benchmark-results/benchmark_20251122_090248.json`

### Top 10 Slowest Endpoints

- **0.448764s** - GET /api/v2/cameras/MCZ_LEFT
- **0.401327s** - GET /api/v2/cameras/MAST?rover=curiosity
- **0.361754s** - GET /api/v2/cameras/MAST
- **0.351945s** - GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&mars_time_golden_hour=true
- **0.333463s** - GET /api/v2/cameras/NAVCAM
- **0.304135s** - GET /api/v2/cameras
- **0.249349s** - GET /api/v1/rovers?format=camelCase
- **0.244960s** - GET /api/v1/rovers/curiosity/photos?earth_date=2012-08-06
- **0.220204s** - GET /api/v2/photos?rovers=curiosity&sort=-earth_date
- **0.213521s** - GET /api/v2/rovers/curiosity/manifest

### Top 10 Fastest Endpoints

- **0.102582s** - GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=10&min_width=1920&min_height=1080
- **0.102660s** - GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=20
- **0.103012s** - GET /api/v2/photos?rovers=curiosity&field_set=extended
- **0.104723s** - GET /api/v2/photos?rovers=curiosity&min_height=1080
- **0.105144s** - GET /api/v2/photos?rovers=curiosity&earth_date=2012-08-06
- **0.106089s** - GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-30&mast_elevation_max=30
- **0.106297s** - GET /api/v2/photos?rovers=curiosity&include=rover,camera
- **0.107812s** - GET /api/v2/locations?rovers=curiosity&min_photos=50
- **0.108555s** - GET /api/v2/photos?rovers=curiosity&date_min=2024-01-01
- **0.108724s** - GET /api/v2/photos?rovers=curiosity,perseverance&mars_time_golden_hour=true

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

**Report Generated:** 2025-11-22T09:03:15-08:00
