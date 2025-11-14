# Decision 006C: Unknown Camera Handling Strategy

**Status:** Active
**Date:** 2025-11-13
**Story:** 006 - NASA API Scraper Service

## Context

When scraping photos, we may encounter cameras not in our seed data:
- NASA adds new instruments to active rovers
- Camera name typos or variations in NASA data
- Our seed data is incomplete or outdated

What should the scraper do when it encounters an unknown camera?

## Options Considered

### Option 1: Auto-Create Camera with Warning (Recommended)

Automatically create the camera record and log a warning:

```csharp
var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

if (camera == null)
{
    _logger.LogWarning(
        "Unknown camera discovered: {CameraName} for {RoverName}. Auto-creating.",
        cameraName, rover.Name);

    camera = new Camera
    {
        Name = cameraName,
        FullName = cameraName, // Placeholder
        RoverId = rover.Id
    };

    _context.Cameras.Add(camera);
    await _context.SaveChangesAsync();
    rover.Cameras.Add(camera);

    _logger.LogInformation("Created camera {CameraName} with ID {CameraId}",
        cameraName, camera.Id);
}

return camera;
```

**Pros:**
- Scraper never crashes on unknown cameras
- Photo data is not lost
- Graceful degradation
- Warning alerts developers to investigate
- Can update camera full name later manually
- Resilient to NASA changes

**Cons:**
- Camera full name is placeholder (same as name)
- Requires manual follow-up to set full name
- Could mask seed data bugs

### Option 2: Throw Exception and Stop

Fail loudly when unknown camera found:

```csharp
var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

if (camera == null)
{
    throw new InvalidOperationException(
        $"Unknown camera: {cameraName} for rover {rover.Name}. " +
        "Update seed data and restart scraper.");
}
```

**Pros:**
- Forces developers to fix seed data
- No placeholder data in database
- Explicit about missing data

**Cons:**
- Scraper crashes (entire batch lost)
- Manual intervention required
- Photo data lost until scraper rerun
- Not resilient to NASA changes
- Bad operational experience

### Option 3: Skip Photo with Unknown Camera

Continue scraping, but skip photos with unknown cameras:

```csharp
var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

if (camera == null)
{
    _logger.LogWarning("Skipping photo with unknown camera: {CameraName}", cameraName);
    return null; // Skip this photo
}
```

**Pros:**
- Scraper doesn't crash
- No placeholder data

**Cons:**
- **Photo data is lost**
- May lose many photos silently
- Hard to notice unless monitoring logs
- Poor data completeness

### Option 4: Queue for Manual Review

Store unknown camera photos in separate "pending" table:

```csharp
if (camera == null)
{
    var pendingPhoto = new PendingPhoto { RawData = imageElement, CameraName = cameraName };
    _context.PendingPhotos.Add(pendingPhoto);
    _logger.LogWarning("Photo queued for review: unknown camera {CameraName}", cameraName);
    return null;
}
```

**Pros:**
- Data not lost
- Can review and reprocess later
- Explicit workflow

**Cons:**
- Complex additional system (pending table, review UI)
- Photos not immediately available
- Over-engineered for rare event
- Requires manual reprocessing

### Option 5: Use Default "Unknown" Camera

Create single "Unknown" camera for all unknown instruments:

```csharp
var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

if (camera == null)
{
    camera = rover.Cameras.First(c => c.Name == "UNKNOWN");
    _logger.LogWarning("Using UNKNOWN camera for {CameraName}", cameraName);
}
```

**Pros:**
- Simple fallback
- Photos not lost

**Cons:**
- Loses camera information (all grouped as "UNKNOWN")
- Can't query photos by actual camera later
- Data quality degradation
- Misleading to users

## Decision

**Use Option 1: Auto-Create Camera with Warning**

## Reasoning

### Why Auto-Create?

1. **Resilience to NASA Changes:**
   - NASA occasionally adds new instruments
   - Example: Perseverance got SuperCam after landing
   - Scraper shouldn't crash on data source changes

2. **Data Preservation:**
   - Photo data is valuable
   - Don't lose photos due to missing camera
   - Can fix camera metadata later

3. **Graceful Degradation:**
   - Camera name is correct (from NASA)
   - Full name placeholder until manual update
   - Better than no data

4. **Operational Excellence:**
   - Warning logs alert developers
   - Can set up alerts: "Unknown camera discovered"
   - Fix at leisure, not emergency

5. **Rare Event:**
   - Happens occasionally (new instruments)
   - Not worth complex queue system
   - Simple auto-create sufficient

### When Does This Happen?

**Legitimate Cases:**
- NASA adds new instrument to active rover
- Our seed data is incomplete for older rovers
- Camera name variations we didn't anticipate

**Error Cases:**
- Typo in NASA data (rare)
- Our seed data has typo
- API format change

Both cases handled gracefully with auto-create + warning.

### Implementation Pattern

