# Mars Vista API Reference

Complete API reference for AI agents and automated tools.
Human-friendly docs: https://marsvista.dev/docs

## Quick Links

- **TypeScript Types**: [types.ts](./types.ts) - Copy into your project for full type safety
- **OpenAPI Spec**: [openapi.json](./openapi.json) - For code generation tools
- **Base URL**: `https://api.marsvista.dev`

---

## Authentication

All requests require the `X-API-Key` header.

```bash
curl -H "X-API-Key: YOUR_API_KEY" "https://api.marsvista.dev/api/v2/photos"
```

Get your free API key at https://marsvista.dev (requires email signup).

### Rate Limits

All users receive the same generous rate limits:
- 10,000 requests/hour
- 100,000 requests/day

Rate limit headers are included in all responses:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Requests remaining
- `X-RateLimit-Reset`: Unix timestamp when limit resets

---

## GET /api/v2/photos

Query Mars rover photos with powerful filtering.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| rovers | string | No | Comma-separated rover names: curiosity,perseverance,opportunity,spirit |
| cameras | string | No | Comma-separated camera names: NAVCAM,FHAZ,RHAZ,MAST,CHEMCAM,MAHLI,MARDI,NAVCAM_LEFT,NAVCAM_RIGHT |
| sol_min | integer | No | Minimum sol (Mars day) |
| sol_max | integer | No | Maximum sol |
| date_min | string | No | Minimum Earth date (YYYY-MM-DD) |
| date_max | string | No | Maximum Earth date (YYYY-MM-DD) |
| site | integer | No | Site number (geological location marker) |
| drive | integer | No | Drive number (rover's drive sequence) |
| site_min | integer | No | Minimum site number |
| site_max | integer | No | Maximum site number |
| location_radius | integer | No | Radius in drives for proximity search (use with site and drive) |
| min_width | integer | No | Minimum image width in pixels |
| min_height | integer | No | Minimum image height in pixels |
| sample_type | string | No | Image type: Full, Thumbnail, Subframe |
| mars_time_min | string | No | Minimum Mars local time (M06:00:00 format, minute precision) |
| mars_time_max | string | No | Maximum Mars local time (minute precision) |
| mars_time_golden_hour | boolean | No | Filter for sunrise/sunset photos (hours 5-7 and 17-19) |
| mast_elevation_min | float | No | Minimum camera elevation angle (degrees) |
| mast_elevation_max | float | No | Maximum camera elevation angle |
| mast_azimuth_min | float | No | Minimum camera azimuth angle (degrees) |
| mast_azimuth_max | float | No | Maximum camera azimuth angle |
| include | string | No | Related resources to include: rover,camera (RECOMMENDED) |
| fields | string | No | Specific fields to return: id,sol,earth_date,images |
| field_set | string | No | Preset field groups: minimal, standard, extended, scientific, complete |
| image_sizes | string | No | Image sizes to include: small,medium,large,full |
| sort | string | No | Sort order: -earth_date (default), sol, camera, -sol. Prefix with - for descending |
| page | integer | No | Page number (default: 1) |
| per_page | integer | No | Results per page (default: 25, max: 100) |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010&per_page=2&include=rover,camera"
```

### Example Response

```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "nasa_id": "NRF_0613MR0025780020402986C00_DXXX",
        "sol": 1000,
        "earth_date": "2015-05-30",
        "date_taken_utc": "2015-05-30T10:23:45Z",
        "date_taken_mars": "Sol-1000M14:23:45",
        "images": {
          "small": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/ncam/NRF_0613MR0025780020402986C00_DXXX-thm.jpg",
          "medium": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/ncam/NRF_0613MR0025780020402986C00_DXXX-med.jpg",
          "large": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/ncam/NRF_0613MR0025780020402986C00_DXXX-lrg.jpg",
          "full": "https://mars.nasa.gov/msl-raw-images/proj/msl/redops/ods/surface/sol/01000/opgs/edr/ncam/NRF_0613MR0025780020402986C00_DXXX.png"
        },
        "dimensions": {
          "width": 1344,
          "height": 1200
        },
        "sample_type": "Full",
        "location": {
          "site": 79,
          "drive": 1204,
          "coordinates": {
            "x": 35.4362,
            "y": 22.5714,
            "z": -9.46445
          }
        },
        "telemetry": {
          "mast_azimuth": 156.098,
          "mast_elevation": -10.1652
        },
        "title": "Sol 1000: Navigation Camera",
        "caption": "This image was taken by the Navigation Camera on Curiosity",
        "credit": "NASA/JPL-Caltech"
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "type": "rover",
          "attributes": {
            "name": "Curiosity",
            "status": "active"
          }
        },
        "camera": {
          "id": "NAVCAM",
          "type": "camera",
          "attributes": {
            "full_name": "Navigation Camera"
          }
        }
      }
    }
  ],
  "meta": {
    "total_count": 15234,
    "returned_count": 2,
    "timestamp": "2025-11-25T12:00:00Z"
  },
  "pagination": {
    "page": 1,
    "per_page": 2,
    "total_pages": 7617
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010&page=1&per_page=2",
    "next": "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010&page=2&per_page=2",
    "first": "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010&page=1&per_page=2",
    "last": "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010&page=7617&per_page=2"
  }
}
```

### Important Notes

1. **Always use `include=rover,camera`** - Without this, the `relationships` object is empty and you won't know which rover/camera took each photo
2. **Use `images.medium` or `images.large`** - The `img_src` field is a legacy field for v1 compatibility
3. **Pagination is required for large results** - Default returns 25 items, max 100

---

## GET /api/v2/photos/{id}

Get a specific photo by ID.

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos/123456?include=rover,camera"
```

