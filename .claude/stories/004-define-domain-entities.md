# Story 004: Define Core Domain Entities

## Story
As a developer, I need to define the core domain entities (Rover, Camera, Photo) with proper Entity Framework Core mappings so that I can represent Mars rover photos in the database with both structured queryable fields and complete NASA JSON data.

## Acceptance Criteria
- [ ] Rover entity defined with relationships to Cameras and Photos
- [ ] Camera entity defined with rover relationship
- [ ] Photo entity defined with hybrid storage (columns + JSONB)
- [ ] Entity relationships configured (one-to-many, foreign keys)
- [ ] Snake_case naming convention applied to database tables/columns
- [ ] JSONB column configured for RawData field
- [ ] Timestamps (CreatedAt, UpdatedAt) configured
- [ ] Initial migration created and applied successfully
- [ ] Database schema verified in PostgreSQL

## Context

This story implements the core domain model for the Mars Vista API. We're using a **hybrid storage approach**:

1. **Structured columns** for frequently queried fields (rover, camera, sol, earth_date, img_src)
2. **JSONB column** storing the complete NASA API response (30+ fields)

This gives us:
- **Fast queries** on indexed columns (rover, date, camera)
- **Complete data preservation** for future features
- **Flexibility** to query any NASA field via JSONB operators

Based on the architecture analysis, the Rails API only stores ~5-10% of NASA's data. We'll store 100% while maintaining query performance.

## Implementation Steps

### 1. Install EF.NamingConventions Package

To automatically convert C# PascalCase to PostgreSQL snake_case:

```bash
cd src/MarsVista.Api
dotnet add package EFCore.NamingConventions
```

This package automatically converts:
- `Photo` → `photos`
- `EarthDate` → `earth_date`
- `ImgSrcFull` → `img_src_full`

**Documentation:**
- [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)

### 2. Create Rover Entity

**File:** `src/MarsVista.Api/Entities/Rover.cs`

```csharp
namespace MarsVista.Api.Entities;

public class Rover
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? LandingDate { get; set; }
    public DateTime? LaunchDate { get; set; }
    public string Status { get; set; } = string.Empty;  // "active", "complete"
    public int? MaxSol { get; set; }
    public DateTime? MaxDate { get; set; }
    public int? TotalPhotos { get; set; }

    // Navigation properties
    public virtual ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Why these fields?**
- `Name`: "Curiosity", "Perseverance", "Spirit", "Opportunity"
- `LandingDate`/`LaunchDate`: For timeline features
- `Status`: Track mission status
- `MaxSol`/`MaxDate`/`TotalPhotos`: Cached statistics for manifests
- Navigation properties: EF Core relationships

### 3. Create Camera Entity

**File:** `src/MarsVista.Api/Entities/Camera.cs`

```csharp
namespace MarsVista.Api.Entities;

