# Decision 004C: Cascade Delete Behavior

**Status:** Active
**Date:** 2025-11-13
**Context:** Story 004 - Define Core Domain Entities

## Context

We have relationships between entities:
- **Rover** → many **Cameras**
- **Rover** → many **Photos**
- **Camera** → many **Photos**

When a parent entity is deleted, we need to decide what happens to child entities. This is a fundamental database design decision that affects data integrity and application behavior.

## Requirements

- **Data integrity:** No orphaned records
- **Logical behavior:** Deletions should make sense in domain context
- **Safety:** Prevent accidental data loss
- **Performance:** Efficient delete operations
- **Testing:** Easy to clean up test data

## Relationship Analysis

### Rover → Cameras

**Domain logic:**
- A camera belongs to exactly one rover
- A camera has no meaning without its rover
- Examples: "Curiosity NAVCAM", "Perseverance MASTCAM-Z"

**Question:** If we delete Curiosity rover, what happens to its cameras?

### Rover → Photos

**Domain logic:**
- A photo is taken by exactly one rover
- A photo has no meaning without knowing which rover took it
- Photos are historical records tied to specific rovers

**Question:** If we delete Curiosity rover, what happens to its 700K photos?

### Camera → Photos

**Domain logic:**
- A photo is taken by exactly one camera
- The camera is essential metadata (NAVCAM vs HAZC vs MASTCAM)
- Photos are categorized primarily by camera type

**Question:** If we delete NAVCAM camera, what happens to its photos?

## Alternatives

### Alternative 1: CASCADE DELETE (RECOMMENDED)

**Implementation:**
```csharp
modelBuilder.Entity<Camera>(entity =>
{
    entity.HasOne(e => e.Rover)
        .WithMany(r => r.Cameras)
        .HasForeignKey(e => e.RoverId)
        .OnDelete(DeleteBehavior.Cascade);  // <- CASCADE
});

modelBuilder.Entity<Photo>(entity =>
{
    entity.HasOne(e => e.Rover)
        .WithMany(r => r.Photos)
        .HasForeignKey(e => e.RoverId)
        .OnDelete(DeleteBehavior.Cascade);  // <- CASCADE

    entity.HasOne(e => e.Camera)
        .WithMany(c => c.Photos)
        .HasForeignKey(e => e.CameraId)
        .OnDelete(DeleteBehavior.Cascade);  // <- CASCADE
});
```

**Behavior:**
```csharp
// Delete rover
await context.Rovers.Where(r => r.Name == "Curiosity").ExecuteDeleteAsync();

// Result: Automatically deletes:
// - All Curiosity cameras (5 cameras)
// - All Curiosity photos (700K photos)
```

**Pros:**
- **Logical:** Photos can't exist without rover/camera
- **No orphaned data:** Database guarantees referential integrity
- **Simple code:** One delete statement handles everything
- **Efficient:** Database handles cascades in single transaction
- **Testing friendly:** Delete test rover → all test data gone
- **Data integrity:** Impossible to have photos with invalid rover_id

**Cons:**
- **Accidental data loss risk:** Delete rover = delete ALL photos
  - Mitigation: Soft deletes, confirmation prompts, restricted permissions
- **Performance:** Deleting 700K photos takes time
  - Mitigation: Usually not a problem (we rarely delete rovers)

**When would we delete a rover?**
- Never in production (rovers are historical data)
- During testing (clean up test data)
- Data cleanup scripts (remove duplicate/corrupted data)

### Alternative 2: SET NULL

**Implementation:**
```csharp
modelBuilder.Entity<Photo>(entity =>
{
    entity.HasOne(e => e.Rover)
        .WithMany(r => r.Photos)
        .HasForeignKey(e => e.RoverId)
        .OnDelete(DeleteBehavior.SetNull);  // <- SET NULL
});
```

**Behavior:**
```csharp
// Delete rover
await context.Rovers.Where(r => r.Name == "Curiosity").ExecuteDeleteAsync();

// Result: Photos still exist, but:
// photo.RoverId = null (orphaned photos)
```

