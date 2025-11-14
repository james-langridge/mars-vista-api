# Decision 005: Database Seeding Strategy

**Date:** 2025-11-13
**Status:** Accepted
**Deciders:** Development Team
**Tags:** database, seeding, data-management

## Context

We need to populate the database with static reference data (4 rovers, 36 cameras) before we can scrape and store photos from NASA's APIs. This data rarely changes and must exist as a prerequisite for the application to function.

The question is: What's the best approach to seed this data?

## Decision Drivers

- **Maintainability**: Easy to update when new rovers/cameras are added
- **Type Safety**: Catch errors at compile time, not runtime
- **Idempotency**: Safe to run multiple times without creating duplicates
- **Developer Experience**: Convenient for development workflow
- **Production Safety**: Controlled seeding in production environments
- **Performance**: Fast enough for 40 records (4 rovers + 36 cameras)

## Options Considered

### Option 1: Application Code with Idempotent Seeding (SELECTED)

Seed data using C# application code that runs on startup in development, with idempotent checks to prevent duplicates.

**Implementation:**
```csharp
public class DatabaseSeeder
{
    public async Task SeedAsync()
    {
        // Check if exists before inserting
        var exists = await _context.Rovers.AnyAsync(r => r.Name == "Perseverance");
        if (!exists)
        {
            _context.Rovers.Add(new Rover { Name = "Perseverance", ... });
        }
        await _context.SaveChangesAsync();
    }
}

// In Program.cs (development only)
if (app.Environment.IsDevelopment())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

**Pros:**
- ✅ Type-safe with C# entities and compiler checks
- ✅ IDE support (IntelliSense, refactoring, navigation)
- ✅ Easy testing and debugging
- ✅ Can use business logic and validation
- ✅ Platform-independent (works with any database)
- ✅ Idempotent - safe to run multiple times
- ✅ Auto-runs in development (zero manual steps)
- ✅ Clear logging of what's being seeded

**Cons:**
- ❌ Runs on every application start in development (adds ~50-100ms)
- ❌ Slightly slower than raw SQL (negligible for 40 records)
- ❌ Need separate mechanism for production seeding

**Performance:**
- Seeding 4 rovers + 36 cameras: ~50-100ms
- Idempotency check per record: ~1-5ms
- Total startup overhead: ~150ms (acceptable for development)

### Option 2: SQL Migration Scripts

Embed SQL INSERT statements in EF Core migrations.

**Implementation:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        INSERT INTO rovers (name, landing_date, status)
        VALUES ('Perseverance', '2021-02-18', 'active');

        INSERT INTO cameras (name, full_name, rover_id)
        VALUES ('NAVCAM_LEFT', 'Navigation Camera - Left',
                (SELECT id FROM rovers WHERE name = 'Perseverance'));
    ");
}
```

**Pros:**
- ✅ Runs once during migration (not on every startup)
- ✅ Very fast execution (native SQL)
- ✅ Explicit database operations
- ✅ Part of migration history

**Cons:**
- ❌ No type safety (SQL is strings)
- ❌ Hard to maintain and refactor
- ❌ Database-specific syntax (PostgreSQL-only)
- ❌ Can't use C# business logic or validation
- ❌ Difficult to test
- ❌ Hard to make idempotent (need complex SQL)
- ❌ If migration fails halfway, partial data left behind

**Idempotency Challenge:**
```sql
-- Need complex SQL to avoid duplicates
INSERT INTO rovers (name, landing_date, status)
SELECT 'Perseverance', '2021-02-18', 'active'
WHERE NOT EXISTS (SELECT 1 FROM rovers WHERE name = 'Perseverance');
```

### Option 3: EF Core HasData Configuration

Use EF Core's `HasData()` method in model configuration.

**Implementation:**
```csharp
modelBuilder.Entity<Rover>().HasData(
    new Rover
    {
        Id = 1,
        Name = "Perseverance",
        LandingDate = new DateTime(2021, 2, 18),
        Status = "active"
    }
);
```

**Pros:**
- ✅ Type-safe with C# entities
- ✅ Part of migration system
- ✅ Runs once during migration

**Cons:**
- ❌ Must hardcode IDs (error-prone, fragile)
- ❌ Can't use auto-generated IDs
- ❌ Difficult to reference related entities
- ❌ Creates migration every time seed data changes
- ❌ Not idempotent by default
- ❌ Mixes model configuration with data

**Hardcoded ID Problem:**
```csharp
// Must know rover ID in advance for camera relationship
new Camera { Id = 1, Name = "NAVCAM", RoverId = 1 } // Fragile!
```

### Option 4: External Seed Script (CLI Command)

Create a separate CLI command/script that must be run manually.

**Implementation:**
```bash
dotnet run -- seed
```

**Pros:**
- ✅ Explicit control over when seeding happens
- ✅ Works in all environments
- ✅ Clear separation of concerns

**Cons:**
- ❌ Manual step required (easy to forget)
- ❌ Poor developer experience (extra command to remember)
- ❌ New developers need documentation
- ❌ CI/CD needs to remember to run it

## Decision Outcome