### Example Response

```json
{
  "data": {
    "id": 123456,
    "type": "photo",
    "attributes": {
      "nasa_id": "NRF_0613MR0025780020402986C00_DXXX",
      "sol": 1000,
      "earth_date": "2015-05-30",
      "images": {
        "small": "https://mars.nasa.gov/.../thm.jpg",
        "medium": "https://mars.nasa.gov/.../med.jpg",
        "large": "https://mars.nasa.gov/.../lrg.jpg",
        "full": "https://mars.nasa.gov/.../full.png"
      }
    },
    "relationships": {
      "rover": {
        "id": "curiosity",
        "type": "rover"
      },
      "camera": {
        "id": "NAVCAM",
        "type": "camera"
      }
    }
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/photos/123456"
  }
}
```

---

## GET /api/v2/photos/stats

Get aggregated photo statistics.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| group_by | string | Yes | Grouping: camera, rover, or sol |
| rovers | string | No | Filter by rovers |
| cameras | string | No | Filter by cameras |
| sol_min | integer | No | Minimum sol |
| sol_max | integer | No | Maximum sol |
| date_min | string | No | Minimum date |
| date_max | string | No | Maximum date |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera"
```

### Example Response

```json
{
  "data": {
    "total_photos": 682660,
    "groups": [
      {
        "key": "NAVCAM",
        "count": 150234
      },
      {
        "key": "MAST",
        "count": 98765
      },
      {
        "key": "FHAZ",
        "count": 75432
      }
    ]
  },
  "meta": {
    "query": {
      "group_by": "camera",
      "total_photos": 682660
    }
  }
}
```

---

## POST /api/v2/photos/batch

Batch retrieve multiple photos by ID.

### Request Body

```json
{
  "ids": [123456, 123457, 123458]
}
```

### Example Request

```bash
curl -X POST -H "X-API-Key: YOUR_KEY" \
  -H "Content-Type: application/json" \
  -d '{"ids": [123456, 123457, 123458]}' \
  "https://api.marsvista.dev/api/v2/photos/batch"