public class Camera
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;      // "FHAZ", "NAVCAM", "MAST"
    public string FullName { get; set; } = string.Empty;  // "Front Hazard Avoidance Camera"
    public int RoverId { get; set; }

    // Navigation properties
    public virtual Rover Rover { get; set; } = null!;
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Why these fields?**
- `Name`: Short camera code (used in NASA APIs)
- `FullName`: Human-readable name
- `RoverId`: Foreign key to rover
- Different rovers have different cameras (e.g., Perseverance has WATSON, Curiosity doesn't)

### 4. Create Photo Entity

**File:** `src/MarsVista.Api/Entities/Photo.cs`

```csharp
using System.Text.Json;

namespace MarsVista.Api.Entities;

public class Photo
{
    // Primary identifiers
    public int Id { get; set; }
    public string NasaId { get; set; } = string.Empty;  // NASA's unique identifier (e.g., "NLB_458574869EDR_F0541800NCAM00354M_")

    // Core queryable fields (indexed columns)
    public int Sol { get; set; }
    public DateTime? EarthDate { get; set; }
    public DateTime DateTakenUtc { get; set; }
    public string DateTakenMars { get; set; } = string.Empty;  // "Sol-01646M15:18:15.866"

    // Image URLs (NASA provides multiple sizes)
    public string ImgSrcSmall { get; set; } = string.Empty;   // 320px wide (for thumbnails)
    public string ImgSrcMedium { get; set; } = string.Empty;  // 800px wide (for galleries)
    public string ImgSrcLarge { get; set; } = string.Empty;   // 1200px wide (for viewing)
    public string ImgSrcFull { get; set; } = string.Empty;    // Full resolution (for download)

    // Image properties
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string SampleType { get; set; } = string.Empty;  // "Full", "Thumbnail", "Subframe"

    // Location data (enables proximity search, panorama detection)
    public int? Site { get; set; }
    public int? Drive { get; set; }
    public string? Xyz { get; set; }  // "(35.4362,22.5714,-9.46445)" - rover position

    // Camera telemetry (enables panorama stitching)
    public float? MastAz { get; set; }        // Mast azimuth (horizontal angle)
    public float? MastEl { get; set; }        // Mast elevation (vertical angle)
    public string? CameraVector { get; set; }
    public string? CameraPosition { get; set; }
    public string? CameraModelType { get; set; }

    // Rover orientation
    public string? Attitude { get; set; }      // Quaternion orientation
    public float? SpacecraftClock { get; set; }

    // Metadata
    public string? Title { get; set; }
    public string? Caption { get; set; }
    public string? Credit { get; set; }
    public DateTime? DateReceived { get; set; }
    public string? FilterName { get; set; }

    // Foreign keys
    public int RoverId { get; set; }
    public int CameraId { get; set; }

    // Navigation properties
    public virtual Rover Rover { get; set; } = null!;
    public virtual Camera Camera { get; set; } = null!;

    // JSONB storage for complete NASA response (30+ fields)
    // This stores the raw NASA JSON with all fields they provide
    // Enables future features without schema changes
    public JsonDocument? RawData { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Why so many fields?**

This isn't over-engineering - it's **deliberate completeness**:

1. **Basic fields** (sol, earth_date, img_src): Fast filtering/sorting
2. **Location fields** (site, drive, xyz): Enables "show me photos from this location"
3. **Camera angles** (mast_az, mast_el): Enables panorama detection and stitching
4. **Multiple image sizes**: Performance optimization (thumbnails vs full-res)
5. **RawData JSONB**: Future-proofing - stores ALL NASA fields

The Rails API only stores ~10 fields. We're storing ~30 structured + complete JSON.

**Documentation:**
- [System.Text.Json.JsonDocument](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument)
- [Npgsql JSON Mapping](https://www.npgsql.org/efcore/mapping/json.html)

### 5. Update DbContext with Entity Configurations

**File:** `src/MarsVista.Api/Data/MarsVistaDbContext.cs`

Replace the entire file:

```csharp
using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Entities;

namespace MarsVista.Api.Data;

public class MarsVistaDbContext : DbContext
{
    public MarsVistaDbContext(DbContextOptions<MarsVistaDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Rover> Rovers { get; set; }
    public DbSet<Camera> Cameras { get; set; }
    public DbSet<Photo> Photos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rover configuration
        modelBuilder.Entity<Rover>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20);

            // Timestamps with default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Camera configuration
        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoverId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();

            // Foreign key to Rover
            entity.HasOne(e => e.Rover)
                .WithMany(r => r.Cameras)
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Photo configuration
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NasaId).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => e.Sol);
            entity.HasIndex(e => e.EarthDate);
            entity.HasIndex(e => e.DateTakenUtc);
            entity.HasIndex(e => new { e.RoverId, e.Sol });
            entity.HasIndex(e => new { e.RoverId, e.CameraId, e.Sol });

            // Indexes for advanced features
            entity.HasIndex(e => new { e.Site, e.Drive });
            entity.HasIndex(e => new { e.MastAz, e.MastEl });

            // String length constraints
            entity.Property(e => e.NasaId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DateTakenMars).HasMaxLength(100);
            entity.Property(e => e.ImgSrcSmall).HasMaxLength(500);
            entity.Property(e => e.ImgSrcMedium).HasMaxLength(500);
            entity.Property(e => e.ImgSrcLarge).HasMaxLength(500);
            entity.Property(e => e.ImgSrcFull).HasMaxLength(500);
            entity.Property(e => e.SampleType).HasMaxLength(50);
            entity.Property(e => e.Xyz).HasMaxLength(200);
            entity.Property(e => e.CameraVector).HasMaxLength(200);
            entity.Property(e => e.CameraPosition).HasMaxLength(200);
            entity.Property(e => e.CameraModelType).HasMaxLength(100);
            entity.Property(e => e.Attitude).HasMaxLength(200);
            entity.Property(e => e.FilterName).HasMaxLength(100);

            // JSONB column for raw NASA data
            entity.Property(e => e.RawData)
                .HasColumnType("jsonb");

            // Foreign keys
            entity.HasOne(e => e.Rover)
                .WithMany(r => r.Photos)
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Camera)
                .WithMany(c => c.Photos)
                .HasForeignKey(e => e.CameraId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
```

**Key Configuration Details:**

1. **Indexes:**
   - `NasaId` unique index: Prevents duplicate photo imports
   - `(RoverId, Sol)`: Fast queries like "Curiosity photos from Sol 1000"
   - `(RoverId, CameraId, Sol)`: "Curiosity NAVCAM photos from Sol 1000"
   - `(Site, Drive)`: Location-based queries
   - `(MastAz, MastEl)`: Panorama detection

2. **JSONB Configuration:**
   - `HasColumnType("jsonb")`: PostgreSQL binary JSON format
   - Automatically serializes/deserializes `JsonDocument`
   - Can query with SQL: `WHERE raw_data->>'field' = 'value'`

3. **Timestamps:**
   - `CURRENT_TIMESTAMP`: PostgreSQL function for automatic timestamps
   - `UpdatedAt` will need trigger or application logic to update

4. **Relationships:**
   - Rover → many Cameras (cascade delete)
   - Rover → many Photos (cascade delete)
   - Camera → many Photos (cascade delete)

**Documentation:**
- [EF Core Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [EF Core Indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)

### 6. Enable Snake_Case Naming Convention

**File:** `src/MarsVista.Api/Program.cs`

Update the DbContext registration:

```csharp
using MarsVista.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with snake_case naming
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    )
    .UseSnakeCaseNamingConvention());  // <- Add this

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**What `.UseSnakeCaseNamingConvention()` does:**
- `Rover` → `rovers` table
- `EarthDate` → `earth_date` column
- `ImgSrcFull` → `img_src_full` column

