# Story 012: API v2 Redesign - Enhanced Data Edition

## Context

We have the FULL NASA data stored (100% of fields in JSONB + rich indexed columns), not just the 5% the original API exposed. Our v2 API can offer revolutionary features never before available in any Mars photo API.

**Data Available:**
- 4 image sizes (small/medium/large/full)
- Mars local time for each photo
- Location data (site/drive/xyz coordinates)
- Camera telemetry (mast angles for panoramas)
- Image dimensions and quality metrics
- Complete NASA metadata in JSONB

## Requirements

### 1. Enhanced Core Photos Endpoint

**Standard fields now include rich metadata:**
```json
// GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1010
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        // Basic fields
        "nasa_id": "NRF_1646_0813073669",
        "sol": 1000,
        "earth_date": "2015-05-30",
        "date_taken_utc": "2015-05-30T10:23:45Z",
        "date_taken_mars": "Sol-1000M14:23:45",  // NEW: Mars local time

        // Multiple image sizes (NEW)
        "images": {
          "small": "https://mars.nasa.gov/.../320.jpg",
          "medium": "https://mars.nasa.gov/.../800.jpg",
          "large": "https://mars.nasa.gov/.../1200.jpg",
          "full": "https://mars.nasa.gov/.../full.png"
        },

        // Image properties (NEW)
        "dimensions": {
          "width": 1920,
          "height": 1080
        },
        "sample_type": "Full",  // Full, Thumbnail, Subframe

        // Location data (NEW)
        "location": {
          "site": 79,
          "drive": 1204,
          "coordinates": {
            "x": 35.4362,
            "y": 22.5714,
            "z": -9.46445
          }
        },

        // Camera telemetry (NEW)
        "telemetry": {
          "mast_azimuth": 156.098,
          "mast_elevation": -10.1652,
          "spacecraft_clock": 813073669.716
        },

        // Metadata
        "title": "Mars Perseverance Sol 1000",
        "caption": "Looking across Jezero Crater",
        "credit": "NASA/JPL-Caltech"
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "type": "rover"
        },
        "camera": {
          "id": "mast",
          "type": "camera",
          "attributes": {
            "full_name": "Mast Camera"
          }
        }
      },
      "meta": {
        // Photo-specific metadata
        "is_panorama_part": true,
        "panorama_sequence_id": "seq_1000_14",
        "has_stereo_pair": true,
        "stereo_pair_id": 123457
      }
    }
  ],
  "meta": {
    "total_count": 15234,
    "locations_covered": 12,
    "date_range": {
      "earth": ["2015-05-30", "2015-06-09"],
      "mars": ["Sol-1000", "Sol-1010"],
      "mars_time_range": ["M06:00:00", "M18:30:00"]
    }
  }
}
```

### 2. Revolutionary Query Capabilities

**Mars Time Queries:**
```
# Photos during Mars sunrise (6-7 AM Mars time)
GET /api/v2/photos?mars_time_min=M06:00:00&mars_time_max=M07:00:00

# Photos at Mars noon
GET /api/v2/photos?mars_time_min=M12:00:00&mars_time_max=M13:00:00

# Golden hour photography
GET /api/v2/photos?mars_time_golden_hour=true
```

**Location-Based Queries:**
```
# Photos at specific location
GET /api/v2/photos?site=79&drive=1204

# Photos near a location (radius in drives)
GET /api/v2/photos?site=79&drive=1204&location_radius=5

# Photos along rover's journey
GET /api/v2/photos?site_min=70&site_max=80
```

**Image Quality Filters:**
```
# High resolution only
GET /api/v2/photos?min_width=1920&min_height=1080

# Full quality images only (not thumbnails)
GET /api/v2/photos?sample_type=Full

# Images with specific dimensions
GET /api/v2/photos?aspect_ratio=16:9
```

**Camera Angle Queries:**
```
# Looking at horizon
GET /api/v2/photos?mast_elevation_min=-5&mast_elevation_max=5

# Looking down at ground
GET /api/v2/photos?mast_elevation_max=-30

# Specific azimuth range (compass direction)
GET /api/v2/photos?mast_azimuth_min=90&mast_azimuth_max=180
```

### 3. Advanced Feature Endpoints