```

### Example Response

```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": { "sol": 1000, "earth_date": "2015-05-30" }
    },
    {
      "id": 123457,
      "type": "photo",
      "attributes": { "sol": 1000, "earth_date": "2015-05-30" }
    }
  ],
  "meta": {
    "total_count": 2,
    "returned_count": 2,
    "query": {
      "ids_requested": 3,
      "ids_found": 2
    }
  }
}
```

### Limits

- Maximum 100 IDs per request

---

## GET /api/v2/rovers

List all Mars rovers.

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v2/rovers"
```

### Example Response

```json
{
  "data": [
    {
      "id": "curiosity",
      "type": "rover",
      "attributes": {
        "name": "Curiosity",
        "landing_date": "2012-08-06",
        "launch_date": "2011-11-26",
        "status": "active",
        "max_sol": 4728,
        "max_date": "2025-11-24",
        "total_photos": 682660
      }
    },
    {
      "id": "perseverance",
      "type": "rover",
      "attributes": {
        "name": "Perseverance",
        "landing_date": "2021-02-18",
        "launch_date": "2020-07-30",
        "status": "active",
        "max_sol": 1379,
        "max_date": "2025-11-24",
        "total_photos": 245123
      }
    },
    {
      "id": "opportunity",
      "type": "rover",
      "attributes": {
        "name": "Opportunity",
        "landing_date": "2004-01-25",
        "launch_date": "2003-07-07",
        "status": "complete",
        "max_sol": 5111,
        "max_date": "2018-06-10",
        "total_photos": 198439
      }
    },
    {
      "id": "spirit",
      "type": "rover",
      "attributes": {
        "name": "Spirit",
        "landing_date": "2004-01-04",
        "launch_date": "2003-06-10",
        "status": "complete",
        "max_sol": 2208,
        "max_date": "2010-03-21",
        "total_photos": 124550
      }
    }
  ],
  "meta": {
    "returned_count": 4
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/rovers"
  }
}
```

---

## GET /api/v2/rovers/{slug}

Get details for a specific rover.

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| slug | string | Rover identifier: curiosity, perseverance, opportunity, spirit |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v2/rovers/curiosity"
```

### Example Response

```json
{
  "data": {
    "id": "curiosity",
    "type": "rover",
    "attributes": {
      "name": "Curiosity",
      "landing_date": "2012-08-06",
      "launch_date": "2011-11-26",
      "status": "active",
      "max_sol": 4728,
      "max_date": "2025-11-24",
      "total_photos": 682660
    }
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/rovers/curiosity"
  }
}
```

---

## GET /api/v2/rovers/{slug}/manifest

Get photo manifest (photo history by sol) for a rover.

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v2/rovers/curiosity/manifest"
```

### Example Response

```json
{
  "data": {
    "id": "curiosity",
    "type": "manifest",
    "attributes": {
      "name": "Curiosity",
      "landing_date": "2012-08-06",
      "launch_date": "2011-11-26",
      "status": "active",
      "max_sol": 4728,
      "max_date": "2025-11-24",
      "total_photos": 682660,
      "photos": [
        {
          "sol": 0,
          "earth_date": "2012-08-06",
          "total_photos": 234,
          "cameras": ["FHAZ", "RHAZ", "NAVCAM"]
        },
        {
          "sol": 1,
          "earth_date": "2012-08-07",
          "total_photos": 156,
          "cameras": ["FHAZ", "RHAZ", "NAVCAM", "MAST"]
        }
      ]
    }
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/rovers/curiosity/manifest"
  }
}
```

---

## GET /api/v2/rovers/{slug}/cameras