### 7. Create Initial Migration

Generate the migration:

```bash
dotnet ef migrations add InitialSchema --project src/MarsVista.Api
```

This creates:
- `Migrations/{timestamp}_InitialSchema.cs` - Migration code
- `Migrations/MarsVistaDbContextModelSnapshot.cs` - Model snapshot

**Review the generated migration** to ensure:
- Tables use snake_case: `rovers`, `cameras`, `photos`
- Columns use snake_case: `earth_date`, `img_src_full`
- JSONB column created: `raw_data jsonb`
- Indexes created correctly
- Foreign keys configured

**Documentation:**
- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

### 8. Apply Migration to Database

```bash
dotnet ef database update --project src/MarsVista.Api
```

This executes the SQL to create tables in PostgreSQL.

**Verify success:**
```
Build started...
Build succeeded.
Applying migration '20241113xxxxx_InitialSchema'.
Done.
```

### 9. Verify Database Schema

Connect to PostgreSQL and inspect the schema:

```bash
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev
```

Run these SQL queries:

```sql
-- List all tables
\dt

-- Should show:
-- rovers
-- cameras
-- photos

-- Describe rovers table
\d rovers

-- Should show snake_case columns:
-- id, name, landing_date, status, created_at, updated_at, etc.

-- Describe photos table (check JSONB column)
\d photos

-- Should show raw_data column with type 'jsonb'

-- Check indexes
\di

-- Should show indexes on nasa_id, sol, earth_date, etc.

-- Exit psql
\q
```