```csharp
private async Task<Camera> GetOrCreateCameraAsync(
    string cameraName,
    Rover rover,
    CancellationToken cancellationToken)
{
    // Try to find existing camera
    var camera = rover.Cameras.FirstOrDefault(c => c.Name == cameraName);

    if (camera != null)
        return camera;

    // Unknown camera - auto-create with warning
    _logger.LogWarning(
        "Unknown camera discovered: {CameraName} for {RoverName}. Auto-creating. " +
        "Please update camera full_name in database.",
        cameraName, rover.Name);

    camera = new Camera
    {
        Name = cameraName,
        FullName = cameraName, // Placeholder - update manually
        RoverId = rover.Id
        // Timestamps set by database defaults
    };

    _context.Cameras.Add(camera);
    await _context.SaveChangesAsync(cancellationToken);

    // Add to rover's collection (in-memory cache)
    rover.Cameras.Add(camera);

    _logger.LogInformation(
        "Created new camera: {CameraName} (ID: {CameraId}) for {RoverName}",
        cameraName, camera.Id, rover.Name);

    return camera;
}
```

### Why Save Immediately?

```csharp
await _context.SaveChangesAsync(cancellationToken);
```

- Need `camera.Id` for photo foreign key
- Subsequent photos in same batch can reuse
- Added to `rover.Cameras` in-memory collection
- No database query for next photo with same camera

### Logging Levels

**Warning:** Unknown camera discovered
- Requires attention but not urgent
- Can monitor and set alerts

**Information:** Camera created successfully
- Operational log
- Good for audit trail

**Debug:** Using existing camera
- Too noisy for normal operation

## Trade-offs Accepted

### Placeholder Full Name
- **Accepted:** Camera created with `full_name = name`
- **Why it's OK:**
  - Name is correct (from NASA)
  - Full name can be updated later via SQL
  - Better than no data
- **Manual Fix:**
  ```sql
  UPDATE cameras
  SET full_name = 'SuperCam Remote Micro Imager'
  WHERE name = 'SUPERCAM_RMI' AND full_name = 'SUPERCAM_RMI';
  ```

### Manual Follow-up Required
- **Accepted:** Developer must update full name manually
- **Why it's OK:**
  - Rare event (maybe once per year)
  - Warning log makes it obvious
  - Can set up monitoring alert
  - Not time-critical

### Could Mask Seed Data Bugs
- **Accepted:** Typo in seed data would auto-create duplicate
- **Why it's OK:**
  - Seed data reviewed (Story 005)
  - Easy to spot in database (similar names)
  - Can fix with SQL merge
- **Prevention:** Good seed data testing

## Alternatives Rejected

### Why Not Throw Exception? (Option 2)
- Crashes entire scrape batch
- Loses all photos in batch
- Terrible operational experience
- Manual emergency intervention required

### Why Not Skip Photo? (Option 3)
- **Data loss** - unacceptable
- Silently loses photos
- Hard to notice
- Defeats purpose of complete data storage

### Why Not Queue System? (Option 4)
- Over-engineered for rare event
- Adds complexity (table, UI, reprocessing)
- Photos not immediately available
- Not worth the cost

### Why Not "Unknown" Camera? (Option 5)
- Loses camera information
- All unknowns grouped together
- Can't query by actual camera
- Bad data quality

## Validation

This strategy is validated by:
- ✅ Scraper doesn't crash on unknown cameras
- ✅ Warning logged when camera auto-created
- ✅ Photo data preserved with correct camera name
- ✅ Camera full name can be updated later
- ✅ Subsequent photos reuse auto-created camera
- ✅ No duplicate camera creation in same batch

## Monitoring & Alerts

**Set up alert for unknown cameras:**

```csharp
// Application Insights or similar
if (camera was auto-created)
{
    _telemetry.TrackEvent("UnknownCameraDiscovered", new Dictionary<string, string>
    {
        { "CameraName", cameraName },
        { "RoverName", rover.Name },
        { "CameraId", camera.Id.ToString() }
    });
}
```

**Alert rule:**
- Trigger: "UnknownCameraDiscovered" event
- Notification: Email/Slack to dev team
- Action: Review NASA API, update seed data if needed

## Historical Example

**Real NASA case:**
- Perseverance landed February 18, 2021
- Ingenuity helicopter added April 2021
- If we scraped helicopter photos, auto-create would handle it
- Update full name manually: "Ingenuity Navigation Camera"

## Related Decisions

- [Decision 005: Database Seeding Strategy](005-seeding-strategy.md) - Initial camera seed
- [Decision 006: Scraper Service Pattern](006-scraper-service-pattern.md) - Scraper design
- [Decision 006B: Duplicate Photo Detection](006b-duplicate-detection.md) - Photo idempotency

## References

- [Graceful Degradation](https://en.wikipedia.org/wiki/Graceful_degradation)
- [Resilient System Design](https://learn.microsoft.com/en-us/azure/architecture/framework/resiliency/principles)
- [NASA Mars 2020 Instruments](https://mars.nasa.gov/mars2020/spacecraft/instruments/)
