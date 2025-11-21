# Technical Decision 019: API v2 Design with Full NASA Data

## Context

We have 100% of NASA data stored (rich indexed columns + complete JSONB), not just the 5% the original API exposed. This fundamentally changes what our v2 API can offer.

**Available Data:**
- 4 image URLs (small/medium/large/full) vs original's 1
- Mars local time for temporal queries
- Location data (site/drive/xyz) for spatial queries
- Camera telemetry (mast angles) for panorama detection
- Image dimensions and quality metadata
- Complete NASA response in JSONB for future features

## Decision Drivers

1. **We have the data**: 100% NASA data stored, why expose only 5%?
2. **Unique value proposition**: Offer features NASA API doesn't have
3. **Modern expectations**: Field selection, rich filtering, relationships
4. **Scientific use cases**: Researchers need telemetry and location data
5. **Performance**: Multiple image sizes reduce bandwidth needs

## Considered Options

### Option 1: Minimal v2 (Match NASA Fields)
Expose only what NASA API exposes, keep extra data hidden.

**Pros:**
- Simple migration from NASA API
- Smaller response payloads
- Less documentation needed

**Cons:**
- Wastes our rich data
- No differentiation from NASA
- Misses revolutionary features

### Option 2: Full Exposure (Everything)
Expose all fields by default in standard responses.

**Pros:**
- Maximum data availability
- No field selection needed
- Complete transparency

**Cons:**
- Large response sizes
- Overwhelming for simple use cases
- Performance impact

### Option 3: Progressive Disclosure (Chosen)
Default to reasonable field set, allow field selection for more.

**Pros:**
- Right-sized responses
- Discoverable complexity
- Optimizable per use case
- Backwards compatible growth

**Cons:**
- Field selection complexity
- Multiple response formats

## Decision

**Implement Option 3: Progressive Disclosure with rich defaults**

## Detailed Design Decisions

### 1. Field Set Strategy

**Decision:** Tiered field sets with sensible defaults

```csharp
public enum FieldSet
{
    Minimal,    // id, sol, images.medium - for galleries
    Standard,   // + earth_date, camera, rover - default
    Extended,   // + location, dimensions, mars_time - rich features
    Scientific, // + telemetry, coordinates - researchers
    Complete    // + raw_data - everything
}
```

**Default:** Standard field set unless specified

**Usage:**
```
GET /api/v2/photos                    # Standard fields
GET /api/v2/photos?fields=minimal     # Just essentials
GET /api/v2/photos?fields=scientific  # Research data
GET /api/v2/photos?fields=id,sol,images.large  # Custom
```

**Rationale:**
- Progressive complexity
- Optimized for use case
- Discoverable through documentation
- Backwards compatible additions

### 2. Image URL Strategy

**Decision:** Object with multiple sizes, not single URL

```json
"images": {
  "small": "https://..._320.jpg",   // Thumbnails, lists
  "medium": "https://..._800.jpg",  // Default viewing
  "large": "https://..._1200.jpg",  // Detailed viewing
  "full": "https://...png"          // Download, analysis
}
```

**Not:**
```json
"img_src": "https://..._1200.jpg"  // Single size
```

**Rationale:**
- Client chooses appropriate size
- Reduces bandwidth up to 75%
- Progressive image loading
- Responsive design support

### 3. Temporal Query Design

**Decision:** Support both Earth and Mars time systems

```
# Earth-based (familiar)
?date_min=2023-01-01&date_max=2023-12-31

# Sol-based (mission-centric)
?sol_min=1000&sol_max=2000

# Mars time-based (NEW - lighting conditions)
?mars_time_min=M06:00:00&mars_time_max=M07:00:00
?mars_time_golden_hour=true
```

**Rationale:**
- Mars time enables sunrise/sunset queries
- Scientists think in sols
- Public thinks in Earth dates
- Photographers want lighting conditions

### 4. Location Query Design

**Decision:** Multi-level location queries

```
# Exact location
?site=79&drive=1204

# Proximity search
?site=79&drive=1204&location_radius=5

# Range search
?site_min=70&site_max=80

# Coordinate-based (future)
?xyz_near=35.4,22.5,-9.4&xyz_radius=100
```

**Rationale:**
- Enables journey visualization
- Supports "photos near landing site"
- Allows geological surveys
- Future-proof for coordinate systems

### 5. Advanced Feature Endpoints

**Decision:** Dedicated endpoints for complex features

```
/api/v2/panoramas          # Auto-detected panoramic sequences
/api/v2/stereo-pairs       # Matched left/right photos
/api/v2/locations          # Unique sites visited
/api/v2/time-machine       # Same location, different times
/api/v2/journey            # Rover path visualization
```

**Not:** Complex query parameters on photos endpoint
```
/api/v2/photos?detect_panoramas=true&group_by_sequence=true
```

**Rationale:**
- Cleaner API design
- Cacheable results
- Specialized response formats
- Room for feature growth

### 6. Response Enrichment

**Decision:** Include computed metadata

