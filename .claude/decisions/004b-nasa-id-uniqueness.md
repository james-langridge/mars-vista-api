# Decision 004B: NASA ID Uniqueness Strategy

**Status:** Active
**Date:** 2025-11-13
**Context:** Story 004 - Define Core Domain Entities

## Context

When scraping NASA's Mars rover APIs, we need a way to prevent duplicate photos from being imported multiple times. The scraper will run on a schedule (hourly, daily) and needs to know which photos already exist in our database.

NASA provides unique identifiers for photos, but different rovers use different identifier schemes:
- **Curiosity:** Uses `imageid` field (e.g., "458574869")
- **Perseverance:** Uses `id` field (e.g., "NLB_458574869EDR_F0541800NCAM00354M_")
- **Spirit/Opportunity:** HTML scraping, uses image filename

We need a strategy to:
1. Uniquely identify photos across imports
2. Prevent duplicate inserts
3. Enable fast "does this photo exist?" lookups
4. Handle different identifier formats across rovers

## Requirements

- **Uniqueness:** One photo appears exactly once in database
- **Performance:** Fast duplicate detection (< 1ms per check)
- **Reliability:** Database enforces uniqueness (can't be bypassed)
- **Simplicity:** Easy for scraper to use
- **Cross-rover:** Works for all rover APIs

## Alternatives

### Alternative 1: No Uniqueness Constraint

**Implementation:**
```csharp
public class Photo
{
    public int Id { get; set; }  // Auto-increment only
    public string NasaId { get; set; }  // No unique constraint
}
```

**Scraper logic:**
```csharp
// Check manually before insert
var exists = await context.Photos
    .AnyAsync(p => p.NasaId == nasaId);

if (!exists)
{
    context.Photos.Add(photo);
}
```

**Pros:**
- Simple entity definition
- Flexible (can store duplicates if needed)

**Cons:**
- **Race condition:** Two scrapers can insert same photo
- **Human error:** Easy to forget the check
- **No database enforcement:** Can bypass via raw SQL
- **Slow:** Requires query for every photo
- **Data corruption:** Duplicates will accumulate

**Example failure:**
```csharp
// Scraper 1
var exists = await context.Photos.AnyAsync(p => p.NasaId == "ABC");  // false
// ... context switch ...
// Scraper 2
var exists = await context.Photos.AnyAsync(p => p.NasaId == "ABC");  // false
// Both insert → duplicate!
```

### Alternative 2: Application-Level Locking

**Implementation:**
```csharp
private static readonly SemaphoreSlim _lock = new(1, 1);

public async Task ImportPhoto(string nasaId)
{
    await _lock.WaitAsync();
    try
    {
        var exists = await context.Photos.AnyAsync(p => p.NasaId == nasaId);
        if (!exists)
        {
            context.Photos.Add(photo);
            await context.SaveChangesAsync();
        }
    }
    finally
    {
        _lock.Release();
    }
}
```

**Pros:**
- Prevents race conditions within one process
- Fast (no database constraint check)

**Cons:**
- **Doesn't work across multiple servers** (lock is in-memory)
- **Complex code:** Manual locking in every import path
- **Brittle:** Easy to forget locking somewhere
- **Still allows duplicates** via raw SQL or other applications
- **Deadlock risk** if locks not released properly

**Real-world scenario:**
- Deploy API to 3 servers (load balanced)
- All 3 run scraper simultaneously
- Each has own in-memory lock
- All 3 insert the same photo → 3 duplicates

### Alternative 3: Unique Index on NasaId (RECOMMENDED)

**Implementation:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public string NasaId { get; set; }
}

// EF Core configuration
modelBuilder.Entity<Photo>(entity =>
{
    entity.HasIndex(e => e.NasaId).IsUnique();
});
```

**Scraper logic:**
```csharp
// Simple: Just try to insert
try
{
    context.Photos.Add(photo);
    await context.SaveChangesAsync();
}
catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
{
    // Photo already exists, skip
    _logger.LogDebug("Photo {NasaId} already exists", photo.NasaId);
}
```

**Or use upsert logic:**
```csharp
// Find-or-create pattern
var existing = await context.Photos
    .FirstOrDefaultAsync(p => p.NasaId == nasaId);

