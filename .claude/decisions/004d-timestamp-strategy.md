# Decision 004D: Timestamp Strategy

**Status:** Active
**Date:** 2025-11-13
**Context:** Story 004 - Define Core Domain Entities

## Context

We need to track when database records are created and modified. This is essential for:
- Debugging (when was this photo imported?)
- Auditing (has this record been modified?)
- Data integrity (detect stale data)
- Business logic (sort by creation date)

Common pattern is to have `created_at` and `updated_at` timestamps on entities.

## Requirements

- **Never null:** Timestamps must always have values
- **Consistent timezone:** All timestamps in UTC
- **Automatic:** Minimal application code
- **Accurate:** Timestamps reflect actual operation time
- **Update tracking:** `updated_at` changes on every update

## Alternatives

### Alternative 1: Application Sets Timestamps

**Implementation:**
```csharp
public class Photo
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// In application code
var photo = new Photo
{
    NasaId = "ABC",
    CreatedAt = DateTime.UtcNow,  // Application sets
    UpdatedAt = DateTime.UtcNow
};

context.Photos.Add(photo);
await context.SaveChangesAsync();
```

**Pros:**
- Explicit control over timestamps
- Can set specific time if needed
- No database magic

**Cons:**
- **Easy to forget:** Developer must remember to set timestamps
- **Inconsistent:** Different developers might use different patterns
- **Testing issues:** Hard-coded `DateTime.UtcNow` makes tests flaky
- **Update problem:** Must remember to update `UpdatedAt` on every change:
  ```csharp
  photo.Sol = 1000;
  photo.UpdatedAt = DateTime.UtcNow;  // Easy to forget!
  await context.SaveChangesAsync();
  ```

**Example bug:**
```csharp
// Developer forgets to set CreatedAt
var photo = new Photo { NasaId = "ABC" };
context.Photos.Add(photo);
await context.SaveChangesAsync();

// photo.CreatedAt = DateTime.MinValue (0001-01-01)
// Causes query errors, sorting issues, etc.
```

### Alternative 2: Database Default Values

**Implementation:**
```csharp
public class Photo
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// EF Core configuration
modelBuilder.Entity<Photo>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");

    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});
```

**Generated SQL:**
```sql
CREATE TABLE photos (
    id SERIAL PRIMARY KEY,
    nasa_id VARCHAR(200) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

**Pros:**
- **Automatic:** Database sets timestamp on insert
- **Never null:** Default value ensures timestamp exists
- **Consistent:** All records use same time source (database server)
- **Simple application code:** No need to set `CreatedAt`

**Cons:**
- **`UpdatedAt` not automatically updated** on updates
  - Still need application logic or trigger
- **Database clock dependency:** Uses database server time (usually fine)

**Application code:**
```csharp
// Insert - automatic timestamps
var photo = new Photo { NasaId = "ABC" };
context.Photos.Add(photo);
await context.SaveChangesAsync();
// photo.CreatedAt and photo.UpdatedAt set by database

// Update - must manually set UpdatedAt
photo.Sol = 1000;
photo.UpdatedAt = DateTime.UtcNow;  // Still need to remember!
await context.SaveChangesAsync();
```

### Alternative 3: Database Default + EF Core SaveChanges Override (RECOMMENDED)

**Implementation:**
```csharp
public class Photo
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// EF Core configuration
modelBuilder.Entity<Photo>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");

    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});

// Override SaveChangesAsync in DbContext
public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    // Automatically update UpdatedAt for modified entities
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Modified);

    foreach (var entry in entries)
    {
        if (entry.Entity is ITimestamped entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    return await base.SaveChangesAsync(cancellationToken);
}

// Interface for timestamped entities
public interface ITimestamped
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public class Photo : ITimestamped
{
    // ...
}
```

**Pros:**
- **Fully automatic:** Both `CreatedAt` and `UpdatedAt` handled automatically
- **Never forget:** Can't forget to update timestamps
- **Consistent:** All updates tracked
- **Testable:** Can mock `DateTime.UtcNow` in tests
- **Clean code:** No timestamp logic in business code

**Cons:**
- **Slight complexity:** Override `SaveChangesAsync` (one-time setup)
- **Every modified entity updates:** Even if no actual changes (minor)

**Application code:**
```csharp
// Insert - automatic
var photo = new Photo { NasaId = "ABC" };
context.Photos.Add(photo);
await context.SaveChangesAsync();
// CreatedAt and UpdatedAt set by database

// Update - automatic
photo.Sol = 1000;
await context.SaveChangesAsync();
// UpdatedAt automatically set by SaveChangesAsync override!
```

### Alternative 4: PostgreSQL Trigger

**Implementation:**
```sql
-- Create trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to photos table
CREATE TRIGGER update_photos_updated_at
BEFORE UPDATE ON photos
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();
```

**EF Core entity:**
```csharp
public class Photo
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

modelBuilder.Entity<Photo>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");

    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});