**Pros:**
- Photos preserved even if rover deleted
- No data loss

**Cons:**
- **Orphaned data:** Photos with no rover (meaningless)
- **Query problems:** Need to handle `WHERE rover_id IS NOT NULL` everywhere
- **Domain violation:** Every photo MUST have a rover (it's required)
- **Nullable foreign key:** Allows invalid state (`rover_id` should be required)

**Example problem:**
```csharp
// User requests: "Show me Curiosity photos"
var photos = await context.Photos
    .Where(p => p.RoverId == curiosityId)
    .ToListAsync();

// Returns 0 photos after rover deleted (bad UX)

// Or worse:
var photo = await context.Photos.Include(p => p.Rover).FirstAsync();
Console.WriteLine(photo.Rover.Name);  // NullReferenceException!
```

**Not acceptable** - Rover is required for photos.

### Alternative 3: RESTRICT (Prevent Delete)

**Implementation:**
```csharp
modelBuilder.Entity<Photo>(entity =>
{
    entity.HasOne(e => e.Rover)
        .WithMany(r => r.Photos)
        .HasForeignKey(e => e.RoverId)
        .OnDelete(DeleteBehavior.Restrict);  // <- RESTRICT
});
```

**Behavior:**
```csharp
// Try to delete rover
await context.Rovers.Where(r => r.Name == "Curiosity").ExecuteDeleteAsync();

// Result: Throws exception!
// "Cannot delete Rover because Photos reference it"
```

**Pros:**
- **Prevents accidental deletions:** Can't delete rover with photos
- **Forces explicit cleanup:** Must delete photos first

**Cons:**
- **Annoying for testing:** Can't quickly clean up test data
- **Complex delete logic:** Must manually delete in correct order:
  ```csharp
  // Delete all photos
  await context.Photos.Where(p => p.RoverId == roverId).ExecuteDeleteAsync();
  // Delete all cameras
  await context.Cameras.Where(c => c.RoverId == roverId).ExecuteDeleteAsync();
  // Finally delete rover
  await context.Rovers.Where(r => r.Id == roverId).ExecuteDeleteAsync();
  ```
- **Production never deletes rovers anyway:** Protection is unnecessary
- **Error messages:** Users see cryptic foreign key errors

**Use case analysis:**
- Rovers are never deleted in production → RESTRICT provides no value
- Testing needs easy cleanup → RESTRICT is annoying
- Data scripts might need cleanup → RESTRICT requires complex logic

### Alternative 4: Soft Deletes (No Physical Delete)

**Implementation:**
```csharp
public class Rover
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime? DeletedAt { get; set; }  // Soft delete marker

    public bool IsDeleted => DeletedAt != null;
}

// Query filter
modelBuilder.Entity<Rover>()
    .HasQueryFilter(r => r.DeletedAt == null);
```

**Behavior:**
```csharp
// "Delete" rover (actually just marks as deleted)
var rover = await context.Rovers.FindAsync(curiosityId);
rover.DeletedAt = DateTime.UtcNow;
await context.SaveChangesAsync();

// Photos still exist and reference rover
// Rover still exists in database but hidden by query filter
```

**Pros:**
- **Recoverable:** Can "undelete" by setting `DeletedAt = null`
- **Audit trail:** Know when things were deleted
- **No cascade issues:** Nothing physically deleted

**Cons:**
- **Complexity:** Every query needs `WHERE deleted_at IS NULL`
- **Database bloat:** Deleted data stays forever
- **Foreign key issues:** Photos reference "deleted" rovers
- **Indexes still include deleted data:** Performance degradation
- **Not needed for our domain:** Rovers are never deleted

**Analysis:**
Soft deletes make sense for:
- User accounts (users might want to reactivate)
- Orders (legal requirements to keep records)
- Posts/comments (moderation needs audit trail)

Soft deletes don't make sense for:
- **Rovers:** Static reference data, never deleted
- **Photos:** Historical records, never deleted
- **Cameras:** Static reference data, never deleted

We're not deleting production data - only test data and cleanup scripts.

## Decision

**Use CASCADE DELETE for all relationships (Alternative 1)**

### Rationale

1. **Logical domain behavior:**
   - Photos can't exist without a rover (which rover took it?)
   - Cameras can't exist without a rover (which rover's camera?)
   - Deleting parent should delete children

