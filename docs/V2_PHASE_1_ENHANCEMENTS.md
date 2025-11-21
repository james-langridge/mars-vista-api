# API v2 Phase 1 Enhancements

## Overview

Phase 1 of the v2 API redesign introduces revolutionary query capabilities that leverage the **full NASA data** we're storing (100% of fields vs the original API's 5%). These enhancements transform the API from a simple photo listing service into a powerful Mars exploration data platform.

## What's New

### 1. Enhanced Resource Structure

Photos now return with rich, nested objects instead of flat fields:

```json
{
  "id": 123456,
  "type": "photo",
  "attributes": {
    "nasa_id": "NRF_1646_0813073669",
    "sol": 1000,
    "earth_date": "2015-05-30",
    "date_taken_utc": "2015-05-30T10:23:45Z",
    "date_taken_mars": "Sol-1000M14:23:45",

    "images": {
      "small": "https://mars.nasa.gov/.../320.jpg",
      "medium": "https://mars.nasa.gov/.../800.jpg",
      "large": "https://mars.nasa.gov/.../1200.jpg",
      "full": "https://mars.nasa.gov/.../full.png"
    },

    "dimensions": {
      "width": 1920,
      "height": 1080
    },

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
      "mast_elevation": -10.1652,
      "spacecraft_clock": 813073669.716
    },

    "sample_type": "Full",
    "title": "Mars Perseverance Sol 1000",
    "caption": "Looking across Jezero Crater",
    "credit": "NASA/JPL-Caltech"
  }
}
```

### 2. Mars Time Filtering

Query photos by Mars local solar time to find images taken during specific lighting conditions.

#### Mars Time Range

```bash
# Photos taken during Mars morning (6-9 AM local time)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mars_time_min=M06:00:00&mars_time_max=M09:00:00"

# Photos taken during Mars sunset (18-19 PM local time)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mars_time_min=M18:00:00&mars_time_max=M19:00:00"
```

#### Golden Hour Photography

```bash
# Photos taken during Mars golden hour (sunrise/sunset)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mars_time_golden_hour=true&rovers=curiosity"
```

**Mars Time Format:**
- `M06:00:00` = 6:00 AM Mars local time
- `M14:30:00` = 2:30 PM Mars local time
- Golden hour: ~5:30-7:30 AM and ~5:30-7:30 PM Mars time

### 3. Location-Based Queries

Search for photos by rover location using site/drive coordinates.

#### Exact Location

```bash
# Photos from a specific location
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204"
```

#### Location Range

```bash
# Photos across a range of sites
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site_min=70&site_max=80"

# Photos across a drive range at specific site
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site=79&drive_min=1200&drive_max=1250"
```

#### Proximity Search

```bash
# Photos within 5 drives of a location
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204&location_radius=5"
```

**Use Cases:**
- Find all photos from a geologically interesting site
- Track rover's journey through specific terrain
- Compare photos from nearby locations
- Build virtual tours of Mars locations

### 4. Image Quality Filters

Filter photos by dimensions, aspect ratio, and sample type.

#### Dimension Filtering

```bash
# High-resolution images only (1920x1080 or larger)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?min_width=1920&min_height=1080"

# Square or near-square images
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?min_width=1024&max_width=1280&min_height=1024&max_height=1280"
```

#### Aspect Ratio

```bash
# 16:9 widescreen images
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?aspect_ratio=16:9"

# 4:3 traditional format
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?aspect_ratio=4:3"
```

#### Sample Type

```bash
# Full quality images only (not thumbnails)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?sample_type=Full"

# Multiple sample types
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?sample_type=Full,Subframe"
```

**Sample Types:**
- `Full`: Full resolution images
- `Thumbnail`: Downsampled thumbnails
- `Subframe`: Partial frame captures
- `Downsampled`: Reduced resolution versions

### 5. Camera Angle Queries

Filter photos by camera pointing direction using mast angles.

#### Looking at the Horizon

```bash
# Photos with camera pointed at horizon (±5 degrees)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_min=-5&mast_elevation_max=5"
```

