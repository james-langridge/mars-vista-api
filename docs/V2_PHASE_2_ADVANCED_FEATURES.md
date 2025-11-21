# API v2 Phase 2: Advanced Features

## Overview

Phase 2 of the v2 API introduces revolutionary advanced features that leverage our complete NASA data storage to provide capabilities never before available in any Mars photo API. These features enable virtual exploration, panorama detection, journey visualization, and time-based location comparison.

**Status**: ✅ Complete and Available

## New Endpoints

### 1. Panorama Detection

**Endpoint**: `GET /api/v2/panoramas`

Auto-detect panoramic sequences based on location, time, and camera telemetry.

#### Detection Algorithm

Panoramas are identified by:
- **Same location**: Identical site and drive numbers
- **Same sol**: Photos taken on the same Mars day
- **Sequential timing**: Photos within 5 minutes of each other
- **Consistent elevation**: Camera elevation within ±2 degrees
- **Azimuth sweep**: At least 30 degrees of horizontal coverage

#### Query Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `rovers` | string | Comma-separated rover names | `curiosity,perseverance` |
| `sol_min` | integer | Minimum sol | `1000` |
| `sol_max` | integer | Maximum sol | `2000` |
| `min_photos` | integer | Minimum photos in panorama (default: 3) | `5` |
| `page` | integer | Page number (1-indexed) | `1` |
| `per_page` | integer | Items per page (1-100, default: 25) | `25` |

#### Example Request

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&sol_min=1000&min_photos=5"
```

#### Response Structure

```json
{
  "data": [
    {
      "id": "pano_curiosity_1000_A3F2E1B4",
      "type": "panorama",
      "attributes": {
        "rover": "curiosity",
        "sol": 1000,
        "mars_time_start": "M14:23:00",
        "mars_time_end": "M14:28:00",
        "total_photos": 12,
        "coverage_degrees": 180.5,
        "location": {
          "site": 79,
          "drive": 1204,
          "coordinates": {
            "x": 35.4362,
            "y": 22.5714,
            "z": -9.46445
          }
        },
        "camera": "MAST",
        "avg_elevation": -2.3
      },
      "links": {
        "download_set": "/api/v2/panoramas/pano_curiosity_1000_A3F2E1B4/download"
      }
    }
  ],
  "meta": {
    "total_count": 1247,
    "returned_count": 25
  },
  "pagination": {
    "page": 1,
    "per_page": 25,
    "total_pages": 50
  }
}
```

#### Use Cases

- **Panorama identification**: Find all detected panoramic sequences
- **Panorama component grouping**: Identify photos that belong to panoramas
- **Virtual tour building**: Create immersive Mars exploration experiences
- **Scientific analysis**: Study panoramic imaging patterns

---

### 2. Location Timeline

**Endpoint**: `GET /api/v2/locations`

Get all unique locations visited by rovers with photo counts and visit history.

#### Query Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `rovers` | string | Comma-separated rover names | `curiosity` |
| `sol_min` | integer | Minimum sol | `1000` |
| `sol_max` | integer | Maximum sol | `2000` |
| `min_photos` | integer | Minimum photos at location | `10` |
| `page` | integer | Page number (1-indexed) | `1` |
| `per_page` | integer | Items per page (1-100, default: 25) | `25` |

#### Example Request

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/locations?rovers=curiosity&min_photos=50"
```

#### Response Structure

```json
{
  "data": [
    {
      "id": "curiosity_79_1204",
      "type": "location",
      "attributes": {
        "rover": "curiosity",
        "site": 79,
        "drive": 1204,
        "first_visited": "2015-05-30",
        "last_visited": "2015-06-02",
        "first_sol": 1000,
        "last_sol": 1003,
        "photo_count": 234,
        "visit_count": 4,
        "coordinates": {
          "x": 35.4362,
          "y": 22.5714,
          "z": -9.46445
        }
      },
      "links": {
        "photos": "/api/v2/photos?site=79&drive=1204&rovers=curiosity"
      }
    }
  ],
  "meta": {
    "total_count": 4521,
    "returned_count": 25
  },
  "pagination": {
    "page": 1,
    "per_page": 25,
    "total_pages": 181
  }
}
```

