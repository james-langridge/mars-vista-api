# Technical Decisions Index

This document serves as an index to all technical decisions made during the development of Mars Vista API. Each decision is documented in detail in a separate file in the `decisions/` directory.

## Why Document Decisions?

Technical decisions, even "obvious" ones, should be documented because:
- Future you won't remember why you chose X over Y
- Team members need to understand the reasoning
- Decisions should be challenged and validated, not blindly followed
- Trade-offs should be explicit
- Implementation guides make assumptions that should be sanity-checked

## Decision Format

Each decision includes:
- **Context** - Why was this decision needed?
- **Alternatives** - What options were considered?
- **Recommendation** - What was chosen and why?
- **Trade-offs** - What are the pros/cons?
- **Implementation** - How is it implemented?
- **References** - Links to docs and articles

## All Decisions

### Story 001: Initialize .NET Project Structure

#### [Decision 001: Controller-Based API vs Minimal API](decisions/001-controller-based-api.md)
**Status:** Active
**Decision:** Use controller-based API with `--use-controllers` flag

**Summary:** Controller-based APIs provide better organization, scalability, and testability for an API with multiple endpoints (rovers, photos, cameras, manifests). Minimal APIs are better for simple microservices with few endpoints.

---

### Story 002: Set Up PostgreSQL with Docker

#### [Decision 002: Database System Selection](decisions/002-database-system-selection.md)
**Status:** Active
**Decision:** Use PostgreSQL with JSONB storage

**Summary:** PostgreSQL's native JSONB support is superior to all alternatives for our hybrid storage model (queryable columns + complete JSON response). Compared to SQL Server (weak JSON, licensing costs), MySQL (less mature JSON), MongoDB (wrong tool for hybrid), and SQLite (not production-ready).

**Key insight:** PostgreSQL's JSONB is binary JSON with native operators (`data->>'field'`) and GIN indexing - perfect for storing 100% of NASA data while maintaining queryable structured fields.

#### [Decision 002A: PostgreSQL Version Selection](decisions/002a-postgresql-version.md)
**Status:** Active
**Decision:** Use PostgreSQL 15

**Summary:** PG15 is the sweet spot - stable (2+ years production use), JSONB performance improvements over PG14, LTS until 2027, default in most Docker images and cloud providers. Not bleeding edge (PG16) but not outdated (PG14).

#### [Decision 002B: Docker Image Variant](decisions/002b-docker-image-variant.md)
**Status:** Active
**Decision:** Use Alpine image (`postgres:15-alpine`)

**Summary:** Alpine is 38% smaller (80MB vs 130MB), faster to download and start, sufficient for standard PostgreSQL features. All standard features work (JSONB, indexes). Can switch to full image seamlessly if needed.

#### [Decision 002C: Development Credentials Strategy](decisions/002c-credentials-strategy.md)
**Status:** Active
**Decision:** Hardcode credentials in docker-compose.yml

**Summary:** Development database is not sensitive. Hardcoded credentials make setup frictionless (`git clone` → `docker-compose up`). Production will use proper secrets management. Simple solution for simple problem.

#### [Decision 002D: PostgreSQL Port Configuration](decisions/002d-port-configuration.md)
**Status:** Active
**Decision:** Use standard port 5432:5432

**Summary:** Convention over configuration. Standard port works with all default tooling. Conflicts are rare and easily resolved with per-developer override file. Don't deviate from standards without good reason.

---

### Story 003: Configure Entity Framework Core

#### [Decision 003: ORM Selection](decisions/003-orm-selection.md)
**Status:** Active
**Decision:** Use Entity Framework Core as ORM

**Summary:** EF Core provides the best balance of productivity and performance for modern .NET applications. Code-first migrations, LINQ queries, and excellent PostgreSQL/JSONB support via Npgsql make it ideal. Compared to Dapper (more boilerplate, no migrations), NHibernate (outdated), and raw ADO.NET (too low-level).

**Key insight:** We can use EF Core for 95% of operations and drop to Dapper/raw SQL for performance-critical paths if needed (hybrid approach).

#### [Decision 003A: Database Naming Convention](decisions/003a-naming-convention.md)
**Status:** Active
**Decision:** Use snake_case for database objects (PostgreSQL convention)

**Summary:** PostgreSQL ecosystem uses snake_case (tables: `photos`, columns: `earth_date`, `img_src_full`). C# entities use PascalCase - EF Core handles mapping automatically. Consistency with PostgreSQL tools, Rails API, and community standards. SQL queries are more readable without quoting.

**Implementation:** Use `EFCore.NamingConventions` package or custom convention to auto-convert.

#### [Decision 003B: Migration Strategy](decisions/003b-migration-strategy.md)
**Status:** Active
**Decision:** Use EF Core code-first migrations

