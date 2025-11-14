# Decision 003: ORM Selection

**Status:** Accepted
**Date:** 2025-11-13
**Decision Makers:** Development Team
**Tags:** #database #orm #architecture

## Context

We need an object-relational mapping (ORM) solution to interact with our PostgreSQL database from C#/.NET. The ORM should:
- Map C# classes to database tables
- Provide a query interface (preferably LINQ)
- Support PostgreSQL-specific features (JSONB, arrays, etc.)
- Handle migrations for schema evolution
- Be maintainable and well-documented

## Decision

**We will use Entity Framework Core (EF Core) as our ORM.**

## Options Considered

### Option 1: Entity Framework Core ⭐ (SELECTED)

**Pros:**
- **Official Microsoft ORM** - First-party support, long-term commitment
- **Code-first migrations** - Define schema in C#, generate SQL automatically
- **Excellent LINQ support** - Write queries in C# with compile-time checking
- **Strong PostgreSQL support** - Npgsql provider has excellent JSONB support
- **Change tracking** - Automatically detects entity changes for updates
- **Lazy/eager loading** - Flexible data loading strategies
- **Mature ecosystem** - Extensive documentation, community, tooling
- **DbContext scoping** - Integrates perfectly with ASP.NET Core DI
- **Navigation properties** - Clean relationship modeling
- **Query translation** - LINQ → SQL is transparent and predictable

**Cons:**
- **Performance overhead** - Slightly slower than raw SQL or micro-ORMs
- **Learning curve** - LINQ and EF conventions take time to master
- **Query complexity** - Complex queries can be harder than raw SQL
- **Over-fetching** - May load more data than needed (mitigated with projections)
- **Tracking overhead** - Change tracking adds memory/CPU cost

**Cost:** Free (open source)
**Maturity:** Very mature (10+ years, v9.0 current)

### Option 2: Dapper (Micro-ORM)

**Pros:**
- **Performance** - Minimal overhead, nearly raw ADO.NET speed
- **Simple** - Just maps query results to objects
- **Control** - You write the SQL, you know what executes
- **No magic** - Explicit, predictable behavior
- **Lightweight** - Minimal dependencies

**Cons:**
- **Manual SQL** - Must write all SQL by hand
- **No migrations** - Schema management is manual or requires separate tools
- **No change tracking** - Must manually track entity state for updates
- **Boilerplate** - Repetitive CRUD code
- **Relationship mapping** - Must manually handle joins and relationships
- **PostgreSQL features** - Less idiomatic JSONB support

**Cost:** Free (open source)
**Maturity:** Very mature (used by Stack Overflow)

### Option 3: NHibernate

**Pros:**
- **Mature** - 15+ years old, battle-tested
- **Powerful** - Very flexible mapping options
- **HQL** - Hibernate Query Language (similar to LINQ)

**Cons:**
- **Complexity** - Steep learning curve, XML configuration
- **Legacy feel** - Falling out of favor in .NET ecosystem
- **Less .NET Core focus** - Slower to adopt modern .NET patterns
- **Smaller community** - Compared to EF Core

**Cost:** Free (open source)
**Maturity:** Very mature but declining

### Option 4: Raw ADO.NET (No ORM)

**Pros:**
- **Maximum performance** - No abstraction overhead
- **Full control** - Complete control over SQL and execution
- **No dependencies** - Built into .NET

**Cons:**
- **Massive boilerplate** - Hundreds of lines for basic CRUD
- **SQL injection risk** - Manual parameter handling
- **No type safety** - String-based queries, runtime errors
- **Maintenance burden** - Every schema change touches many files
- **No migrations** - Manual schema versioning

**Cost:** Free (built-in)
**Maturity:** Very mature

## Trade-off Analysis

| Criterion | EF Core | Dapper | NHibernate | Raw ADO.NET |
|-----------|---------|--------|------------|-------------|
| **Productivity** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ |
| **Performance** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **PostgreSQL/JSONB Support** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Type Safety** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ |
| **Migrations** | ⭐⭐⭐⭐⭐ | ❌ | ⭐⭐⭐ | ❌ |
| **Learning Curve** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Documentation** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Community** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Maintainability** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ |

## Rationale

**Entity Framework Core is the best choice for this project because:**

1. **Productivity wins** - We're building a full API from scratch. Code-first migrations and LINQ queries will save hundreds of hours compared to writing SQL manually.

2. **PostgreSQL JSONB support** - Npgsql's EF Core provider has excellent JSONB support using `JsonDocument`. We can query JSON fields with LINQ, which is critical for our hybrid storage model (structured columns + raw NASA JSON).

3. **Maintainability** - As the API grows (panoramas, stereo pairs, analytics), EF Core's navigation properties and eager loading will keep code clean.

4. **Migrations** - Schema evolution is first-class. We can version control migrations and deploy them automatically.

5. **Performance is sufficient** - For a NASA photo API, we're limited by external API calls and image processing, not database queries. EF Core's overhead is negligible in this context.

6. **Team expertise** - Standard .NET stack, easier onboarding for future developers.

**When we might use Dapper:**
- Hot path queries after profiling shows EF Core overhead
- Complex reporting queries better expressed as raw SQL
- Bulk operations (EF Core has bulk extensions, but Dapper is simpler)

**Hybrid approach:** We can use EF Core for 95% of operations and drop down to Dapper/ADO.NET for specific performance-critical paths if needed.

## Implementation Notes

**NuGet Packages:**
```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**DbContext registration:**
```csharp
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
```

**JSONB mapping example:**
```csharp
public class Photo
{
    public int Id { get; set; }
    public JsonDocument RawData { get; set; }  // PostgreSQL JSONB
}

// In OnModelCreating:
modelBuilder.Entity<Photo>()
    .Property(p => p.RawData)
    .HasColumnType("jsonb");
```

## Consequences

**Positive:**
- Rapid development of CRUD operations
- Type-safe queries with LINQ
- Automatic schema migrations
- Clean separation of data access layer

**Negative:**
- Learning curve for LINQ and EF conventions
- May need profiling to catch N+1 query issues
- Slightly higher memory usage due to change tracking

**Mitigation:**
- Use AsNoTracking() for read-only queries
- Enable query logging in development
- Profile with MiniProfiler or Application Insights
- Use explicit loading to avoid N+1 problems

## References

- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql EF Core Provider](https://www.npgsql.org/efcore/)
- [EF Core vs Dapper Comparison](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-implementation-entity-framework-core)
- [JSONB Support in Npgsql](https://www.npgsql.org/efcore/mapping/json.html)

## Review Notes

This decision should be reviewed if:
- Performance profiling shows unacceptable query overhead (> 100ms for simple queries)
- Team finds EF Core's abstraction too limiting for complex queries
- Migration strategy proves cumbersome

Expected review date: After initial API implementation (3-6 months)