if (existing == null)
{
    context.Photos.Add(photo);
}
else
{
    // Update if needed
    existing.UpdatedAt = DateTime.UtcNow;
}
```

**Pros:**
- **Database enforces uniqueness** - impossible to bypass
- **Race condition safe** - database handles concurrency
- **Fast lookups** - index makes `WHERE nasa_id = ?` instant
- **Works across multiple servers** - database is single source of truth
- **Simple code** - no manual locking needed
- **Fail-fast** - duplicate insert throws immediately

**Cons:**
- Exception-based flow if using try/catch approach (acceptable)
- Slightly slower inserts due to index check (~0.1ms overhead)

**Performance:**
```sql
-- Without index: Full table scan
SELECT * FROM photos WHERE nasa_id = 'ABC';  -- 500ms for 1M rows

-- With unique index: Instant
SELECT * FROM photos WHERE nasa_id = 'ABC';  -- 0.5ms
```

### Alternative 4: Composite Unique Index (Rover + NasaId)

**Implementation:**
```csharp
modelBuilder.Entity<Photo>(entity =>
{
    entity.HasIndex(e => new { e.RoverId, e.NasaId }).IsUnique();
});
```

**Reasoning:**
- What if different rovers can have same photo ID?
- Add rover_id to uniqueness constraint

**Pros:**
- Allows same ID across rovers (if needed)

**Cons:**
- **NASA IDs are globally unique** - they include rover info
  - Curiosity: "NLB_458574869EDR_F0541800NCAM00354M_"
  - Perseverance: "NRB_458574869EDR_F0541800ZCAM08352M_"
- **More complex queries:** Need both rover_id and nasa_id
- **Unnecessary complexity** for no benefit

**Analysis:**
NASA photo IDs include:
- Rover indicator (NLB = Curiosity, NRB = Perseverance)
- Camera info
- Timestamp
- Sequence number

They are globally unique by design.

### Alternative 5: Surrogate Natural Key

**Implementation:**
```csharp
public class Photo
{
    // No auto-increment Id
    public string NasaId { get; set; }  // Primary key

    // Other fields...
}

modelBuilder.Entity<Photo>(entity =>
{
    entity.HasKey(e => e.NasaId);
});
```

**Pros:**
- Natural key as primary key
- One less column (no separate Id)
- Foreign keys reference NASA ID directly

**Cons:**
- **Variable-length primary key** (200 chars)
- **Slower joins** (string comparison vs integer)
- **Larger indexes** (200 bytes vs 4 bytes)
- **Less flexible** if NASA ID format changes
- **Harder to reference** in foreign keys

**Performance comparison:**
```sql
-- Integer primary key: 4 bytes
JOIN photos ON photos.id = comments.photo_id

-- String primary key: 200 bytes
JOIN photos ON photos.nasa_id = comments.photo_nasa_id
```

For 1M photos:
- Integer index: 4MB
- String index: 200MB

**Not recommended** - surrogate integer keys are better for databases.

## Decision

**Use unique index on `nasa_id` column (Alternative 3)**

### Rationale

1. **Database enforces uniqueness** - can't be bypassed by code bugs
2. **Race condition safe** - works across multiple servers/processes
3. **Fast duplicate detection** - indexed lookups are instant (0.5ms)
4. **Simple scraper code** - no manual locking or complex checks
5. **Fail-fast** - duplicate insert throws immediately with clear error

### Implementation Strategy

**Entity:**
```csharp
public class Photo
{
    public int Id { get; set; }                    // Surrogate key (auto-increment)
    public string NasaId { get; set; }             // Natural key (unique index)
    // other fields...
}
```

**EF Core configuration:**
```csharp
modelBuilder.Entity<Photo>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.NasaId).IsUnique();
    entity.Property(e => e.NasaId)
        .HasMaxLength(200)
        .IsRequired();
});
```

**Scraper pattern (find-or-create):**
```csharp
public async Task<Photo> ImportPhoto(NasaPhotoData nasaData)
{
    var existing = await _context.Photos
        .FirstOrDefaultAsync(p => p.NasaId == nasaData.Id);

    if (existing != null)
    {
        _logger.LogDebug("Photo {NasaId} already exists", nasaData.Id);
        return existing;
    }

    var photo = new Photo
    {
        NasaId = nasaData.Id,
        Sol = nasaData.Sol,
        // ... map other fields
    };

    _context.Photos.Add(photo);
    await _context.SaveChangesAsync();

    _logger.LogInformation("Imported photo {NasaId}", photo.NasaId);
    return photo;
}
```

**Bulk import optimization:**
```csharp
// Get all existing NASA IDs in one query
var nasaIds = nasaPhotos.Select(p => p.Id).ToList();
var existingIds = await _context.Photos
    .Where(p => nasaIds.Contains(p.NasaId))
    .Select(p => p.NasaId)
    .ToListAsync();

