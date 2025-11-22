# Comprehensive Production API Test URL List

This document lists ALL possible public (non-admin) endpoint combinations with various filters and query parameters for comprehensive manual testing and benchmarking of the production API at `https://api.marsvista.dev`.

**Note:** All requests require an `X-API-Key` header. Replace `YOUR_API_KEY` with your actual API key.

---

## Test Organization

- **API v1**: NASA-compatible endpoints
- **API v2**: Modern REST API with enhanced features
- **Health & Discovery**: System endpoints

---

## API v1 Endpoints (NASA-Compatible)

### 1. Health Check

```
GET https://api.marsvista.dev/health
```
*No authentication required*

### 2. Get All Rovers

```
GET https://api.marsvista.dev/api/v1/rovers
GET https://api.marsvista.dev/api/v1/rovers?format=camelCase
```

### 3. Get Specific Rover

```
GET https://api.marsvista.dev/api/v1/rovers/curiosity
GET https://api.marsvista.dev/api/v1/rovers/perseverance
GET https://api.marsvista.dev/api/v1/rovers/opportunity
GET https://api.marsvista.dev/api/v1/rovers/spirit
GET https://api.marsvista.dev/api/v1/rovers/invalid
```

### 4. Get Rover Photos (Basic Queries)

**Single rover, specific sol:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=100
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=4683
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=500
GET https://api.marsvista.dev/api/v1/rovers/opportunity/photos?sol=1
GET https://api.marsvista.dev/api/v1/rovers/spirit/photos?sol=1
```

**Single rover, specific Earth date:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2012-08-06
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2023-01-01
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2024-11-20
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?earth_date=2021-02-18
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?earth_date=2024-01-01
```

**Camera filtering (Curiosity):**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=MAST
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=NAVCAM
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=FHAZ
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=RHAZ
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=CHEMCAM
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=MAHLI
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=MARDI
```

**Camera filtering (Perseverance):**
```
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=NAVCAM_LEFT
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=MCZ_LEFT
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=FRONT_HAZCAM_LEFT_A
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=REAR_HAZCAM_LEFT
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=SHERLOC_WATSON
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=1&camera=SKYCAM
```

**Pagination:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=1&per_page=10
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=1&per_page=25
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=1&per_page=50
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=1&per_page=100
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=2&per_page=25
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&page=5&per_page=10
```

**Response format:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=snake_case
```

**Combined filters:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=NAVCAM&page=1&per_page=50
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2015-05-30&camera=MAST&format=camelCase
GET https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=500&camera=MCZ_LEFT&per_page=25
```

**Error cases:**
```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=invalid-date
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&camera=INVALID_CAMERA
GET https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1&per_page=101
```

### 5. Get Latest Photos

```
GET https://api.marsvista.dev/api/v1/rovers/curiosity/latest
GET https://api.marsvista.dev/api/v1/rovers/curiosity/latest?per_page=10
GET https://api.marsvista.dev/api/v1/rovers/curiosity/latest?per_page=50&page=2
GET https://api.marsvista.dev/api/v1/rovers/curiosity/latest_photos
GET https://api.marsvista.dev/api/v1/rovers/perseverance/latest
GET https://api.marsvista.dev/api/v1/rovers/perseverance/latest?format=camelCase
GET https://api.marsvista.dev/api/v1/rovers/opportunity/latest
GET https://api.marsvista.dev/api/v1/rovers/spirit/latest
```

### 6. Get Photo by ID

```
GET https://api.marsvista.dev/api/v1/photos/1
GET https://api.marsvista.dev/api/v1/photos/1000
GET https://api.marsvista.dev/api/v1/photos/100000
GET https://api.marsvista.dev/api/v1/photos/999999999
GET https://api.marsvista.dev/api/v1/photos/1?format=camelCase
```

### 7. Get Rover Manifest

```
GET https://api.marsvista.dev/api/v1/manifests/curiosity
GET https://api.marsvista.dev/api/v1/manifests/perseverance
GET https://api.marsvista.dev/api/v1/manifests/opportunity
GET https://api.marsvista.dev/api/v1/manifests/spirit
GET https://api.marsvista.dev/api/v1/manifests/invalid
```

---

