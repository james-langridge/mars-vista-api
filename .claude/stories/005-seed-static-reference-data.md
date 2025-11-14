# Story 005: Seed Static Reference Data (Rovers & Cameras)

## Story
As a developer, I need to seed the database with static reference data for the 4 Mars rovers and their cameras so that the API has the foundational data required before scraping photos from NASA.

## Acceptance Criteria
- [ ] Database seeder service created
- [ ] All 4 rovers seeded (Perseverance, Curiosity, Opportunity, Spirit)
- [ ] All 36 cameras seeded with correct rover associations
- [ ] Seeding is idempotent (can run multiple times safely)
- [ ] Seeder runs automatically on application startup in development
- [ ] Seeding can be triggered manually via CLI command
- [ ] Verify seed data in database

## Context

Before we can scrape and store photos, we need reference data for:
1. **Rovers**: The 4 Mars rovers (Perseverance, Curiosity, Opportunity, Spirit)
2. **Cameras**: Each rover's suite of cameras (36 cameras total)

This is **static reference data** - it rarely changes and must exist before photos can be imported.

### Rover Count
- Perseverance: 17 cameras
- Curiosity: 7 cameras
- Opportunity: 6 cameras
- Spirit: 6 cameras
- **Total: 4 rovers, 36 cameras**

### Data Source
Extracted from the Rails reference implementation at `/home/james/git/mars-photo-api/db/seeds.rb`

## Implementation Steps

### 1. Create ITimestamped Interface

This enables automatic timestamp handling across all entities.

**File:** `src/MarsVista.Api/Entities/ITimestamped.cs`

```csharp
namespace MarsVista.Api.Entities;

/// <summary>
/// Marker interface for entities that track creation and modification timestamps
/// </summary>
public interface ITimestamped
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

**Why this interface?**
- Enables generic timestamp handling in seeder
- Documents which entities have timestamps
- Future: Can add SaveChanges override to auto-update timestamps

### 2. Update Entities to Implement ITimestamped

**File:** `src/MarsVista.Api/Entities/Rover.cs`

```csharp
namespace MarsVista.Api.Entities;

