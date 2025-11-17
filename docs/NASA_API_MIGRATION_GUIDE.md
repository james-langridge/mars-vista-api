# Migrating from NASA Mars Photo Rails API

Mars Vista API is a **drop-in replacement** for the original NASA Mars Photo Rails API (https://github.com/corincerami/mars-photo-api).

## Quick Migration (Zero Code Changes Required)

Simply replace the base URL in your application:

**Before** (original Rails API):
```
https://api.nasa.gov/mars-photos/api/v1/...
https://mars-photos.herokuapp.com/api/v1/...
```

**After** (Mars Vista API):
```
https://api.marsvista.dev/api/v1/...
```

**That's it!** All query parameters and response formats remain 100% compatible.

## API Compatibility

### ✅ 100% Backward Compatible

| Feature | Original API | Mars Vista API |
|---------|--------------|----------------|
| Endpoint structure | `/api/v1/rovers/{name}/photos` | ✅ Identical |
| Field naming | `snake_case` (e.g., `img_src`, `earth_date`) | ✅ Identical (default) |
| Query parameters | `?sol=X&earth_date=Y&camera=Z` | ✅ Identical |
| Pagination | `?page=1&per_page=25` | ✅ Identical |
| `/latest_photos` endpoint | ✅ Supported | ✅ Supported |
| Response structure | JSON with `photos` and `pagination` | ✅ Identical |

### Example: Same Request, Same Response

**Original NASA API request:**
```bash
curl "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&camera=MAST&page=1"
```

**Mars Vista API equivalent (identical response):**
```bash
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=MAST&page=1"
```

**Response format (identical):**
```json
{
  "photos": [
    {
      "id": 583821,
      "sol": 1000,
      "camera": {
        "id": 20,
        "name": "MAST",
        "full_name": "Mast Camera"
      },
      "img_src": "https://mars.jpl.nasa.gov/...",
      "earth_date": "2015-05-30",
      "rover": {
        "id": 2,
        "name": "Curiosity",
        "landing_date": "2012-08-06",
        "launch_date": "2011-11-26",
        "status": "active"
      }
    }
  ],
  "pagination": {
    "total_count": 156,
    "page": 1,
    "per_page": 25,
    "total_pages": 7
  }
}
```

## Bonus Feature: Modern JavaScript Support

Mars Vista API adds **optional** camelCase format for modern JavaScript/TypeScript applications:

```javascript
// Add ?format=camelCase to any endpoint
const response = await fetch(
  'https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase'
);

const { photos } = await response.json();
console.log(photos[0].imgSrc);      // camelCase properties
console.log(photos[0].earthDate);   // instead of img_src, earth_date
console.log(photos[0].camera.fullName); // instead of full_name
```

This is **entirely optional** - the default format remains `snake_case` for backward compatibility.

## Enhanced Features

Mars Vista API provides several improvements over the original:

### 1. More Complete Metadata

- **Original API**: Stores 10-15 fields per photo
- **Mars Vista API**: Stores **all 30-55 NASA fields** in JSONB format
- Enables future advanced features (panoramas, stereo pairs, location search)

### 2. Richer MER Data (Opportunity & Spirit)

- **Original API**: Limited to NASA's official API fields (~10 fields)
- **Mars Vista API**: Scrapes **PDS archives directly** (55+ fields per photo)
- Includes additional metadata not available in the original API

### 3. Better Performance

- Optimized PostgreSQL indexes
- Connection pooling and retry policies
- Hosted on Railway Pro (better uptime than original Heroku deployment)

### 4. Actually Online

The original Heroku deployment (mars-photos.herokuapp.com) currently returns **404 errors**. Mars Vista API is actively maintained and monitored.

## Complete API Reference

### All Endpoints Supported

| Endpoint | Description | Example |
|----------|-------------|---------|
| `GET /api/v1/rovers` | List all rovers | `/api/v1/rovers` |
| `GET /api/v1/rovers/{name}` | Get specific rover | `/api/v1/rovers/curiosity` |
| `GET /api/v1/rovers/{name}/photos` | Query photos | `/api/v1/rovers/curiosity/photos?sol=1000` |
| `GET /api/v1/rovers/{name}/latest` | Latest photos | `/api/v1/rovers/curiosity/latest` |
| `GET /api/v1/rovers/{name}/latest_photos` | Latest photos (alias) | `/api/v1/rovers/curiosity/latest_photos` |
| `GET /api/v1/manifests/{name}` | Photo manifest | `/api/v1/manifests/curiosity` |
| `GET /api/v1/photos/{id}` | Get specific photo | `/api/v1/photos/583821` |

### Query Parameters

All parameters from the original API are supported:

- `sol` - Martian sol number (e.g., `?sol=1000`)
- `earth_date` - Earth date in YYYY-MM-DD format (e.g., `?earth_date=2015-05-30`)
- `camera` - Camera name (e.g., `?camera=MAST`)
- `page` - Page number (default: 1)
- `per_page` - Results per page (default: 25, max: 100)

**New parameter:**
- `format` - Response format: `snake_case` (default) or `camelCase` (optional)

### Supported Rovers

All 4 Mars rovers are supported:

- **Perseverance** (451,602 photos)
- **Curiosity** (675,765 photos)
- **Opportunity** (548,817 photos)
- **Spirit** (301,336 photos)

**Total**: 1,977,520 photos

## Migration Examples

### Python

**Before:**
```python
import requests

url = "https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos"
params = {"sol": 1000, "api_key": "DEMO_KEY"}
response = requests.get(url, params=params)
photos = response.json()["photos"]
```

**After (just change the URL):**
```python
import requests

url = "https://api.marsvista.dev/api/v1/rovers/curiosity/photos"
params = {"sol": 1000}  # No API key required (for now)
response = requests.get(url, params=params)
photos = response.json()["photos"]
```

### JavaScript

**Before:**
```javascript
fetch('https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos?sol=1000&api_key=DEMO_KEY')
  .then(res => res.json())
  .then(data => console.log(data.photos[0].img_src));
```

**After (option 1: snake_case, drop-in compatible):**
```javascript
fetch('https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000')
  .then(res => res.json())
  .then(data => console.log(data.photos[0].img_src)); // snake_case
```

**After (option 2: modern camelCase):**
```javascript
fetch('https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase')
  .then(res => res.json())
  .then(data => console.log(data.photos[0].imgSrc)); // camelCase
```

### Ruby

**Before:**
```ruby
require 'net/http'
require 'json'

uri = URI('https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos')
params = { sol: 1000, api_key: 'DEMO_KEY' }
uri.query = URI.encode_www_form(params)
response = Net::HTTP.get_response(uri)
photos = JSON.parse(response.body)['photos']
```

**After (just change the URL):**
```ruby
require 'net/http'
require 'json'

uri = URI('https://api.marsvista.dev/api/v1/rovers/curiosity/photos')
params = { sol: 1000 }
uri.query = URI.encode_www_form(params)
response = Net::HTTP.get_response(uri)
photos = JSON.parse(response.body)['photos']
```

## Testing Your Migration

Use these test requests to verify your migration:

```bash
# Test 1: Get rovers list
curl "https://api.marsvista.dev/api/v1/rovers"

# Test 2: Get Curiosity rover info
curl "https://api.marsvista.dev/api/v1/rovers/curiosity"

# Test 3: Query photos by sol
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&per_page=5"

# Test 4: Query photos by earth_date
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2015-05-30"

# Test 5: Get latest photos
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/latest_photos?per_page=10"

# Test 6: Get photo manifest
curl "https://api.marsvista.dev/api/v1/manifests/curiosity"

# Test 7: Get specific photo by ID
curl "https://api.marsvista.dev/api/v1/photos/583821"

# Test 8: Modern camelCase format (optional)
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase"
```

## Differences from Original API

### Breaking Changes

**None!** Mars Vista API is 100% backward compatible.

### Optional Enhancements

1. **`?format=camelCase` parameter** - Modern JavaScript apps can opt into camelCase property names
2. **`/latest` endpoint** - Shorter alternative to `/latest_photos` (both work)
3. **More metadata** - Response includes all NASA fields, not just a subset

### Future Features

With complete NASA metadata storage, Mars Vista API can support advanced features in the future:

- Panorama image grouping
- Stereo pair matching
- Location-based search
- Image quality filtering
- Camera position analytics

## Support

For issues or questions:
- GitHub: https://github.com/your-repo/mars-vista-api
- Documentation: https://github.com/your-repo/mars-vista-api/docs

## Acknowledgments

Mars Vista API is inspired by and compatible with the original **NASA Mars Photo Rails API** by Cori Cerami:
https://github.com/corincerami/mars-photo-api

The data comes from NASA's official Mars Rover Photo APIs and PDS archives.