#### Looking at the Ground

```bash
# Photos pointing downward (useful for terrain analysis)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_max=-30"
```

#### Looking at the Sky

```bash
# Photos pointing upward
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_min=30"
```

#### Directional Queries (Azimuth)

```bash
# Photos looking east (90-180 degrees)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?mast_azimuth_min=90&mast_azimuth_max=180"
```

**Angle Ranges:**
- **Elevation**: -90° (straight down) to +90° (straight up)
- **Azimuth**: 0°-360° (compass direction)
- Horizon views: elevation near 0°
- Ground surveys: elevation < -30°
- Sky observations: elevation > 30°

### 6. Field Set Control

Control response size and complexity with predefined field sets.

#### Field Set Options

```bash
# Minimal: Just essentials (id, sol, images.medium)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=minimal"

# Standard: Basic fields (default)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=standard"

# Extended: Include location, dimensions, Mars time
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=extended"

# Scientific: All telemetry and coordinates
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=scientific"

# Complete: Everything including raw NASA data
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?field_set=complete"
```

#### Image Size Control

```bash
# Only specific image sizes to reduce bandwidth
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?image_sizes=medium,large"

# Exclude images entirely (metadata only)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?exclude_images=true"
```

## Combined Query Examples

The real power comes from combining multiple filters:

### Example 1: High-Quality Sunrise Photography

```bash
# Curiosity's best sunrise photos: high-res, golden hour, looking at horizon
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?\
rovers=curiosity&\
mars_time_golden_hour=true&\
min_width=1920&\
sample_type=Full&\
mast_elevation_min=-5&\
mast_elevation_max=5"
```

### Example 2: Location Survey

```bash
# All high-quality photos from a specific site
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?\
site=79&\
drive_min=1200&\
drive_max=1250&\
min_width=1024&\
sample_type=Full"
```

### Example 3: Panorama Components

```bash
# Photos likely part of panoramas (sequential, same location, horizon view)
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?\
rovers=perseverance&\
site=50&\
drive=800&\
mast_elevation_min=-5&\
mast_elevation_max=5&\
min_width=1920"
```

### Example 4: Scientific Dataset

```bash
# Ground survey data for terrain analysis
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v2/photos?\
rovers=curiosity&\
sol_min=3000&\
sol_max=3100&\
mast_elevation_max=-20&\
field_set=scientific"
```

## Query Parameters Reference

### Basic Filters (Existing)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `rovers` | string | Comma-separated rover names | `curiosity,perseverance` |
| `cameras` | string | Comma-separated camera names | `FHAZ,MAST,NAVCAM` |
| `sol` | integer | Exact sol | `1000` |
| `sol_min` | integer | Minimum sol (inclusive) | `1000` |
| `sol_max` | integer | Maximum sol (inclusive) | `2000` |
| `earth_date` | string | Exact Earth date (YYYY-MM-DD) | `2023-01-15` |
| `date_min` | string | Minimum Earth date | `2023-01-01` |
| `date_max` | string | Maximum Earth date | `2023-12-31` |

### Mars Time Filters (NEW)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `mars_time_min` | string | Minimum Mars local time | `M06:00:00` |
| `mars_time_max` | string | Maximum Mars local time | `M18:00:00` |
| `mars_time_golden_hour` | boolean | Filter for golden hour photos | `true` |

### Location Filters (NEW)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `site` | integer | Exact site number | `79` |
| `site_min` | integer | Minimum site number | `70` |
| `site_max` | integer | Maximum site number | `80` |
| `drive` | integer | Exact drive number | `1204` |
| `drive_min` | integer | Minimum drive number | `1200` |
| `drive_max` | integer | Maximum drive number | `1250` |
| `location_radius` | integer | Proximity radius in drives (requires `site` and `drive`) | `5` |

### Image Quality Filters (NEW)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `min_width` | integer | Minimum image width in pixels | `1920` |
| `max_width` | integer | Maximum image width in pixels | `2560` |
| `min_height` | integer | Minimum image height in pixels | `1080` |
| `max_height` | integer | Maximum image height in pixels | `1440` |
| `aspect_ratio` | string | Aspect ratio (width:height) | `16:9` |
| `sample_type` | string | Comma-separated sample types | `Full,Subframe` |