```

**Pros:**
- **Database enforces:** Works even if application bypassed
- **Automatic:** No application code needed
- **Efficient:** Database-level, very fast
- **Works for raw SQL:** Even `UPDATE photos SET ...` updates timestamp

**Cons:**
- **EF Core doesn't know:** `photo.UpdatedAt` not updated in memory
  ```csharp
  photo.Sol = 1000;
  await context.SaveChangesAsync();
  // Database updated updated_at, but photo.UpdatedAt still has old value!
  // Need to reload from database to see new timestamp
  ```
- **Migration complexity:** Need to manage trigger in migrations
- **Not visible in code:** Developers don't know trigger exists

**Example issue:**
```csharp
photo.Sol = 1000;
await context.SaveChangesAsync();

Console.WriteLine(photo.UpdatedAt);  // Shows old timestamp!
// Database has new timestamp, but entity in memory is stale

// Need to reload:
await context.Entry(photo).ReloadAsync();
Console.WriteLine(photo.UpdatedAt);  // Now shows correct timestamp
```

### Alternative 5: Hybrid (Database Default + Trigger for Safety)

**Implementation:**
- Database defaults for `CreatedAt` and `UpdatedAt`
- EF Core `SaveChangesAsync` override updates `UpdatedAt` in app
- PostgreSQL trigger as backup (belt and suspenders)

**Pros:**
- **Redundant safety:** Trigger catches updates missed by app
- **EF Core entities accurate:** `SaveChangesAsync` updates entity
- **Works for raw SQL:** Trigger handles non-EF updates

**Cons:**
- **Complexity:** Two mechanisms doing same thing
- **Maintenance:** Keep trigger and EF code in sync
- **Overkill:** For most applications, EF override is sufficient

## Decision

**Use Database Default + EF Core SaveChanges Override (Alternative 3)**

### Rationale

1. **Fully automatic:**
   - `CreatedAt` set by database on insert
   - `UpdatedAt` set by EF Core on update
   - Developers never touch timestamp code

2. **EF Core entities stay in sync:**
   - `SaveChangesAsync` updates entity in memory
   - No need to reload from database
   - Accurate timestamps in application code

3. **Simple implementation:**
   - One `SaveChangesAsync` override (5 lines of code)
   - No external triggers to manage
   - Easy to understand and maintain

4. **Testable:**
   - Can mock `DateTime.UtcNow` for deterministic tests
   - Trigger-based approach harder to test

5. **Sufficient for our needs:**
   - We use EF Core for all database access
   - No raw SQL updates (except migrations)
   - Don't need trigger redundancy

### Trade-offs

**Accepted:**
- Slight complexity (override `SaveChangesAsync`)
- Only works for EF Core updates (not raw SQL)

**Gained:**
- Automatic timestamp management
- Never forget to update timestamps
- Clean business logic code
- Consistent timestamp values

## Implementation

### Step 1: Define Interface

```csharp
namespace MarsVista.Api.Entities;

public interface ITimestamped
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

### Step 2: Update Entities

```csharp
public class Rover : ITimestamped
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Camera : ITimestamped
{
    // ... fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Photo : ITimestamped
{
    // ... fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Step 3: EF Core Configuration

```csharp
// In MarsVistaDbContext.OnModelCreating
modelBuilder.Entity<Rover>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});

modelBuilder.Entity<Camera>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});