## API v2 Endpoints (Modern REST API)

### 8. API Discovery

```
GET https://api.marsvista.dev/api/v2
```

### 9. Get All Rovers (v2)

```
GET https://api.marsvista.dev/api/v2/rovers
```

### 10. Get Specific Rover (v2)

```
GET https://api.marsvista.dev/api/v2/rovers/curiosity
GET https://api.marsvista.dev/api/v2/rovers/perseverance
GET https://api.marsvista.dev/api/v2/rovers/opportunity
GET https://api.marsvista.dev/api/v2/rovers/spirit
GET https://api.marsvista.dev/api/v2/rovers/invalid
```

### 11. Get Rover Manifest (v2)

```
GET https://api.marsvista.dev/api/v2/rovers/curiosity/manifest
GET https://api.marsvista.dev/api/v2/rovers/perseverance/manifest
GET https://api.marsvista.dev/api/v2/rovers/opportunity/manifest
GET https://api.marsvista.dev/api/v2/rovers/spirit/manifest
```

### 12. Get Rover Cameras (v2)

```
GET https://api.marsvista.dev/api/v2/rovers/curiosity/cameras
GET https://api.marsvista.dev/api/v2/rovers/perseverance/cameras
GET https://api.marsvista.dev/api/v2/rovers/opportunity/cameras
GET https://api.marsvista.dev/api/v2/rovers/spirit/cameras
```

### 13. Get Rover Journey (v2)

```
GET https://api.marsvista.dev/api/v2/rovers/curiosity/journey
GET https://api.marsvista.dev/api/v2/rovers/curiosity/journey?sol_min=1&sol_max=100
GET https://api.marsvista.dev/api/v2/rovers/perseverance/journey?sol_min=1
GET https://api.marsvista.dev/api/v2/rovers/perseverance/journey?sol_max=500
```

### 14. Query Photos (v2 - Basic)

**Single rover:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance
GET https://api.marsvista.dev/api/v2/photos?rovers=opportunity
GET https://api.marsvista.dev/api/v2/photos?rovers=spirit
```

**Multiple rovers:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity,opportunity,spirit
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance,opportunity,spirit
```

**Single camera:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=MAST
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=NAVCAM
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT
```

**Multiple cameras:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=MAST,NAVCAM
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=FHAZ,RHAZ,NAVCAM
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&cameras=NAVCAM_LEFT,MCZ_LEFT
```

### 15. Query Photos (v2 - Sol Filtering)

**Sol exact (using min=max):**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol_min=1&sol_max=1
```

**Sol ranges:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=100&sol_max=200
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1&sol_max=10
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=2000
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol_min=1&sol_max=100
```

**Sol minimum only:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=4000
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol_min=1000
```

**Sol maximum only:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_max=100
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol_max=10
```

### 16. Query Photos (v2 - Date Filtering)

**Date ranges:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_min=2012-08-06&date_max=2012-12-31
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_min=2023-01-01&date_max=2023-12-31
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&date_min=2021-02-18&date_max=2021-12-31
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&date_min=2024-01-01&date_max=2024-11-20
```

**Date minimum only:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_min=2024-01-01
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&date_min=2024-10-01
```

**Date maximum only:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_max=2013-01-01
GET https://api.marsvista.dev/api/v2/photos?rovers=opportunity&date_max=2019-01-01
```

### 17. Query Photos (v2 - Sorting)

```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=earth_date
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=-earth_date
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=sol
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=-sol
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=camera
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sort=-earth_date,camera
```

### 18. Query Photos (v2 - Field Selection)

```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&fields=id,img_src
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&fields=id,img_src,sol
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&fields=id,img_src,sol,earth_date
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&fields=id,img_src,sol,earth_date,camera
```

### 19. Query Photos (v2 - Include Relationships)

```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&include=rover
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&include=camera
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&include=rover,camera
```

### 20. Query Photos (v2 - Pagination)

**Page-based:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=1&per_page=10
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=1&per_page=25
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=1&per_page=50
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=1&per_page=100
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=2&per_page=25
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=10&per_page=100
```

### 21. Query Photos (v2 - Complex Combined Filters)

