# Performance Guide - Mars Vista API

**Last Updated:** November 22, 2025

## Overview

The Mars Vista API provides access to nearly 2 million Mars rover photos. While we've optimized for performance, some queries naturally take longer due to the dataset size. This guide helps you understand expected response times and how to optimize your API usage.

## Current Database Statistics

- **Total Photos:** 1,988,601 photos
- **Rovers:** Curiosity (675,765), Perseverance (451,602), Opportunity (548,306), Spirit (224,234)
- **Average Response Time:** 0.68 seconds
- **P95 Response Time:** 2.4 seconds
- **P99 Response Time:** 3.1 seconds

## Expected Response Times

### Fast Queries (< 1 second)

| Endpoint | Typical Time | Description |
|----------|-------------|-------------|
| `GET /api/v1/rovers` | 50-100ms | List all rovers |
| `GET /api/v1/rovers/{name}` | 50-100ms | Get specific rover |
| `GET /api/v1/photos/{id}` | 100-300ms | Get photo by ID |
| `GET /api/v2/photos` (simple filters) | 200-500ms | Basic queries with rover/camera filters |
| `GET /api/v2/rovers` | 100-200ms | List rovers with details |

### Moderate Queries (1-2 seconds)

| Endpoint | Typical Time | Description |
|----------|-------------|-------------|
| `GET /api/v2/photos` (date ranges) | 500ms-1.5s | Queries with date_min/date_max |
| `GET /api/v2/photos` (sol ranges) | 500ms-1.5s | Queries with sol_min/sol_max |
| `GET /api/v2/photos` (combined filters) | 1-2s | Multiple filters combined |
| `GET /api/v2/rovers/{name}/manifest` | 500ms-1s | Photo manifest by sol |

### Slower Queries (2-5 seconds)

| Endpoint | Typical Time | Why It's Slower |
|----------|-------------|----------------|
| `GET /api/v2/photos` (image quality filters) | 2-3s | Filtering by width/height across 2M photos |
| `GET /api/v2/photos` (aspect ratio) | 2-3s | Calculated field across large dataset |
| `GET /api/v2/photos` (complex combinations) | 2-5s | Multiple complex filters |
| `GET /api/v2/locations` | 1-3s | Aggregating location data |

### Analysis Queries (5-16 seconds)

| Endpoint | Typical Time | Why It's Slower |
|----------|-------------|----------------|
| `GET /api/v2/panoramas` | 5-16s | Analyzes photo sequences and camera angles |
| `GET /api/v2/photos/stats` | 2-5s | Aggregating statistics across dataset |
| `GET /api/v2/time-machine` | 3-8s | Complex location-based temporal queries |

## Performance Optimization Tips

### 1. Use Specific Filters

**Slow - Broad query:**
```bash
GET /api/v2/photos?rovers=curiosity
# Returns thousands of results, takes 2-3s
```

**Fast - Narrow query:**
```bash
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100&cameras=NAVCAM
# Returns hundreds of results, takes 0.5-1s
```

### 2. Paginate Results

Always use `per_page` to limit result sets:

```bash
# Default (25 per page) - Fast
GET /api/v2/photos?rovers=curiosity&sol=1000

# Large page (100 per page) - Slower
GET /api/v2/photos?rovers=curiosity&sol=1000&per_page=100

# Best practice: Request what you need
GET /api/v2/photos?rovers=curiosity&sol=1000&per_page=10
```

**Limits:**
- Default: 25 results per page
- Maximum: 100 results per page

### 3. Use Field Selection

Request only the data you need using `fields` or `field_set`:

**Slow - Full data:**
```bash
GET /api/v2/photos?rovers=curiosity&per_page=50
# Returns all fields (50-100 KB response)
```

**Fast - Minimal data:**
```bash
GET /api/v2/photos?rovers=curiosity&per_page=50&field_set=minimal
# Returns only id, sol, img_src (10-20 KB response)
```

**Available field sets:**
- `minimal` - Just id, sol, and medium image
- `standard` - Basic photo info (default)
- `extended` - Adds location, dimensions, Mars time
- `scientific` - All telemetry and coordinates
- `complete` - Everything including raw NASA data

### 4. Cache Responses

Mars photo data updates infrequently. Implement caching for better performance:

**For inactive rovers** (Spirit, Opportunity):
- Cache for 1 year (data never changes)
- Use ETags for conditional requests

