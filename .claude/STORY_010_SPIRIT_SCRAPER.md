# Story 010: Spirit Rover Scraper

## Status
- **State**: Planning
- **Priority**: Medium
- **Estimated Effort**: 2-4 hours
- **Dependencies**: Story 009 (Opportunity Scraper) ✅

## Context

Spirit (MER-2) was Opportunity's twin rover that landed on Mars on January 3, 2004, three weeks before Opportunity. Spirit operated until March 2010 when it became stuck in soft sand and eventually lost communication. Like Opportunity, Spirit's data is only available through PDS (Planetary Data System) index files, not NASA's JSON API.

**Mission Statistics:**
- Landing: January 3, 2004
- Last Communication: March 22, 2010
- Mission Duration: ~6 years
- Total Sols: ~2,269
- Status: Mission ended (stuck in sand, communication lost)

## Requirements

### Functional Requirements

1. **Scrape Spirit photos from PDS index files**
   - Support all 5 camera volumes (PANCAM, NAVCAM, HAZCAM, MI, DESCENT)
   - Parse tab-delimited edrindex.tab files with variable field counts
   - Handle both 59-field (standard) and 52-field (DESCENT) formats

2. **Camera Support**
   - PANCAM (Panoramic Camera) - mer2ps_0xxx
   - NAVCAM (Navigation Camera) - mer2ns_0xxx
   - HAZCAM (Front/Rear Hazard Avoidance) - mer2hs_0xxx
   - MI (Microscopic Imager) - mer2ms_0xxx
   - DESCENT (Entry, Descent, Landing Camera) - mer2ds_0xxx

3. **API Endpoints**
   - POST /api/scraper/spirit/volume/{volumeName} - Scrape single volume
   - POST /api/scraper/spirit/all - Scrape all volumes sequentially
   - GET /api/scraper/spirit/progress - Monitor scraping progress

4. **Data Storage**
   - Store all 55+ PDS metadata fields in raw_data JSONB column
   - Extract essential fields to indexed columns (sol, date, camera, etc.)
   - Map Spirit's MER2 instrument IDs to database camera names
   - Generate browse JPG URLs from PDS data paths

### Non-Functional Requirements

1. **Performance**
   - Use in-memory HashSet for duplicate checking (learned from Opportunity)
   - Batch inserts of 1000 photos per commit
   - Stream parsing for large index files (avoid loading into memory)
   - Target: ~1000 photos per second throughput

2. **Reliability**
   - Idempotent scraping (can resume from failure)
   - Graceful handling of duplicate key constraints
   - Entity detachment on batch errors
   - Continue processing after individual photo failures

3. **Data Integrity**
   - 100% metadata preservation in raw_data column
   - Accurate field parsing for both 59-field and 52-field formats
   - Correct field offset handling for DESCENT camera (-2 offset)

## Learnings from Opportunity Implementation

### Critical Issues Discovered and Resolved

#### 1. Variable Field Counts by Camera Type
**Discovery**: Different camera volumes have different field counts
- PANCAM, NAVCAM, HAZCAM, MI: 59 fields
- DESCENT: 52 fields

**Solution**: Dynamic field count detection in parser

#### 2. DESCENT Field Structure Mismatch
**Problem**: DESCENT is missing PathName AND FileName fields (not just PathName)
- Standard format: VolumeId, DataSetId, InstrumentHostId, InstrumentId, **PathName**, **FileName**, ReleaseId, ProductId...
- DESCENT format: VolumeId, DataSetId, InstrumentHostId, InstrumentId, ReleaseId, ProductId, ProductCreationTime...

**Solution**: Field offset of -2 for DESCENT (not -1)
- Detect DESCENT by: 52 fields + DESCAM instrument ID
- Set PathName and FileName to empty strings
- Apply -2 offset to all subsequent field reads

#### 3. Camera Name Mapping Issues
**Problem**: PDS uses full descriptive names, not abbreviations
- Expected: FHAZ_LEFT, RHAZ_RIGHT
- Actual: FRONT_HAZCAM_LEFT, REAR_HAZCAM_RIGHT

