# Decision 002: Database System Selection

**Date:** 2025-10-13
**Story:** 002 - Set Up PostgreSQL with Docker
**Status:** Active

## Context

The Mars Vista API needs to store Mars rover photo metadata with two key requirements:
1. **Queryable structured data** - rover name, sol, earth date, camera type for filtering/searching
2. **Complete data preservation** - Store 100% of NASA's API response (not just 5-10% like the Rails API does)

The implementation guide assumes PostgreSQL, but we should validate this choice against alternatives.

**Key Requirements:**
- JSON/JSONB storage with querying capability (NASA responses are JSON)
- Structured relational data for filtering (rover, sol, camera, date)
- Hybrid approach: columns + JSON in same table
- Good .NET/Entity Framework Core support
- Production-ready and scalable
- Reasonable cost (free or affordable)

## Alternatives Considered

### Option 1: PostgreSQL (RECOMMENDED)
**Pros:**
- **Native JSONB support** - Binary JSON storage with indexing and querying
- **Hybrid storage** - Relational columns + JSONB in same table (perfect for our use case)
- **JSONB operators** - Query into JSON: `data->>'field'`, `data @> '{"key":"value"}'`
- **JSONB indexes** - GIN indexes for fast JSON queries
- **Excellent EF Core support** - First-class with Npgsql provider
- **Free and open source** - No licensing costs
- **Production proven** - Used by GitHub, Instagram, Spotify, etc.
- **Docker-ready** - Official images, easy local development
- **Cloud deployment** - Available on AWS RDS, Azure, GCP, Heroku, etc.
- **Aligns with Rails API** - Original Mars Photo API could use PostgreSQL (common Rails choice)

**Cons:**
- Requires separate server (but Docker solves this for dev)
- Slightly more complex than SQLite for beginners
- Not Microsoft's primary database (but still excellent .NET support)

### Option 2: SQL Server
**Pros:**
- Microsoft's database - native .NET integration
- JSON support (added in SQL Server 2016+)
- Excellent Visual Studio/Rider integration
- LocalDB for development (lightweight)

**Cons:**
- **Weaker JSON support** - JSON stored as text, not binary like JSONB
- **No JSON indexing** - Can't efficiently index into JSON fields (must extract to columns)
- **No native JSON operators** - Must use `JSON_VALUE()`, `JSON_QUERY()` (verbose)
- **Licensing costs** - Free for dev (Express edition), expensive for production
- **Limited cloud options** - Azure SQL (expensive), AWS RDS (limited)
- **Overkill for this project** - Enterprise features we don't need
- Not common in Rails/Python ecosystem (different from original API's environment)

### Option 3: MySQL/MariaDB
**Pros:**
- Popular, widely deployed
- JSON support (added in MySQL 5.7+)
- Free and open source
- Good cloud support

**Cons:**
- **JSON less mature than PostgreSQL** - Text-based until recently
- **Limited JSON querying** - Less powerful operators than PostgreSQL
- **EF Core support weaker** - PostgreSQL has better .NET provider
- **Missing features** - No true JSONB binary format
- PostgreSQL generally considered more advanced for complex queries

### Option 4: MongoDB (NoSQL)
**Pros:**
- Native JSON (BSON) storage
- Flexible schema
- Excellent for document storage

**Cons:**
- **No relational queries** - Filtering by rover + sol + camera requires different approach
- **Poor fit for hybrid model** - Either all JSON or all documents, not structured + JSON
- **EF Core support limited** - MongoDB driver is separate, not standard EF Core
- **Overkill** - We don't need document database features
- **Different paradigm** - Less familiar for relational data + API endpoints
- Over-engineering for this use case

### Option 5: SQLite
**Pros:**
- Zero configuration - file-based database
- Built into .NET
- Perfect for development and small projects
- JSON1 extension for JSON support

**Cons:**
- **Not production-ready for web APIs** - Single file, no concurrent writes
- **Limited JSON support** - Extension required, less mature than PostgreSQL
- **No scalability** - Can't handle multiple API servers
- **Not suitable for deployment** - Fine for mobile/desktop, wrong tool for API

## Decision

**Use PostgreSQL with JSONB storage**

## Reasoning

1. **Perfect Fit for Requirements** - Hybrid relational + JSONB is exactly our use case. Store queryable columns (rover, sol, camera) alongside complete JSON response.

2. **JSONB is Superior** - PostgreSQL's binary JSON format with native operators and GIN indexing is the best JSON storage among relational databases:
   ```sql
   -- PostgreSQL: Simple and fast
   WHERE data->>'camera' = 'FHAZ'

   -- SQL Server: Verbose
   WHERE JSON_VALUE(data, '$.camera') = 'FHAZ'
   ```

3. **Production-Ready** - Free, scalable, cloud-deployable. No licensing surprises.

4. **Excellent .NET Support** - Npgsql + EF Core is mature and widely used. Better than MySQL/MariaDB .NET support.

5. **Future-Proofing** - If we want advanced features later (full-text search, PostGIS for location data, advanced aggregations), PostgreSQL has them.

6. **Ecosystem Alignment** - Rails developers often use PostgreSQL. Using the same database technology helps if we want to compare implementations.

7. **Grug-Approved** - PostgreSQL is boring technology (in the best way). Proven, stable, widely understood. Not chasing trends.

## Trade-offs Accepted

- Not Microsoft's database (but Npgsql support is excellent)
- Requires running a database server (Docker makes this trivial)
- Slightly more complex than SQLite (but we need a real database anyway)

## Implementation

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:15-alpine
    # ... configuration
```

```csharp
// Connection string in appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password"
}
```

## Why Not the Others

- **SQL Server**: JSON support is second-class citizen. Licensing costs for production.
- **MySQL**: PostgreSQL's JSON support is more mature and powerful.
- **MongoDB**: Wrong tool - we need relational queries AND JSON, not pure documents.
- **SQLite**: Not suitable for web API production deployment.

## References

- [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)
- [PostgreSQL vs MySQL for JSON](https://www.postgresql.org/about/featurematrix/)
- [Npgsql - PostgreSQL .NET Provider](https://www.npgsql.org/)
- [EF Core PostgreSQL Provider](https://www.npgsql.org/efcore/)
- [SQL Server JSON vs PostgreSQL JSONB](https://www.mssqltips.com/sqlservertip/6134/sql-server-json-vs-postgresql-jsonb/)