**For active rovers** (Curiosity, Perseverance):
- Cache for 1 hour (new photos added daily)
- Use ETags for conditional requests

**Example with ETags:**
```bash
# Initial request
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&sol=1000"
# Response includes: ETag: "abc123"

# Subsequent request
curl -H "X-API-Key: YOUR_KEY" \
     -H "If-None-Match: \"abc123\"" \
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&sol=1000"
# Returns 304 Not Modified if unchanged
```

### 5. Avoid Broad Date/Sol Ranges

**Slow - Large range:**
```bash
GET /api/v2/photos?rovers=curiosity&sol_min=1&sol_max=4683
# Processes thousands of sols, takes 5-10s
```

**Fast - Specific range:**
```bash
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010
# Processes 10 sols, takes 0.5-1s
```

### 6. Use Batch Requests Wisely

If you need multiple specific photos, use batch endpoint:

```bash
# Fast - Single batch request
POST /api/v2/photos/batch
{"ids": [123456, 123457, 123458]}
# 300-500ms for 3 photos

# Slow - Multiple individual requests
GET /api/v2/photos/123456  # 300ms
GET /api/v2/photos/123457  # 300ms
GET /api/v2/photos/123458  # 300ms
# Total: 900ms + 3x network overhead
```

### 7. Optimize Panorama Queries

Panorama detection analyzes photo sequences and is the most computationally expensive operation:

**Very Slow - Broad panorama search:**
```bash
GET /api/v2/panoramas?rovers=curiosity
# Analyzes all 675K photos, takes 60-90s
```

**Moderate - Specific sol range:**
```bash
GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=1100
# Analyzes ~50K photos, takes 5-10s
```

**Fast - Narrow range with minimum photos:**
```bash
GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=1050&min_photos=10
# Analyzes ~25K photos, filters to substantial panoramas, takes 2-5s
```

### 8. Use Image Sizes Appropriately

Request only the image sizes you need:

```bash
# Get all 4 sizes (small/medium/large/full) - Large response
GET /api/v2/photos?rovers=curiosity&sol=1000

# Get only medium images - Smaller response
GET /api/v2/photos?rovers=curiosity&sol=1000&image_sizes=medium

# Get metadata only (no images) - Smallest response
GET /api/v2/photos?rovers=curiosity&sol=1000&exclude_images=true
```

## Understanding Slow Queries

### Why Panorama Detection is Slow

Panorama detection:
1. Queries photos by sol range
2. Groups by camera and timestamp proximity
3. Analyzes camera angle sequences (azimuth, elevation)
4. Calculates angular coverage
5. Filters by minimum photo count

**Solution:** Always specify `sol_min`, `sol_max`, and `min_photos` parameters.

### Why Large Date Ranges are Slow

Date range queries like `date_min=2020-01-01&date_max=2024-12-31` may match hundreds of thousands of photos across hundreds of sols.

**Solution:** Use smaller date ranges (weeks or months instead of years).

### Why Image Quality Filters are Slower

Filters like `min_width=1920&min_height=1080` must check dimensions for all 2M photos.

**Solution:** Combine with other filters to reduce the dataset first:
```bash
# Slow
GET /api/v2/photos?min_width=1920

# Faster
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100&min_width=1920
```

## Performance Best Practices Summary

### DO:
✅ Use specific filters (rover, camera, sol/date ranges)
✅ Limit results with `per_page` (10-25 for UI, 50-100 for batch)
✅ Use `field_set=minimal` for listings
✅ Cache responses (especially for inactive rovers)
✅ Implement ETags for conditional requests
✅ Use batch endpoints for multiple specific photos
✅ Specify `sol_min`/`sol_max` for panorama queries

### DON'T:
❌ Query all rovers without filters
❌ Use `per_page=100` by default
❌ Request `field_set=complete` unless necessary
❌ Query broad date ranges (> 1 year)
❌ Run panorama detection without sol limits
❌ Make multiple individual requests when batch is available
❌ Ignore ETags and Cache-Control headers

## Example: Optimized Photo Gallery

Here's how to build a performant photo gallery:

### Gallery Listing (Fast)
```bash
# Thumbnail grid: minimal data, small images only
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100&field_set=minimal&image_sizes=small&per_page=50
# Response time: 500-800ms
```

### Photo Detail (Fast)
```bash
# Detail view: extended data, all image sizes
GET /api/v2/photos/123456?field_set=extended
# Response time: 200-400ms
```

