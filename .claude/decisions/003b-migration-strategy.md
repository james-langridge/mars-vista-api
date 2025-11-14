# Decision 003B: Database Migration Strategy

**Status:** Accepted
**Date:** 2025-11-13
**Decision Makers:** Development Team
**Tags:** #database #migrations #schema-evolution

## Context

We need a strategy for managing database schema changes throughout the project lifecycle. The migration system should:
- Track schema versions
- Apply changes in development, staging, and production
- Support rollback if a migration fails
- Work with team collaboration (multiple developers)
- Integrate with CI/CD pipelines

## Decision

**We will use Entity Framework Core code-first migrations.**

Migrations will be:
- Generated from C# entity classes
- Stored in `src/MarsVista.Api/Data/Migrations/`
- Version controlled in Git
- Applied automatically on application startup (development only)
- Applied manually via CLI in production

## Options Considered

### Option 1: EF Core Code-First Migrations ⭐ (SELECTED)

**Workflow:**
1. Define/change entities in C#
2. Run: `dotnet ef migrations add MigrationName`
3. Review generated migration code
4. Apply: `dotnet ef database update` (or auto-apply on startup)

**Example Migration:**
```csharp
public partial class AddRoversTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "rovers",
            columns: table => new
            {
                id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(maxLength: 50, nullable: false),
                landing_date = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_rovers", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "rovers");
    }
}
```

**Pros:**
- **Single source of truth** - Entities define schema
- **Type safety** - Changes checked at compile time
- **Automatic generation** - No manual SQL writing (can override)
- **Version controlled** - Migrations are C# files in Git
- **Rollback support** - Down() methods for reverting
- **Idempotent** - Can safely re-run on any environment
- **Team friendly** - Merge conflicts are normal C# conflicts
- **CI/CD integration** - `dotnet ef database update` in pipeline

**Cons:**
- **Generated code complexity** - Migrations can be verbose
- **Must review migrations** - Generated SQL may not be optimal
- **Database-specific** - PostgreSQL features require manual tweaks
- **Merge conflicts** - Timestamp prefixes can conflict
- **Learning curve** - Understanding Up/Down methods

**Tooling:**
```bash
# Create migration
dotnet ef migrations add InitialCreate --project src/MarsVista.Api

# Apply migrations
dotnet ef database update --project src/MarsVista.Api

# Rollback to specific migration
dotnet ef database update PreviousMigration --project src/MarsVista.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project src/MarsVista.Api

# Generate SQL script
dotnet ef migrations script --project src/MarsVista.Api --output migration.sql
```

### Option 2: Database-First with Reverse Engineering

**Workflow:**
1. Manually create/alter database schema
2. Run: `dotnet ef dbcontext scaffold`
3. EF Core generates C# entities from database

**Pros:**
- **Direct SQL control** - Write exactly the SQL you want
- **Database expertise** - DBAs can manage schema
- **Advanced features** - Easy to use PostgreSQL-specific features

**Cons:**
- **Manual schema management** - No automatic versioning
- **Entity overwriting** - Scaffolding overwrites C# changes
- **No rollback** - Must manually write rollback scripts
- **Team coordination** - Who applies the SQL changes?
- **CI/CD complexity** - Must track which scripts have been run

### Option 3: Raw SQL Migrations (Custom System)

**Workflow:**
1. Write SQL files manually: `001_create_rovers.sql`, `002_add_cameras.sql`
2. Track applied migrations in a `schema_migrations` table
3. Build custom migration runner

**Example:**
```sql
-- migrations/001_create_rovers.sql
CREATE TABLE rovers (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    landing_date DATE NOT NULL
);
```

**Pros:**
- **Full SQL control** - Write exactly what you need
- **Simple** - Easy to understand (just SQL files)
- **Database agnostic** - Could switch databases

**Cons:**
- **No type safety** - Entities and SQL can drift apart
- **Manual tracking** - Must build migration runner
- **No rollback support** - Must write separate down files
- **Team coordination** - Numbering conflicts
- **No tooling** - Must build everything yourself

### Option 4: Fluent Migrator

**Third-party migration framework:**
```csharp
[Migration(202511130001)]
public class AddRoversTable : Migration
{
    public override void Up()
    {
        Create.Table("rovers")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(50).NotNullable();
    }

    public override void Down()
    {
        Delete.Table("rovers");
    }
}
```

**Pros:**
- **Database agnostic** - Supports multiple databases
- **Fluent API** - Expressive migration syntax
- **Standalone** - Not tied to ORM

**Cons:**
- **Extra dependency** - Another framework to learn
- **Less integrated** - Doesn't use EF Core entities
- **Drift risk** - Entities and migrations can diverge
- **Community size** - Smaller than EF Core

## Trade-off Analysis

| Criterion | EF Core Migrations | Database-First | Raw SQL | Fluent Migrator |
|-----------|-------------------|----------------|---------|-----------------|
| **Type Safety** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐ | ⭐⭐⭐⭐ |
| **Ease of Use** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Version Control** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Rollback Support** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **SQL Control** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Team Collaboration** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **CI/CD Integration** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Tooling** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐ | ⭐⭐⭐⭐ |

## Rationale

**EF Core code-first migrations are the best choice because:**

1. **Single source of truth** - C# entities drive the schema. No risk of entities and database drifting apart.