**Solution**: Add full PDS instrument names to MerCameraMapper
```csharp
{ "FRONT_HAZCAM_LEFT", "FHAZ" },
{ "FRONT_HAZCAM_RIGHT", "FHAZ" },
{ "REAR_HAZCAM_LEFT", "RHAZ" },
{ "REAR_HAZCAM_RIGHT", "RHAZ" },
{ "DESCAM", "ENTRY" },
```

#### 4. Performance Degradation
**Problem**: Scraper slowed to 1 photo/second near completion
**Root Cause**: Individual database query for each duplicate check
**Solution**: Load all existing NASA IDs into HashSet at start
- O(1) in-memory lookup vs database query
- ~1000x performance improvement
- Update HashSet after each successful batch insert

#### 5. Batch Insert Failures
**Problem**: Duplicate key constraint violations stalled scraper
**Solution**:
- Wrap batch inserts in try-catch
- Track pending NASA IDs in HashSet to detect in-batch duplicates
- Detach failed entities to prevent EF tracking issues
- Clear pending batch and continue processing

### Metadata Superiority

**PDS vs NASA JSON API Comparison:**
- Mast telemetry (azimuth/elevation): PDS 100% vs NASA API 30%
- Solar position data: PDS 100% vs NASA API 0%
- Mars local time: PDS 100% vs NASA API 30%
- Filter names: Both 100%

**PDS provides complete scientific metadata** for every single photo, enabling:
- Panorama building (sequential mast angles)
- Stereo pair finding (LEFT/RIGHT matching)
- Shadow analysis (solar position)
- Time-based search (Mars local time)
- Seasonal analysis (solar longitude)

## Spirit-Specific Considerations

### Volume Names
Spirit uses MER2 prefix (vs Opportunity's MER1):
- mer2ps_0xxx - PANCAM
- mer2ns_0xxx - NAVCAM
- mer2hs_0xxx - HAZCAM
- mer2ms_0xxx - MI (Microscopic Imager)
- mer2ds_0xxx - DESCENT

### Instrument IDs
Spirit should use same PDS instrument naming as Opportunity:
- PANCAM_LEFT / PANCAM_RIGHT
- NAVCAM_LEFT / NAVCAM_RIGHT
- FRONT_HAZCAM_LEFT / FRONT_HAZCAM_RIGHT
- REAR_HAZCAM_LEFT / REAR_HAZCAM_RIGHT
- MI
- DESCAM

### Expected Photo Count
Spirit's shorter mission (~6 years vs Opportunity's 14.5 years) means:
- Estimated total: 200,000-400,000 photos
- Fewer than Opportunity's 548,817 photos
- Similar distribution across cameras

### Database Configuration
- Rover ID: 4 (Spirit)
- Camera seeds: Already exist in database (inserted in Story 003)
- Should map to cameras with rover_id = 4

## Implementation Plan

### Phase 1: Service Reuse (1 hour)
1. **Reuse Existing Services** - NO changes needed
   - PdsIndexParser already handles variable field counts ✅
   - MerCameraMapper already has full PDS instrument names ✅
   - PdsBrowseUrlBuilder already has Spirit volume names ✅
   - PdsIndexRow already handles 55+ fields ✅

2. **Create SpiritScraper Service**
   - Copy OpportunityScraper.cs → SpiritScraper.cs
   - Change rover name from "Opportunity" to "Spirit"
   - Change rover ID from 3 to 4
   - Update volume names to use mer2 prefix (mer2ps, mer2ns, etc.)
   - Update base URL path from "opportunity" to "spirit"

### Phase 2: Controller Integration (30 minutes)
1. **Add Spirit Endpoints to ScraperController**
   - POST /api/scraper/spirit/volume/{volumeName}
   - POST /api/scraper/spirit/all
   - GET /api/scraper/spirit/progress

2. **Register DI Services**
   - Add SpiritScraper as keyed scoped service
   - Register with "spirit" key

### Phase 3: Testing & Validation (1 hour)
1. **Test Single Volume Scrape**
   - Start with smallest volume (DESCENT - only ~9 photos)
   - Verify correct field parsing
   - Check ProductIds are valid
   - Confirm camera mapping works