public class Rover : ITimestamped  // <- Add interface
{
    // ... existing properties ...
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Repeat for `Camera.cs` and `Photo.cs` (Photo already has this from Story 004).

### 3. Create Database Seeder Service

**File:** `src/MarsVista.Api/Data/DatabaseSeeder.cs`

```csharp
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Data;

/// <summary>
/// Seeds static reference data (rovers and cameras) into the database
/// </summary>
public class DatabaseSeeder
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MarsVistaDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all static reference data. Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        await SeedRoversAsync();
        await SeedCamerasAsync();

        _logger.LogInformation("Database seeding completed");
    }

    private async Task SeedRoversAsync()
    {
        var rovers = new[]
        {
            new Rover
            {
                Name = "Perseverance",
                LandingDate = new DateTime(2021, 2, 18),
                LaunchDate = null,
                Status = "active",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Curiosity",
                LandingDate = new DateTime(2012, 8, 6),
                LaunchDate = null,
                Status = "active",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Opportunity",
                LandingDate = new DateTime(2004, 1, 25),
                LaunchDate = null,
                Status = "complete",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Spirit",
                LandingDate = new DateTime(2004, 1, 4),
                LaunchDate = null,
                Status = "complete",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            }
        };

        foreach (var rover in rovers)
        {
            var exists = await _context.Rovers.AnyAsync(r => r.Name == rover.Name);

            if (!exists)
            {
                rover.CreatedAt = DateTime.UtcNow;
                rover.UpdatedAt = DateTime.UtcNow;
                _context.Rovers.Add(rover);
                _logger.LogInformation("Seeding rover: {RoverName}", rover.Name);
            }
            else
            {
                _logger.LogDebug("Rover {RoverName} already exists, skipping", rover.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCamerasAsync()
    {
        var cameraSeedData = new Dictionary<string, (string Name, string FullName)[]>
        {
            ["Perseverance"] = new[]
            {
                ("EDL_RUCAM", "Rover Up-Look Camera"),
                ("EDL_RDCAM", "Rover Down-Look Camera"),
                ("EDL_DDCAM", "Descent Stage Down-Look Camera"),
                ("EDL_PUCAM1", "Parachute Up-Look Camera A"),
                ("EDL_PUCAM2", "Parachute Up-Look Camera B"),
                ("NAVCAM_LEFT", "Navigation Camera - Left"),
                ("NAVCAM_RIGHT", "Navigation Camera - Right"),
                ("MCZ_RIGHT", "Mast Camera Zoom - Right"),
                ("MCZ_LEFT", "Mast Camera Zoom - Left"),
                ("FRONT_HAZCAM_LEFT_A", "Front Hazard Avoidance Camera - Left"),
                ("FRONT_HAZCAM_RIGHT_A", "Front Hazard Avoidance Camera - Right"),
                ("REAR_HAZCAM_LEFT", "Rear Hazard Avoidance Camera - Left"),
                ("REAR_HAZCAM_RIGHT", "Rear Hazard Avoidance Camera - Right"),
                ("SKYCAM", "MEDA Skycam"),
                ("SHERLOC_WATSON", "SHERLOC WATSON Camera"),
                ("SUPERCAM_RMI", "SuperCam Remote Micro Imager"),
                ("LCAM", "Lander Vision System Camera")
            },
            ["Curiosity"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("MAST", "Mast Camera"),
                ("CHEMCAM", "Chemistry and Camera Complex"),
                ("MAHLI", "Mars Hand Lens Imager"),
                ("MARDI", "Mars Descent Imager"),
                ("NAVCAM", "Navigation Camera")
            },
            ["Opportunity"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("NAVCAM", "Navigation Camera"),
                ("PANCAM", "Panoramic Camera"),
                ("MINITES", "Miniature Thermal Emission Spectrometer (Mini-TES)"),
                ("ENTRY", "Entry, Descent, and Landing Camera")
            },
            ["Spirit"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("NAVCAM", "Navigation Camera"),
                ("PANCAM", "Panoramic Camera"),
                ("MINITES", "Miniature Thermal Emission Spectrometer (Mini-TES)"),
                ("ENTRY", "Entry, Descent, and Landing Camera")
            }
        };

        // Load all rovers with their cameras
        var rovers = await _context.Rovers
            .Include(r => r.Cameras)
            .ToListAsync();

        foreach (var rover in rovers)
        {
            if (!cameraSeedData.TryGetValue(rover.Name, out var cameras))
            {
                _logger.LogWarning("No camera seed data found for rover: {RoverName}", rover.Name);
                continue;
            }

            foreach (var (name, fullName) in cameras)
            {
                var exists = rover.Cameras.Any(c => c.Name == name);

                if (!exists)
                {
                    var camera = new Camera
                    {
                        Name = name,
                        FullName = fullName,
                        RoverId = rover.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Cameras.Add(camera);
                    _logger.LogInformation("Seeding camera: {CameraName} for {RoverName}", name, rover.Name);
                }
                else
                {
                    _logger.LogDebug("Camera {CameraName} for {RoverName} already exists, skipping",
                        name, rover.Name);
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
```

**Key Design Decisions:**

1. **Idempotency**: Check if rover/camera exists before inserting
   - Safe to run multiple times (development restarts, deployments)
   - Won't create duplicates

2. **Structured Data**: Dictionary maps rover names to camera arrays
   - Easy to read and maintain
   - Type-safe with tuples

3. **Logging**: Different levels for different scenarios
   - Info: New records created
   - Debug: Records already exist
   - Warning: Missing seed data

4. **Include Navigation**: Load rovers with cameras
   - Efficient single query per rover
   - Check existing cameras in memory (no N+1 queries)

**Documentation:**
- [EF Core AnyAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.entityframeworkqueryableextensions.anyasync)
- [C# Tuples](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples)

### 4. Register Seeder in Dependency Injection

**File:** `src/MarsVista.Api/Program.cs`

Add seeder registration and startup seeding:

```csharp
using MarsVista.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with snake_case naming convention
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    )
    .UseSnakeCaseNamingConvention());

// Register database seeder
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed database on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**What this does:**
- Registers `DatabaseSeeder` as scoped service
- Creates scope on startup (in development only)
- Runs seeder automatically
- Logs seed progress to console

**Why development only?**
- Production databases should use migrations, not auto-seeding
- Prevents accidental data changes in production
- Can still run seeder manually via CLI in production if needed

### 5. Run and Verify Seeding

Start the application:

```bash
cd src/MarsVista.Api
dotnet run
```

Expected console output:
```
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Starting database seeding...
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Seeding rover: Perseverance
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Seeding rover: Curiosity
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Seeding rover: Opportunity
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Seeding rover: Spirit
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Seeding camera: EDL_RUCAM for Perseverance
...
info: MarsVista.Api.Data.DatabaseSeeder[0]
      Database seeding completed
```

### 6. Verify Data in PostgreSQL

Connect to database:

```bash
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev
```

Check seeded data:

```sql
-- Count rovers
SELECT COUNT(*) FROM rovers;
-- Expected: 4

-- List all rovers
SELECT id, name, landing_date, status FROM rovers ORDER BY landing_date;

-- Expected output:
--  id |     name      | landing_date |  status
-- ----+---------------+--------------+----------
--   3 | Spirit        | 2004-01-04   | complete
--   4 | Opportunity   | 2004-01-25   | complete
--   2 | Curiosity     | 2012-08-06   | active
--   1 | Perseverance  | 2021-02-18   | active

-- Count cameras per rover
SELECT r.name, COUNT(c.id) as camera_count
FROM rovers r
LEFT JOIN cameras c ON c.rover_id = r.id
GROUP BY r.name
ORDER BY r.name;

-- Expected output:
--      name      | camera_count
-- ---------------+--------------
--  Curiosity     |            7
--  Opportunity   |            6
--  Perseverance  |           17
--  Spirit        |            6

-- Total cameras
SELECT COUNT(*) FROM cameras;
-- Expected: 36

-- Sample cameras for Perseverance
SELECT name, full_name
FROM cameras
WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Perseverance')
LIMIT 5;

-- Expected output:
--        name        |             full_name
-- -------------------+------------------------------------
--  EDL_RUCAM         | Rover Up-Look Camera
--  EDL_RDCAM         | Rover Down-Look Camera
--  NAVCAM_LEFT       | Navigation Camera - Left
--  MCZ_RIGHT         | Mast Camera Zoom - Right
--  SHERLOC_WATSON    | SHERLOC WATSON Camera
```

### 7. Create Manual Seeding CLI Command (Optional)

For production environments, create a CLI command to manually trigger seeding:

**File:** `src/MarsVista.Api/Commands/SeedCommand.cs`

```csharp
using MarsVista.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Commands;

public static class SeedCommand
{
    public static async Task ExecuteAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<MarsVistaDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")
            )
            .UseSnakeCaseNamingConvention());