**Panorama Detection & Retrieval:**
```
GET /api/v2/panoramas
{
  "data": [
    {
      "id": "pano_curiosity_1000_14",
      "type": "panorama",
      "attributes": {
        "rover": "curiosity",
        "sol": 1000,
        "mars_time_start": "Sol-1000M14:23:00",
        "mars_time_end": "Sol-1000M14:28:00",
        "total_photos": 12,
        "coverage_degrees": 360,
        "location": { "site": 79, "drive": 1204 }
      },
      "photos": [/* ordered array of photos */],
      "links": {
        "stitched_preview": "/api/v2/panoramas/pano_curiosity_1000_14/preview",
        "download_set": "/api/v2/panoramas/pano_curiosity_1000_14/download"
      }
    }
  ]
}
```

**Stereo Pair Detection:**
```
GET /api/v2/stereo-pairs
{
  "data": [
    {
      "id": "stereo_123456",
      "type": "stereo_pair",
      "left_photo": { /* photo object */ },
      "right_photo": { /* photo object */ },
      "time_delta_seconds": 0.5,
      "baseline_meters": 0.2,
      "links": {
        "anaglyph": "/api/v2/stereo-pairs/stereo_123456/anaglyph",
        "depth_map": "/api/v2/stereo-pairs/stereo_123456/depth"
      }
    }
  ]
}
```

**Location Timeline (Virtual Tourism):**
```
GET /api/v2/locations
{
  "data": [
    {
      "site": 79,
      "drive": 1204,
      "name": "Jezero Crater Overlook",
      "first_visited": "2023-05-15",
      "last_visited": "2023-05-18",
      "photo_count": 234,
      "coordinates": { "x": 35.4, "y": 22.5, "z": -9.4 },
      "links": {
        "photos": "/api/v2/photos?site=79&drive=1204",
        "360_view": "/api/v2/locations/79/1204/360",
        "time_lapse": "/api/v2/locations/79/1204/timelapse"
      }
    }
  ]
}
```

**Journey Tracking:**
```
GET /api/v2/rovers/curiosity/journey?sol_min=1000&sol_max=2000
{
  "data": {
    "distance_traveled_km": 2.34,
    "locations_visited": 45,
    "elevation_change_m": 123,
    "path": [
      {
        "sol": 1000,
        "site": 79,
        "drive": 1204,
        "coordinates": { "x": 35.4, "y": 22.5, "z": -9.4 },
        "photos_taken": 89
      }
    ],
    "links": {
      "map_visualization": "/api/v2/rovers/curiosity/journey/map",
      "kml_export": "/api/v2/rovers/curiosity/journey/export/kml"
    }
  }
}
```

**Time Machine (Same Location, Different Times):**
```
GET /api/v2/time-machine?site=79&drive=1204&mars_time=M14:00:00
{
  "data": [
    {
      "sol": 1000,
      "earth_date": "2015-05-30",
      "photo": { /* photo taken at ~14:00 Mars time */ }
    },
    {
      "sol": 1234,
      "earth_date": "2015-12-20",
      "photo": { /* photo taken at ~14:00 Mars time */ }
    }
  ]
}
```

### 4. Analytics Endpoints

**Photography Statistics:**
```
GET /api/v2/analytics/photography
{
  "data": {
    "by_time_of_day": {
      "morning": 23456,  // 06:00-12:00 Mars time
      "afternoon": 34567, // 12:00-18:00
      "evening": 12345    // 18:00-06:00
    },
    "by_camera_angle": {
      "horizon": 15234,   // -5 to +5 degrees
      "ground": 8765,     // < -30 degrees
      "sky": 3456         // > +30 degrees
    },
    "by_quality": {
      "full": 45678,
      "thumbnail": 12345,
      "subframe": 5678
    },
    "average_photo_size": {
      "width": 1654,
      "height": 1232
    }
  }
}
```

**Coverage Heatmap:**
```
GET /api/v2/analytics/coverage-map
{
  "data": {
    "grid_size_meters": 100,
    "coverage": [
      {
        "grid_x": 10,
        "grid_y": 20,
        "photo_count": 234,
        "last_visited_sol": 2456,
        "total_visits": 5
      }
    ]
  }
}
```

### 5. Discovery & Recommendations

**Interesting Photos (ML-Scored):**
```
GET /api/v2/discover/interesting
{
  "data": [
    {
      "photo": { /* photo object */ },
      "interest_score": 95.2,
      "reasons": [
        "Unique location - first photos from this site",
        "Golden hour lighting (Mars sunset)",
        "Rarely used camera angle",
        "High resolution panorama component"
      ]
    }
  ]
}
```