Get cameras for a specific rover.

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v2/rovers/curiosity/cameras"
```

### Example Response

```json
{
  "data": [
    {
      "id": "FHAZ",
      "type": "camera",
      "attributes": {
        "name": "FHAZ",
        "full_name": "Front Hazard Avoidance Camera"
      }
    },
    {
      "id": "RHAZ",
      "type": "camera",
      "attributes": {
        "name": "RHAZ",
        "full_name": "Rear Hazard Avoidance Camera"
      }
    },
    {
      "id": "NAVCAM",
      "type": "camera",
      "attributes": {
        "name": "NAVCAM",
        "full_name": "Navigation Camera"
      }
    },
    {
      "id": "MAST",
      "type": "camera",
      "attributes": {
        "name": "MAST",
        "full_name": "Mast Camera"
      }
    }
  ],
  "meta": {
    "returned_count": 4
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/rovers/curiosity/cameras"
  }
}
```

---

## GET /api/v2/rovers/{slug}/journey

Get journey tracking data for a rover.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sol_min | integer | No | Minimum sol |
| sol_max | integer | No | Maximum sol |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/curiosity/journey?sol_min=1000&sol_max=1100"
```

---

## GET /api/v2/rovers/{slug}/traverse

Get deduplicated traverse path for a rover, optimized for map visualization.

Unlike the journey endpoint which groups by sol/site/drive (including duplicates when the rover stays at the same location), traverse returns one point per unique location with actual 3D distance calculations, elevation data, and optional path simplification.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| sol_min | integer | No | Minimum sol |
| sol_max | integer | No | Maximum sol |
| format | string | No | Output format: json (default) or geojson |
| simplify | float | No | Douglas-Peucker tolerance in meters (0 = no simplification) |
| include_segments | boolean | No | Include per-segment distance/bearing data |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/perseverance/traverse"
```

### Example Response (JSON)

```json
{
  "data": {
    "type": "traverse",
    "attributes": {
      "rover": "perseverance",
      "sol_range": { "start": 0, "end": 1698 },
      "total_distance_m": 28547.3,
      "total_elevation_gain_m": 156.7,
      "total_elevation_loss_m": 112.4,
      "net_elevation_change_m": 44.3,
      "point_count": 1847,
      "bounding_box": {
        "min": { "x": -623.4, "y": -413.4, "z": -45.2 },
        "max": { "x": 0.0, "y": 0.0, "z": 32.5 }
      }
    },
    "path": [
      {
        "x": 0.0,
        "y": 0.0,
        "z": 0.0,
        "sol_first": 0,
        "sol_last": 15,
        "cumulative_distance_m": 0.0
      },
      {
        "x": -27.567,
        "y": -7.029,
        "z": 0.093,
        "sol_first": 100,
        "sol_last": 105,
        "cumulative_distance_m": 28.5
      }
    ],
    "links": {
      "geo_json": "/api/v2/rovers/perseverance/traverse?format=geojson"
    }
  }
}
```

### GeoJSON Format

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/perseverance/traverse?format=geojson"
```

Returns a GeoJSON FeatureCollection with a LineString geometry, ready for map libraries like Leaflet or Mapbox:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "LineString",
        "coordinates": [
          [0.0, 0.0, 0.0],
          [-27.567, -7.029, 0.093]
        ]
      },
      "properties": {
        "rover": "perseverance",
        "sol_range": [0, 1698],
        "total_distance_m": 28547.3,
        "point_count": 1847
      }
    }
  ]
}
```

### Path Simplification

Use the `simplify` parameter to reduce point count for overview maps:

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/perseverance/traverse?simplify=10"
```

This uses the Douglas-Peucker algorithm to remove points within 10 meters of the simplified path.

### Segment Data

Include per-segment details with `include_segments=true`:

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/perseverance/traverse?include_segments=true"
```

Each path point (except the first) will include a `segment` object:

```json
{
  "x": -27.567,
  "y": -7.029,
  "z": 0.093,
  "cumulative_distance_m": 28.5,
  "segment": {
    "distance_m": 28.5,
    "bearing_deg": 194.3,
    "elevation_change_m": 0.093
  }
}
```

---

## GET /api/v2/cameras

List all cameras across all rovers.

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v2/cameras"
```