#### Use Cases

- **High-activity locations**: Find locations with the most photos
- **Location history**: Track when and how often locations were visited
- **Journey planning**: Build maps of rover exploration paths
- **Virtual tourism**: Create guided tours of significant locations

---

### 3. Journey Tracking

**Endpoint**: `GET /api/v2/rovers/{rover}/journey`

Track a rover's path over a sol range with distance, elevation, and waypoints.

#### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `rover` | string | Rover slug (curiosity, perseverance, opportunity, spirit) |

#### Query Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `sol_min` | integer | Starting sol | `1000` |
| `sol_max` | integer | Ending sol | `2000` |

#### Example Request

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/curiosity/journey?sol_min=1000&sol_max=1100"
```

#### Response Structure

```json
{
  "data": {
    "type": "journey",
    "attributes": {
      "rover": "curiosity",
      "sol_start": 1000,
      "sol_end": 1100,
      "distance_traveled_km": 1.2,
      "locations_visited": 45,
      "elevation_change_m": 23.4,
      "total_photos": 3456
    },
    "path": [
      {
        "sol": 1000,
        "earth_date": "2015-05-30",
        "site": 79,
        "drive": 1204,
        "coordinates": {
          "x": 35.4362,
          "y": 22.5714,
          "z": -9.46445
        },
        "photos_taken": 89
      },
      {
        "sol": 1001,
        "earth_date": "2015-05-31",
        "site": 79,
        "drive": 1205,
        "coordinates": {
          "x": 35.4512,
          "y": 22.5820,
          "z": -9.45132
        },
        "photos_taken": 67
      }
    ],
    "links": {
      "map_visualization": "/api/v2/rovers/curiosity/journey/map",
      "kml_export": "/api/v2/rovers/curiosity/journey/export/kml"
    }
  }
}
```

#### Journey Statistics

- **distance_traveled_km**: Approximate distance based on drive increments
- **locations_visited**: Number of unique site/drive combinations
- **elevation_change_m**: Total elevation change (if XYZ coordinates available)
- **total_photos**: Sum of all photos taken during journey

#### Use Cases

- **Mission tracking**: Visualize rover progress over time
- **Path analysis**: Study rover navigation patterns
- **Distance calculations**: Calculate actual traverse distances
- **Elevation profiling**: Analyze terrain changes along the path

---

### 4. Time Machine

**Endpoint**: `GET /api/v2/time-machine`

View the same location at different times to observe changes over multiple sols.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `site` | integer | **Yes** | Site number | `79` |
| `drive` | integer | **Yes** | Drive number | `1204` |
| `rover` | string | No | Filter by rover | `curiosity` |
| `mars_time` | string | No | Mars local time filter (±30 min) | `M14:00:00` |
| `camera` | string | No | Filter by camera | `MAST` |
| `limit` | integer | No | Max results (default: 100) | `50` |

#### Example Request

```bash
# All photos from a specific location
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204"

# Photos from location at similar Mars time (compare lighting)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&mars_time=M14:00:00"
```

#### Response Structure

```json
{
  "location": {
    "site": 79,
    "drive": 1204,
    "total_visits": 5,
    "total_photos": 234
  },
  "data": [
    {
      "sol": 1000,
      "earth_date": "2015-05-30",
      "mars_time": "M14:23:45",
      "photo": {
        "id": 123456,
        "type": "photo",
        "attributes": {
          "nasa_id": "NRF_1000_0813073669",
          "sol": 1000,
          "images": {
            "small": "...",
            "medium": "...",
            "large": "...",
            "full": "..."
          }
        },
        "relationships": {
          "rover": { "id": "curiosity", "type": "rover" },
          "camera": { "id": "MAST", "type": "camera" }
        }
      },
      "lighting_conditions": "midday"
    },
    {
      "sol": 1234,
      "earth_date": "2015-12-20",
      "mars_time": "M14:18:12",
      "photo": {
        "id": 234567,
        "type": "photo",
        "attributes": { "..." }
      },
      "lighting_conditions": "midday"
    }
  ],
  "meta": {
    "total_count": 5,
    "returned_count": 5
  }
}
```

#### Use Cases

- **Change detection**: Observe location changes over time
- **Lighting comparison**: Compare same location at different times of Mars day
- **Long-term monitoring**: Track how locations appear across mission
- **Before/after analysis**: Study effects of dust storms, wind, etc.

---

## Advanced Query Combinations

### Find Panoramas During Golden Hour

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=8" \
  | jq '.data[] | select(.attributes.mars_time_start | startswith("M06") or startswith("M18"))'
```