**Chosen Option:** Option 1 - Application Code with Idempotent Seeding

**Reasoning:**
1. **Best Developer Experience:** Auto-runs in development, zero manual steps
2. **Type Safety:** Full C# type checking and IDE support
3. **Maintainability:** Easy to read, update, and refactor
4. **Safety:** Idempotency prevents duplicate data
5. **Performance:** 150ms startup overhead is acceptable for development
6. **Flexibility:** Can add validation, logging, business logic
7. **Environment-Aware:** Auto in dev, manual in production

**Implementation Strategy:**
- Create `DatabaseSeeder` service with idempotent seeding
- Auto-run on startup in development only
- Provide manual CLI command for production
- Use structured data (dictionaries/tuples) for maintainability
- Log all seeding operations for visibility

## Trade-offs

### Accepted Trade-offs

1. **Startup Performance in Development**
   - Cost: ~150ms added to dev startup time
   - Benefit: Zero manual steps, always fresh data
   - Mitigation: Only runs in development

2. **Two Seeding Mechanisms**
   - Cost: Need both auto-seed (dev) and manual command (prod)
   - Benefit: Safety in production, convenience in development
   - Mitigation: Same `DatabaseSeeder` service, different triggers

3. **Idempotency Checks**
   - Cost: Extra database queries on every startup
   - Benefit: Safe to run multiple times, no duplicates
   - Mitigation: Queries are fast (~1-5ms each)

### Rejected Trade-offs

1. **SQL Migration Scripts** - Rejected maintainability cost
2. **HasData Configuration** - Rejected hardcoded ID requirement
3. **Manual CLI Only** - Rejected poor developer experience

## Consequences

### Positive

- Developers get seeded data automatically on first run
- Type-safe seed data with compile-time checking
- Easy to add new rovers/cameras in the future
- Clear logging shows what's being seeded
- Can run seeder in tests for isolated test data
- Production deployments have explicit control

### Negative

- Small startup performance cost in development (~150ms)
- Need to document manual seeding for production
- Two code paths (auto vs manual) to maintain

### Neutral

- Need service registration in DI container
- Seed data embedded in application code (not external file)

## Implementation Notes

### Idempotency Pattern

```csharp
var exists = await _context.Rovers.AnyAsync(r => r.Name == rover.Name);
if (!exists)
{
    _context.Rovers.Add(rover);
    _logger.LogInformation("Seeding rover: {RoverName}", rover.Name);
}
else
{
    _logger.LogDebug("Rover {RoverName} already exists, skipping", rover.Name);
}
```

### Environment-Aware Seeding

```csharp
// Auto-seed in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

### Manual Production Seeding

```bash
# Create CLI command for production
dotnet run -- seed

# Or via separate executable
dotnet MarsVista.Api.dll seed
```

## Monitoring and Validation

### Success Criteria

- 4 rovers seeded successfully
- 36 cameras seeded successfully
- Correct camera counts per rover:
  - Perseverance: 17 cameras
  - Curiosity: 7 cameras
  - Opportunity: 6 cameras
  - Spirit: 6 cameras
- No duplicate records created
- Startup time < 200ms in development

### Validation Queries

```sql
-- Verify rover count
SELECT COUNT(*) FROM rovers; -- Should be 4

-- Verify camera count
SELECT COUNT(*) FROM cameras; -- Should be 36

-- Verify camera distribution
SELECT r.name, COUNT(c.id) as camera_count
FROM rovers r
LEFT JOIN cameras c ON c.rover_id = r.id
GROUP BY r.name;
```

## Future Considerations

### If We Add More Rovers

When new Mars rovers are added (e.g., "Mars Sample Return Rover" in 2030s):

1. Add rover data to `DatabaseSeeder`
2. Add camera array to `cameraSeedData` dictionary
3. Run seeder (auto in dev, manual in prod)
4. Verify counts with SQL queries

**Effort:** ~5 minutes

### If NASA Changes Camera Names

If NASA renames cameras in their APIs:

1. Update camera names in `DatabaseSeeder`
2. May need data migration to update existing records
3. Scraper must handle both old and new names during transition

**Effort:** ~30 minutes + testing

### If We Need to Update Existing Rovers

If rover metadata changes (e.g., mission status):

1. Current implementation: Updates require manual SQL
2. Future enhancement: Add update logic to seeder
3. Better approach: Separate "update seeds" from "create seeds"

```csharp
// Future enhancement
var perseverance = await _context.Rovers.FirstAsync(r => r.Name == "Perseverance");
if (perseverance.Status != "complete")
{
    perseverance.Status = "complete";
    perseverance.UpdatedAt = DateTime.UtcNow;
}
```

## References

- [EF Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [Rails Reference Implementation Seeds](~/git/mars-photo-api/db/seeds.rb)
- [Idempotency Patterns](https://martinfowler.com/articles/patterns-of-distributed-systems/idempotent-receiver.html)
- [.NET Hosted Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

## Related Decisions

- **003:** Entity Framework Core provider selection (PostgreSQL) - Affects seeding syntax
- **004:** Domain entity design - Defines what data to seed
- **Future:** Scraper implementation - Will consume seeded rovers/cameras
