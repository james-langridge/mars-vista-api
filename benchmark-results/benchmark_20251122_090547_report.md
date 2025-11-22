# Mars Vista API - Production Benchmark Report

**Generated:** 2025-11-22T09:10:46-08:00
**Duration:** 4m 59s
**Base URL:** https://api.marsvista.dev

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total Tests** | 133 |
| **Passed** | 128 |
| **Failed** | 5 |
| **Pass Rate** | 96.00% |
| **Total Time** | 292.440475s |
| **Average Response Time** | 2.1988s |
| **Min Response Time** | 0.118518s |
| **Max Response Time** | 94.851834s |

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
| P50 (median) | 0.365797s |
| P90 | 2.039236s |
| P95 | 3.438860s |
| P99 | 35.610815s |

---

## Failed Tests

The following tests failed:


---

## Detailed Results

Full test results are available in JSON format:
`./benchmark-results/benchmark_20251122_090547.json`

### Top 10 Slowest Endpoints

- **94.851834s** - GET /api/v2/panoramas?rovers=curiosity&sol_min=1000
- **44.034280s** - GET /api/v1/rovers/curiosity/photos?earth_date=2012-08-06
- **35.610815s** - GET /api/v2/photos?rovers=curiosity&sol_max=100
- **31.797340s** - GET /api/v2/photos?rovers=curiosity&mars_time_min=M14:00:00&mars_time_max=M16:00:00&mast_elevation_min=0&mast_elevation_max=45&cameras=MAST
- **5.094059s** - GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100
- **4.847320s** - GET /api/v2/photos?rovers=curiosity&min_width=1024
- **3.456607s** - GET /api/v2/photos?rovers=curiosity&sample_type=Full
- **3.438860s** - GET /api/v2/photos?rovers=curiosity&min_height=1080
- **3.329639s** - GET /api/v2/panoramas
- **3.296905s** - GET /api/v2/photos?rovers=curiosity&min_width=1920&min_height=1080

### Top 10 Fastest Endpoints

- **0.118518s** - GET /api/v2/time-machine
- **0.122521s** - GET /api/v2/photos?rovers=invalid_rover
- **0.129851s** - GET /api/v2/photos?rovers=curiosity&sol_min=-1
- **0.130914s** - GET /api/v2/cameras/INVALID_CAMERA
- **0.131806s** - GET /api/v2/photos?rovers=curiosity&date_min=invalid-date
- **0.141584s** - GET /api/v2/photos/stats?rovers=curiosity&group_by=invalid
- **0.143157s** - GET /api/v2/photos/stats?rovers=curiosity
- **0.144167s** - GET /api/v1/photos/999999999
- **0.155551s** - GET /api/v1/photos/451991
- **0.162097s** - GET /api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT,MCZ_RIGHT

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

**Report Generated:** 2025-11-22T09:10:46-08:00
