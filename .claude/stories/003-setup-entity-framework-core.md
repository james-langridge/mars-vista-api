# Story 003: Configure Entity Framework Core with PostgreSQL

## Story
As a developer, I need to configure Entity Framework Core with PostgreSQL so that I can use object-relational mapping (ORM) to interact with the database using C# entities instead of raw SQL.

## Acceptance Criteria
- [ ] Entity Framework Core NuGet packages installed
- [ ] Npgsql (PostgreSQL provider) package installed
- [ ] DbContext class created and configured
- [ ] Connection string configured in appsettings.json
- [ ] DbContext registered in dependency injection container
- [ ] Can successfully connect to PostgreSQL database
- [ ] Basic entity (Rover) created and mapped to test EF Core setup
- [ ] Design-time DbContext factory configured for migrations

## Context
Entity Framework Core (EF Core) is Microsoft's modern object-relational mapper (ORM) for .NET. It allows us to:
- Work with databases using C# objects instead of SQL
- Define database schema using C# classes (code-first approach)
- Manage schema changes with migrations
- Query data using LINQ (Language Integrated Query)

We're using PostgreSQL with the Npgsql provider, which has excellent support for PostgreSQL-specific features like JSONB.

This story sets up the foundation for database access. Future stories will add the actual domain models and migrations.

## Implementation Steps

### 1. Install Required NuGet Packages

Add the following packages to `src/MarsVista.Api/MarsVista.Api.csproj`:

```bash
cd src/MarsVista.Api
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**Package explanations:**
- `Microsoft.EntityFrameworkCore` - Core EF functionality
- `Microsoft.EntityFrameworkCore.Design` - Design-time tools (required for migrations)
- `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL provider with JSONB support

**Documentation:**
- [Entity Framework Core Overview](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)

### 2. Create the DbContext Class

Create a new directory and DbContext class:

**File:** `src/MarsVista.Api/Data/MarsVistaDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Data;

public class MarsVistaDbContext : DbContext
{
    public MarsVistaDbContext(DbContextOptions<MarsVistaDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added in future stories
    // Example: public DbSet<Photo> Photos { get; set; }
    // Example: public DbSet<Rover> Rovers { get; set; }
    // Example: public DbSet<Camera> Cameras { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added here
        // This is where we'll configure:
        // - Table names (snake_case convention)
        // - JSONB columns
        // - Indexes
        // - Relationships
        // - Constraints
    }
}
```

**What is DbContext?**
DbContext is the primary class for interacting with the database. It:
- Represents a session with the database
- Manages entity instances and tracks changes
- Provides DbSet<T> properties for querying and saving entities
- Configures the database schema via OnModelCreating

**Documentation:**
- [DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)

### 3. Configure Connection String

Update `src/MarsVista.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password"
  }
}
```

**Connection string breakdown:**
- `Host=localhost` - Database server (localhost because Docker maps port)
- `Port=5432` - PostgreSQL default port
- `Database=marsvista_dev` - Database name (from docker-compose.yml)
- `Username=marsvista` - Database user
- `Password=marsvista_dev_password` - Database password

**Security note:** This is fine for development. Production will use environment variables or Azure Key Vault.

Also create `appsettings.Development.json` for development-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

This enables SQL query logging in development mode.

**Documentation:**
- [Connection Strings](https://www.npgsql.org/doc/connection-string-parameters.html)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

### 4. Register DbContext in Dependency Injection

Update `src/MarsVista.Api/Program.cs`:

Add the following after `var builder = WebApplication.CreateBuilder(args);`:

```csharp
// Add services to the container
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));
```

**Full Program.cs structure:**
```csharp
using MarsVista.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));

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

**What does this do?**
- `AddDbContext<T>()` - Registers DbContext with DI container
- `UseNpgsql()` - Configures PostgreSQL provider
- `EnableRetryOnFailure()` - Automatically retries on transient failures (recommended for production)

**Documentation:**
- [DbContext Dependency Injection](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#dbcontext-in-dependency-injection-for-aspnet-core)

### 5. Create Design-Time DbContext Factory

EF Core migration tools need to create a DbContext at design time (when running `dotnet ef` commands).

Create `src/MarsVista.Api/Data/MarsVistaDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarsVista.Api.Data;

public class MarsVistaDbContextFactory : IDesignTimeDbContextFactory<MarsVistaDbContext>
{
    public MarsVistaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarsVistaDbContext>();

        // Use the connection string from appsettings.json
        // In a real scenario, you might read this from environment variables
        var connectionString = "Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password";

        optionsBuilder.UseNpgsql(connectionString);

        return new MarsVistaDbContext(optionsBuilder.Options);
    }
}
```

**Why do we need this?**
When you run `dotnet ef migrations add`, the tool needs to instantiate your DbContext without running your web application. This factory provides the connection string at design time.

**Documentation:**
- [Design-time DbContext Creation](https://learn.microsoft.com/en-us/ef/core/cli/dbcontext-creation)

### 6. Install EF Core Tools

Install the global EF Core tools for running migrations:

```bash
dotnet tool install --global dotnet-ef
```

Or update if already installed:
```bash
dotnet tool update --global dotnet-ef
```

**Verify installation:**
```bash
dotnet ef --version
```

Should show version 9.0.x or similar.

**Documentation:**
- [Entity Framework Core tools reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

### 7. Test Database Connection

Create a simple test endpoint to verify EF Core can connect to PostgreSQL.

**File:** `src/MarsVista.Api/Controllers/HealthController.cs`

```csharp
using MarsVista.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly MarsVistaDbContext _context;

    public HealthController(MarsVistaDbContext context)
    {
        _context = context;
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Try to connect to the database
            var canConnect = await _context.Database.CanConnectAsync();

            if (canConnect)
            {
                return Ok(new
                {
                    status = "healthy",
                    database = "connected",
                    message = "Successfully connected to PostgreSQL"
                });
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "disconnected",
                message = "Cannot connect to PostgreSQL"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "error",
                message = ex.Message
            });
        }
    }
}
```

**Test the endpoint:**
1. Make sure PostgreSQL is running: `docker compose ps`
2. Run the API: `dotnet run --project src/MarsVista.Api`
3. Navigate to: `https://localhost:5001/api/health/db`
4. Should return: `{"status":"healthy","database":"connected",...}`