### Camera Angle Filters (NEW)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `mast_elevation_min` | float | Minimum mast elevation in degrees (-90 to 90) | `-5` |
| `mast_elevation_max` | float | Maximum mast elevation in degrees | `5` |
| `mast_azimuth_min` | float | Minimum mast azimuth in degrees (0 to 360) | `90` |
| `mast_azimuth_max` | float | Maximum mast azimuth in degrees | `180` |

### Response Control (NEW)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `field_set` | string | Predefined field set (minimal, standard, extended, scientific, complete) | `extended` |
| `image_sizes` | string | Comma-separated image sizes to include | `medium,large,full` |
| `exclude_images` | boolean | Exclude all image URLs (metadata only) | `true` |

### Pagination & Sorting (Existing)

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `page` | integer | Page number (1-indexed) | `2` |
| `per_page` | integer | Items per page (1-100, default: 25) | `50` |
| `sort` | string | Sort fields (prefix with `-` for descending) | `-earth_date,camera` |
| `fields` | string | Specific fields to include (custom field selection) | `id,sol,images` |
| `include` | string | Related resources to include | `rover,camera` |

## Benefits Over Original NASA API

### What NASA API Provides

- Single image URL
- Sol and Earth date
- Camera name
- Basic rover info

### What Our v2 API Adds

1. **Multiple image sizes** → Progressive loading, bandwidth optimization
2. **Mars local time** → Sunrise/sunset/golden hour queries
3. **Location data** → Journey visualization, proximity search
4. **Camera angles** → Panorama detection, directional queries
5. **Image dimensions** → Quality filtering, aspect ratio matching
6. **Telemetry data** → Scientific analysis, panorama stitching
7. **Nested structure** → Cleaner, more semantic responses
8. **Rich metadata** → Complete NASA data accessible

## Response Structure

Phase 1 introduces nested objects for better organization:

```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "nasa_id": "NRF_1646_0813073669",
        "sol": 1000,
        "earth_date": "2015-05-30",
        "date_taken_utc": "2015-05-30T10:23:45Z",
        "date_taken_mars": "Sol-1000M14:23:45",

        "images": {
          "small": "...",
          "medium": "...",
          "large": "...",
          "full": "..."
        },

        "dimensions": {
          "width": 1920,
          "height": 1080
        },

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
          "mast_elevation": -10.1652,
          "spacecraft_clock": 813073669.716
        },

        "sample_type": "Full",
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
          "id": "MAST",
          "type": "camera",
          "attributes": {
            "full_name": "Mast Camera"
          }
        }
      }
    }
  ],
  "meta": {
    "total_count": 15234,
    "returned_count": 1
  },
  "pagination": {
    "page": 1,
    "per_page": 1,
    "total_pages": 15234
  },
  "links": {
    "self": "...",
    "next": "...",
    "first": "...",
    "last": "..."
  }
}
```

## Performance Notes

- **Mars time filtering**: Uses client-side evaluation (parse DateTakenMars field)
- **Aspect ratio matching**: Uses 5% tolerance for floating-point comparisons
- **Location proximity**: Optimized with site equality + drive range
- **Recommended page size**: 25-50 items for rich queries with multiple filters

## Coming in Phase 2

- **Panorama detection**: Auto-detected panoramic sequences
- **Stereo pairs**: Matched left/right camera pairs
- **Journey visualization**: Rover path tracking with elevation changes
- **Time machine**: Same location across different sols
- **Location timeline**: All visits to specific sites

## Coming in Phase 3

- **Analytics endpoints**: Photography statistics, coverage heatmaps
- **Photo relationships**: Temporal/spatial connections
- **Interesting photo detection**: ML-scored recommendations
- **Export operations**: Bulk download preparation

---

**Phase 1 Status**: ✅ Complete and Available
**Last Updated**: November 2025
**API Version**: v2.0