```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=MAST,NAVCAM&sol_min=1000&sol_max=1100
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=FHAZ&date_min=2023-01-01&date_max=2023-12-31&sort=-earth_date
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&sol_min=1&sol_max=10&include=rover,camera
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&cameras=MAST&sol_min=1000&fields=id,img_src,sol&per_page=50
GET https://api.marsvista.dev/api/v2/photos?rovers=perseverance&cameras=MCZ_LEFT,MCZ_RIGHT&date_min=2024-01-01&sort=-earth_date&include=camera&per_page=25
```

### 22. Get Photo by ID (v2)

```
GET https://api.marsvista.dev/api/v2/photos/1
GET https://api.marsvista.dev/api/v2/photos/1000
GET https://api.marsvista.dev/api/v2/photos/100000
GET https://api.marsvista.dev/api/v2/photos/1?include=rover,camera
GET https://api.marsvista.dev/api/v2/photos/1000?fields=id,img_src,sol
GET https://api.marsvista.dev/api/v2/photos/999999999
```

### 23. Photo Statistics (v2)

**Group by camera:**
```
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=perseverance&group_by=camera
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity,perseverance&group_by=camera
```

**Group by rover:**
```
GET https://api.marsvista.dev/api/v2/photos/stats?group_by=rover
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity,perseverance&group_by=rover
```

**Group by sol:**
```
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=sol
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&sol_min=1&sol_max=100&group_by=sol
```

**With date filtering:**
```
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2023-01-01&date_max=2023-12-31
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=perseverance&group_by=rover&date_min=2024-01-01
```

**Error cases:**
```
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity
GET https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=invalid
```

### 24. Batch Get Photos (v2)

**Note:** POST requests require Content-Type: application/json header

```
POST https://api.marsvista.dev/api/v2/photos/batch
Body: {"ids": [1, 2, 3, 4, 5]}

POST https://api.marsvista.dev/api/v2/photos/batch
Body: {"ids": [1000, 2000, 3000]}

POST https://api.marsvista.dev/api/v2/photos/batch?include=rover,camera
Body: {"ids": [1, 2, 3]}

POST https://api.marsvista.dev/api/v2/photos/batch?fields=id,img_src
Body: {"ids": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]}
```

### 25. Get All Cameras (v2)

```
GET https://api.marsvista.dev/api/v2/cameras
```

### 26. Get Camera by ID (v2)

```
GET https://api.marsvista.dev/api/v2/cameras/MAST
GET https://api.marsvista.dev/api/v2/cameras/NAVCAM
GET https://api.marsvista.dev/api/v2/cameras/FHAZ
GET https://api.marsvista.dev/api/v2/cameras/CHEMCAM
GET https://api.marsvista.dev/api/v2/cameras/MCZ_LEFT
GET https://api.marsvista.dev/api/v2/cameras/NAVCAM_LEFT
GET https://api.marsvista.dev/api/v2/cameras/SHERLOC_WATSON
GET https://api.marsvista.dev/api/v2/cameras/MAST?rover=curiosity
GET https://api.marsvista.dev/api/v2/cameras/NAVCAM?rover=perseverance
GET https://api.marsvista.dev/api/v2/cameras/INVALID
```

### 27. Panoramas (v2)

**Basic queries:**
```
GET https://api.marsvista.dev/api/v2/panoramas
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity
GET https://api.marsvista.dev/api/v2/panoramas?rovers=perseverance
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity,perseverance
```

**Sol filtering:**
```
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&sol_min=1000
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=2000
GET https://api.marsvista.dev/api/v2/panoramas?rovers=perseverance&sol_max=500
```

**Minimum photos:**
```
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=5
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=10
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=20
```

**Pagination:**
```
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&page=1&per_page=10
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&page=1&per_page=25
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&page=2&per_page=10
```

**Combined:**
```
GET https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&sol_min=1000&min_photos=10&per_page=25
```

**Get specific panorama:**
```
GET https://api.marsvista.dev/api/v2/panoramas/pano_curiosity_1000_14
GET https://api.marsvista.dev/api/v2/panoramas/invalid_id
```

### 28. Locations (v2)