### 8. Verify EF Core Setup

Run the following command to verify EF Core is configured correctly:

```bash
dotnet ef dbcontext info --project src/MarsVista.Api
```

Should output:
```
Provider name: Npgsql.EntityFrameworkCore.PostgreSQL
Database name: marsvista_dev
Data source: localhost
Options: None
```

If you get errors, the design-time factory isn't configured correctly.

## Technical Decisions

Document the following decisions in `.claude/decisions/`:

### Decision 003: ORM Selection
**Recommendation:** Entity Framework Core
- Official Microsoft ORM for .NET
- Code-first migrations (define schema in C#)
- Excellent LINQ support for queries
- Strong PostgreSQL/JSONB support via Npgsql
- Mature, well-documented, production-ready

**Alternatives considered:**
- Dapper (micro-ORM, more control but more boilerplate)
- NHibernate (mature but complex, falling out of favor)
- Raw ADO.NET (too low-level, no ORM benefits)

**Why EF Core wins:** Best balance of productivity and performance for modern .NET apps. Code-first approach aligns with our development workflow.

### Decision 003A: Database Naming Convention
**Recommendation:** Snake_case (PostgreSQL convention)
- Table names: `photos`, `rovers`, `cameras`
- Column names: `img_src_full`, `earth_date`, `created_at`
- Follows PostgreSQL and Rails API conventions
- More readable in SQL queries
- EF Core will map C# PascalCase properties automatically

**Alternative:** PascalCase (C# convention)
**Why snake_case:** Consistency with PostgreSQL ecosystem and original Rails API

### Decision 003B: Migration Strategy
**Recommendation:** Code-first migrations
- Define entities in C# classes
- Generate SQL migrations from code changes
- Version control tracks schema evolution
- Easy to roll back or apply migrations

**Alternative:** Database-first (reverse-engineer from existing DB)
**Why code-first:** We're building from scratch, code-first is more maintainable

## Testing Checklist

- [ ] Packages installed successfully (`dotnet restore`)
- [ ] Project builds (`dotnet build`)
- [ ] PostgreSQL is running (`docker compose ps`)
- [ ] API starts without errors (`dotnet run`)
- [ ] Health endpoint returns "healthy" (`/api/health/db`)
- [ ] `dotnet ef dbcontext info` shows correct configuration
- [ ] Can see SQL logs in development (check console output)

## Key Documentation Links

**Essential Reading:**
1. [Getting Started with EF Core](https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app)
2. [EF Core DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
3. [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
4. [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

**PostgreSQL-Specific:**
5. [Npgsql JSONB Support](https://www.npgsql.org/efcore/mapping/json.html)
6. [PostgreSQL Connection Strings](https://www.npgsql.org/doc/connection-string-parameters.html)

**Helpful for Understanding:**
7. [EF Core vs Dapper](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core)
8. [Dependency Injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)

## Success Criteria

✅ Entity Framework Core configured with PostgreSQL
✅ Connection to database verified
✅ DbContext registered in DI container
✅ Design-time factory enables migrations
✅ Health check endpoint confirms database connectivity
✅ SQL query logging enabled in development
✅ Ready to define entities and create migrations

## Next Steps

After completing this story, you'll be ready for:
- **Story 004:** Define core domain entities (Rover, Camera, Photo)
- **Story 005:** Create initial database migration
- **Story 006:** Seed static data (rovers, cameras)
- **Story 007:** Build NASA API scraper service

## Notes

**Why Npgsql?**
Npgsql is the most popular and well-maintained PostgreSQL provider for .NET. It has:
- First-class JSONB support (we'll use `JsonDocument` type)
- Array mapping
- Full-text search support
- PostGIS spatial extensions (if needed later)

**Development vs Production Configuration**
- Development: Connection string in appsettings.json, SQL logging enabled
- Production: Connection string from environment variables, minimal logging
- We'll add environment-specific configuration in later stories

**Connection Pooling**
Npgsql automatically manages connection pooling. Default settings are:
- Minimum pool size: 0
- Maximum pool size: 100
- Connection lifetime: 0 (unlimited)

These defaults work well for most applications.