---

## GET /api/v2/panoramas

Get auto-detected panorama sequences.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| rovers | string | No | Filter by rovers |
| min_photos | integer | No | Minimum photos in panorama |
| sol_min | integer | No | Minimum sol |
| sol_max | integer | No | Maximum sol |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=10"
```

---

## GET /api/v2/locations

Get unique locations visited by rovers.

### Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| rovers | string | No | Filter by rovers |
| min_photos | integer | No | Minimum photos at location |
| sol_min | integer | No | Minimum sol |
| sol_max | integer | No | Maximum sol |

### Example Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/locations?rovers=perseverance&min_photos=50"
```

---

## Legacy API (v1)

Drop-in replacement for the archived NASA Mars Rover Photos API.

### GET /api/v1/rovers/{name}/photos

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"
```

### Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| sol | integer | Mars sol (required if no earth_date) |
| earth_date | string | Earth date YYYY-MM-DD (required if no sol) |
| camera | string | Camera name |
| page | integer | Page number (default: 1) |
| per_page | integer | Results per page (default: 25) |

### GET /api/v1/rovers

List all rovers.

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v1/rovers"
```

### GET /api/v1/manifests/{name}

Get rover manifest.

```bash
curl -H "X-API-Key: YOUR_KEY" "https://api.marsvista.dev/api/v1/manifests/curiosity"
```

---

## Error Handling

Errors follow RFC 7807 Problem Details format.

### Error Response

```json
{
  "type": "/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request contains invalid parameters",
  "instance": "/api/v2/photos?date_min=invalid",
  "errors": [
    {
      "field": "date_min",
      "value": "invalid",
      "message": "Must be in YYYY-MM-DD format",
      "example": "2023-01-01"
    }
  ]
}
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 304 | Not Modified (cache hit) |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing or invalid API key) |
| 404 | Not Found |
| 429 | Too Many Requests (rate limit exceeded) |
| 500 | Internal Server Error |

---

## HTTP Caching

All endpoints support ETags for efficient caching.

### First Request

```bash
curl -v -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?sol=1000"
# Response includes: ETag: "abc123"
# Response includes: Cache-Control: public, max-age=3600
```

### Conditional Request

```bash
curl -H "X-API-Key: YOUR_KEY" \
  -H 'If-None-Match: "abc123"' \
  "https://api.marsvista.dev/api/v2/photos?sol=1000"
# Returns 304 Not Modified if unchanged
```

### Cache Durations

| Content | Cache Duration |
|---------|----------------|
| Photos from active rovers | 1 hour |
| Photos from inactive rovers | 1 year |
| Rover list | 24 hours |

---

## Common Use Cases

### Get Latest Photos

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=-earth_date&per_page=10&include=rover,camera"
```

### Get Photos by Date Range

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?date_min=2024-01-01&date_max=2024-01-31&include=rover,camera"
```

### Get High-Resolution Photos Only

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?sample_type=Full&min_width=1920&include=rover,camera"
```

### Get Photos from Multiple Rovers

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&per_page=20&include=rover,camera"
```

### Get Golden Hour Photos (Sunrise/Sunset)

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mars_time_golden_hour=true&include=rover,camera"
```

### Get Photos at a Specific Location

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204&include=rover,camera"
```

### Get Horizon Photos (for Panoramas)

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_min=-5&mast_elevation_max=5&include=rover,camera"
```

### Get Photos at Location Within Time Window (Panorama Sequences)

Filter by location AND time to get photos from a single panorama capture session:

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&site=108&drive=876&mars_time_min=M12:28:00&mars_time_max=M12:31:00&include=rover,camera"
```

Time filtering uses minute-level precision, so `M12:28:00` to `M12:31:00` returns only photos captured within that 3-minute window.
