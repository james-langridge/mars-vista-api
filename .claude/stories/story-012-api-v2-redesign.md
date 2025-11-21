# Story 012: API v2 Redesign - Modern REST API Implementation

## Context

We've reimplemented the NASA Mars Photo API but inherited its design flaws. With no production users yet, we have a unique opportunity to create a superior API design while maintaining NASA compatibility through v1.

**Strategy:**
- **v1**: Keep as-is for NASA API compatibility (drop-in replacement)
- **v2**: Complete redesign following modern REST best practices

## Requirements

### 1. Core Design Principles

**Resource-Oriented Architecture:**
```
# Unified photo resource with powerful filtering
GET /api/v2/photos
GET /api/v2/photos/{id}

# Clear rover hierarchy
GET /api/v2/rovers
GET /api/v2/rovers/{slug}
GET /api/v2/rovers/{slug}/manifest
GET /api/v2/rovers/{slug}/cameras

# Camera resources
GET /api/v2/cameras
GET /api/v2/cameras/{id}
```

### 2. Unified Photos Endpoint

**Single endpoint for all photo queries:**
```
# Multiple rovers
GET /api/v2/photos?rovers=curiosity,perseverance

# Multiple cameras
GET /api/v2/photos?cameras=FHAZ,NAVCAM,MAST

# Date ranges (both sol and earth_date)
GET /api/v2/photos?sol_min=100&sol_max=200
GET /api/v2/photos?date_min=2023-01-01&date_max=2023-12-31

# Combined filters
GET /api/v2/photos?rovers=curiosity&cameras=MAST,CHEMCAM&sol_min=1000&sol_max=2000

# Sorting (- prefix for descending)
GET /api/v2/photos?sort=-earth_date,camera

# Field selection
GET /api/v2/photos?fields=id,img_src,sol,earth_date

# Include related resources
GET /api/v2/photos?include=rover,camera
```

### 3. Standardized Response Format

**Consistent envelope structure:**
```json
{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "img_src": "https://mars.nasa.gov/msl/123456.jpg",
        "sol": 1000,
        "earth_date": "2015-05-30",
        "created_at": "2015-05-30T20:12:34Z"
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
      }
    }
  ],
  "meta": {
    "total_count": 15234,
    "returned_count": 25,
    "query": {
      "rovers": ["curiosity"],
      "sol_min": 1000,
      "sol_max": 2000
    }
  },
  "pagination": {
    "page": 1,
    "per_page": 25,
    "total_pages": 610,
    "cursor": {
      "current": "eyJpZCI6MTIzNDU2fQ==",
      "next": "eyJpZCI6MTIzNDgxfQ==",
      "previous": null
    }
  },
  "links": {
    "self": "https://api.marsvista.io/v2/photos?rovers=curiosity&page=1",
    "next": "https://api.marsvista.io/v2/photos?rovers=curiosity&page=2",
    "first": "https://api.marsvista.io/v2/photos?rovers=curiosity&page=1",
    "last": "https://api.marsvista.io/v2/photos?rovers=curiosity&page=610"
  }
}
```

### 4. Comprehensive Error Handling

**RFC 7807 Problem Details format:**
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

### 5. Always-On Pagination

**Never return unbounded results:**
```csharp
// Always paginate with sensible defaults
var pageSize = Math.Min(request.PerPage ?? 25, 100); // Cap at 100
var page = request.Page ?? 1;

// Support both page-based and cursor-based
if (request.Cursor != null)
{
    return GetCursorPaginatedResults(request.Cursor, pageSize);
}
else
{
    return GetPagePaginatedResults(page, pageSize);
}
```

### 6. Advanced Filtering

**Support complex queries:**
```csharp
public class PhotoQueryParameters
{
    // Multiple values
    public List<string> Rovers { get; set; }
    public List<string> Cameras { get; set; }

    // Range queries
    public int? SolMin { get; set; }
    public int? SolMax { get; set; }
    public DateTime? DateMin { get; set; }
    public DateTime? DateMax { get; set; }

    // Sorting
    public List<string> Sort { get; set; } // ["-earth_date", "camera"]

    // Field selection
    public List<string> Fields { get; set; }
    public List<string> Include { get; set; }

    // Pagination
    public int? Page { get; set; }
    public int? PerPage { get; set; }
    public string Cursor { get; set; }
}
```

### 7. HTTP Caching

**Implement proper caching headers:**
```csharp
// For inactive rovers (Spirit, Opportunity)
response.Headers.CacheControl = new CacheControlHeaderValue
{
    Public = true,
    MaxAge = TimeSpan.FromDays(365)
};

// For active rovers
response.Headers.CacheControl = new CacheControlHeaderValue
{
    Public = true,
    MaxAge = TimeSpan.FromHours(1)
};

// Add ETag support
var etag = GenerateETag(photos);
response.Headers.ETag = new EntityTagHeaderValue($"\"{etag}\"");
```

### 8. Discovery Endpoints

**Self-documenting API:**
```json
// GET /api/v2
{
  "version": "2.0.0",
  "resources": {
    "photos": {
      "href": "/api/v2/photos",
      "methods": ["GET"],
      "filters": {
        "rovers": {
          "type": "array",
          "values": ["curiosity", "perseverance", "opportunity", "spirit"]
        },
        "cameras": {
          "type": "array",
          "values": ["FHAZ", "RHAZ", "MAST", "CHEMCAM", "MAHLI", "MARDI", "NAVCAM"]
        },
        "sol_min": { "type": "integer", "min": 0 },
        "sol_max": { "type": "integer", "min": 0 },
        "date_min": { "type": "date", "format": "YYYY-MM-DD" },
        "date_max": { "type": "date", "format": "YYYY-MM-DD" }
      }
    }
  }
}

// GET /api/v2/rovers/curiosity/cameras
{
  "data": [
    {
      "id": "mast",
      "name": "MAST",
      "full_name": "Mast Camera",
      "type": "imaging",
      "photo_count": 423567,
      "first_photo_sol": 0,
      "last_photo_sol": 4102
    }
  ]
}
```