2. **Test Complete Scrape**
   - Run all 5 volumes
   - Monitor performance (should match Opportunity ~1000/sec)
   - Verify in-memory duplicate checking works
   - Check final photo counts

3. **Verify Data Quality**
   - Confirm all cameras have photos
   - Check metadata completeness in raw_data
   - Verify browse URLs are accessible
   - Validate sol ranges match mission duration

### Phase 4: Documentation (30 minutes)
1. **Create SPIRIT_SCRAPER_GUIDE.md**
   - Copy from OPPORTUNITY_SCRAPER_GUIDE.md
   - Update rover-specific details
   - Document Spirit's mission history
   - Include scraping statistics

2. **Update API_ENDPOINTS.md**
   - Add Spirit scraper endpoints
   - Include example curl commands
   - Document expected photo counts

3. **Update README.md**
   - Add Spirit to supported rovers list
   - Update total photo count statistics
   - Note mission completion status

## Technical Decisions

### Decision 1: Reuse vs Refactor
**Decision**: Reuse existing services with minimal changes
**Rationale**:
- PDS format is identical for Spirit and Opportunity (both MER rovers)
- All complexity already handled in shared services
- Spirit scraper only needs rover-specific configuration
- Reduces duplication and maintenance burden

**Alternative Considered**: Create abstract base class for MER scrapers
**Rejected Because**: Only 2 MER rovers (Opportunity and Spirit), not worth abstraction overhead

### Decision 2: Volume Processing Order
**Decision**: Process volumes sequentially (PANCAM → NAVCAM → HAZCAM → MI → DESCENT)
**Rationale**:
- Same order as Opportunity for consistency
- PANCAM is largest, process first for early feedback
- DESCENT is smallest, process last (lowest priority)
- Single-threaded to avoid database contention

### Decision 3: Duplicate Checking Strategy
**Decision**: Use in-memory HashSet loaded at volume start
**Rationale**:
- Proven 1000x performance improvement from Opportunity scraper
- Spirit has fewer photos than Opportunity, fits easily in memory
- Critical for performance with 200K-400K photos

### Decision 4: Error Handling
**Decision**: Continue processing on batch errors, log and skip
**Rationale**:
- Same strategy as Opportunity (proven to work)
- Allows scraper to complete even with partial failures
- Failed photos can be retried individually later
- Entity detachment prevents EF tracking issues

## Acceptance Criteria

### Functional
- [ ] All 5 Spirit camera volumes can be scraped individually
- [ ] POST /api/scraper/spirit/all scrapes all volumes sequentially
- [ ] Progress endpoint returns accurate counts during scraping
- [ ] Photos are correctly mapped to Spirit cameras (rover_id = 4)
- [ ] Browse JPG URLs are correctly constructed with mer2 prefix
- [ ] DESCENT camera photos parse with correct ProductIds

### Data Quality
- [ ] All cameras have photos (6 of 6 cameras)
- [ ] FHAZ photos exist (was 0 in initial Opportunity scrape)
- [ ] RHAZ photos exist (was 0 in initial Opportunity scrape)
- [ ] ENTRY/DESCENT photos exist with valid ProductIds
- [ ] All photos have complete metadata in raw_data JSONB
- [ ] Sol ranges match Spirit's mission (sols 1-2269 approximately)

### Performance
- [ ] Scraping achieves ~1000 photos/second throughput
- [ ] No performance degradation near completion
- [ ] In-memory duplicate checking works correctly
- [ ] Memory usage stays reasonable (HashSet overhead acceptable)

### Reliability
- [ ] Scraper can be stopped and resumed without data loss
- [ ] Duplicate photos are skipped correctly
- [ ] Batch errors don't crash scraper
- [ ] Entity Framework tracking issues don't occur

## Estimated Photo Counts

Based on Spirit's 6-year mission and Opportunity's 14.5-year mission:

