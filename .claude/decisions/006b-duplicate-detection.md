# Decision 006B: Duplicate Photo Detection Strategy

**Status:** Active
**Date:** 2025-11-13
**Story:** 006 - NASA API Scraper Service

## Context

The scraper will run multiple times (hourly, daily, manually triggered). We need to prevent inserting duplicate photos into the database. How do we detect and skip photos that already exist?

## Options Considered

### Option 1: Database Query Before Insert (Recommended)

Check if photo exists using `AnyAsync` on `nasa_id`:

```csharp
var nasaId = imageElement.GetProperty("imageid").GetString();

var exists = await _context.Photos
    .AnyAsync(p => p.NasaId == nasaId, cancellationToken);

if (exists)
{
    _logger.LogDebug("Photo {NasaId} already exists, skipping", nasaId);
    continue;
}

// Insert photo
_context.Photos.Add(photo);
```

**Pros:**
- Simple and reliable
- Leverages unique index on `nasa_id`
- Works across multiple scraper instances
- No extra state management
- Database is source of truth

**Cons:**
- One query per photo (could be slow)
- Network round trip to database

**Performance:**
- With unique index: ~0.5ms per check
- For 100 photos: ~50ms overhead
- Acceptable for background scraping

### Option 2: Batch Check All nasa_ids

Query all nasa_ids in one query:

```csharp
var nasaIds = images.Select(img => img.GetProperty("imageid").GetString()).ToList();

var existingIds = await _context.Photos
    .Where(p => nasaIds.Contains(p.NasaId))
    .Select(p => p.NasaId)
    .ToListAsync();

var existingSet = new HashSet<string>(existingIds);

foreach (var image in images)
{
    var nasaId = image.GetProperty("imageid").GetString();
    if (existingSet.Contains(nasaId))
        continue;
    // Insert
}
```

**Pros:**
- Single database query
- Faster for large batches (1000+ photos)
- Less database load

**Cons:**
- More complex code
- Memory overhead (HashSet)
- Not necessary for typical batch sizes (50-200 photos)
- Premature optimization

**Performance:**
- 1 query vs 100 queries
- ~5ms vs ~50ms
- Saves 45ms (negligible for background job)

### Option 3: Insert and Catch Unique Constraint Violation

Try to insert, catch duplicate key exception:

```csharp
try
{
    _context.Photos.Add(photo);
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex) when (IsDuplicateKeyError(ex))
{
    _logger.LogDebug("Photo {NasaId} already exists (duplicate key)", nasaId);
    // Continue
}
```

**Pros:**
- No pre-check needed
- "Optimistic" approach
- Fast when most photos are new

**Cons:**
- Exception handling for control flow (anti-pattern)
- Slower when many duplicates
- Transaction rollback overhead
- Can't batch insert (need try/catch per photo)
- Hard to distinguish duplicate from other DB errors

### Option 4: In-Memory Cache of nasa_ids

Keep cache of all nasa_ids in memory:

```csharp
private static HashSet<string> _existingNasaIds = new();

public async Task InitializeCacheAsync()
{
    _existingNasaIds = (await _context.Photos
        .Select(p => p.NasaId)
        .ToListAsync()).ToHashSet();
}

public async Task ScrapeAsync()
{
    foreach (var image in images)
    {
        var nasaId = image.GetProperty("imageid").GetString();
        if (_existingNasaIds.Contains(nasaId))
            continue;

        // Insert
        _existingNasaIds.Add(nasaId);
    }
}
```

**Pros:**
- Very fast lookups (O(1))
- No database queries during scrape

**Cons:**
- Memory overhead (~40MB for 1M photos)
- Cache invalidation complexity
- Doesn't work with multiple scraper instances
- Stale data if photos deleted
- Premature optimization

### Option 5: Upsert (INSERT ... ON CONFLICT DO NOTHING)

Use PostgreSQL's upsert:

```csharp
await _context.Database.ExecuteSqlRawAsync(@"
    INSERT INTO photos (nasa_id, sol, rover_id, ...)
    VALUES (@nasaId, @sol, @roverId, ...)
    ON CONFLICT (nasa_id) DO NOTHING
", parameters);
```

**Pros:**
- Database handles duplicates
- Single operation (no check)
- Atomic

**Cons:**
- Bypasses EF Core tracking
- Can't use EF entities
- Manual SQL for all fields (40+ fields!)
- Loses EF benefits (validation, navigation properties)
- Hard to get "inserted count"
- Complex with JSONB field

## Decision

**Use Option 1: Database Query Before Insert**

## Reasoning

### Why Pre-Check?

1. **Simplicity:**
   ```csharp
   if (await _context.Photos.AnyAsync(p => p.NasaId == nasaId))
       continue; // Simple and obvious
   ```

2. **Leverages Unique Index:**
   - We already have unique index on `nasa_id` (Decision 004B)
   - Query uses index: ~0.5ms per lookup
   - Database optimized for this

3. **Works Across Instances:**
   - Multiple scrapers can run simultaneously
   - Database is single source of truth
   - No cache synchronization needed

4. **No Exception Handling:**
   - Clean control flow
   - No try/catch for expected case
   - Easier to debug

5. **Logging & Observability:**
   ```csharp
   _logger.LogDebug("Photo {NasaId} already exists, skipping", nasaId);
   ```
   - Know exactly what was skipped
   - Can track scraper efficiency