### Filters (Fast)
```bash
# User filters by date range and camera
GET /api/v2/photos?rovers=curiosity&date_min=2024-01-01&date_max=2024-01-31&cameras=MAST,NAVCAM&field_set=minimal&per_page=25
# Response time: 800ms-1.5s
```

## Caching Recommendations

### Application-Level Caching

```python
import requests
from datetime import datetime, timedelta

class MarsVistaClient:
    def __init__(self, api_key):
        self.api_key = api_key
        self.cache = {}

    def get_photos(self, **params):
        cache_key = str(params)
        cached = self.cache.get(cache_key)

        # Cache inactive rovers for 24 hours
        # Cache active rovers for 1 hour
        if cached and self._is_valid_cache(cached, params):
            return cached['data']

        response = requests.get(
            "https://api.marsvista.dev/api/v2/photos",
            headers={"X-API-Key": self.api_key},
            params=params
        )

        self.cache[cache_key] = {
            'data': response.json(),
            'timestamp': datetime.now(),
            'etag': response.headers.get('ETag')
        }

        return self.cache[cache_key]['data']
```

### HTTP-Level Caching

Use a reverse proxy like Nginx or Varnish with our cache headers:

```nginx
# Nginx config example
proxy_cache_path /var/cache/nginx/marsvista levels=1:2 keys_zone=marsvista:10m;

location /api/ {
    proxy_pass https://api.marsvista.dev;
    proxy_cache marsvista;
    proxy_cache_key "$request_uri|$http_x_api_key";
    proxy_cache_valid 200 1h;
    add_header X-Cache-Status $upstream_cache_status;
}
```

## Monitoring Your Usage

Monitor response times and adjust your queries:

```bash
# Check response time
time curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol=1000"

# Check rate limit headers
curl -I -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol=1000"
```

Response headers include:
```http
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9987
X-RateLimit-Reset: 1731859200
```

## When Performance Matters Most

### Real-Time Applications
- Use `field_set=minimal`
- Limit to 10-25 results per request
- Cache aggressively
- Expect 200-500ms response times

### Batch Processing
- Use larger `per_page` (50-100)
- Implement retries for slow queries
- Process in background jobs
- Expect 500ms-2s response times

### Data Analysis
- Query specific date/sol ranges
- Use statistics endpoints
- Export data and analyze locally
- Expect 1-5s response times

### Panorama Discovery
- Always specify sol ranges
- Use `min_photos` filter
- Consider pre-computing results
- Expect 2-16s response times

## Database Performance Characteristics

The Mars Vista API uses PostgreSQL with optimized indexes:

**Indexed Columns (Fast queries):**
- `rover_id` - Filter by rover
- `camera_id` - Filter by camera
- `sol` - Filter by Martian sol
- `earth_date` - Filter by Earth date
- `site`, `drive` - Location-based queries
- `width`, `height` - Image dimension filters
- `sample_type` - Image quality filters
- `rover_id, mars_time_hour` - Mars time queries

**Calculated Fields (Slower queries):**
- Aspect ratio (calculated from width/height)
- Panorama detection (analyzes sequences)
- Location radius (distance calculations)

## Future Performance Improvements

We're continuously working on optimizations:

**Planned:**
- Pre-computed panoramas (90% improvement)
- Redis caching layer (50% improvement for hot paths)
- Cursor-based pagination (better for large offsets)
- Read replicas (horizontal scaling)

**Under Consideration:**
- GraphQL API (request exactly what you need)
- WebSocket API (real-time updates)
- CDN for image URLs (faster image loading)

## Getting Help

If you're experiencing slow queries:

1. Check this guide for optimization tips
2. Review your query parameters
3. Test with smaller datasets first
4. Enable caching in your application
5. Contact support with specific slow endpoints

## See Also

- [API Endpoints Documentation](API_ENDPOINTS.md) - Complete API reference
- [Authentication Guide](AUTHENTICATION_GUIDE.md) - Rate limits and API keys
- [Database Access Guide](DATABASE_ACCESS.md) - Database performance details
- [Main README](../README.md) - Project overview

---

**Last Performance Audit:** November 22, 2025
**Database Size:** 1,988,601 photos
**Average Response Time:** 0.68s (P50: 0.33s, P95: 2.4s, P99: 3.1s)