| Camera | Opportunity | Spirit (Est.) | Reasoning |
|--------|-------------|---------------|-----------|
| PANCAM | 366,503 | ~150,000 | 41% of mission duration |
| NAVCAM | 119,926 | ~50,000 | 41% of mission duration |
| FHAZ | 25,573 | ~10,000 | 41% of mission duration |
| RHAZ | 12,041 | ~5,000 | 41% of mission duration |
| MINITES | 24,765 | ~10,000 | 41% of mission duration |
| ENTRY | 9 | ~9 | Same landing sequence |
| **Total** | **548,817** | **~225,000** | 41% of Opportunity total |

Note: Actual counts may vary based on science operations and mission events.

## Known Issues & Solutions

### Issue 1: Rover ID Mismatch
**Problem**: Spirit rover seed data uses rover_id = 4
**Solution**: Verify seed data before scraping
```sql
SELECT id, name FROM rovers WHERE name = 'Spirit';
-- Expected: id = 4
```

### Issue 2: Camera Seeds
**Problem**: Camera names must match exactly
**Solution**: Verify camera seeds exist
```sql
SELECT id, name FROM cameras WHERE rover_id = 4;
-- Expected: 6 cameras (FHAZ, RHAZ, NAVCAM, PANCAM, MINITES, ENTRY)
```

### Issue 3: PDS URL Changes
**Problem**: PDS URLs may have changed since Opportunity implementation
**Solution**: Test index file accessibility before scraping
```bash
curl -I https://planetarydata.jpl.nasa.gov/img/data/mer/spirit/mer2ps_0xxx/index/edrindex.tab
# Should return 200 OK
```

### Issue 4: DESCENT Format Variations
**Problem**: Spirit's DESCENT may have different field count than Opportunity
**Solution**: Parser already handles variable field counts, but verify
- Check first DESCENT row has 52 fields
- Verify offset -2 produces correct ProductIds

## Scripts to Create

### scrape-spirit-all.sh
```bash
#!/bin/bash
# Scrape all Spirit volumes sequentially

API_URL="http://localhost:5127"

echo "=========================================="
echo "Spirit Complete Scraper"
echo "=========================================="
echo ""
echo "This will scrape all 5 Spirit camera volumes:"
echo "  - mer2ps_0xxx (PANCAM)"
echo "  - mer2ns_0xxx (NAVCAM)"
echo "  - mer2hs_0xxx (HAZCAM)"
echo "  - mer2ms_0xxx (MI)"
echo "  - mer2ds_0xxx (DESCENT)"
echo ""

curl -X POST "$API_URL/api/scraper/spirit/all"

echo ""
echo "Scrape complete! Check database for results."
```

### scrape-spirit-status.sh
```bash
#!/bin/bash
# Quick status check for Spirit photos

PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev << 'SQL'
SELECT
  c.name as camera,
  COUNT(p.id) as photos,
  MIN(p.sol) as min_sol,
  MAX(p.sol) as max_sol
FROM cameras c
LEFT JOIN photos p ON c.id = p.camera_id
WHERE c.rover_id = 4
GROUP BY c.id, c.name
ORDER BY c.id;

SELECT COUNT(*) as total_spirit_photos FROM photos WHERE rover_id = 4;
SQL
```

## Success Metrics

- All 5 Spirit camera volumes successfully scraped
- 200,000-400,000 photos imported (estimated range)
- All 6 cameras have photos (100% coverage)
- 100% metadata preservation in raw_data
- Performance matches Opportunity scraper (~1000/sec)
- Zero data loss incidents
- Documentation complete and accurate

## Related Documentation

- `.claude/STORY_009_OPPORTUNITY_SCRAPER.md` - Sister implementation
- `.claude/MER_DATA_SOURCE_INVESTIGATION.md` - PDS format research
- `docs/OPPORTUNITY_SCRAPER_GUIDE.md` - Implementation reference
- `docs/API_ENDPOINTS.md` - API documentation
- `docs/DATABASE_ACCESS.md` - Database queries

## Notes

- Spirit and Opportunity are twin rovers with identical hardware
- Spirit's mission ended prematurely due to getting stuck in sand
- Spirit's data is equally rich as Opportunity's (100% PDS metadata)
- Implementation should be nearly identical to Opportunity scraper
- Main differences are rover_id and volume name prefixes (mer2 vs mer1)
- Completing Spirit scraper gives us **all 4 active/historic Mars rovers**