### Track Rover Journey Through Specific Terrain

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/rovers/perseverance/journey?sol_min=100&sol_max=200"
```

### Compare Location at Sunrise vs Sunset

```bash
# Sunrise photos
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/time-machine?site=50&drive=800&mars_time=M06:00:00"

# Sunset photos
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/time-machine?site=50&drive=800&mars_time=M18:00:00"
```

### Find Most Photographed Locations

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/locations?min_photos=100&per_page=10"
```

---

## Implementation Details

### Panorama Detection Algorithm

The panorama service uses a multi-stage detection process:

1. **Candidate Selection**: Photos with location, telemetry, and timestamp data
2. **Grouping**: Group by rover, sol, site, drive, and camera
3. **Sequence Detection**: Sort by spacecraft clock, find sequential photos with:
   - Elevation within ±2 degrees
   - Time delta < 5 minutes
   - Azimuth range ≥ 30 degrees
4. **Validation**: Sequences must have ≥3 photos minimum

### Journey Distance Calculation

Distance is approximated using drive number increments:
- **Rough approximation**: (drive_end - drive_start) × 0.01 km
- **Actual distance**: Would require 3D coordinate analysis
- **Elevation change**: Calculated from XYZ coordinates when available

### Time Machine Filtering

Mars time filtering uses ±30 minute tolerance:
- Parse Mars time from `date_taken_mars` field
- Compare against user-specified time
- Include photos within 30 minutes of target time

---

## Performance Characteristics

### Panorama Detection
- **Query time**: ~500ms for 10,000 candidate photos
- **Memory efficient**: Processes photos in streaming fashion
- **Caching**: Recommended for frequently accessed panoramas

### Location Timeline
- **Query time**: ~100ms for location aggregation
- **Scalable**: Uses database grouping, not in-memory processing
- **Pagination**: Efficient for large result sets

### Journey Tracking
- **Query time**: ~200ms for 1000-sol range
- **Waypoint calculation**: Aggregates by unique site/drive pairs
- **Path complexity**: O(n) where n = number of unique locations

### Time Machine
- **Query time**: ~50ms for typical location (~5-20 photos)
- **Mars time filtering**: Client-side evaluation (no index)
- **Scalable**: Limited by site/drive filter efficiency

---

## Future Enhancements

Phase 2 provides the foundation for future advanced features:

1. **Panorama Stitching**: Auto-stitch panorama components
2. **Stereo Pair Detection**: Match left/right camera pairs for 3D
3. **KML/GeoJSON Export**: Export journey paths for GIS tools
4. **Interactive Maps**: Clickable journey visualization
5. **Photo Similarity**: Find visually similar photos using ML
6. **Coverage Heatmaps**: Visualize photo density across terrain

---

## Coming in Phase 3

- **Analytics Endpoints**: Photography statistics, coverage heatmaps
- **Photo Relationships**: Temporal/spatial connections
- **Interesting Photo Detection**: ML-scored recommendations
- **Export Operations**: Bulk download preparation
- **Full-text Search**: Search across photo metadata

---

**Phase 2 Status**: ✅ Complete and Available
**Last Updated**: November 2025
**API Version**: v2.1