2. **Data integrity:**
   - No orphaned photos with invalid `rover_id`
   - Database enforces referential integrity
   - Impossible to have inconsistent state

3. **Simplicity:**
   - One delete statement
   - Database handles cascade automatically
   - No manual cleanup logic needed

4. **Testing-friendly:**
   - Delete test rover → all test data cleaned up
   - No need to manually delete children
   - Fast test cleanup

5. **Production doesn't delete rovers:**
   - Rovers are historical data (never deleted)
   - Photos are archival (never deleted)
   - CASCADE DELETE is used only for testing and data cleanup
   - Risk of accidental deletion is managed by permissions and UI

### Safety Measures

To prevent accidental deletions in production:

1. **Database permissions:**
   ```sql
   -- Application user can't DELETE rovers
   REVOKE DELETE ON rovers FROM marsvista_app;

   -- Only admin user can delete
   GRANT DELETE ON rovers TO marsvista_admin;
   ```

2. **Application layer checks:**
   ```csharp
   public async Task DeleteRover(int roverId)
   {
       if (Environment.IsProduction())
       {
           throw new InvalidOperationException(
               "Cannot delete rovers in production"
           );
       }

       // Only in dev/test environments
       await context.Rovers
           .Where(r => r.Id == roverId)
           .ExecuteDeleteAsync();
   }
   ```

3. **UI confirmations:**
   ```csharp
   // Admin interface
   [Authorize(Roles = "Admin")]
   [RequireConfirmation("Are you sure? This will delete ALL photos!")]
   public async Task<IActionResult> DeleteRover(int id)
   {
       // ...
   }
   ```

4. **Backups:**
   - Daily PostgreSQL backups
   - Point-in-time recovery enabled
   - Can restore if accidental deletion

## Implementation

