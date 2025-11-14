# Decision 003A: Database Naming Convention

**Status:** Accepted
**Date:** 2025-11-13
**Decision Makers:** Development Team
**Tags:** #database #conventions #code-style

## Context

We need to decide on a naming convention for database objects (tables, columns, indexes). This affects:
- SQL query readability
- Compatibility with PostgreSQL ecosystem tools
- Consistency with the original Rails API (which we're recreating)
- Mapping between C# (PascalCase) and database (TBD)

## Decision

**We will use snake_case for all database object names (PostgreSQL convention).**

- Table names: `photos`, `rovers`, `cameras`, `mars_locations`
- Column names: `img_src_full`, `earth_date`, `created_at`, `updated_at`
- Foreign keys: `rover_id`, `camera_id`
- Indexes: `idx_photos_sol`, `idx_photos_earth_date`

**C# entities will use PascalCase, and EF Core will handle the mapping automatically.**

## Options Considered

### Option 1: snake_case (PostgreSQL/Rails convention) ⭐ (SELECTED)

**Example:**
```sql
CREATE TABLE photos (
    id INTEGER PRIMARY KEY,
    nasa_id VARCHAR(255),
    img_src_full TEXT,
    earth_date DATE,
    created_at TIMESTAMP
);
```

**C# mapping:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public string NasaId { get; set; }
    public string ImgSrcFull { get; set; }
    public DateTime? EarthDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Pros:**
- **PostgreSQL convention** - Idiomatic for Postgres ecosystem
- **Original API consistency** - Rails API uses snake_case
- **Readability in SQL** - Lowercase is easier to read: `SELECT earth_date FROM photos`
- **No quoting needed** - Lowercase doesn't require double quotes
- **Tool compatibility** - Works well with pgAdmin, psql, etc.
- **Community standard** - Most PostgreSQL tutorials use snake_case

**Cons:**
- **Case mismatch** - C# uses PascalCase, requires mapping
- **Verbosity** - Slightly longer: `created_at` vs `CreatedAt`
- **Migration boilerplate** - Need to configure EF Core conventions

### Option 2: PascalCase (C# convention)

**Example:**
```sql
CREATE TABLE Photos (
    Id INTEGER PRIMARY KEY,
    NasaId VARCHAR(255),
    ImgSrcFull TEXT,
    EarthDate DATE,
    CreatedAt TIMESTAMP
);
```

**Pros:**
- **C# consistency** - Matches C# naming exactly
- **No mapping needed** - EF Core default behavior
- **Less configuration** - Fewer EF Core conventions to set up

**Cons:**
- **Not PostgreSQL convention** - Unusual in Postgres world
- **Requires quoting** - Must quote identifiers: `SELECT "EarthDate" FROM "Photos"`
- **Case sensitivity issues** - PostgreSQL folds unquoted identifiers to lowercase
- **Tool incompatibility** - Many Postgres tools expect lowercase
- **Community mismatch** - Most Postgres examples won't work directly

### Option 3: camelCase

**Example:**
```sql
CREATE TABLE photos (
    id INTEGER PRIMARY KEY,
    nasaId VARCHAR(255),
    imgSrcFull TEXT,
    earthDate DATE,
    createdAt TIMESTAMP
);
```

**Pros:**
- **JavaScript consistency** - Matches JSON API responses
- **Single word easy** - `id`, `sol`, `name` same as snake_case

**Cons:**
- **Not idiomatic anywhere** - Neither C# nor PostgreSQL convention
- **Worst of both worlds** - Doesn't match either ecosystem
- **Readability** - Harder to read: `imgSrcFull` vs `img_src_full`

## Trade-off Analysis

| Criterion | snake_case | PascalCase | camelCase |
|-----------|------------|------------|-----------|
| **PostgreSQL convention** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐ |
| **C# convention** | ⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **Rails API consistency** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐ |
| **SQL readability** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **Tool compatibility** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |
| **EF Core mapping** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |

## Rationale

**snake_case is the right choice because:**

1. **PostgreSQL ecosystem** - We're using PostgreSQL, not SQL Server. Following Postgres conventions makes our database easier to work with using standard Postgres tools (pgAdmin, psql, Postico, etc.).

2. **Original API consistency** - The Rails API we're recreating uses snake_case. If we ever need to compare schemas or migrate data, this alignment is valuable.

3. **SQL readability** - Raw SQL queries are much more readable:
   ```sql
   -- snake_case (clear, lowercase)
   SELECT earth_date, img_src_full FROM photos WHERE sol > 1000;

   -- PascalCase (requires quoting, harder to read)
   SELECT "EarthDate", "ImgSrcFull" FROM "Photos" WHERE "Sol" > 1000;
   ```

4. **Industry standard** - Nearly all PostgreSQL documentation, tutorials, and open-source projects use snake_case.

5. **Mapping is trivial** - EF Core can handle this automatically with a simple convention:
   ```csharp
   protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
   {
       configurationBuilder.Properties<string>().HaveMaxLength(255);
       configurationBuilder.Conventions.Add(_ => new TableNameConvention());
   }
   ```

## Implementation

### EF Core Convention Configuration

Create a custom convention to map C# PascalCase to SQL snake_case:

**File:** `src/MarsVista.Api/Data/Conventions/SnakeCaseNamingConvention.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarsVista.Api.Data.Conventions;

public static class SnakeCaseExtensions
{
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return string.Concat(
            input.Select((c, i) => i > 0 && char.IsUpper(c)
                ? $"_{char.ToLower(c)}"
                : char.ToLower(c).ToString()
            )
        );
    }
}

public class SnakeCaseNamingConvention
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            // Table names
            entity.SetTableName(entity.GetTableName()?.ToSnakeCase());

            // Column names
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            // Foreign keys
            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            // Indexes
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }
    }
}
```

**Usage in DbContext:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    SnakeCaseNamingConvention.Apply(modelBuilder);

    // Rest of configuration...
}
```

### Alternative: Use EfCore.NamingConventions Package

Even simpler - use a community package:

```bash
dotnet add package EFCore.NamingConventions
```

```csharp
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention()
);
```

This automatically converts all names to snake_case without manual configuration.

## Examples

**C# Entity:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public string NasaId { get; set; }
    public string ImgSrcFull { get; set; }
    public DateTime? EarthDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RoverId { get; set; }

    public virtual Rover Rover { get; set; }
}
```

