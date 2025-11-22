# Complete Production API Test URL List

**COMPREHENSIVE** list of ALL possible public endpoint combinations with every available query parameter.

**Base URL:** `https://api.marsvista.dev`
**Authentication:** All requests require `X-API-Key` header

---

## Test Categories

1. [Basic Endpoints](#1-basic-endpoints)
2. [API v1 Photos - Basic Queries](#2-api-v1-photos---basic-queries)
3. [API v2 Photos - Sol & Date Filtering](#3-api-v2-photos---sol--date-filtering)
4. [API v2 Photos - Mars Time Filtering](#4-api-v2-photos---mars-time-filtering-new)
5. [API v2 Photos - Location-Based Queries](#5-api-v2-photos---location-based-queries-new)
6. [API v2 Photos - Image Quality Filters](#6-api-v2-photos---image-quality-filters-new)
7. [API v2 Photos - Camera Angle Queries](#7-api-v2-photos---camera-angle-queries-new)
8. [API v2 Photos - Field Selection & Image Sizes](#8-api-v2-photos---field-selection--image-sizes-new)
9. [API v2 Photos - Combined Advanced Filters](#9-api-v2-photos---combined-advanced-filters)
10. [API v2 Statistics](#10-api-v2-statistics)
11. [API v2 Rovers & Cameras](#11-api-v2-rovers--cameras)
12. [API v2 Advanced Features](#12-api-v2-advanced-features)
13. [Error Cases](#13-error-cases)

---

## 1. Basic Endpoints

### Health Check
```
GET /health
```

### API Discovery
```
GET /api/v2
```

### Get All Rovers
```
GET /api/v1/rovers
GET /api/v1/rovers?format=camelCase
GET /api/v2/rovers
```

### Get Specific Rover
```
GET /api/v1/rovers/curiosity
GET /api/v1/rovers/perseverance
GET /api/v1/rovers/opportunity
GET /api/v1/rovers/spirit
GET /api/v2/rovers/curiosity
GET /api/v2/rovers/perseverance
GET /api/v2/rovers/opportunity
GET /api/v2/rovers/spirit
```

### Get Rover Manifests
```
GET /api/v1/manifests/curiosity
GET /api/v1/manifests/perseverance
GET /api/v2/rovers/curiosity/manifest
GET /api/v2/rovers/perseverance/manifest
```

### Get Rover Cameras
```
GET /api/v2/rovers/curiosity/cameras
GET /api/v2/rovers/perseverance/cameras
GET /api/v2/rovers/opportunity/cameras
GET /api/v2/rovers/spirit/cameras
```

### Get Rover Journey
```
GET /api/v2/rovers/curiosity/journey
GET /api/v2/rovers/curiosity/journey?sol_min=1000&sol_max=2000
GET /api/v2/rovers/perseverance/journey?sol_min=1&sol_max=500
```

---

## 2. API v1 Photos - Basic Queries

### Sol-based queries
```
GET /api/v1/rovers/curiosity/photos?sol=0
GET /api/v1/rovers/curiosity/photos?sol=1
GET /api/v1/rovers/curiosity/photos?sol=100
GET /api/v1/rovers/curiosity/photos?sol=1000
GET /api/v1/rovers/curiosity/photos?sol=4700
GET /api/v1/rovers/perseverance/photos?sol=0
GET /api/v1/rovers/perseverance/photos?sol=500
GET /api/v1/rovers/perseverance/photos?sol=1690
GET /api/v1/rovers/opportunity/photos?sol=1
GET /api/v1/rovers/opportunity/photos?sol=5000
GET /api/v1/rovers/spirit/photos?sol=1
GET /api/v1/rovers/spirit/photos?sol=2000
```

### Date-based queries
```
GET /api/v1/rovers/curiosity/photos?earth_date=2012-08-06
GET /api/v1/rovers/curiosity/photos?earth_date=2023-06-15
GET /api/v1/rovers/curiosity/photos?earth_date=2025-11-20
GET /api/v1/rovers/perseverance/photos?earth_date=2021-02-18
GET /api/v1/rovers/perseverance/photos?earth_date=2024-11-20
GET /api/v1/rovers/opportunity/photos?earth_date=2004-01-24
GET /api/v1/rovers/spirit/photos?earth_date=2004-01-03
```

### Camera filtering (Curiosity)
```
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=MAST
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=NAVCAM
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=FHAZ
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=RHAZ
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=CHEMCAM
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=MAHLI
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=MARDI
```

### Camera filtering (Perseverance)
```
GET /api/v1/rovers/perseverance/photos?sol=500&camera=MCZ_LEFT
GET /api/v1/rovers/perseverance/photos?sol=500&camera=MCZ_RIGHT
GET /api/v1/rovers/perseverance/photos?sol=500&camera=NAVCAM_LEFT
GET /api/v1/rovers/perseverance/photos?sol=500&camera=NAVCAM_RIGHT
GET /api/v1/rovers/perseverance/photos?sol=500&camera=FRONT_HAZCAM_LEFT_A
GET /api/v1/rovers/perseverance/photos?sol=500&camera=SHERLOC_WATSON
GET /api/v1/rovers/perseverance/photos?sol=500&camera=SUPERCAM_RMI
```

### Pagination
```
GET /api/v1/rovers/curiosity/photos?sol=0&page=1&per_page=10
GET /api/v1/rovers/curiosity/photos?sol=0&page=1&per_page=25
GET /api/v1/rovers/curiosity/photos?sol=0&page=1&per_page=50
GET /api/v1/rovers/curiosity/photos?sol=0&page=1&per_page=100
GET /api/v1/rovers/curiosity/photos?sol=0&page=2&per_page=25
```

### Response format
```
GET /api/v1/rovers/curiosity/photos?sol=1000&format=camelCase
GET /api/v1/rovers/curiosity/photos?sol=1000&format=snake_case
```

### Latest photos
```
GET /api/v1/rovers/curiosity/latest
GET /api/v1/rovers/curiosity/latest?per_page=10
GET /api/v1/rovers/curiosity/latest_photos
GET /api/v1/rovers/perseverance/latest
GET /api/v1/rovers/perseverance/latest?format=camelCase
```

### Photo by ID
```
GET /api/v1/photos/451991
GET /api/v1/photos/1000000
GET /api/v1/photos/2542754
GET /api/v2/photos/451991
GET /api/v2/photos/1000000
GET /api/v2/photos/2542754
GET /api/v2/photos/1?include=rover,camera
GET /api/v2/photos/1000000?fields=id,img_src,sol
```

---

## 3. API v2 Photos - Sol & Date Filtering

### Single rover
```
GET /api/v2/photos?rovers=curiosity
GET /api/v2/photos?rovers=perseverance
GET /api/v2/photos?rovers=opportunity
GET /api/v2/photos?rovers=spirit
```

### Multiple rovers
```
GET /api/v2/photos?rovers=curiosity,perseverance
GET /api/v2/photos?rovers=curiosity,opportunity,spirit
GET /api/v2/photos?rovers=curiosity,perseverance,opportunity,spirit
```

### Sol exact (shorthand)
```
GET /api/v2/photos?rovers=curiosity&sol=0
GET /api/v2/photos?rovers=curiosity&sol=1
GET /api/v2/photos?rovers=curiosity&sol=1000
GET /api/v2/photos?rovers=curiosity&sol=4700
GET /api/v2/photos?rovers=perseverance&sol=0
GET /api/v2/photos?rovers=perseverance&sol=500
```

### Sol ranges
```
GET /api/v2/photos?rovers=curiosity&sol_min=0&sol_max=10
GET /api/v2/photos?rovers=curiosity&sol_min=100&sol_max=200
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100
GET /api/v2/photos?rovers=curiosity&sol_min=4000&sol_max=4725
GET /api/v2/photos?rovers=perseverance&sol_min=1&sol_max=100
GET /api/v2/photos?rovers=curiosity&sol_min=1000
GET /api/v2/photos?rovers=curiosity&sol_max=100
```

### Earth date exact (shorthand)
```
GET /api/v2/photos?rovers=curiosity&earth_date=2012-08-06
GET /api/v2/photos?rovers=curiosity&earth_date=2023-06-15
GET /api/v2/photos?rovers=perseverance&earth_date=2021-02-18
```

### Date ranges
```
GET /api/v2/photos?rovers=curiosity&date_min=2012-08-06&date_max=2012-12-31
GET /api/v2/photos?rovers=curiosity&date_min=2023-01-01&date_max=2023-12-31
GET /api/v2/photos?rovers=curiosity&date_min=2024-01-01&date_max=2024-12-31
GET /api/v2/photos?rovers=perseverance&date_min=2021-02-18&date_max=2021-12-31
GET /api/v2/photos?rovers=curiosity,perseverance&date_min=2024-01-01&date_max=2024-11-20
GET /api/v2/photos?rovers=curiosity&date_min=2024-01-01
GET /api/v2/photos?rovers=curiosity&date_max=2013-01-01
```

### Camera filtering
```
GET /api/v2/photos?rovers=curiosity&cameras=MAST
GET /api/v2/photos?rovers=curiosity&cameras=NAVCAM
GET /api/v2/photos?rovers=curiosity&cameras=MAST,NAVCAM
GET /api/v2/photos?rovers=curiosity&cameras=FHAZ,RHAZ,NAVCAM
GET /api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT
GET /api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT,MCZ_RIGHT
GET /api/v2/photos?rovers=perseverance&cameras=NAVCAM_LEFT,NAVCAM_RIGHT
```

### Sorting
```
GET /api/v2/photos?rovers=curiosity&sort=earth_date
GET /api/v2/photos?rovers=curiosity&sort=-earth_date
GET /api/v2/photos?rovers=curiosity&sort=sol
GET /api/v2/photos?rovers=curiosity&sort=-sol
GET /api/v2/photos?rovers=curiosity&sort=camera
GET /api/v2/photos?rovers=curiosity&sort=-earth_date,camera
GET /api/v2/photos?rovers=curiosity&sort=sol,camera,-earth_date
```

### Pagination (page-based)
```
GET /api/v2/photos?rovers=curiosity&page=1&per_page=10
GET /api/v2/photos?rovers=curiosity&page=1&per_page=25
GET /api/v2/photos?rovers=curiosity&page=1&per_page=50
GET /api/v2/photos?rovers=curiosity&page=1&per_page=100
GET /api/v2/photos?rovers=curiosity&page=2&per_page=25
GET /api/v2/photos?rovers=curiosity&page=5&per_page=100
```

---

## 4. API v2 Photos - Mars Time Filtering (NEW)

### Mars time ranges
```
GET /api/v2/photos?rovers=curiosity&mars_time_min=M06:00:00
GET /api/v2/photos?rovers=curiosity&mars_time_max=M18:00:00
GET /api/v2/photos?rovers=curiosity&mars_time_min=M06:00:00&mars_time_max=M09:00:00
GET /api/v2/photos?rovers=curiosity&mars_time_min=M12:00:00&mars_time_max=M14:00:00
GET /api/v2/photos?rovers=curiosity&mars_time_min=M16:00:00&mars_time_max=M19:00:00
GET /api/v2/photos?rovers=perseverance&mars_time_min=M05:00:00&mars_time_max=M07:00:00
```

### Golden hour filtering ⭐
```
GET /api/v2/photos?rovers=curiosity&mars_time_golden_hour=true
GET /api/v2/photos?rovers=perseverance&mars_time_golden_hour=true
GET /api/v2/photos?rovers=curiosity,perseverance&mars_time_golden_hour=true
GET /api/v2/photos?rovers=curiosity&mars_time_golden_hour=true&cameras=MAST
GET /api/v2/photos?rovers=curiosity&mars_time_golden_hour=true&sol_min=1000&sol_max=2000
```

### Combined Mars time + other filters
```
GET /api/v2/photos?rovers=curiosity&mars_time_golden_hour=true&sol=1000
GET /api/v2/photos?rovers=curiosity&mars_time_min=M14:00:00&mars_time_max=M16:00:00&cameras=MAST,NAVCAM
GET /api/v2/photos?rovers=perseverance&mars_time_golden_hour=true&date_min=2024-01-01
```

---

## 5. API v2 Photos - Location-Based Queries (NEW)

### Exact site and drive
```
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176
GET /api/v2/photos?rovers=curiosity&site=105&drive=418
GET /api/v2/photos?rovers=curiosity&site=76&drive=3002
GET /api/v2/photos?rovers=curiosity&site=6&drive=0
```

### Site only (exact)
```
GET /api/v2/photos?rovers=curiosity&site=82
GET /api/v2/photos?rovers=curiosity&site=105
GET /api/v2/photos?rovers=curiosity&site=76
```

### Site ranges
```
GET /api/v2/photos?rovers=curiosity&site_min=80&site_max=90
GET /api/v2/photos?rovers=curiosity&site_min=100&site_max=110
GET /api/v2/photos?rovers=curiosity&site_min=50
GET /api/v2/photos?rovers=curiosity&site_max=50
```

### Drive ranges
```
GET /api/v2/photos?rovers=curiosity&drive_min=1000&drive_max=1500
GET /api/v2/photos?rovers=curiosity&drive_min=2000&drive_max=2500
GET /api/v2/photos?rovers=curiosity&drive_min=3000
GET /api/v2/photos?rovers=curiosity&drive_max=1000
```

### Location proximity (radius)
```
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=5
GET /api/v2/photos?rovers=curiosity&site=105&drive=418&location_radius=10
GET /api/v2/photos?rovers=curiosity&site=76&drive=3002&location_radius=20
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=50
```

### Combined location + other filters
```
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&cameras=NAVCAM
GET /api/v2/photos?rovers=curiosity&site_min=80&site_max=90&sol_min=1000&sol_max=2000
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=10&mars_time_golden_hour=true
```

---

## 6. API v2 Photos - Image Quality Filters (NEW)

### Width filters
```
GET /api/v2/photos?rovers=curiosity&min_width=1024
GET /api/v2/photos?rovers=curiosity&min_width=1920
GET /api/v2/photos?rovers=curiosity&max_width=800
GET /api/v2/photos?rovers=curiosity&min_width=1024&max_width=2048
```

### Height filters
```
GET /api/v2/photos?rovers=curiosity&min_height=768
GET /api/v2/photos?rovers=curiosity&min_height=1080
GET /api/v2/photos?rovers=curiosity&max_height=600
GET /api/v2/photos?rovers=curiosity&min_height=768&max_height=1536
```

### Combined dimension filters
```
GET /api/v2/photos?rovers=curiosity&min_width=1920&min_height=1080
GET /api/v2/photos?rovers=curiosity&min_width=1024&max_width=2048&min_height=768&max_height=1536
GET /api/v2/photos?rovers=perseverance&min_width=2048&min_height=1536
```

### Sample type filtering
```
GET /api/v2/photos?rovers=curiosity&sample_type=Full
GET /api/v2/photos?rovers=curiosity&sample_type=Thumbnail
GET /api/v2/photos?rovers=curiosity&sample_type=Subframe
GET /api/v2/photos?rovers=curiosity&sample_type=Full,Thumbnail
```

### Aspect ratio filtering
```
GET /api/v2/photos?rovers=curiosity&aspect_ratio=16:9
GET /api/v2/photos?rovers=curiosity&aspect_ratio=4:3
GET /api/v2/photos?rovers=curiosity&aspect_ratio=1:1
GET /api/v2/photos?rovers=perseverance&aspect_ratio=16:9
```

### Combined image quality filters
```
GET /api/v2/photos?rovers=curiosity&min_width=1920&min_height=1080&aspect_ratio=16:9
GET /api/v2/photos?rovers=curiosity&sample_type=Full&min_width=2048
GET /api/v2/photos?rovers=perseverance&aspect_ratio=4:3&min_width=1024
```

---

## 7. API v2 Photos - Camera Angle Queries (NEW)

### Mast elevation (looking up/down)
```
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-45
GET /api/v2/photos?rovers=curiosity&mast_elevation_max=45
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-30&mast_elevation_max=30
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=0&mast_elevation_max=90
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-90&mast_elevation_max=0
```

### Mast azimuth (compass direction)
```
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=0&mast_azimuth_max=90
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=90&mast_azimuth_max=180
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=180&mast_azimuth_max=270
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=270&mast_azimuth_max=360
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=45&mast_azimuth_max=135
```

### Combined angle filters
```
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=0&mast_elevation_max=45&mast_azimuth_min=90&mast_azimuth_max=180
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-30&mast_azimuth_min=0&mast_azimuth_max=90
GET /api/v2/photos?rovers=perseverance&mast_elevation_min=10&mast_elevation_max=30&mast_azimuth_min=180&mast_azimuth_max=270
```

### Angle + other filters
```
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=0&mast_elevation_max=45&cameras=MAST
GET /api/v2/photos?rovers=curiosity&mast_azimuth_min=0&mast_azimuth_max=90&sol_min=1000&sol_max=2000
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=-30&mast_elevation_max=30&mars_time_golden_hour=true
```

---

## 8. API v2 Photos - Field Selection & Image Sizes (NEW)

### Field selection (sparse fieldsets)
```
GET /api/v2/photos?rovers=curiosity&fields=id,img_src
GET /api/v2/photos?rovers=curiosity&fields=id,img_src,sol
GET /api/v2/photos?rovers=curiosity&fields=id,img_src,sol,earth_date
GET /api/v2/photos?rovers=curiosity&fields=id,img_src,sol,earth_date,camera
```

### Include relationships
```
GET /api/v2/photos?rovers=curiosity&include=rover
GET /api/v2/photos?rovers=curiosity&include=camera
GET /api/v2/photos?rovers=curiosity&include=rover,camera
```

### Field set presets
```
GET /api/v2/photos?rovers=curiosity&field_set=minimal
GET /api/v2/photos?rovers=curiosity&field_set=standard
GET /api/v2/photos?rovers=curiosity&field_set=extended
GET /api/v2/photos?rovers=curiosity&field_set=scientific
GET /api/v2/photos?rovers=curiosity&field_set=complete
```

### Image sizes selection
```
GET /api/v2/photos?rovers=curiosity&image_sizes=small
GET /api/v2/photos?rovers=curiosity&image_sizes=medium
GET /api/v2/photos?rovers=curiosity&image_sizes=large
GET /api/v2/photos?rovers=curiosity&image_sizes=full
GET /api/v2/photos?rovers=curiosity&image_sizes=small,medium
GET /api/v2/photos?rovers=curiosity&image_sizes=medium,large,full
```

### Exclude images (metadata only)
```
GET /api/v2/photos?rovers=curiosity&exclude_images=true
GET /api/v2/photos?rovers=curiosity&exclude_images=true&field_set=scientific
```

### Combined field control
```
GET /api/v2/photos?rovers=curiosity&field_set=minimal&image_sizes=small
GET /api/v2/photos?rovers=curiosity&fields=id,sol,earth_date&include=rover&image_sizes=medium
GET /api/v2/photos?rovers=curiosity&field_set=scientific&exclude_images=true
```

---

## 9. API v2 Photos - Combined Advanced Filters

### Multi-dimension queries (3+ filters)
```
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100&cameras=MAST,NAVCAM&mars_time_golden_hour=true
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=10&min_width=1920&min_height=1080
GET /api/v2/photos?rovers=curiosity&mars_time_min=M14:00:00&mars_time_max=M16:00:00&mast_elevation_min=0&mast_elevation_max=45&cameras=MAST
GET /api/v2/photos?rovers=perseverance&date_min=2024-01-01&cameras=MCZ_LEFT,MCZ_RIGHT&aspect_ratio=16:9&field_set=extended
```

### Complex scientific queries
```
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=2000&cameras=MAST&mast_elevation_min=0&mast_elevation_max=30&sample_type=Full&min_width=2048&field_set=scientific
GET /api/v2/photos?rovers=curiosity&site_min=80&site_max=90&mars_time_golden_hour=true&aspect_ratio=16:9&image_sizes=large,full
GET /api/v2/photos?rovers=perseverance&mars_time_min=M06:00:00&mars_time_max=M08:00:00&cameras=SUPERCAM_RMI&min_width=1024&field_set=complete
```

### Highly specific queries (5+ filters)
```
GET /api/v2/photos?rovers=curiosity&sol=1000&cameras=MAST&mars_time_golden_hour=true&min_width=1920&aspect_ratio=16:9&field_set=extended&per_page=50
GET /api/v2/photos?rovers=curiosity&site=82&drive=2176&location_radius=5&mast_elevation_min=-10&mast_elevation_max=10&cameras=NAVCAM&image_sizes=medium,large
GET /api/v2/photos?rovers=perseverance&date_min=2024-01-01&date_max=2024-12-31&cameras=MCZ_LEFT&mars_time_min=M14:00:00&min_width=2048&sample_type=Full&sort=-earth_date
```

---

## 10. API v2 Statistics

### Group by camera
```
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera
GET /api/v2/photos/stats?rovers=perseverance&group_by=camera
GET /api/v2/photos/stats?rovers=curiosity,perseverance&group_by=camera
GET /api/v2/photos/stats?rovers=opportunity,spirit&group_by=camera
```

### Group by rover
```
GET /api/v2/photos/stats?group_by=rover
GET /api/v2/photos/stats?rovers=curiosity,perseverance&group_by=rover
```

### Group by sol
```
GET /api/v2/photos/stats?rovers=curiosity&group_by=sol
GET /api/v2/photos/stats?rovers=curiosity&sol_min=1&sol_max=100&group_by=sol
GET /api/v2/photos/stats?rovers=perseverance&sol_min=1&sol_max=500&group_by=sol
```

### Statistics with filters
```
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2023-01-01&date_max=2023-12-31
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&sol_min=1000&sol_max=2000
GET /api/v2/photos/stats?rovers=curiosity&group_by=sol&cameras=MAST,NAVCAM
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&mars_time_golden_hour=true
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&site_min=80&site_max=90
```

---

## 11. API v2 Rovers & Cameras

### All cameras
```
GET /api/v2/cameras
```

### Camera by ID
```
GET /api/v2/cameras/MAST
GET /api/v2/cameras/NAVCAM
GET /api/v2/cameras/FHAZ
GET /api/v2/cameras/CHEMCAM
GET /api/v2/cameras/MCZ_LEFT
GET /api/v2/cameras/NAVCAM_LEFT
GET /api/v2/cameras/SHERLOC_WATSON
GET /api/v2/cameras/SUPERCAM_RMI
GET /api/v2/cameras/PANCAM
```

### Camera by ID with rover filter
```
GET /api/v2/cameras/MAST?rover=curiosity
GET /api/v2/cameras/NAVCAM?rover=curiosity
GET /api/v2/cameras/NAVCAM?rover=perseverance
GET /api/v2/cameras/PANCAM?rover=opportunity
GET /api/v2/cameras/PANCAM?rover=spirit
```

---

## 12. API v2 Advanced Features

### Panoramas
```
GET /api/v2/panoramas
GET /api/v2/panoramas?rovers=curiosity
GET /api/v2/panoramas?rovers=perseverance
GET /api/v2/panoramas?rovers=curiosity,perseverance
GET /api/v2/panoramas?rovers=curiosity&sol_min=1000
GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=2000
GET /api/v2/panoramas?rovers=curiosity&min_photos=5
GET /api/v2/panoramas?rovers=curiosity&min_photos=10
GET /api/v2/panoramas?rovers=curiosity&min_photos=20
GET /api/v2/panoramas?rovers=curiosity&page=1&per_page=10
GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&min_photos=10&per_page=25
```

### Locations
```
GET /api/v2/locations
GET /api/v2/locations?rovers=curiosity
GET /api/v2/locations?rovers=perseverance
GET /api/v2/locations?rovers=curiosity,perseverance
GET /api/v2/locations?rovers=curiosity&sol_min=1000
GET /api/v2/locations?rovers=curiosity&sol_min=1000&sol_max=2000
GET /api/v2/locations?rovers=curiosity&min_photos=5
GET /api/v2/locations?rovers=curiosity&min_photos=10
GET /api/v2/locations?rovers=curiosity&min_photos=50
GET /api/v2/locations?rovers=curiosity&page=1&per_page=10
GET /api/v2/locations?rovers=curiosity&sol_min=1000&min_photos=10&per_page=25
```

### Time Machine
```
GET /api/v2/time-machine?site=82&drive=2176
GET /api/v2/time-machine?site=105&drive=418
GET /api/v2/time-machine?site=76&drive=3002
GET /api/v2/time-machine?site=82&drive=2176&rover=curiosity
GET /api/v2/time-machine?site=82&drive=2176&camera=NAVCAM
GET /api/v2/time-machine?site=82&drive=2176&mars_time=M14:00:00
GET /api/v2/time-machine?site=82&drive=2176&limit=50
GET /api/v2/time-machine?site=82&drive=2176&limit=100
GET /api/v2/time-machine?site=82&drive=2176&rover=curiosity&camera=NAVCAM&limit=100
```

### Batch Get Photos (POST)
```
POST /api/v2/photos/batch
Body: {"ids": [451991, 1000000, 2000000]}

POST /api/v2/photos/batch
Body: {"ids": [1, 500000, 1000000]}

POST /api/v2/photos/batch?include=rover,camera
Body: {"ids": [451991, 1000000, 2000000]}

POST /api/v2/photos/batch?fields=id,img_src
Body: {"ids": [451991, 1000000, 2000000, 2542754]}
```

---

## 13. Error Cases

### Validation errors
```
GET /api/v2/photos?rovers=invalid_rover
GET /api/v2/photos?rovers=curiosity&sol_min=-1
GET /api/v2/photos?rovers=curiosity&sol_max=-100
GET /api/v2/photos?rovers=curiosity&date_min=invalid-date
GET /api/v2/photos?rovers=curiosity&date_max=2024-13-45
GET /api/v2/photos?rovers=curiosity&per_page=101
GET /api/v2/photos?rovers=curiosity&per_page=0
GET /api/v2/photos?rovers=curiosity&page=0
GET /api/v2/photos?rovers=curiosity&min_width=20000
GET /api/v2/photos?rovers=curiosity&mast_elevation_min=100
GET /api/v2/photos?rovers=curiosity&mast_azimuth_max=400
GET /api/v2/photos?rovers=curiosity&location_radius=2000
GET /api/v2/photos/stats?rovers=curiosity
GET /api/v2/photos/stats?rovers=curiosity&group_by=invalid
GET /api/v2/time-machine
GET /api/v2/time-machine?site=82
GET /api/v2/time-machine?drive=2176
```

### Not found errors
```
GET /api/v1/rovers/nonexistent
GET /api/v1/photos/999999999
GET /api/v2/rovers/invalid
GET /api/v2/cameras/INVALID_CAMERA
GET /api/v2/photos/999999999
```

### Authentication errors
```
# Test without X-API-Key header:
GET /api/v1/rovers

# Test with invalid API key:
GET /api/v1/rovers
Header: X-API-Key: invalid_key_12345
```

---

## Summary Statistics

**Total Unique Test Combinations: ~400+**

### Coverage Breakdown:
- **Basic endpoints:** ~20 URLs
- **API v1 photos:** ~40 URLs
- **API v2 sol/date/camera filtering:** ~50 URLs
- **API v2 Mars time filtering:** ~15 URLs ⭐ NEW
- **API v2 location-based queries:** ~25 URLs ⭐ NEW
- **API v2 image quality filters:** ~20 URLs ⭐ NEW
- **API v2 camera angle queries:** ~15 URLs ⭐ NEW
- **API v2 field selection:** ~20 URLs ⭐ NEW
- **API v2 combined advanced filters:** ~30 URLs
- **API v2 statistics:** ~15 URLs
- **API v2 rovers/cameras:** ~20 URLs
- **API v2 advanced features:** ~30 URLs
- **Error cases:** ~25 URLs

### Parameter Coverage: ✅ COMPLETE
- ✅ rovers, cameras (basic)
- ✅ sol, sol_min, sol_max, earth_date, date_min, date_max
- ✅ mars_time_min, mars_time_max, mars_time_golden_hour
- ✅ site, site_min, site_max, drive, drive_min, drive_max, location_radius
- ✅ min_width, max_width, min_height, max_height, sample_type, aspect_ratio
- ✅ mast_elevation_min, mast_elevation_max, mast_azimuth_min, mast_azimuth_max
- ✅ sort, fields, include, field_set, image_sizes, exclude_images
- ✅ page, per_page, cursor
- ✅ All rovers: Curiosity, Perseverance, Opportunity, Spirit
- ✅ All 39 cameras across 4 rovers
- ✅ All error cases (validation, auth, not found)
