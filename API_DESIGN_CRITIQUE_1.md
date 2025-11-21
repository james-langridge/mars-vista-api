# Mars Photo API Design Review

## Executive Summary

This document provides a critical review of the Mars Photo API design from a client application perspective, analyzing what works well and what could be improved if designing from scratch.

## Current API Structure

The API follows a RESTful design with nested resources:

```
GET  /api/v1/rovers                           # List all rovers
GET  /api/v1/rovers/:id                       # Show rover details
GET  /api/v1/rovers/:rover_id/photos          # Query photos
GET  /api/v1/rovers/:rover_id/latest_photos   # Latest photos
GET  /api/v1/photos/:id                       # Show individual photo
GET  /api/v1/manifests/:id                    # Mission manifest
```

## What Works Well

### 1. Logical Resource Hierarchy
The nested structure (`/rovers/:rover_id/photos`) clearly communicates that photos belong to rovers. This is intuitive and prevents ambiguity about which rover's photos are being queried.

### 2. Flexible Filtering Options
Supporting both `sol` (Martian day) and `earth_date` parameters accommodates different user needs:
- Scientists might think in sols
- General users prefer Earth dates
- Both can combine with camera filters

### 3. Domain-Specific Endpoints
The `/latest_photos` endpoint is a thoughtful addition that saves clients from first querying the manifest to find the latest sol.

### 4. Smart Caching Strategy
The manifest caching differentiates between active rovers (1-day expiry) and inactive rovers (no expiry), balancing freshness with efficiency.

## Critical Design Issues

### 1. Inconsistent Parameter Handling

**Problem:** Different endpoints handle rover names differently:
- Some use `capitalize()`, others use `titleize()`
- No clear documentation on case sensitivity
- Leads to unpredictable 400 errors

**Better Design:** Use numeric IDs or standardize on case-insensitive string matching across all endpoints.

### 2. Poor Error Communication

**Current State:**
- Invalid parameters often return empty results (200 OK) instead of errors
- Invalid dates crash with 500 instead of returning 400
- No distinction between "no results" and "invalid query"

**Better Design:**
```json
// Clear error response
{
  "error": {
    "code": "INVALID_DATE_FORMAT",
    "message": "Date must be in YYYY-MM-DD format",
    "field": "earth_date"
  }
}
```

### 3. Mixed Resource Hierarchy

**Issue:** Photos can be accessed both nested (`/rovers/:rover_id/photos/:id`) and flat (`/photos/:id`), breaking REST consistency.

**Better Design:** Choose one approach consistently. If photos are truly rover-scoped, remove the flat endpoint.

### 4. Missing Input Validation

**Current Issues:**
- No maximum `per_page` limit
- Invalid camera names silently return empty results
- Out-of-range sols not validated

**Better Design:** Validate all inputs and return meaningful errors:
```json
{
  "error": {
    "code": "INVALID_CAMERA",
    "message": "Camera 'INVALID' not found. Valid cameras: FHAZ, RHAZ, MAST, CHEMCAM, NAVCAM",
    "field": "camera"
  }
}
```

### 5. Unpaginated Manifest Endpoint

The `/manifests/:id` endpoint returns potentially thousands of sol entries without pagination support. For Curiosity with 4000+ sols, this creates unnecessarily large responses.

## What I Would Do Differently

### 1. Consistent Resource Identification
```
# Use numeric IDs consistently
GET /api/v1/rovers/1/photos

# OR use slugs with clear case handling
GET /api/v1/rovers/curiosity/photos  # Always lowercase
```

### 2. Required vs Optional Parameters
Make it explicit which parameters are required:
```
GET /api/v1/rovers/:rover_id/photos
# Returns 400: "Either 'sol' or 'earth_date' parameter is required"

GET /api/v1/rovers/:rover_id/photos?sol=1000
# Returns photos for sol 1000
```

### 3. Comprehensive Error Responses
Implement RFC 7807 Problem Details:
```json
{
  "type": "/errors/invalid-date-format",
  "title": "Invalid Date Format",
  "status": 400,
  "detail": "The date '2015-13-45' is not valid. Expected format: YYYY-MM-DD",
  "instance": "/api/v1/rovers/curiosity/photos?earth_date=2015-13-45"
}
```

### 4. Query Builder Pattern
Instead of multiple filtering parameters, support a query DSL:
```
GET /api/v1/photos?q=rover:curiosity+AND+(sol:1000+OR+date:2015-06-03)+AND+camera:FHAZ
```
This scales better for complex queries without URL length issues.

### 5. Cursor-Based Pagination
Replace page-based with cursor-based pagination for better performance and consistency:
```json
{
  "data": [...],
  "cursor": {
    "next": "eyJzb2wiOjEwMDEsImlkIjo0NTY3fQ==",
    "prev": "eyJzb2wiOjk5OSwiaWQiOjQ1NjV9",
    "has_next": true,
    "has_prev": true
  }
}
```

### 6. Field Selection
Allow clients to specify which fields they need:
```
GET /api/v1/rovers/curiosity/photos?fields=id,img_src,sol
```
This reduces payload size and improves performance.

### 7. Batch Operations
Support fetching multiple resources efficiently:
```
GET /api/v1/photos/batch?ids=123,456,789
```

### 8. Standardized Response Envelope
Consistent response structure across all endpoints:
```json
{
  "data": {...},
  "meta": {
    "total_count": 1000,
    "filtered_count": 25,
    "page": 1,
    "per_page": 25
  },
  "links": {
    "self": "/api/v1/rovers/curiosity/photos?sol=1000&page=1",
    "next": "/api/v1/rovers/curiosity/photos?sol=1000&page=2",
    "prev": null
  }
}
```

### 9. OpenAPI Specification
Provide machine-readable API documentation:
```yaml
openapi: 3.0.0
paths:
  /api/v1/rovers/{rover_id}/photos:
    get:
      parameters:
        - name: sol
          in: query
          schema:
            type: integer
            minimum: 0
            maximum: 10000
```

### 10. Rate Limiting Headers
Include rate limit information in responses:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1620000000
```

## Alternative Approach: Event-Driven API

Instead of REST, consider an event-driven approach for real-time updates:

```javascript
// WebSocket connection for live updates
ws.subscribe('rover.curiosity.new_photos', (event) => {
  console.log('New photos available:', event.photos);
});

// Combined with REST for historical queries
GET /api/v1/photos?after=2024-01-01&before=2024-12-31
```

## Performance Optimizations

### 1. Response Compression
Always gzip responses, especially for manifest endpoints.

### 2. ETags and Conditional Requests
Support caching with ETags:
```
GET /api/v1/rovers/curiosity/photos?sol=1000
If-None-Match: "686897696a7c8761"
# Returns 304 Not Modified if unchanged
```

### 3. Database Query Optimization
Current N+1 risks in manifest generation could be resolved with better eager loading.

## Security Considerations

### 1. Input Sanitization
All string parameters should be sanitized to prevent injection attacks.

### 2. Request Size Limits
Enforce maximum values for `per_page` and query complexity.

### 3. CORS Configuration
Properly configure CORS for browser-based clients.

## Conclusion

The Mars Photo API has a solid foundation with good REST principles and domain-appropriate design. The main improvements needed are:

1. **Consistency** - Standardize parameter handling across endpoints
2. **Validation** - Comprehensive input validation with clear error messages
3. **Documentation** - Machine-readable API specification
4. **Performance** - Better pagination for large datasets
5. **Error Handling** - Distinguish between empty results and invalid queries

These changes would significantly improve the developer experience without requiring a complete redesign. The current architecture is appropriate for the domain - it just needs refinement in implementation details.