**Photo Relationships:**
```
GET /api/v2/photos/123456/related
{
  "data": {
    "temporal": {
      "previous": { /* photo taken just before */ },
      "next": { /* photo taken just after */ }
    },
    "spatial": {
      "same_location": [/* other photos at this site/drive */],
      "nearby": [/* photos within 5 drives */]
    },
    "panorama": {
      "sequence": [/* if part of panorama */],
      "position": 3,
      "total": 12
    },
    "stereo": {
      "pair": { /* matching stereo photo if exists */ }
    }
  }
}
```

### 6. Export & Batch Operations

**Bulk Download Preparation:**
```
POST /api/v2/exports
{
  "filters": {
    "rovers": ["curiosity"],
    "sol_min": 1000,
    "sol_max": 1100,
    "sample_type": "Full"
  },
  "format": "zip",
  "image_size": "large",
  "include_metadata": true
}

Response:
{
  "job_id": "export_abc123",
  "status": "processing",
  "estimated_size_gb": 2.3,
  "estimated_photos": 1234,
  "links": {
    "status": "/api/v2/exports/export_abc123/status",
    "download": "/api/v2/exports/export_abc123/download"
  }
}
```

### 7. Field Selection & Performance

**Sparse Fieldsets:**
```
# Minimal data for gallery
GET /api/v2/photos?fields=id,images.medium,sol

# Scientific data only
GET /api/v2/photos?fields=nasa_id,telemetry,location,date_taken_mars

# Everything except raw_data
GET /api/v2/photos?exclude=raw_data
```

**Response Size Control:**
```
# Only specific image sizes
GET /api/v2/photos?image_sizes=medium,large

# Exclude images entirely (metadata only)
GET /api/v2/photos?exclude_images=true
```

## Implementation Phases

### Phase 1: Core Enhanced Endpoints (Week 1)
1. Enhanced photos endpoint with all rich fields
2. Mars time filtering
3. Location-based queries
4. Image quality filters
5. Multiple image size support

### Phase 2: Advanced Features (Week 2)
1. Panorama detection and endpoints
2. Stereo pair detection
3. Location timeline
4. Journey tracking
5. Time machine feature

### Phase 3: Analytics & Discovery (Week 3)
1. Photography statistics
2. Coverage heatmap
3. Interesting photo detection
4. Photo relationships
5. Export/batch operations

## Technical Decisions

### 1. Default Field Sets
```csharp
public enum FieldSet
{
    Minimal,    // id, sol, images.medium
    Standard,   // + earth_date, camera, rover
    Extended,   // + location, dimensions, mars_time
    Full,       // Everything except raw_data
    Complete    // Everything including raw_data
}
```

### 2. Query Optimization
- Use PostgreSQL's JSONB indexes for raw_data queries
- Materialized views for panorama/stereo detection
- Spatial indexes for location queries
- Time-based partitioning for large datasets

### 3. Response Caching
- Inactive rovers: 1 year cache
- Active rovers: 1 hour for queries, 5 minutes for latest
- Panorama/stereo detection: 1 day cache
- Analytics: 6 hour cache

## Benefits Over Original API

### Original NASA API Provides
- Single image URL
- Sol/Earth date
- Camera name
- Basic rover info

### Our v2 API Adds
- 4 image sizes for progressive loading
- Mars local time (sunrise/sunset queries!)
- Location tracking (journey visualization!)
- Camera angles (panorama detection!)
- Image dimensions (quality filtering!)
- Photo relationships (temporal/spatial/panoramic)
- Analytics and insights
- Bulk operations

## Success Metrics

1. **Query Performance**
   - < 100ms for filtered queries
   - < 500ms for panorama detection
   - < 1s for analytics endpoints

2. **Data Richness**
   - 20x more queryable fields than NASA API
   - 100% of NASA data accessible via raw_data
   - Multiple image sizes reduce bandwidth by 60%

3. **Developer Experience**
   - Self-documenting API
   - Consistent patterns
   - Rich filtering options
   - Field selection for optimization

## Migration Notes

**From NASA API:**
```
NASA: GET /mars-photos/api/v1/rovers/curiosity/photos?sol=1000
Ours: GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000
```

**From our v1:**
```
v1: GET /api/v1/rovers/curiosity/photos?sol=1000
v2: GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000&fields=extended
```

## Why This Design?

With 100% of NASA data stored, we can offer features that would require:
- Multiple NASA API calls (we do it in one)
- Complex client-side processing (we do it server-side)
- External data correlation (we have it integrated)
- Machine learning analysis (we can add it)

This isn't just a Mars photo API - it's a Mars exploration data platform.