2. **Developer productivity** - Schema changes are automatic:
   ```csharp
   // Add a property
   public string ImgSrcFull { get; set; }

   // Run migration
   dotnet ef migrations add AddImgSrcFull

   // Done! No manual SQL
   ```

3. **Version control integration** - Migrations are C# files that Git tracks. Merge conflicts are normal C# merge conflicts.

4. **Team collaboration** - Multiple developers can create migrations independently. Conflicts are rare and easy to resolve.

5. **Rollback safety** - Every migration has Up() and Down() methods. Easy to roll back if something goes wrong.

6. **Production deployment** - Generate SQL scripts for review:
   ```bash
   dotnet ef migrations script --output deploy.sql
   ```
   DBAs can review before applying.

7. **PostgreSQL support** - Npgsql provider generates PostgreSQL-specific SQL (JSONB, arrays, etc.).

**When to manually edit migrations:**
- JSONB indexes: `CREATE INDEX USING gin`
- Custom SQL for data migrations
- PostgreSQL-specific performance optimizations
- Complex constraints or triggers

## Implementation

### Migration Naming Convention

Use descriptive names with imperative mood:
- ✅ `AddPhotosTable`
- ✅ `AddRawDataJsonbColumn`
- ✅ `CreateIndexOnSolAndEarthDate`
- ❌ `Migration1`, `Update2`, `FixStuff`

### Migration Workflow

**Development:**
1. Modify entities in C#
2. Run: `dotnet ef migrations add DescriptiveName`
3. Review generated migration
4. Apply: `dotnet ef database update`
5. Test migration
6. Commit migration files to Git

**CI/CD Pipeline:**
```bash
# In deployment script
dotnet ef database update --project src/MarsVista.Api --connection "$CONN_STRING"
```

**Production:**
```bash
# Generate SQL script for DBA review
dotnet ef migrations script --output production-migration.sql

# Or apply directly (with care!)
dotnet ef database update --connection "$PROD_CONN_STRING"
```

### Auto-Apply Migrations on Startup (Development Only)

**Program.cs:**
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
    await context.Database.MigrateAsync();
}
```

**⚠️ Production:** Never auto-apply migrations in production. Always review and test first.

### Handling Merge Conflicts

**Scenario:** Two developers create migrations simultaneously.

**Resolution:**
1. Pull latest changes
2. Remove your local migration: `dotnet ef migrations remove`
3. Re-create migration: `dotnet ef migrations add YourMigrationName`
4. EF Core generates a new timestamp (sorts after existing)

### Migration File Structure

```
src/MarsVista.Api/Data/Migrations/
├── 20251113120000_InitialCreate.cs
├── 20251113120000_InitialCreate.Designer.cs
├── 20251114093000_AddPhotosTable.cs
├── 20251114093000_AddPhotosTable.Designer.cs
├── 20251115101500_AddRawDataJsonbColumn.cs
├── 20251115101500_AddRawDataJsonbColumn.Designer.cs
└── MarsVistaDbContextModelSnapshot.cs
```

**Files:**
- `*_MigrationName.cs` - Up() and Down() methods
- `*_MigrationName.Designer.cs` - Metadata (auto-generated)
- `MarsVistaDbContextModelSnapshot.cs` - Current model state

## Consequences

**Positive:**
- Fast schema iteration during development
- Type-safe schema changes
- Automatic migration generation
- Built-in rollback support
- Excellent team collaboration

**Negative:**
- Must review generated SQL for optimization
- PostgreSQL-specific features may need manual tweaks
- Timestamp conflicts in rare cases

**Mitigation:**
- Review all migrations before merging
- Use `migrationBuilder.Sql()` for custom SQL
- Document PostgreSQL-specific patterns
- Test migrations in staging before production

## Examples

### Basic Entity Change

**Before:**
```csharp
public class Rover
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

**After:**
```csharp
public class Rover
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime LandingDate { get; set; }  // NEW
}
```

**Migration:**
```bash
dotnet ef migrations add AddRoverLandingDate
```

**Generated:**
```csharp
public partial class AddRoverLandingDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "landing_date",
            table: "rovers",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "landing_date",
            table: "rovers");
    }
}
```

### Custom SQL in Migration

**Add JSONB GIN index:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(/* ... */);

    // Custom SQL for PostgreSQL-specific index
    migrationBuilder.Sql(@"
        CREATE INDEX idx_photos_raw_data_gin
        ON photos
        USING gin (raw_data);
    ");
}
```

### Data Migration

**Migrate data when changing schema:**
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // 1. Add new column
    migrationBuilder.AddColumn<string>(
        name: "full_name",
        table: "rovers",
        nullable: true);

    // 2. Migrate data
    migrationBuilder.Sql(@"
        UPDATE rovers
        SET full_name = name || ' Rover';
    ");

    // 3. Make column non-nullable
    migrationBuilder.AlterColumn<string>(
        name: "full_name",
        table: "rovers",
        nullable: false);
}
```

## References

- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Migration Commands](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [Applying Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Custom Migration Operations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations)

## Review Notes

This decision should be reviewed if:
- Team finds generated migrations too complex
- Need for database-agnostic migrations (unlikely)
- Migration conflicts become frequent (poor coordination)

Expected review date: After 20-30 migrations (6-12 months)