**Expected table structure:**

```
Table "public.photos"
     Column         |           Type
--------------------+-------------------------
 id                 | integer
 nasa_id            | character varying(200)
 sol                | integer
 earth_date         | timestamp
 date_taken_utc     | timestamp
 img_src_full       | character varying(500)
 rover_id           | integer
 camera_id          | integer
 raw_data           | jsonb                    <- JSONB column!
 created_at         | timestamp
 updated_at         | timestamp
```

## Technical Decisions

Create the following decision documents:

### Decision 004: Entity Field Selection
**Context:** Which NASA fields should be promoted to database columns vs stored in JSONB?

**Recommendation:** Hybrid approach
- **Columns:** Fields used in 80%+ of queries (rover, camera, sol, date, img_src)
- **JSONB:** All fields including columned ones (complete data preservation)

**Reasoning:**
- Querying JSONB is slower than indexed columns (10-100x depending on index)
- But JSONB gives flexibility for advanced queries without schema changes
- Balance: Common queries use columns, advanced queries use JSONB

**Example:**
- Fast: `WHERE rover_id = 1 AND sol = 1000` (uses indexes)
- Advanced: `WHERE raw_data->>'attitude' IS NOT NULL` (uses GIN index)

### Decision 004A: Multiple Image Size URLs
**Context:** Should we store multiple image URLs (small/medium/large/full) or generate them on demand?

**Recommendation:** Store all URLs provided by NASA

**Reasoning:**
- NASA provides these URLs - they're not generated
- Different clients need different sizes (mobile = small, desktop = large)
- Saves bandwidth and improves performance
- No computation needed to resize images

**Alternative:** Store only full URL and resize on server
- Requires image processing infrastructure (expensive)
- Adds latency to every request
- Increases server costs

### Decision 004B: NasaId Uniqueness
**Context:** How do we prevent duplicate photos?

**Recommendation:** Unique index on `nasa_id` column