### EF Core Configuration

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Rover → Cameras (CASCADE)
    modelBuilder.Entity<Camera>(entity =>
    {
        entity.HasOne(e => e.Rover)
            .WithMany(r => r.Cameras)
            .HasForeignKey(e => e.RoverId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    // Rover → Photos (CASCADE)
    modelBuilder.Entity<Photo>(entity =>
    {
        entity.HasOne(e => e.Rover)
            .WithMany(r => r.Photos)
            .HasForeignKey(e => e.RoverId)
            .OnDelete(DeleteBehavior.Cascade);

        // Camera → Photos (CASCADE)
        entity.HasOne(e => e.Camera)
            .WithMany(c => c.Photos)
            .HasForeignKey(e => e.CameraId)
            .OnDelete(DeleteBehavior.Cascade);
    });
}
```

### Generated SQL

```sql
CREATE TABLE cameras (
    id SERIAL PRIMARY KEY,
    rover_id INT NOT NULL,
    name VARCHAR(50) NOT NULL,
    FOREIGN KEY (rover_id)
        REFERENCES rovers(id)
        ON DELETE CASCADE  -- <- Database enforces cascade
);

CREATE TABLE photos (
    id SERIAL PRIMARY KEY,
    rover_id INT NOT NULL,
    camera_id INT NOT NULL,
    nasa_id VARCHAR(200) NOT NULL,
    FOREIGN KEY (rover_id)
        REFERENCES rovers(id)
        ON DELETE CASCADE,
    FOREIGN KEY (camera_id)
        REFERENCES cameras(id)
        ON DELETE CASCADE
);
```

### Test Helper

```csharp
// Testing utility
public class TestDataHelper
{
    public async Task CleanupTestData(MarsVistaDbContext context)
    {
        // Delete test rover → CASCADE deletes all cameras and photos
        await context.Rovers
            .Where(r => r.Name.StartsWith("TEST_"))
            .ExecuteDeleteAsync();

        // Or delete all test data
        await context.Database.ExecuteRawSqlAsync(
            "DELETE FROM rovers WHERE name LIKE 'TEST_%'"
        );
    }
}
```

## Trade-offs

**Accepted:**
- Risk of accidental deletion (mitigated by permissions and checks)
- Time to delete large datasets (acceptable for rare operation)

**Gained:**
- Simple code (one delete statement)
- Data integrity (no orphans)
- Fast test cleanup
- Database enforces rules
- Logical domain behavior

## Validation Criteria

Success metrics:
- Deleting rover also deletes all its cameras and photos
- No orphaned photos with invalid rover_id
- Test cleanup is fast and simple
- Production has safeguards against accidental deletions

Test cases:
```csharp
[Fact]
public async Task DeleteRover_CascadesTo_Cameras()
{
    var rover = new Rover { Name = "TEST_ROVER" };
    context.Rovers.Add(rover);
    var camera = new Camera { Rover = rover, Name = "CAM" };
    context.Cameras.Add(camera);
    await context.SaveChangesAsync();

    context.Rovers.Remove(rover);
    await context.SaveChangesAsync();

    var cameraCount = await context.Cameras.CountAsync();
    Assert.Equal(0, cameraCount);  // Camera was deleted too
}

[Fact]
public async Task DeleteRover_CascadesTo_Photos()
{
    var rover = new Rover { Name = "TEST_ROVER" };
    context.Rovers.Add(rover);
    var camera = new Camera { Rover = rover, Name = "CAM" };
    var photo = new Photo { Rover = rover, Camera = camera, NasaId = "ABC" };
    context.Photos.Add(photo);
    await context.SaveChangesAsync();

    context.Rovers.Remove(rover);
    await context.SaveChangesAsync();

    var photoCount = await context.Photos.CountAsync();
    Assert.Equal(0, photoCount);  // Photo was deleted too
}
```

## References

- [EF Core Relationships - Delete Behaviors](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete)
- [PostgreSQL Foreign Keys](https://www.postgresql.org/docs/current/ddl-constraints.html#DDL-CONSTRAINTS-FK)
- [Database Cascading Deletes Best Practices](https://stackoverflow.com/questions/4650264/when-to-use-on-delete-cascade)

## Related Decisions

- **Decision 004:** Entity relationships (defines what relationships exist)
- **Future:** Soft delete strategy (if needed for other entities like user data)

## Notes

### Real-World Delete Scenarios

**Development:**
- Delete test rover after tests
- Clean up duplicate data from scraper bugs
- Reset database to clean state

**Production:**
- Never delete rovers (historical data)
- Never delete photos (archival records)
- Maybe delete duplicate photos (if import bug)

**Data maintenance:**
- Remove corrupted records
- Clean up failed imports
- Dedup duplicates from scraper issues

### Performance Considerations

Deleting rover with 700K photos:
```sql
DELETE FROM rovers WHERE name = 'Curiosity';
-- Cascade deletes:
-- - 5 cameras (~instant)
-- - 700K photos (~10-30 seconds)
```

This is acceptable because:
- Happens only during maintenance
- Can run during low-traffic windows
- PostgreSQL handles efficiently
- Wrapped in transaction (atomic operation)

### Alternative: Batch Deletes

For very large deletes, could batch:
```csharp
// Delete photos in batches
const int batchSize = 10000;
int deleted;
do
{
    deleted = await context.Photos
        .Where(p => p.RoverId == roverId)
        .Take(batchSize)
        .ExecuteDeleteAsync();
} while (deleted > 0);

// Then delete rover
await context.Rovers
    .Where(r => r.Id == roverId)
    .ExecuteDeleteAsync();
```

But for our use case, simple CASCADE is sufficient.