// Filter out existing photos
var newPhotos = nasaPhotos
    .Where(p => !existingIds.Contains(p.Id))
    .Select(MapToEntity)
    .ToList();

// Bulk insert only new photos
_context.Photos.AddRange(newPhotos);
await _context.SaveChangesAsync();

_logger.LogInformation("Imported {Count} new photos", newPhotos.Count);
```

## Trade-offs

**Accepted:**
- ~0.1ms overhead per insert (index maintenance)
- ~200MB storage for index on 1M photos (200 chars each)
- Exception thrown on duplicate (handled gracefully)

**Gained:**
- **Guaranteed uniqueness** - no duplicates possible
- **Fast lookups** - 0.5ms vs 500ms full scan
- **Simple code** - no manual locking
- **Concurrent safe** - multiple scrapers can run
- **Reliable** - database enforces constraint

## Validation Criteria

Success metrics:
- Inserting duplicate photo throws `DbUpdateException`
- Finding photo by `nasa_id` takes < 1ms
- Multiple scrapers can run concurrently without duplicates
- Bulk import efficiently filters out existing photos

Test cases:
```csharp
[Fact]
public async Task ImportPhoto_DuplicateNasaId_ThrowsException()
{
    var photo1 = new Photo { NasaId = "ABC123", /* ... */ };
    context.Photos.Add(photo1);
    await context.SaveChangesAsync();

    var photo2 = new Photo { NasaId = "ABC123", /* ... */ };  // Same NASA ID
    context.Photos.Add(photo2);

    await Assert.ThrowsAsync<DbUpdateException>(() =>
        context.SaveChangesAsync()
    );
}

[Fact]
public async Task FindByNasaId_WithIndex_IsFast()
{
    // Insert 100k photos
    // ...

    var stopwatch = Stopwatch.StartNew();
    var photo = await context.Photos
        .FirstOrDefaultAsync(p => p.NasaId == "ABC123");
    stopwatch.Stop();

    Assert.True(stopwatch.ElapsedMilliseconds < 10);  // Should be < 1ms
}
```

## References

- [PostgreSQL Unique Indexes](https://www.postgresql.org/docs/current/indexes-unique.html)
- [EF Core Indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
- [Database Normalization Best Practices](https://learn.microsoft.com/en-us/office/troubleshoot/access/database-normalization-description)

## Related Decisions

- **Decision 004:** Entity field selection (defines NasaId field)
- **Future:** Bulk import optimization strategy (Story 006)

## Notes

### NASA ID Format Examples

**Curiosity (older format):**
```
"imageid": "458574869"
```

**Perseverance (newer format):**
```
"id": "NLB_458574869EDR_F0541800NCAM00354M_"
```

**Spirit/Opportunity (legacy):**
```
filename: "1P131591893EFF0500P2363L2M1.JPG"
```

We store the identifier NASA provides, regardless of format. The unique index ensures no duplicates regardless of identifier scheme.

### Index Size Calculation

For 1 million photos:
- NASA ID length: ~50-200 characters average
- Index entry: string + pointer = ~200 bytes
- Total index size: 1M × 200 = 200MB

Acceptable for uniqueness guarantee and fast lookups.

### Future: Compound Indexes

We might add compound indexes for common queries:
```csharp
entity.HasIndex(e => new { e.RoverId, e.NasaId });  // Find by rover + NASA ID
entity.HasIndex(e => new { e.NasaId, e.DateTakenUtc });  // Find + sort by date
```

But the primary unique index on `nasa_id` alone is sufficient for uniqueness enforcement.