modelBuilder.Entity<Photo>(entity =>
{
    entity.Property(e => e.CreatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
    entity.Property(e => e.UpdatedAt)
        .HasDefaultValueSql("CURRENT_TIMESTAMP");
});
```

### Step 4: Override SaveChangesAsync

```csharp
public class MarsVistaDbContext : DbContext
{
    // ... DbSets

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Update timestamps for modified entities
        var entries = ChangeTracker.Entries<ITimestamped>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Update timestamps for modified entities
        var entries = ChangeTracker.Entries<ITimestamped>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChanges();
    }
}
```

### Usage

```csharp
// Insert - CreatedAt and UpdatedAt set by database
var photo = new Photo
{
    NasaId = "ABC123",
    Sol = 1000,
    // No need to set CreatedAt or UpdatedAt
};

context.Photos.Add(photo);
await context.SaveChangesAsync();

Console.WriteLine(photo.CreatedAt);  // Set by database
Console.WriteLine(photo.UpdatedAt);  // Set by database

// Update - UpdatedAt automatically updated
photo.Sol = 1001;
await context.SaveChangesAsync();

Console.WriteLine(photo.UpdatedAt);  // Updated by SaveChangesAsync override
```

## Validation Criteria

Success metrics:
- `CreatedAt` never null
- `UpdatedAt` automatically updated on every change
- No manual timestamp code in business logic
- Timestamps use UTC
- Tests can pass with consistent timestamps

Test cases:
```csharp
[Fact]
public async Task CreatePhoto_SetsCreatedAt()
{
    var photo = new Photo { NasaId = "ABC" };
    context.Photos.Add(photo);
    await context.SaveChangesAsync();

    Assert.NotEqual(default, photo.CreatedAt);
    Assert.NotEqual(default, photo.UpdatedAt);
}

[Fact]
public async Task UpdatePhoto_UpdatesUpdatedAt()
{
    var photo = new Photo { NasaId = "ABC" };
    context.Photos.Add(photo);
    await context.SaveChangesAsync();

    var originalUpdatedAt = photo.UpdatedAt;

    await Task.Delay(100);  // Ensure time passes

    photo.Sol = 1000;
    await context.SaveChangesAsync();

    Assert.True(photo.UpdatedAt > originalUpdatedAt);
}

[Fact]
public async Task UpdatePhoto_DoesNotChangeCreatedAt()
{
    var photo = new Photo { NasaId = "ABC" };
    context.Photos.Add(photo);
    await context.SaveChangesAsync();

    var originalCreatedAt = photo.CreatedAt;

    photo.Sol = 1000;
    await context.SaveChangesAsync();

    Assert.Equal(originalCreatedAt, photo.CreatedAt);
}
```

## References

- [EF Core - Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)
- [EF Core - Overriding SaveChanges](https://learn.microsoft.com/en-us/ef/core/saving/basic#savechanges)
- [PostgreSQL - Date/Time Functions](https://www.postgresql.org/docs/current/functions-datetime.html)

## Related Decisions

- **Decision 004:** Entity definitions (where timestamps are added)
- **Future:** Audit logging (might extend timestamp strategy)

## Notes

### Why UTC?

Always use UTC for database timestamps:
- **No timezone confusion:** Server might be in different timezone than users
- **Consistent sorting:** Daylight saving time doesn't affect order
- **Easy conversion:** Convert to user timezone in presentation layer

```csharp
// Store UTC in database
entity.CreatedAt = DateTime.UtcNow;

// Convert to user timezone in API response
public class PhotoDto
{
    public DateTime CreatedAt { get; set; }

    public static PhotoDto FromEntity(Photo photo, TimeZoneInfo userTimezone)
    {
        return new PhotoDto
        {
            CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(
                photo.CreatedAt,
                userTimezone
            )
        };
    }
}
```

### Future: Trigger Option

If we later need triggers (for raw SQL updates), can add:

```sql
-- migration: add-updated-at-trigger
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_rovers_updated_at
BEFORE UPDATE ON rovers FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cameras_updated_at
BEFORE UPDATE ON cameras FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_photos_updated_at
BEFORE UPDATE ON photos FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();
```

But for now, EF Core override is sufficient.

### Testing Timestamps

For deterministic tests, can use a time provider:

```csharp
public interface ITimeProvider
{
    DateTime UtcNow { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

// In SaveChangesAsync
private readonly ITimeProvider _timeProvider;

entry.Entity.UpdatedAt = _timeProvider.UtcNow;

// In tests, use fake:
public class FakeTimeProvider : ITimeProvider
{
    public DateTime UtcNow { get; set; } = new DateTime(2024, 1, 1);
}
```

This makes tests deterministic and allows testing time-based logic.