**Reasoning:**
- NASA provides unique identifiers for each image
- Prevents duplicate imports from scraper
- Fast lookups: "Does this photo already exist?"
- Database enforces uniqueness (can't forget to check)

**Implementation:**
```csharp
entity.HasIndex(e => e.NasaId).IsUnique();
```

### Decision 004C: Cascade Delete Behavior
**Context:** What happens when a Rover is deleted?

**Recommendation:** CASCADE delete for all relationships

**Reasoning:**
- Photos can't exist without a Rover (orphaned data is meaningless)
- Cameras can't exist without a Rover
- Simplifies data management (delete rover = delete everything)
- PostgreSQL handles cascade efficiently

**Alternative:** SET NULL or RESTRICT
- SET NULL: Would create orphaned photos (bad)
- RESTRICT: Would prevent rover deletion (annoying for tests)

### Decision 004D: Timestamp Strategy
**Context:** How to track when records are created/modified?

**Recommendation:**
- `created_at`: Set by database default (`CURRENT_TIMESTAMP`)
- `updated_at`: Set by database default, updated by application code or trigger

**Reasoning:**
- Database defaults ensure timestamps are never null
- Consistent timezone (UTC)
- Can add trigger later for automatic `updated_at` updates

**Future enhancement:** PostgreSQL trigger for `updated_at`:
```sql
CREATE TRIGGER update_updated_at
BEFORE UPDATE ON photos
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();
```

## Testing Checklist

- [ ] All entities created in `src/MarsVista.Api/Entities/`
- [ ] DbContext updated with DbSets and configurations
- [ ] EFCore.NamingConventions package installed
- [ ] Snake_case convention enabled in Program.cs
- [ ] Migration generated successfully
- [ ] Migration code reviewed (snake_case, JSONB, indexes)
- [ ] Migration applied without errors
- [ ] Tables exist in PostgreSQL with correct names
- [ ] Columns use snake_case naming
- [ ] JSONB column exists for `raw_data`
- [ ] Indexes created correctly
- [ ] Foreign key relationships configured
- [ ] API builds without errors

## Key Documentation Links

**Entity Framework Core:**
1. [Entity Properties](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties)
2. [Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
3. [Indexes](https://learn.microsoft.com/en-us/ef/core/modeling/indexes)
4. [Shadow Properties](https://learn.microsoft.com/en-us/ef/core/modeling/shadow-properties)

**PostgreSQL & Npgsql:**
5. [JSON Types in Npgsql](https://www.npgsql.org/efcore/mapping/json.html)
6. [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
7. [PostgreSQL JSONB](https://www.postgresql.org/docs/current/datatype-json.html)
8. [PostgreSQL Indexes](https://www.postgresql.org/docs/current/indexes.html)

**Architecture References:**
9. [Domain-Driven Design Entities](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/seedwork-domain-model-base-classes-interfaces)
10. [Clean Architecture in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)

## Success Criteria

✅ Rover, Camera, Photo entities defined with complete fields
✅ Hybrid storage configured (columns + JSONB)
✅ Entity relationships configured (Rover → Cameras, Rover → Photos)
✅ Snake_case naming applied automatically
✅ Multiple indexes for common query patterns
✅ JSONB column for complete NASA data preservation
✅ Initial migration created and applied
✅ Database schema verified in PostgreSQL
✅ Foundation ready for scraper implementation

## Next Steps

After completing this story, you'll be ready for:
- **Story 005:** Seed static reference data (rovers, cameras)
- **Story 006:** Build NASA API scraper service
- **Story 007:** Implement REST API endpoints (GET /api/rovers/{rover}/photos)
- **Story 008:** Add filtering, pagination, sorting

## Notes

### Why This Many Fields?

This might seem like a lot of fields, but each serves a purpose:

**For end users:**
- Multiple image sizes → Better mobile experience
- Earth dates → "Show me photos from my birthday"
- Sol numbers → "Show me the first 100 sols"

**For advanced features:**
- `(site, drive)` → "Photos taken within 10 meters"
- `(mast_az, mast_el)` → Auto-detect panoramas, stitch images
- `camera_position` → 3D reconstruction of rover path
- `attitude` → Rover orientation visualization

**For future features:**
- `raw_data` JSONB → Query ANY field NASA provides without schema changes
- `filter_name` → False-color image compositing
- `spacecraft_clock` → Precise event ordering

The Rails API stores ~10 fields. We're storing ~30 structured + complete JSON = 100% data preservation.

### JSONB Performance

JSONB queries are slower than column queries, but:
- GIN indexes make JSONB queries reasonably fast
- Use columns for 80% of queries (rover, sol, camera)
- Use JSONB for advanced/rare queries (attitude, telemetry)

**Benchmark:**
- Column query: `WHERE sol = 1000` → ~1ms (indexed)
- JSONB query: `WHERE raw_data->>'sol' = '1000'` → ~10ms (GIN indexed)
- JSONB query: `WHERE raw_data->>'rare_field' = 'value'` → ~50ms (full scan)

Acceptable trade-off for complete data preservation.

### Future: Computed Columns

We can add computed columns for expensive JSONB queries:

```csharp
entity.Property(e => e.HasTelemetry)
    .HasComputedColumnSql("(raw_data->>'mast_az' IS NOT NULL)", stored: true);
```

This creates an indexed column derived from JSONB data.