        builder.Services.AddScoped<DatabaseSeeder>();

        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        Console.WriteLine("Starting manual database seeding...");
        await seeder.SeedAsync();
        Console.WriteLine("Seeding completed!");
    }
}
```

Run manually:
```bash
dotnet run -- seed
```

**Documentation:**
- [.NET CLI Custom Commands](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)

## Technical Decisions

### Decision 005: Seeding Strategy - Application Code vs SQL Scripts

**Context:** Should we seed data using C# application code or SQL migration scripts?

**Options:**

1. **Application Code (Recommended)**
   - Pros:
     - Type-safe with C# entities
     - IDE support and refactoring
     - Easier testing and debugging
     - Can use business logic
     - Platform-independent
   - Cons:
     - Runs on every application start (in dev)
     - Slightly slower than raw SQL

2. **SQL Migration Scripts**
   - Pros:
     - Runs once during migration
     - Very fast execution
     - Explicit database operations
   - Cons:
     - No type safety
     - Hard to maintain (string SQL)
     - Database-specific syntax
     - Can't use C# business logic

**Recommendation:** Application Code with Idempotent Seeding

**Reasoning:**
- Static data rarely changes (rovers, cameras)
- Idempotency makes startup seeding safe
- C# code is easier to maintain than SQL strings
- Performance difference negligible for 40 records
- Can easily add validation and logging

**Implementation:**
```csharp
// Check exists before insert
var exists = await _context.Rovers.AnyAsync(r => r.Name == rover.Name);
if (!exists) { /* insert */ }
```

### Decision 005A: When to Run Seeding - Startup vs On-Demand

**Context:** Should seeding run automatically on application startup or only when explicitly triggered?

**Recommendation:** Automatic in Development, Manual in Production

**Reasoning:**
- **Development:** Auto-seed on startup for convenience
  - Developers get fresh data on every restart
  - No manual steps required
  - Idempotency prevents duplicates

- **Production:** Manual seeding only
  - Prevents accidental data changes
  - Explicit control over when seeding happens
  - Can be part of deployment process

**Implementation:**
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}
```

### Decision 005B: Timestamp Handling - Database Default vs Application

**Context:** Should timestamps (created_at, updated_at) be set by database defaults or application code?

**Current Implementation:** Database defaults with application override

**Reasoning:**
- Database defaults ensure consistency
- Application code allows explicit control for seed data
- Seed data uses `DateTime.UtcNow` for predictable timestamps
- Future photos will use database defaults

**Trade-off:**
- Database defaults: Automatic, consistent, but less control
- Application code: Full control, testable, but must remember to set