6. **Batch Insert Still Possible:**
   - Check each photo
   - Collect new photos in list
   - Single `AddRangeAsync` + `SaveChangesAsync`

### Performance Analysis

**Typical scrape (100 photos, 10 new):**

Option 1 (Pre-check):
- 100 AnyAsync queries: 50ms
- 10 inserts: 20ms
- **Total: 70ms**

Option 3 (Try/catch):
- 90 duplicate key exceptions: 180ms (2ms each)
- 10 successful inserts: 20ms
- **Total: 200ms**

Option 2 (Batch check):
- 1 WHERE IN query: 5ms
- 10 inserts: 20ms
- **Total: 25ms**
- **Saves:** 45ms (not worth complexity)

For background scraping, 70ms is perfectly acceptable.

### Code Example

```csharp
private async Task<int> ProcessResponseAsync(string jsonResponse, CancellationToken cancellationToken)
{
    var images = JsonDocument.Parse(jsonResponse)
        .RootElement
        .GetProperty("images")
        .EnumerateArray();

    var rover = await _context.Rovers
        .Include(r => r.Cameras)
        .FirstAsync(r => r.Name == RoverName, cancellationToken);

    var newPhotos = new List<Photo>();

    foreach (var imageElement in images)
    {
        var nasaId = imageElement.GetProperty("imageid").GetString()!;

        // Idempotency check
        var exists = await _context.Photos
            .AnyAsync(p => p.NasaId == nasaId, cancellationToken);

        if (exists)
        {
            _logger.LogDebug("Photo {NasaId} already exists, skipping", nasaId);
            continue;
        }

        // Extract and add to batch
        var photo = await ExtractPhotoDataAsync(imageElement, rover, cancellationToken);
        if (photo != null)
            newPhotos.Add(photo);
    }

    // Bulk insert new photos
    if (newPhotos.Any())
    {
        await _context.Photos.AddRangeAsync(newPhotos, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    return newPhotos.Count;
}
```

## Implementation

### Unique Index (Already Done in Story 004)

```csharp
// In MarsVistaDbContext.OnModelCreating
modelBuilder.Entity<Photo>()
    .HasIndex(p => p.NasaId)
    .IsUnique();
```

### Scraper Check

```csharp
var exists = await _context.Photos
    .AnyAsync(p => p.NasaId == nasaId, cancellationToken);

if (exists)
{
    _logger.LogDebug("Photo {NasaId} already exists, skipping", nasaId);
    continue;
}
```

### Why AnyAsync?

- Returns `bool` (not entity)
- Faster than `FirstOrDefaultAsync` (stops at first match)
- Generates efficient SQL: `SELECT EXISTS(SELECT 1 FROM photos WHERE nasa_id = @p0)`
- Uses index

## Trade-offs Accepted

### Multiple Database Queries
- **Accepted:** N queries for N photos (vs 1 batch query)
- **Why it's OK:**
  - 0.5ms per query with index
  - Background operation (not user-facing)
  - 50ms overhead for 100 photos is negligible
- **When to revisit:** If scraping 10,000+ photos at once

### Not Using Database Upsert
- **Accepted:** Application-level check instead of `ON CONFLICT`
- **Why it's OK:**
  - Keeps EF Core benefits (entities, validation, navigation)
  - 40+ fields + JSONB complex for raw SQL
  - Pre-check is fast enough
- **When to use upsert:** Bulk import tool (separate from scraper)

## Alternatives Rejected

### Why Not Batch Check? (Option 2)
- Adds complexity (HashSet, batch query)
- Saves only 45ms
- Premature optimization
- Wait until proven bottleneck

### Why Not Try/Catch? (Option 3)
- Exception handling for control flow (anti-pattern)
- Slower on duplicates (2ms vs 0.5ms)
- Can't batch insert
- Unclear error handling

### Why Not In-Memory Cache? (Option 4)
- 40MB memory overhead
- Cache invalidation problems
- Doesn't work with multiple instances
- Premature optimization

### Why Not Upsert? (Option 5)
- Bypasses EF Core
- Manual SQL for 40+ fields
- Hard with JSONB
- Loses entity benefits

## Validation

This strategy is validated by:
- ✅ Running scraper twice yields same photo count
- ✅ No duplicate `nasa_id` in database
- ✅ Logs show "already exists" for duplicates
- ✅ Multiple scraper instances don't create duplicates
- ✅ Performance acceptable (< 100ms for 100 photos)

## Future Optimization

If scraping becomes a bottleneck:

1. **Batch check** (Option 2) - for 1000+ photo batches
2. **Parallel scraping** - multiple rovers simultaneously
3. **Database partitioning** - by rover or sol
4. **Read replica** - separate read DB for checks

For now, simple is best.

## Related Decisions

- [Decision 004B: NASA ID Uniqueness Strategy](004b-nasa-id-uniqueness.md) - Unique index
- [Decision 006: Scraper Service Pattern](006-scraper-service-pattern.md) - Scraper design
- [Decision 006D: Bulk Insert Strategy](006d-bulk-insert-strategy.md) - Batching inserts

## References

- [EF Core AnyAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.anyasync)
- [PostgreSQL Unique Constraints](https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-UNIQUE-CONSTRAINTS)
- [Idempotency](https://en.wikipedia.org/wiki/Idempotence)
- [Database Index Performance](https://use-the-index-luke.com/)