**Basic queries:**
```
GET https://api.marsvista.dev/api/v2/locations
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity
GET https://api.marsvista.dev/api/v2/locations?rovers=perseverance
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity,perseverance
```

**Sol filtering:**
```
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&sol_min=1000
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&sol_min=1000&sol_max=2000
GET https://api.marsvista.dev/api/v2/locations?rovers=perseverance&sol_max=500
```

**Minimum photos:**
```
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&min_photos=5
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&min_photos=10
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&min_photos=50
```

**Pagination:**
```
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&page=1&per_page=10
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&page=1&per_page=25
```

**Combined:**
```
GET https://api.marsvista.dev/api/v2/locations?rovers=curiosity&sol_min=1000&min_photos=10&per_page=25
```

**Get specific location:**
```
GET https://api.marsvista.dev/api/v2/locations/curiosity_79_1204
GET https://api.marsvista.dev/api/v2/locations/invalid_id
```

### 29. Time Machine (v2)

**Required parameters (site + drive):**
```
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&rover=curiosity
```

**Optional filters:**
```
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&camera=NAVCAM
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&mars_time=M14:00:00
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&limit=50
```

**Combined:**
```
GET https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&rover=curiosity&camera=NAVCAM&limit=100
```

**Error cases:**
```
GET https://api.marsvista.dev/api/v2/time-machine
GET https://api.marsvista.dev/api/v2/time-machine?site=79
GET https://api.marsvista.dev/api/v2/time-machine?drive=1204
```

### 30. HTTP Caching Tests (v2)

**Test ETag caching:**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=opportunity&per_page=10
# Save ETag from response headers, then:
GET https://api.marsvista.dev/api/v2/photos?rovers=opportunity&per_page=10
# Add header: If-None-Match: "<etag-value>"
# Should return 304 Not Modified
```

**Test Cache-Control (active vs inactive rovers):**
```
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=10
# Check Cache-Control header: max-age=3600 (1 hour)

GET https://api.marsvista.dev/api/v2/photos?rovers=opportunity&per_page=10
# Check Cache-Control header: max-age=31536000 (1 year)
```

---

## Error Case Testing

### Validation Errors

```
GET https://api.marsvista.dev/api/v2/photos?rovers=invalid_rover
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=-1
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_max=-100
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_min=invalid-date
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&date_max=2024-13-45
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=101
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=0
GET https://api.marsvista.dev/api/v2/photos?rovers=curiosity&page=0
```

### Authentication Errors

```
GET https://api.marsvista.dev/api/v1/rovers
# Without X-API-Key header - should return 401

GET https://api.marsvista.dev/api/v1/rovers
# With invalid API key - should return 401
```

### Not Found Errors

```
GET https://api.marsvista.dev/api/v1/rovers/nonexistent
GET https://api.marsvista.dev/api/v1/photos/999999999
GET https://api.marsvista.dev/api/v2/rovers/invalid
GET https://api.marsvista.dev/api/v2/cameras/INVALID_CAMERA
GET https://api.marsvista.dev/api/v2/panoramas/invalid_id
GET https://api.marsvista.dev/api/v2/locations/invalid_id
```

---

## Summary Statistics

**Total Test Combinations:**
- API v1: ~80 test URLs
- API v2: ~150 test URLs
- Error cases: ~20 test URLs
- **Total: ~250 unique test combinations**

**Coverage:**
- ✅ All 4 rovers (Curiosity, Perseverance, Opportunity, Spirit)
- ✅ All major cameras (7 for Curiosity, 14 for Perseverance)
- ✅ Sol filtering (exact, ranges, min only, max only)
- ✅ Date filtering (exact, ranges, min only, max only)
- ✅ Pagination (various page sizes: 10, 25, 50, 100)
- ✅ Sorting (all fields, ascending/descending)
- ✅ Field selection
- ✅ Include relationships
- ✅ Statistics grouping (camera, rover, sol)
- ✅ HTTP caching (ETags, Cache-Control)
- ✅ Error cases (validation, authentication, not found)
- ✅ Advanced features (panoramas, locations, time-machine)

---

## Next Steps

1. Review this list for completeness
2. Approve for benchmarking execution
3. Test each URL sequentially
4. Record response times and results
5. Generate comprehensive benchmark report
