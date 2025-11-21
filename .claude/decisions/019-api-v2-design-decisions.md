# Technical Decision 019: API v2 Design Architecture

## Context

Based on 6 independent API design critiques, we're redesigning the API from scratch as v2 while keeping v1 for NASA compatibility. With no production users yet, we can implement the ideal design without legacy constraints.

## Decision Drivers

1. **Universal critique findings**: All 6 reviews identified the same critical issues
2. **No production users**: Freedom to make breaking changes
3. **NASA compatibility need**: Some users expect NASA API format
4. **Modern expectations**: Developers expect features like field selection, batch operations
5. **Performance at scale**: Current design has N+1 issues and unbounded results

## Considered Options

### Option 1: Incremental v1 Improvements
- Fix critical issues in v1
- Add new features carefully
- Maintain backwards compatibility

**Pros:**
- Single API version to maintain
- Gradual improvement path
- No migration needed

**Cons:**
- Limited by original design flaws
- Can't fix fundamental issues
- Inconsistencies remain

### Option 2: Complete v2 Redesign (Chosen)
- Keep v1 exactly as NASA API
- Build v2 from scratch with modern patterns
- No backwards compatibility constraints for v2

**Pros:**
- Clean, consistent design
- Implement all modern features
- Learn from all critiques
- NASA compatibility preserved in v1

**Cons:**
- Two API versions to maintain
- More initial development work

### Option 3: GraphQL-First Approach
- Replace REST with GraphQL
- Provide REST compatibility layer

**Pros:**
- Solves field selection perfectly
- Flexible querying
- Single request for complex queries

**Cons:**
- Larger paradigm shift
- Steeper learning curve
- More complex caching

## Decision

**Implement Option 2: Complete v2 Redesign with dual API versions**

## Detailed Design Decisions

### 1. Versioning Strategy

**Decision:** Parallel v1/v2 operation
```
/api/v1/ - NASA-compatible (unchanged)
/api/v2/ - Modern redesign
```

**Rationale:**
- v1 serves as drop-in NASA replacement
- v2 unconstrained by legacy design
- Clear migration path for users

### 2. Resource Architecture

**Decision:** Unified photos endpoint with powerful filtering

**v1 (NASA-style):**
```
GET /api/v1/rovers/:rover_id/photos
GET /api/v1/rovers/:rover_id/latest_photos
GET /api/v1/manifests/:id
```

**v2 (Unified):**
```
GET /api/v2/photos?rovers=curiosity,perseverance&cameras=FHAZ,MAST
GET /api/v2/rovers/curiosity/manifest
```

**Rationale:**
- Single endpoint reduces complexity
- Enables cross-rover queries
- More flexible filtering

### 3. Response Format

**Decision:** Consistent envelope inspired by JSON:API

```json
{
  "data": [...],
  "meta": { "total_count": 1000 },
  "pagination": { "page": 1, "per_page": 25 },
  "links": { "next": "...", "prev": "..." }
}
```

**Rationale:**
- Predictable structure
- Clear separation of concerns
- Room for metadata
- Industry-familiar pattern

### 4. Error Handling

**Decision:** RFC 7807 Problem Details

```json
{
  "type": "/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "Invalid date format",
  "errors": [{
    "field": "date_min",
    "message": "Must be YYYY-MM-DD"
  }]
}
```

**Rationale:**
- Industry standard
- Machine-readable
- Detailed field-level errors
- Helpful for debugging

### 5. Pagination

**Decision:** Always paginate with hybrid approach

```csharp
// Always paginate
var pageSize = Math.Min(request.PerPage ?? 25, 100);

// Support both page and cursor
if (request.Cursor != null)
    return CursorPaginate(request.Cursor, pageSize);
else
    return PagePaginate(request.Page ?? 1, pageSize);
```

**Rationale:**
- Prevents performance issues
- Supports both pagination styles
- Clear limits and defaults

### 6. Field Selection

**Decision:** Implement sparse fieldsets from start

```
GET /api/v2/photos?fields=id,img_src,sol
GET /api/v2/photos?include=rover,camera
```

**Rationale:**
- Reduces bandwidth
- Improves performance
- Standard pattern
- Optional but powerful

### 7. Parameter Naming

**Decision:** Consistent snake_case with clear patterns

```
rovers (plural for arrays)
cameras (plural for arrays)
sol_min, sol_max (ranges)
date_min, date_max (ranges)
sort (single parameter with multiple values)
```

**Rationale:**
- Predictable patterns
- Clear intent
- Industry standard
- Avoid ambiguity

### 8. Caching Strategy

**Decision:** HTTP caching with content-based ETags

```csharp
// Inactive rovers - immutable
Cache-Control: public, max-age=31536000

// Active rovers - short TTL
Cache-Control: public, max-age=3600
ETag: "sha256-hash-of-content"
```

**Rationale:**
- Leverages HTTP standards
- Reduces server load
- Improves performance
- Client-controlled

### 9. Query Capabilities

**Decision:** Rich filtering without query DSL

```
Simple: ?rovers=curiosity
Multiple: ?rovers=curiosity,perseverance
Ranges: ?sol_min=100&sol_max=200
Combined: ?rovers=curiosity&cameras=MAST&date_min=2023-01-01
```

**Not using:** Complex query DSL like `?q=rover:curiosity AND sol:[100 TO 200]`

**Rationale:**
- Simple to understand
- Easy to construct
- URL-readable
- Covers 99% of use cases

### 10. Batch Operations

**Decision:** Dedicated batch endpoint for specific operations

```
POST /api/v2/photos/batch
{
  "ids": [123, 456, 789]
}
```

**Rationale:**
- Solves N+1 client problem
- Clear, simple interface
- POST for body support
- Optional enhancement

## Implementation Priority

### Phase 1: Critical (Must Have)
1. Error handling framework
2. Always-on pagination
3. Unified photos endpoint
4. Basic filtering (rover, camera, date)

### Phase 2: Important (Should Have)
1. Field selection
2. HTTP caching headers
3. Range queries
4. Discovery endpoints

### Phase 3: Nice to Have (Could Have)
1. Batch operations
2. Statistics endpoints
3. Cursor pagination
4. Export functionality

## Consequences

### Positive
- Clean, consistent API design
- Solves all critical issues identified
- Modern developer experience
- High performance from start
- Clear migration path from NASA API

### Negative
- Maintaining two API versions
- Initial development effort
- Documentation for both versions
- Testing overhead

### Neutral
- Learning curve for v2 features
- Migration effort for future users

## Validation

Success metrics:
1. All 6 critique's critical issues resolved
2. < 100ms response time for typical queries
3. Zero N+1 query problems
4. 100% of requests properly paginated
5. Clear error messages for all failure cases

## Notes

- This decision assumes we have the freedom to redesign
- v1 remains exactly as NASA API for compatibility
- v2 is our chance to build it right
- Focus on developer experience over implementation ease
- Design for scale even if starting small