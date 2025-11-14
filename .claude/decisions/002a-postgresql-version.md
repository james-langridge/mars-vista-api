# Decision 002A: PostgreSQL Version Selection

**Date:** 2025-10-13
**Story:** 002 - Set Up PostgreSQL with Docker
**Status:** Active

## Context

We need to choose a PostgreSQL version for the Mars Vista API database. The database will store Mars rover photo metadata with JSONB columns for preserving complete NASA API responses. We need JSONB support, stability, and features that will serve this project long-term.

## Alternatives Considered

### Option 1: PostgreSQL 14
**Pros:**
- Very stable - released September 2021
- Widely deployed in production
- Well-tested with .NET/EF Core
- Long-term support (until November 2026)
- All features we need (JSONB, indexing, etc.)

**Cons:**
- Missing newer performance improvements
- Missing newer JSONB enhancements from PG15/16
- Will be "old" by project completion

### Option 2: PostgreSQL 15 (RECOMMENDED)
**Pros:**
- Current stable version (released October 2022)
- **JSONB performance improvements** (better indexing, faster operations)
- **Improved MERGE statement** (useful for upserts)
- **Better query optimizer** (20-30% faster on complex queries)
- LTS until November 2027
- Battle-tested in production for 2+ years
- Good balance of stability and features
- Default in most Docker images and cloud providers

**Cons:**
- Very minor - slightly newer than PG14 (but still 2+ years old)
- No significant downsides for this use case

### Option 3: PostgreSQL 16
**Pros:**
- Latest stable (released September 2023)
- Even better performance improvements
- Most modern features

**Cons:**
- Newer = less battle-tested
- May have undiscovered edge cases
- Overkill for this project's needs
- Not default in many environments yet

## Decision

**Use PostgreSQL 15**

## Reasoning

1. **JSONB Performance** - PG15 has specific improvements for JSONB operations, which is core to our hybrid storage strategy
2. **Stability + Features** - Sweet spot: stable enough (2+ years in production) but modern enough for latest features
3. **Industry Standard** - PG15 is now the default in most Docker images, cloud providers (AWS RDS, Azure, GCP), and hosting platforms
4. **Long Support** - Supported until November 2027, giving us 4+ years of maintenance
5. **EF Core Compatible** - Excellent support in Entity Framework Core and Npgsql
6. **Grug-Approved** - Not bleeding edge (complexity demon), not outdated. Just right.

## Trade-offs Accepted

- Slightly newer than PG14 (but this is negligible given 2+ years of production use)
- Not the absolute latest (PG16), but we don't need cutting-edge features

## Implementation

```yaml
services:
  postgres:
    image: postgres:15-alpine
```

## References

- [PostgreSQL 15 Release Notes](https://www.postgresql.org/docs/15/release-15.html)
- [PostgreSQL Version Support Policy](https://www.postgresql.org/support/versioning/)
- [Npgsql (PostgreSQL .NET driver) compatibility](https://www.npgsql.org/doc/compatibility.html)