**Database Table:**
```sql
CREATE TABLE photos (
    id INTEGER PRIMARY KEY,
    nasa_id VARCHAR(255),
    img_src_full TEXT,
    earth_date DATE,
    created_at TIMESTAMP,
    rover_id INTEGER REFERENCES rovers(id)
);
```

**LINQ Query (C#):**
```csharp
var photos = await _context.Photos
    .Where(p => p.EarthDate > DateTime.Today.AddDays(-7))
    .Include(p => p.Rover)
    .ToListAsync();
```

**Generated SQL:**
```sql
SELECT p.id, p.nasa_id, p.img_src_full, p.earth_date, p.created_at, p.rover_id
FROM photos AS p
INNER JOIN rovers AS r ON p.rover_id = r.id
WHERE p.earth_date > @date;
```

## Consequences

**Positive:**
- Database feels natural to PostgreSQL developers
- Consistency with original Rails API
- Better SQL query readability
- Compatible with all PostgreSQL tools

**Negative:**
- One-time setup cost (install package or write convention)
- Slight disconnect between C# and SQL (mitigated by tooling)
- Must remember convention when writing raw SQL

**Mitigation:**
- Use EfCore.NamingConventions package (5 minutes setup)
- Document convention in README
- Use EF Core for 95% of queries (automatic mapping)

## References

- [PostgreSQL Naming Conventions](https://wiki.postgresql.org/wiki/Don%27t_Do_This#Don.27t_use_upper_case_table_or_column_names)
- [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)
- [Rails ActiveRecord Naming](https://guides.rubyonrails.org/active_record_basics.html#naming-conventions)

## Review Notes

This decision should be reviewed if:
- Team finds the C#/SQL mapping confusing (unlikely with good tooling)
- Future integration requires PascalCase (can override per-entity)
- Migration to different database system (SQL Server prefers PascalCase)

Expected review date: N/A (foundational decision)