**Summary:** Single source of truth (C# entities), automatic migration generation, version-controlled in Git, built-in rollback support. Can manually edit migrations for PostgreSQL-specific features (JSONB indexes, custom SQL). Production deployments use reviewed SQL scripts.

**Workflow:** Modify entities → `dotnet ef migrations add` → review → `dotnet ef database update`.

---

### Story 004: Define Core Domain Entities

#### [Decision 004: Entity Field Selection Strategy](decisions/004-entity-field-selection.md)
**Status:** Active
**Decision:** Use hybrid columns + JSONB approach

**Summary:** Store frequently queried fields (rover, camera, sol, date, img_src) as indexed columns for fast queries (1-10ms). Store complete NASA API response in JSONB for 100% data preservation and advanced queries. Balance of performance and completeness - compared to minimal columns (loses 90% of data), all columns (too wide, slow), or pure JSONB (10-100x slower queries).

**Key insight:** 20% storage overhead to preserve 100% of NASA data while maintaining fast common queries. Enables advanced features: panoramas, location search, 3D reconstruction, analytics.

#### [Decision 004A: Multiple Image URL Storage](decisions/004a-multiple-image-urls.md)
**Status:** Active
**Decision:** Store all image URLs provided by NASA (small/medium/large/full)

**Summary:** NASA provides optimized image sizes (320px/800px/1200px/full). Storing all URLs costs 800 bytes per photo ($0.02/month for 1M photos) but eliminates need for server-side image processing ($500-6000/month), reduces latency, and optimizes bandwidth (166x savings for gallery view: 1.5MB vs 250MB).

#### [Decision 004B: NASA ID Uniqueness Strategy](decisions/004b-nasa-id-uniqueness.md)
**Status:** Active
**Decision:** Unique database index on `nasa_id` column

**Summary:** Database enforces uniqueness via unique index - prevents duplicates even with concurrent scrapers, race conditions, or human error. Fast lookups (0.5ms vs 500ms full scan). Application uses find-or-create pattern. No manual locking needed. Works across multiple servers. Simple and reliable.

#### [Decision 004C: Cascade Delete Behavior](decisions/004c-cascade-delete-behavior.md)
**Status:** Active
**Decision:** CASCADE delete for all relationships (Rover→Cameras, Rover→Photos, Camera→Photos)

**Summary:** Photos can't exist without rover/camera (domain logic). Cascade delete ensures data integrity, prevents orphaned records, simplifies code (one delete statement). Used only for testing and data cleanup - production never deletes rovers. Safety via database permissions, application checks, UI confirmations, and backups.

#### [Decision 004D: Timestamp Strategy](decisions/004d-timestamp-strategy.md)
**Status:** Active
**Decision:** Database defaults + EF Core SaveChanges override

**Summary:** `created_at` set by database default (CURRENT_TIMESTAMP). `updated_at` set by database default on insert, then automatically updated by overriding DbContext.SaveChangesAsync(). Fully automatic - developers never touch timestamp code. EF entities stay in sync (no reload needed). Simpler than triggers, sufficient for EF Core-only updates.

---

### Story 005: Seed Static Reference Data

#### [Decision 005: Database Seeding Strategy](decisions/005-seeding-strategy.md)
**Status:** Active
**Decision:** Application code with idempotent seeding (auto in dev, manual in prod)

**Summary:** Seed rovers and cameras using C# application code instead of SQL scripts. Type-safe, maintainable, testable. Auto-runs on startup in development (zero manual steps). Idempotent checks prevent duplicates. Manual CLI command for production. Compared to SQL migrations (no type safety), HasData (hardcoded IDs), and manual-only scripts (poor DX).

**Key insight:** Static data changes rarely (4 rovers, 36 cameras). 150ms startup overhead in dev is worth the convenience and safety. Developers get fresh data automatically, production has explicit control.

---

### Story 006: NASA API Scraper Service

#### [Decision 006: Scraper Service Pattern](decisions/006-scraper-service-pattern.md)
**Status:** Active
**Decision:** One scraper service per rover implementing `IScraperService`

**Summary:** Each rover has different API format (Perseverance JSON vs Curiosity JSON vs Spirit/Opportunity HTML). Separate classes provide clean separation, testability, and extensibility. Can inject `IEnumerable<IScraperService>` and scrape all rovers in parallel. Compared to single scraper with switch statements (god class), background service (Story 010), and static functions (no DI/testing).

**Key insight:** Strategy pattern with dependency injection. Easy to add new rovers without modifying existing code (Open/Closed Principle).

#### [Decision 006A: HTTP Resilience Strategy](decisions/006a-http-resilience.md)
**Status:** Active
**Decision:** Polly with retry (exponential backoff) + circuit breaker

**Summary:** NASA API has transient failures (timeouts, 5xx errors). Polly retry policy waits 2s, 4s, 8s between attempts (3 retries total). Circuit breaker opens after 5 consecutive failures and stops trying for 1 minute (fail fast). Industry-standard resilience library used by Microsoft, AWS, Azure. Compared to manual retry (boilerplate), Azure resilience (too new), and no resilience (poor UX).

**Key insight:** Exponential backoff respects server load. Circuit breaker prevents cascading failures and saves resources (no wasted timeouts). After 5 failures totaling 70s, immediately fail for next 95 requests instead of waiting 23 minutes.

#### [Decision 006B: Duplicate Photo Detection](decisions/006b-duplicate-detection.md)
**Status:** Active
**Decision:** Check database by `nasa_id` before inserting using `AnyAsync`

**Summary:** Pre-check if photo exists using unique index on `nasa_id` (0.5ms per query). Simple, reliable, works across multiple scraper instances. Database is single source of truth. Compared to batch check (saves 45ms but adds complexity), try/catch duplicate key (exception handling for control flow), in-memory cache (doesn't work with multiple instances), and upsert (bypasses EF Core).

**Key insight:** For 100 photos, pre-check adds 50ms overhead. Acceptable for background scraping. Simple is better than premature optimization.

#### [Decision 006C: Unknown Camera Handling](decisions/006c-unknown-camera-handling.md)
**Status:** Active
**Decision:** Auto-create camera record with warning, use camera name as placeholder full name

**Summary:** When NASA adds new instruments or seed data is incomplete, auto-create camera instead of crashing. Photo data preserved, camera full name can be updated later manually. Warning log alerts developers. Compared to throwing exception (scraper crashes, data lost), skipping photo (data lost), queue for review (over-engineered), and using "Unknown" camera (loses information).

**Key insight:** Resilience to NASA changes. Rare event (maybe once per year). Better to have placeholder data than no data. Set up monitoring alert on "UnknownCameraDiscovered" event.

#### [Decision 006D: Bulk Insert Strategy](decisions/006d-bulk-insert-strategy.md)
**Status:** Active
**Decision:** Bulk insert with `AddRangeAsync` + single `SaveChangesAsync`

**Summary:** Collect all new photos in list, then insert with one `SaveChangesAsync` call. 100x faster than individual inserts (500ms vs 50 seconds for 100 photos). Single transaction ensures atomicity. Compared to individual inserts (very slow), batched inserts (more complex, still slower), PostgreSQL COPY (bypasses EF Core, overkill), and ExecuteUpdate (bypasses EF tracking).

**Key insight:** Single database round trip with batched INSERT statements. Memory overhead negligible (100KB for 100 photos). All-or-nothing transaction ensures consistency. Can optimize to COPY command later if scraping 10,000+ photos becomes bottleneck.

---

## Decision Statistics

- **Total Decisions:** 18
- **Active:** 18
- **Superseded:** 0
- **Deprecated:** 0

## By Category

**Architecture & API Design:**
- Controller-Based API (001)
- Scraper Service Pattern (006)

**Data Storage:**
- Database System Selection (002)
- PostgreSQL Version (002A)
- ORM Selection (003)
- Entity Field Selection (004)
- Database Seeding Strategy (005)

**Data Integrity:**
- NASA ID Uniqueness (004B)
- Cascade Delete Behavior (004C)
- Timestamp Strategy (004D)
- Duplicate Photo Detection (006B)

**Code Conventions:**
- Database Naming Convention (003A)
- Migration Strategy (003B)

**Performance & Optimization:**
- Multiple Image URL Storage (004A)
- Bulk Insert Strategy (006D)

**Resilience & Error Handling:**
- HTTP Resilience Strategy (006A)
- Unknown Camera Handling (006C)

**Infrastructure & DevOps:**
- Docker Image Variant (002B)
- Credentials Strategy (002C)
- Port Configuration (002D)

---

## How to Read a Decision

1. **Start with Context** - Understand the problem being solved
2. **Review Alternatives** - See what options were considered
3. **Read the Reasoning** - Understand why this choice was made
4. **Check Trade-offs** - Know what you're accepting
5. **Look at Implementation** - See concrete examples

## Challenging Decisions

All decisions can be challenged and revisited if circumstances change:
- New information becomes available
- Requirements change
- Technology improves
- Trade-offs no longer make sense

To challenge a decision:
1. Read the original decision document
2. Understand the original reasoning and trade-offs
3. Document what has changed
4. Propose alternative with updated trade-off analysis
5. Create new decision document if approved (mark old as "Superseded")

---

**Note:** Decision documents are in `decisions/` directory. See [decisions/README.md](decisions/README.md) for more information.
