# Curiosity Rover Scraper Guide

Complete guide for running the Curiosity scraper.

> **See Also:**
> - [API Endpoints Documentation](API_ENDPOINTS.md) - All API endpoints with examples
> - [Database Access Guide](DATABASE_ACCESS.md) - Database queries and management

---

## Quick Start

### 1. Start the API
```bash
cd src/MarsVista.Api
dotnet run
```

The API will start on `http://localhost:5127`

### 2. Run Bulk Scrape

**Small test (sols 1-10):**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=10"
```

**Full scrape (all 4,683 sols):**
```bash
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=4683"
```

**Resume from specific sol:**
```bash
# If scraping stopped at sol 2000, resume from there
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=2000&endSol=4683"
```

### 3. Monitor Progress

```bash
./scrape-monitor.sh curiosity
```

This displays a real-time dashboard with:
- Total photos scraped
- Progress percentage
- Speed (photos/sec)
- ETA
- Sol range

**Expected Performance:**
- ~500 photos per sol
- ~25 photos/sec
- **Total time: 9-10 hours** for all 4,683 sols

---

## Curiosity Cameras

Curiosity has 7 different camera systems:

| Camera Code | Full Name | Description |
|-------------|-----------|-------------|
| MAST | Mast Camera (Mastcam) | Color imaging, stereo pair |
| NAVCAM | Navigation Camera | Black and white, wide angle |
| FHAZ | Front Hazard Avoidance Camera | Obstacle detection |
| RHAZ | Rear Hazard Avoidance Camera | Rear obstacle detection |
| CHEMCAM | Chemistry and Camera Complex | Remote micro-imager |
| MAHLI | Mars Hand Lens Imager | Close-up imaging |
| MARDI | Mars Descent Imager | Landing sequence |

---

## Implementation Details

### NASA API Endpoint

Curiosity uses a different API than Perseverance:
```
https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc&per_page=200&condition_1=msl:mission&condition_2={sol}:sol:in
```

### Camera Name Mapping

NASA's API returns instrument variations like:
- `MAST_LEFT`, `MAST_RIGHT` → mapped to `MAST`
- `NAV_LEFT_A`, `NAV_RIGHT_B` → mapped to `NAVCAM`
- `FHAZ_LEFT_A`, `FHAZ_RIGHT_B` → mapped to `FHAZ`

The `MapInstrumentToCamera()` function in `CuriosityScraper.cs` handles this mapping automatically.

### Data Storage

Each photo stores:
1. **Indexed columns** - sol, earth_date, camera, NASA ID, etc.
2. **Image URLs** - `img_src_full` (HTTPS), `img_src_small` (HTTP thumbnail)
3. **Telemetry data** - site, drive, xyz coordinates, spacecraft clock
4. **Camera orientation** - mast_az, mast_el, camera vectors
5. **Complete NASA response** - stored as JSONB in `raw_data` column

This preserves 100% of NASA's data for future features.

---

## Verification

### Check Progress

```bash
curl "http://localhost:5127/api/scraper/curiosity/progress"
```

### Query Photos

```bash
# Get photos from sol 1
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=1"

# Filter by camera
curl "http://localhost:5127/api/v1/rovers/curiosity/photos?sol=100&camera=MAHLI"

# Get latest photos
curl "http://localhost:5127/api/v1/rovers/curiosity/latest?per_page=10"
```

### Database Verification

```bash
# Check photo count
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev \
  -c "SELECT COUNT(*) FROM photos WHERE rover_id = 2;"

# Check sol coverage
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev \
  -c "SELECT MIN(sol), MAX(sol), COUNT(DISTINCT sol) FROM photos WHERE rover_id = 2;"
```

See [Database Access Guide](DATABASE_ACCESS.md) for more queries.

---

## Troubleshooting

### Port Already in Use

```bash
# Kill process on port 5127
lsof -ti:5127 | xargs kill -9

# Restart API
cd src/MarsVista.Api && dotnet run
```

### Scraper Stalled

```bash
# Check status
curl "http://localhost:5127/api/scraper/curiosity/progress"

# Resume from latest sol
curl -X POST "http://localhost:5127/api/scraper/curiosity/resume"
```

### Empty Image URLs in Query API

This was a bug in the `PhotoQueryService` that has been fixed. The null coalescing operator `??` doesn't work with empty strings, only null values.

**Fixed in:** `src/MarsVista.Api/Services/PhotoQueryService.cs`
```csharp
// Before (broken):
ImgSrc = p.ImgSrcMedium ?? p.ImgSrcLarge ?? p.ImgSrcFull ?? ""

// After (fixed):
ImgSrc = !string.IsNullOrEmpty(p.ImgSrcMedium) ? p.ImgSrcMedium :
         !string.IsNullOrEmpty(p.ImgSrcLarge) ? p.ImgSrcLarge :
         !string.IsNullOrEmpty(p.ImgSrcFull) ? p.ImgSrcFull : ""
```

### Wrong Database Credentials

**Correct credentials:**
- Host: localhost:5432
- Database: `marsvista_dev` (not `marsvista_db`)
- Username: `marsvista` (not `marsvista_user`)
- Password: `marsvista_dev_password`

See [Database Access Guide](DATABASE_ACCESS.md) for details.

---

## Next Steps

After completing the Curiosity scrape:

1. **Implement retry script** for any failed sols
2. **Add other rovers** (Opportunity, Spirit) using similar pattern
3. **Build advanced features** using the rich JSONB data:
   - Panorama stitching (using mast orientation data)
   - Location-based queries (using xyz coordinates)
   - Stereo pair matching (using camera vectors)

---

## See Also

- [API Endpoints Documentation](API_ENDPOINTS.md)
- [Database Access Guide](DATABASE_ACCESS.md)
- [Main README](../README.md)
- [CuriosityScraper.cs](../src/MarsVista.Api/Services/CuriosityScraper.cs)
