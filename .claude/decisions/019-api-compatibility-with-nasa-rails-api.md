# Decision 019: API Compatibility with NASA Mars Photo Rails API

**Date**: 2025-11-16
**Status**: Proposed (Awaiting Decision)
**Priority**: High (affects API adoption)

## Context

The Mars Vista API is functionally equivalent to the original NASA Mars Photo Rails API (https://github.com/corincerami/mars-photo-api), but has two breaking changes that prevent it from being a drop-in replacement:

1. **Field naming convention**: camelCase (`imgSrc`, `earthDate`) vs snake_case (`img_src`, `earth_date`)
2. **Endpoint name**: `/latest` vs `/latest_photos`

The original Heroku deployment of the Rails API is currently down (404 error), making Mars Vista API a strong candidate to be the new standard. However, existing users of the NASA API would need to modify their code to switch.

## Current Differences

### Field Naming
**Original Rails API**:
```json
{
  "id": 123,
  "sol": 1000,
  "img_src": "https://...",
  "earth_date": "2015-05-30",
  "camera": { "name": "MAST", "full_name": "Mast Camera" },
  "rover": { "landing_date": "2012-08-06" }
}
```

**Mars Vista API (current)**:
```json
{
  "id": 123,
  "sol": 1000,
  "imgSrc": "https://...",
  "earthDate": "2015-05-30",
  "camera": { "name": "MAST", "fullName": "Mast Camera" },
  "rover": { "landingDate": "2012-08-06" }
}
```

### Endpoint Naming
- **Original**: `/api/v1/rovers/{name}/latest_photos`
- **Mars Vista**: `/api/v1/rovers/{name}/latest`

## API Equivalence Analysis

### ✅ Feature Parity (100% equivalent)

| Feature | Rails API | Mars Vista API | Status |
|---------|-----------|----------------|--------|
| GET /api/v1/rovers | ✅ | ✅ | ✅ Equivalent |
| GET /api/v1/rovers/{name} | ✅ | ✅ | ✅ Equivalent |
| GET /api/v1/rovers/{name}/photos | ✅ | ✅ | ✅ Equivalent |
| GET /api/v1/photos/{id} | ✅ | ✅ | ✅ Equivalent |
| GET /api/v1/manifests/{name} | ✅ | ✅ | ✅ Equivalent |
| Query by sol (?sol=X) | ✅ | ✅ | ✅ Equivalent |
| Query by earth_date | ✅ | ✅ | ✅ Equivalent |
| Query by camera | ✅ | ✅ | ✅ Equivalent |
| Pagination (?page, ?per_page) | ✅ | ✅ | ✅ Equivalent |
| All 4 rovers supported | ✅ | ✅ | ✅ Equivalent |

### ⭐ Mars Vista Advantages

1. **More complete data**: Stores all 30-55 NASA fields (JSONB + indexed columns) vs 10-15 fields
2. **Richer MER data**: PDS scraper for Opportunity/Spirit provides 55 fields vs NASA API's 10
3. **Actually online**: Original Heroku deployment returns 404
4. **Modern stack**: .NET 9, PostgreSQL 15 vs Rails 4.x
5. **Built-in admin**: Scraper endpoints for data updates
6. **Better performance**: Optimized indexes, query patterns

### ❌ Breaking Changes

1. **Field naming**: camelCase vs snake_case (JavaScript-friendly vs Ruby convention)
2. **Endpoint name**: `/latest` vs `/latest_photos` (shorter vs explicit)

## Proposed Solutions

### Option 1: Support Both Formats (Dual Fields) ❌ Not Recommended

Return both naming conventions in every response:

```json
{
  "id": 123,
  "img_src": "https://...",
  "imgSrc": "https://...",
  "earth_date": "2015-05-30",
  "earthDate": "2015-05-30"
}
```

**Pros**:
- 100% backward compatible
- No breaking changes
- Works for everyone immediately

**Cons**:
- ~30% larger response size (wasted bandwidth)
- Messy and redundant
- Confusing for new users
- Poor API design

**Decision**: ❌ Rejected - not worth the bloat

### Option 2: Query Parameter for Format ✅ Recommended

Add `?format=snake_case` or `?format=camelCase` query parameter:

```bash
# Original API compatibility (default)
GET /api/v1/rovers/curiosity/photos?sol=1000
# Returns: { "img_src": "...", "earth_date": "..." }

# Modern JavaScript-friendly
GET /api/v1/rovers/curiosity/photos?sol=1000&format=camelCase
# Returns: { "imgSrc": "...", "earthDate": "..." }
```

**Implementation**:
1. Default JSON serialization to snake_case (backward compatible)
2. Add `/latest_photos` endpoint (alias to `/latest`)
3. Accept `?format=camelCase` parameter to switch to camelCase
4. Document migration path clearly

**Pros**:
- ✅ Drop-in compatible (just change base URL)
- ✅ Clean responses (no duplication)
- ✅ Explicit user control
- ✅ Easy to document
- ✅ Supports modern JavaScript frameworks that prefer camelCase

**Cons**:
- Adds small complexity to serialization layer
- Need to maintain two serialization formats

**Decision**: ✅ **Recommended** - best balance of compatibility and clean design

### Option 3: Version in URL Path ❌ Not Recommended

Create separate endpoints:
- `/api/v1/*` - snake_case (original compatible)
- `/api/v2/*` - camelCase (modern)

**Pros**:
- Clear separation
- Standard API versioning

**Cons**:
- Doubles maintenance burden
- Confusing for users (same data, different URLs)
- Not really a "version" change (same functionality)

**Decision**: ❌ Rejected - overkill for naming convention

### Option 4: Default to camelCase, Break Compatibility ❌ Not Recommended

Accept the breaking change and just document it:

**Pros**:
- Simpler codebase
- Modern JavaScript convention

**Cons**:
- ❌ Forces all existing users to update code
- ❌ Not a drop-in replacement
- ❌ Reduces adoption

**Decision**: ❌ Rejected - hurts adoption

## Recommended Implementation

### Changes Required

1. **Add JSON serialization format switching** (~20 min)
   - Middleware to detect `?format=` parameter
   - Snake_case converter for System.Text.Json
   - Default to snake_case

2. **Add `/latest_photos` endpoint alias** (~5 min)
   - Map to existing `/latest` endpoint
   - Document both endpoints

3. **Update response DTOs** (~10 min)
   - Add JsonPropertyName attributes for snake_case
   - Keep class properties in camelCase (C# convention)

4. **Documentation** (~15 min)
   - Add migration guide from NASA Rails API
   - Document both formats
   - Example requests for both conventions

**Total effort**: ~50 minutes

### Migration Guide (for documentation)

```markdown
## Migrating from NASA Mars Photo Rails API

Mars Vista API is a drop-in replacement for the original NASA Mars Photo API.

### Quick Migration (Zero Code Changes)

Simply replace the base URL:

**Before**:
```
https://api.nasa.gov/mars-photos/api/v1/...
https://mars-photos.herokuapp.com/api/v1/...
```

**After**:
```
https://api.marsvista.dev/api/v1/...
```

All query parameters and response formats remain the same.

### Modern JavaScript Applications

For JavaScript/TypeScript applications that prefer camelCase:

```javascript
// Add ?format=camelCase to any endpoint
const response = await fetch(
  'https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&format=camelCase'
);

// Response uses camelCase: imgSrc, earthDate, fullName, etc.
const { photos } = await response.json();
console.log(photos[0].imgSrc); // camelCase properties
```

### Breaking Changes from NASA API

**None** - 100% backward compatible when using default format.

### Enhanced Features

Mars Vista API includes additional features not in the original:
- All 30-55 NASA metadata fields (vs 10-15)
- Richer Opportunity/Spirit data from PDS archives
- Health check endpoint: `GET /health`
- Built-in scraper status: `GET /api/scraper/{rover}/progress`
```

## Future Considerations

### When to deprecate snake_case default?

**Option A**: Never (always support both)
- Pros: Maximum compatibility forever
- Cons: Maintenance burden

**Option B**: After 2 years, switch default to camelCase
- Pros: Eventually aligns with modern conventions
- Cons: Requires migration announcement

**Recommendation**: Support both indefinitely. The serialization overhead is minimal, and maximizing compatibility helps adoption.

### API Key Parameter Compatibility

The original NASA API requires `?api_key=DEMO_KEY`. Mars Vista currently doesn't.

**Future**: When implementing API keys (Story 015):
- Accept `?api_key=` parameter for compatibility
- Also accept modern `Authorization: Bearer <token>` header
- Make API key optional for public tier

## Comparison: Mars Vista vs NASA Rails API

### Data Completeness

**Production Database** (Mars Vista):
- Perseverance: 451,602 photos ✅
- Curiosity: 675,765 photos ✅
- Opportunity: 548,817 photos ✅
- Spirit: 301,336 photos ✅
- **Total**: 1,977,520 photos

**Original Rails API**:
- Heroku deployment: **DOWN** (404 error)
- Data completeness: Unknown (can't verify)
- Field coverage: ~10-15 fields per photo

**NASA Official API**:
- Only provides 10-15 fields
- Mars Vista's JSONB storage preserves **all** NASA fields

### Marketing Position

**Mars Vista API should be positioned as:**

> "Modern alternative to NASA's Mars Rover Photo API - same interface, richer data, always online"

**Key selling points**:
1. Drop-in replacement for the original (now-defunct) NASA Rails API
2. More complete metadata (all 55 PDS fields for MER rovers)
3. Modern infrastructure (.NET 9, PostgreSQL 15, Railway Pro)
4. Actually maintained and online
5. Better performance with optimized indexes
6. Built-in admin tools

### Target Users

1. **Existing NASA API users** (original is down):
   - Just change base URL → works immediately
   - Zero code changes required

2. **New JavaScript developers**:
   - Add `?format=camelCase` for natural integration
   - Documented examples in React/Vue/Angular

3. **Data scientists/researchers**:
   - Access to complete NASA metadata (55 fields vs 10)
   - JSONB storage enables custom queries

4. **Educational projects**:
   - Free tier (no API key required initially)
   - Well-documented, easy to use
   - Supports all 4 rovers

## Decision

**Status**: Awaiting decision from developer

**Recommended**: Implement Option 2 (query parameter for format)

**Rationale**:
- Maximizes adoption (drop-in replacement)
- Clean implementation (no response bloat)
- Supports both legacy and modern users
- Minimal development effort (~50 minutes)
- No ongoing maintenance burden

**Next Steps if Approved**:
1. Implement snake_case JSON serialization
2. Add `/latest_photos` endpoint alias
3. Update documentation with migration guide
4. Test with examples from original NASA API docs
5. Deploy to production
6. Market as drop-in replacement

## Related Decisions

- **Story 015**: API key system (should support `?api_key=` for compatibility)
- **Decision TBD**: URL structure (`api.marsvista.dev` vs `marsvista.dev`)

## References

- Original NASA Mars Photo Rails API: https://github.com/corincerami/mars-photo-api
- Heroku deployment: https://mars-photos.herokuapp.com (currently 404)
- NASA Official API: https://api.nasa.gov/mars-photos/api/v1/