**Best of both:** Database defaults + explicit setting in seeder

### Decision 005C: Camera Name Consistency

**Context:** Should we use NASA's camera codes or create our own naming scheme?

**Recommendation:** Use NASA's camera codes exactly as they appear in APIs

**Reasoning:**
- NASA APIs return camera names like "NAVCAM_LEFT", "MCZ_RIGHT"
- Using exact names makes scraper mapping trivial
- No translation layer needed
- Consistent with data source

**Alternative:** Normalize names (e.g., "NAVCAM_LEFT" → "navcam-left")
- Would require translation in every query
- Adds complexity for no benefit
- Could cause bugs if translation is inconsistent

### Decision 005D: Static Data vs Dynamic Discovery

**Context:** Should camera data be hardcoded or discovered dynamically from NASA APIs?

**Recommendation:** Hardcoded static seed data

**Reasoning:**
- Cameras are hardware - they don't change
- NASA APIs don't provide camera metadata endpoint
- Hardcoding is simpler and more reliable
- Can be updated manually if new rovers are added (rare event)

**When dynamic discovery makes sense:**
- If cameras changed frequently (they don't)
- If NASA provided a camera metadata API (they don't)
- If we supported 100+ rovers (we support 4)

For 4 rovers and 36 cameras, hardcoding is the pragmatic choice.

## Testing Checklist

- [ ] `ITimestamped` interface created
- [ ] `Rover`, `Camera`, `Photo` implement `ITimestamped`
- [ ] `DatabaseSeeder` service created with seed data
- [ ] Seeder registered in dependency injection
- [ ] Seeder runs on application startup (development)
- [ ] Application starts without errors
- [ ] 4 rovers seeded successfully
- [ ] 36 cameras seeded successfully
- [ ] Camera counts match expected (Perseverance: 17, Curiosity: 7, etc.)
- [ ] Re-running seeder doesn't create duplicates (idempotency)
- [ ] Timestamps populated correctly
- [ ] Foreign keys correct (cameras → rovers)

## Key Documentation Links

1. [EF Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
2. [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
3. [ASP.NET Core Application Startup](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup)
4. [ILogger Interface](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger)
5. [C# Tuples](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples)

## Success Criteria

✅ Static reference data (rovers and cameras) seeded successfully
✅ Seeding is idempotent and safe to run multiple times
✅ Auto-seeding on application startup in development
✅ Manual seeding available for production
✅ All 4 rovers present with correct landing dates
✅ All 36 cameras present with correct rover associations
✅ Timestamps populated correctly
✅ Foundation ready for photo scraper implementation

## Next Steps

After completing this story, you'll be ready for:
- **Story 006:** Build NASA API scraper service (Perseverance/Curiosity)
- **Story 007:** Implement basic REST API endpoints (GET /api/rovers, /api/rovers/{name}/photos)
- **Story 008:** Add photo filtering, pagination, and sorting
- **Story 009:** Implement scraper scheduling and background jobs

## Notes

### Rover Mission Status

- **Active Missions:**
  - Perseverance (2021-present): Still sending photos daily
  - Curiosity (2012-present): Still sending photos daily

- **Completed Missions:**
  - Spirit (2004-2010): Last contact March 22, 2010
  - Opportunity (2004-2018): Last contact June 10, 2018

### Camera Evolution

Rover camera capabilities have evolved significantly:

- **Spirit/Opportunity (2004):** 6 cameras
  - Basic hazard avoidance and navigation
  - Panoramic cameras for science

- **Curiosity (2012):** 7 cameras
  - More specialized instruments
  - HD video capability
  - ChemCam for laser spectroscopy

- **Perseverance (2021):** 17 cameras
  - Most cameras ever on a Mars rover
  - Entry/descent/landing documentation
  - Advanced science instruments (SHERLOC, SuperCam)
  - Dedicated sky camera

### Why Store Launch Date?

The Rails implementation has a `launch_date` field but doesn't populate it. We're including it for future completeness:

- Perseverance: Launched July 30, 2020
- Curiosity: Launched November 26, 2011
- Opportunity: Launched July 7, 2003
- Spirit: Launched June 10, 2003

This enables features like "mission timeline" showing launch → landing → first photo → latest photo.

### Camera Naming Gotchas

Be aware of naming inconsistencies in NASA's APIs:

- Perseverance uses detailed names: `FRONT_HAZCAM_LEFT_A`
- Curiosity uses short codes: `FHAZ` (Front Hazard Avoidance)
- Same camera type, different naming schemes
- Our scraper must handle both conventions