```json
"meta": {
  "is_panorama_part": true,
  "panorama_sequence_id": "seq_1000_14",
  "has_stereo_pair": true,
  "stereo_pair_id": 123457,
  "lighting_conditions": "golden_hour",
  "location_visits": 3
}
```

**Rationale:**
- Server computes expensive relationships
- Client doesn't need complex logic
- Enables UI features immediately
- Leverages our data advantage

### 7. Query Capability Expansion

**Decision:** Rich, intuitive query parameters

**Quality Filters:**
```
?min_width=1920
?aspect_ratio=16:9
?sample_type=Full
```

**Camera Angles:**
```
?mast_elevation_min=-5&mast_elevation_max=5  # Horizon
?mast_azimuth_min=90&mast_azimuth_max=180    # Eastward
```

**Combinations:**
```
?rovers=curiosity,perseverance
&mars_time_golden_hour=true
&min_width=1920
&site_min=70&site_max=80
```

**Rationale:**
- Natural parameter names
- Powerful combinations
- Discoverable patterns
- Covers real use cases

### 8. Performance Strategy

**Decisions:**

**Materialized Views:**
```sql
CREATE MATERIALIZED VIEW panorama_sequences AS ...
CREATE MATERIALIZED VIEW stereo_pairs AS ...
CREATE MATERIALIZED VIEW location_visits AS ...
```

**JSONB Indexes:**
```sql
CREATE INDEX idx_raw_data_extended ON photos
USING gin ((raw_data->'extended'));
```

**Response Caching:**
- Inactive rovers: immutable, cache forever
- Active rovers: 1 hour standard, 5 min for latest
- Panoramas/stereo: daily detection, cache results
- Analytics: 6 hour cache

**Rationale:**
- Pre-compute expensive operations
- Leverage PostgreSQL capabilities
- Appropriate cache TTLs
- Scales with load

### 9. Migration Strategy

**Decision:** Three-tier approach

```
Tier 1: /api/v1/ - NASA-compatible (unchanged)
Tier 2: /api/v2/ - Rich modern API (new)
Tier 3: /api/graphql - Future flexibility (planned)
```

**v1 → v2 Migration Path:**
```
v1: GET /api/v1/rovers/curiosity/photos?sol=1000
v2: GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000

// With field selection to match v1 response:
v2: GET /api/v2/photos?rovers=curiosity&sol=1000&fields=minimal
```

### 10. Documentation Strategy

**Decision:** Interactive discovery

```
GET /api/v2/capabilities
{
  "available_fields": {
    "minimal": ["id", "sol", "images.medium"],
    "standard": ["...", "earth_date", "camera"],
    "extended": ["...", "location", "mars_time"]
  },
  "queryable_fields": {
    "mars_time": {
      "description": "Mars local solar time",
      "format": "Sol-XXXXMhh:mm:ss",
      "example": "Sol-1000M14:23:45"
    }
  },
  "special_endpoints": {
    "panoramas": "Auto-detected panoramic sequences",
    "stereo-pairs": "Matched left/right camera pairs"
  }
}
```

## Implementation Priority

### Phase 1: Core Value (Week 1)
1. Photos endpoint with rich default fields
2. Multiple image sizes
3. Mars time queries
4. Location queries
5. Field selection

### Phase 2: Differentiation (Week 2)
1. Panorama detection
2. Stereo pair matching
3. Journey visualization
4. Time machine
5. Location timeline

### Phase 3: Platform Features (Week 3)
1. Analytics endpoints
2. ML-based recommendations
3. Export/batch operations
4. Real-time updates for active rovers

## Consequences

### Positive
- **Unique value**: Features NASA doesn't offer
- **Scientific utility**: Researchers get telemetry data
- **Performance**: Multiple image sizes save bandwidth
- **Future-proof**: JSONB allows new features without schema changes
- **Discoverable**: Progressive complexity

### Negative
- **Complexity**: More parameters and endpoints
- **Documentation**: More to explain
- **Caching**: Complex invalidation rules
- **Storage**: Materialized views add overhead

### Risks & Mitigations

**Risk**: Overwhelming complexity
**Mitigation**: Progressive disclosure, good defaults, clear documentation

**Risk**: Performance with rich queries
**Mitigation**: Materialized views, proper indexes, caching

**Risk**: Breaking changes as we discover patterns
**Mitigation**: Versioned API, deprecation policy

## Validation Metrics

1. **Performance**
   - p50 < 50ms for standard queries
   - p95 < 200ms for complex filters
   - p99 < 500ms for panorama detection

2. **Data Utilization**
   - 80% of stored fields exposed via API
   - 100% accessible via raw_data
   - 0% data loss from original NASA

3. **Developer Adoption**
   - 50% of requests use field selection
   - 30% use advanced features (panoramas, etc)
   - 90% satisfaction vs NASA API

## Decision Outcome

Build v2 as a **data-rich exploration platform**, not just a photo API. Leverage our 100% NASA data storage to offer revolutionary features while maintaining simplicity through progressive disclosure.

The key insight: **We have the data to build something NASA doesn't offer**. Let's use it.