### 9. Batch Operations

**Support efficient bulk queries:**
```json
// POST /api/v2/photos/batch
{
  "ids": [123456, 123457, 123458, 123459, 123460]
}

// Returns the requested photos in a single response
```

### 10. Statistics Endpoints

**Aggregate data for analytics:**
```
GET /api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2023-01-01

Response:
{
  "data": {
    "period": {
      "from": "2023-01-01",
      "to": "2024-11-20"
    },
    "by_camera": [
      {
        "camera": "MAST",
        "count": 5234,
        "percentage": 34.2,
        "avg_per_sol": 14.3
      }
    ]
  }
}
```

## Implementation Steps

### Phase 1: Core Infrastructure (Week 1)

1. **Create v2 namespace and routing:**
   - Set up `/api/v2/` routes separate from v1
   - Create base controller with error handling
   - Implement global exception filters

2. **Standardized response formats:**
   - Create response envelope DTOs
   - Implement RFC 7807 error responses
   - Add response interceptor for consistent formatting

3. **Parameter validation framework:**
   - Create validation attributes
   - Implement comprehensive input validation
   - Add clear validation error messages

### Phase 2: Photos Endpoint (Week 1-2)

4. **Unified photos controller:**
   - Implement multi-rover filtering
   - Add multi-camera support
   - Create date range queries
   - Add sorting capabilities

5. **Pagination implementation:**
   - Always paginate with defaults
   - Add cursor-based pagination
   - Include pagination metadata
   - Generate navigation links

6. **Field selection:**
   - Parse fields parameter
   - Implement sparse fieldsets
   - Add include parameter for relationships

### Phase 3: Resource Endpoints (Week 2)

7. **Rovers endpoint redesign:**
   - Use slug-based identification
   - Move manifest under rover
   - Add cameras sub-resource
   - Include statistics

8. **Cameras endpoint:**
   - Create dedicated camera resource
   - Include photo counts and date ranges
   - Link to filtered photo queries

9. **Discovery endpoint:**
   - Create root API endpoint
   - Document available filters
   - Show valid parameter values

### Phase 4: Performance & Polish (Week 2-3)

10. **HTTP caching:**
    - Add ETag generation
    - Implement conditional requests
    - Set appropriate cache headers
    - Different TTL for active/inactive rovers

11. **Query optimization:**
    - Ensure proper eager loading
    - Optimize database queries
    - Add database indexes for common filters

12. **Batch operations:**
    - Create batch photo retrieval
    - Add statistics endpoints
    - Consider export functionality

### Phase 5: Documentation & Testing (Week 3)

13. **OpenAPI specification:**
    - Generate OpenAPI 3.0 spec
    - Add examples for all endpoints
    - Document all parameters

14. **Comprehensive testing:**
    - Unit tests for all controllers
    - Integration tests for complex queries
    - Performance tests for large datasets
    - Error condition testing

## Technical Decisions

### 1. Keep v1 for NASA Compatibility
- Maintain exact NASA API structure in v1
- No breaking changes to v1
- Document as "NASA-compatible" mode

### 2. Resource Identification
- Use slugs for human-readable IDs (rovers, cameras)
- Use numeric IDs for photos
- Support both where sensible

### 3. Parameter Naming
- Use snake_case for consistency
- Plural for array parameters (rovers, cameras)
- Clear range indicators (_min, _max suffixes)

### 4. Response Format
- Follow JSON:API-inspired structure without full spec compliance
- Consistent envelope with data, meta, pagination, links
- Clear separation of attributes and relationships

### 5. Error Strategy
- RFC 7807 Problem Details
- Field-level validation errors
- Helpful suggestions in error messages
- Never return empty results for invalid queries

## Success Criteria

1. **All critical issues resolved:**
   - ✓ Comprehensive error handling
   - ✓ Always paginate with metadata
   - ✓ Consistent parameter handling
   - ✓ Proper validation

2. **High-value features implemented:**
   - ✓ HTTP caching headers
   - ✓ Field selection
   - ✓ Multiple filter values
   - ✓ Date range queries
   - ✓ Proper resource hierarchy

3. **Performance targets:**
   - < 100ms response time for paginated queries
   - Proper caching reduces redundant data transfer
   - No N+1 query issues

4. **Developer experience:**
   - Self-documenting through discovery endpoints
   - Clear, helpful error messages
   - Consistent patterns throughout
   - Comprehensive documentation

## Migration Notes

### For Future NASA API Users

When users migrate from NASA API to our API:
- v1 endpoint remains NASA-compatible
- v2 offers enhanced features
- Migration guide showing equivalent queries
- Side-by-side comparison of responses

### Example Migration

**NASA/v1 Query:**
```
GET /api/v1/rovers/curiosity/photos?sol=1000&camera=FHAZ
```

**Equivalent v2 Query:**
```
GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000&cameras=FHAZ
```

**Enhanced v2 Query (not possible in v1):**
```
GET /api/v2/photos?rovers=curiosity,perseverance&sol_min=1000&sol_max=1100&cameras=FHAZ,NAVCAM&fields=id,img_src,sol&sort=-sol
```

## Notes

- This is our chance to build the API right from the start
- No backwards compatibility constraints for v2
- Focus on developer experience and modern patterns
- Learn from all 6 critiques to avoid common pitfalls
- Build for scale even if starting small