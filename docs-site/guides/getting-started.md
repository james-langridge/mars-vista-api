# Getting Started with Mars Vista API

Welcome! This guide will help you make your first API request in less than 5 minutes.

## What is Mars Vista?

Mars Vista is a fast, reliable API for querying Mars rover photography data from NASA's missions. It's 100% compatible with NASA's Mars Photo API but offers:

- **50-70% faster response times** (< 260ms average)
- **No API key required** for public endpoints
- **Complete NASA metadata** preservation
- **4 rovers supported**: Curiosity, Perseverance, Opportunity, Spirit

## Your First Request

### 1. Get All Rovers

```bash
curl "https://api.marsvista.dev/api/v1/rovers"
```

This returns a list of all Mars rovers with their details, cameras, and photo counts.

### 2. Query Photos by Sol

Get photos from Curiosity rover on sol 1000 (Martian day 1000):

```bash
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"
```

### 3. Filter by Camera

Get only NAVCAM (Navigation Camera) photos:

```bash
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=NAVCAM"
```

### 4. Query by Earth Date

Get photos from a specific Earth date:

```bash
curl "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?earth_date=2021-04-19"
```

## Response Format

All responses are JSON. Here's a sample photo object:

```json
{
  "photos": [
    {
      "id": 123456,
      "sol": 1000,
      "camera": {
        "id": 21,
        "name": "NAVCAM",
        "fullName": "Navigation Camera"
      },
      "imgSrc": "https://mars.jpl.nasa.gov/msl-raw-images/.../NLB_450123456EDR_F0482342NCAM00237M_.JPG",
      "earthDate": "2021-04-19",
      "rover": {
        "id": 2,
        "name": "Curiosity",
        "landingDate": "2012-08-06",
        "status": "active"
      }
    }
  ],
  "pagination": {
    "total_count": 45,
    "page": 1,
    "per_page": 25,
    "total_pages": 2
  }
}
```

## Common Use Cases

### Get Latest Photos from a Rover

```bash
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/latest"
```

### Paginate Through Results

```bash
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&page=2&per_page=50"
```

### Get Rover Manifest (Photo Availability by Sol)

```bash
curl "https://api.marsvista.dev/api/v1/manifests/curiosity"
```

This shows which sols have photos and how many photos per sol.

## Rate Limits

- **100 requests per minute** per IP address
- No API key required for these limits
- Exceeded limit returns HTTP 429 with retry-after header

## Next Steps

- **Code Examples**: See language-specific examples in [Code Examples](./examples.md)
- **API Reference**: Explore the full API in the interactive reference above
- **NASA API Migration**: Switching from NASA's API? See [Migration Guide](./nasa-migration.md)

## Need Help?

- Check the API status: https://api.marsvista.dev/health
- Report issues: [GitHub Issues](https://github.com/yourusername/mars-vista-api/issues)
- Browse example code: [Examples Guide](./examples.md)
