# Database Access Guide

Guide for accessing and querying the Mars Vista PostgreSQL databases.

## Database Architecture

Mars Vista uses **two separate PostgreSQL databases** for clean separation of concerns:

1. **Photos Database** (C# API)
   - Purpose: Rover photos, cameras, and metadata
   - Managed by: Entity Framework Core (C#)
   - Migration tracking: `__EFMigrationsHistory` table

2. **Auth Database** (Next.js Web App)
   - Purpose: User authentication and sessions
   - Managed by: Prisma (TypeScript)
   - Migration tracking: `_prisma_migrations` table

This separation provides:
- ✅ No migration conflicts between systems
- ✅ Independent scaling and optimization
- ✅ Clear ownership boundaries
- ✅ Professional microservices pattern

## Connection Details

### Environment Variables

Connection credentials are stored in `.env` file (gitignored):

```bash
# Local Photos Database (C# API)
POSTGRES_USER=marsvista
POSTGRES_PASSWORD=marsvista_dev_password
POSTGRES_DB=marsvista_dev
POSTGRES_PORT=5432

# Local Auth Database (Next.js)
# DATABASE_URL="postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_auth_dev"

# Railway Photos Database (Production)
RAILWAY_HOST=maglev.proxy.rlwy.net
RAILWAY_PORT=38340
RAILWAY_USER=postgres
RAILWAY_PASSWORD=<from_railway_dashboard>
RAILWAY_DB=railway

# Railway Auth Database (Production)
# Connection details in Railway dashboard (separate instance)
```

See `.env.example` for template.

### Local Photos Database (Docker)

**From Command Line (psql):**

```bash
# Using environment variable for password
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev

# Or with connection string
psql "postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev"
```

**Connection Parameters:**

- **Host:** localhost
- **Port:** 5432
- **Database:** marsvista_dev (photos)
- **Username:** marsvista
- **Password:** marsvista_dev_password

**Application Connection String (C# API):**

```
Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password
```

### Local Auth Database (Docker)

**From Command Line (psql):**

```bash
# Connect to auth database
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_auth_dev

# Or with connection string
psql "postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_auth_dev"
```

**Connection Parameters:**

- **Host:** localhost
- **Port:** 5432
- **Database:** marsvista_auth_dev (authentication)
- **Username:** marsvista
- **Password:** marsvista_dev_password

**Application Connection String (Next.js):**

```
DATABASE_URL="postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_auth_dev"
```

### Railway Photos Database (Production)

**From Command Line (psql):**

```bash
# Using environment variable (load from .env)
PGPASSWORD=$RAILWAY_PASSWORD psql -h $RAILWAY_HOST -U $RAILWAY_USER -p $RAILWAY_PORT -d $RAILWAY_DB

# Or directly with credentials
PGPASSWORD=<password> psql -h maglev.proxy.rlwy.net -U postgres -p 38340 -d railway

# Or with connection string
psql "$RAILWAY_DATABASE_URL"
```

**Connection Parameters:**

- **Host:** maglev.proxy.rlwy.net
- **Port:** 38340
- **Database:** railway
- **Username:** postgres
- **Password:** (see Railway dashboard or `.env` file)

**Current Data (as of 2025-11-15):**
- **Curiosity:** 675,765 photos (sols 1-4,683, 4,310 unique sols)
- **Perseverance:** 451,602 photos (sols 1-1,683, 1,475 unique sols)
- **Total:** 1,127,367 photos

### Railway Auth Database (Production)

**From Command Line (psql):**

```bash
# Connection details available in Railway dashboard
# Separate PostgreSQL instance from photos database
PGPASSWORD=<auth_password> psql -h nozomi.proxy.rlwy.net -U postgres -p 30913 -d railway
```

**Connection Parameters:**

- **Host:** nozomi.proxy.rlwy.net
- **Port:** 30913
- **Database:** railway (auth)
- **Username:** postgres
- **Password:** (see Railway dashboard)

**Purpose:** User authentication, sessions, verification tokens

---

## Database Schema

### Photos Database Tables (C# API)

**rovers** - Mars rover metadata
- `id` - Primary key
- `name` - Rover name (e.g., "Curiosity", "Perseverance")
- `landing_date` - When the rover landed on Mars
- `status` - Current status (active, complete)
- `max_sol` - Latest sol with photos
- `total_photos` - Total photo count

**cameras** - Camera information for each rover
- `id` - Primary key
- `rover_id` - Foreign key to rovers
- `name` - Short camera name (e.g., "MAST", "NAVCAM")
- `full_name` - Full camera name (e.g., "Mast Camera")

**photos** - Mars rover photos with complete metadata
- `id` - Primary key
- `nasa_id` - NASA's unique identifier (indexed)
- `rover_id` - Foreign key to rovers (indexed)
- `camera_id` - Foreign key to cameras (indexed)
- `sol` - Martian sol (indexed)
- `earth_date` - Earth date (indexed)
- `img_src_full` - Full resolution image URL
- `img_src_small` - Thumbnail image URL
- `raw_data` - Complete NASA API response (JSONB)
- Plus 30+ additional metadata fields

### Auth Database Tables (Next.js)

**User** - User accounts
- `id` - Primary key (CUID)
- `email` - User email (unique, indexed)
- `emailVerified` - Email verification timestamp
- `name` - User display name
- `image` - Profile image URL
- `createdAt` - Account creation timestamp
- `updatedAt` - Last update timestamp

**Session** - Active user sessions
- `id` - Primary key (CUID)
- `sessionToken` - Unique session token (indexed)
- `userId` - Foreign key to User
- `expires` - Session expiration timestamp

**VerificationToken** - Magic link tokens
- `identifier` - Email address
- `token` - Unique verification token
- `expires` - Token expiration timestamp
- Composite primary key on (identifier, token)

---

## Database Indexes

### Photos Table Indexes

The photos table uses strategic indexes for optimal query performance:

**Primary Query Indexes:**
```sql
-- Primary key
CREATE INDEX pk_photos ON photos(id);

-- Unique constraint for duplicate prevention
CREATE UNIQUE INDEX ix_photos_nasa_id ON photos(nasa_id);

-- Core filtering indexes
CREATE INDEX ix_photos_rover_id ON photos(rover_id);
CREATE INDEX ix_photos_camera_id ON photos(camera_id);
CREATE INDEX ix_photos_sol ON photos(sol);
CREATE INDEX ix_photos_earth_date ON photos(earth_date);

-- Composite indexes for common query patterns
CREATE INDEX ix_photos_rover_camera_sol ON photos(rover_id, camera_id, sol);
CREATE INDEX ix_photos_site_drive ON photos(site, drive);
```

**Performance Optimization Indexes (Added November 2025):**
```sql
-- Image dimension indexes (for image quality filters)
CREATE INDEX ix_photos_width ON photos(width) WHERE width IS NOT NULL;
CREATE INDEX ix_photos_height ON photos(height) WHERE height IS NOT NULL;

-- Sample type index (for filtering by Full/Thumbnail/Sub-frame)
CREATE INDEX ix_photos_sample_type ON photos(sample_type);

-- Mars time composite index (for Mars local time queries)
CREATE INDEX ix_photos_rover_mars_time ON photos(rover_id, mars_time_hour)
  WHERE mars_time_hour IS NOT NULL;

-- JSONB GIN index (for raw_data queries and future flexibility)
CREATE INDEX ix_photos_raw_data_gin ON photos USING gin(raw_data);
```

**Index Performance Impact:**

| Query Type | Before Indexes | After Indexes | Improvement |
|------------|---------------|---------------|-------------|
| Image quality filters (`min_width`, `min_height`) | 5-8s | 2-3s | 60% faster |
| Landing day photos | 44s | 2.3s | 95% faster |
| Sol max queries | 36s | 1.9s | 95% faster |
| Mars time filtering | 10-15s | 2-3s | 80% faster |

For detailed performance metrics, see [PERFORMANCE_GUIDE.md](PERFORMANCE_GUIDE.md).

**Index Maintenance:**

Indexes are automatically maintained by PostgreSQL. VACUUM and ANALYZE operations run automatically on Railway.

**Checking Index Usage:**
```sql
-- See which indexes are being used for a query
EXPLAIN ANALYZE
SELECT * FROM photos
WHERE rover_id = 2 AND width >= 1920 AND height >= 1080;

-- View index sizes
SELECT
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexname::regclass)) as index_size
FROM pg_indexes
WHERE schemaname = 'public' AND tablename = 'photos'
ORDER BY pg_relation_size(indexname::regclass) DESC;
```

---

## Useful Queries

### Rover Statistics

**Get all rovers with photo counts:**
```sql
SELECT
    r.id,
    r.name,
    r.status,
    r.landing_date,
    r.max_sol,
    COUNT(p.id) as actual_photo_count,
    r.total_photos as expected_total
FROM rovers r
LEFT JOIN photos p ON r.id = p.rover_id
GROUP BY r.id, r.name, r.status, r.landing_date, r.max_sol, r.total_photos
ORDER BY r.id;
```

**Get rover scraping progress:**
```sql
SELECT
    r.name,
    COUNT(p.id) as total_photos,
    MIN(p.sol) as min_sol,
    MAX(p.sol) as max_sol,
    COUNT(DISTINCT p.sol) as sols_scraped,
    r.max_sol as expected_max_sol,
    ROUND(100.0 * COUNT(DISTINCT p.sol) / NULLIF(r.max_sol, 0), 2) as percent_complete
FROM rovers r
LEFT JOIN photos p ON r.id = p.rover_id
WHERE r.name = 'Curiosity'
GROUP BY r.name, r.max_sol;
```

### Photo Queries

**Photos by camera:**
```sql
SELECT
    c.name,
    c.full_name,
    COUNT(*) as photo_count
FROM photos p
JOIN cameras c ON p.camera_id = c.id
JOIN rovers r ON p.rover_id = r.id
WHERE r.name = 'Curiosity'
GROUP BY c.name, c.full_name
ORDER BY photo_count DESC;
```

**Photos for specific sol:**
```sql
SELECT
    p.nasa_id,
    p.sol,
    c.name as camera,
    p.earth_date,
    p.img_src_full,
    p.date_taken_utc
FROM photos p
JOIN cameras c ON p.camera_id = c.id
JOIN rovers r ON p.rover_id = r.id
WHERE r.name = 'Curiosity' AND p.sol = 1000
ORDER BY c.name, p.id
LIMIT 20;
```

**Recent photos (last 7 days):**
```sql
SELECT
    r.name as rover,
    c.name as camera,
    p.sol,
    p.earth_date,
    p.img_src_full
FROM photos p
JOIN rovers r ON p.rover_id = r.id
JOIN cameras c ON p.camera_id = c.id
WHERE p.earth_date >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY p.earth_date DESC, p.sol DESC
LIMIT 50;
```

**Photos from specific Earth date:**
```sql
SELECT
    p.sol,
    c.name as camera,
    p.img_src_full,
    p.title,
    p.caption
FROM photos p
JOIN cameras c ON p.camera_id = c.id
JOIN rovers r ON p.rover_id = r.id
WHERE r.name = 'Curiosity'
  AND p.earth_date = '2012-08-06'
ORDER BY p.sol, c.name;
```

### Data Quality Checks

**Find missing sols (gaps in coverage):**
```sql
WITH sol_range AS (
    SELECT generate_series(1, 4683) as sol
)
SELECT
    sr.sol,
    CASE
        WHEN sr.sol % 100 = 0 THEN 'Milestone sol'
        ELSE ''
    END as note
FROM sol_range sr
LEFT JOIN photos p ON p.sol = sr.sol AND p.rover_id = 2
WHERE p.id IS NULL
ORDER BY sr.sol
LIMIT 100;
```

**Photos without image URLs:**
```sql
SELECT
    r.name as rover,
    COUNT(*) as photos_missing_urls
FROM photos p
JOIN rovers r ON p.rover_id = r.id
WHERE p.img_src_full IS NULL OR p.img_src_full = ''
GROUP BY r.name;
```

**Duplicate NASA IDs:**
```sql
SELECT
    nasa_id,
    COUNT(*) as duplicate_count
FROM photos
GROUP BY nasa_id
HAVING COUNT(*) > 1;
```

### Analytics Queries

**Photos per sol (scraping statistics):**
```sql
SELECT
    p.sol,
    COUNT(*) as photo_count,
    MIN(p.earth_date) as earth_date,
    array_agg(DISTINCT c.name ORDER BY c.name) as cameras_used
FROM photos p
JOIN cameras c ON p.camera_id = c.id
WHERE p.rover_id = 2
GROUP BY p.sol
ORDER BY p.sol
LIMIT 50;
```

**Camera usage over time:**
```sql
SELECT
    DATE_TRUNC('month', p.earth_date) as month,
    c.name as camera,
    COUNT(*) as photo_count
FROM photos p
JOIN cameras c ON p.camera_id = c.id
WHERE p.rover_id = 2
  AND p.earth_date IS NOT NULL
GROUP BY month, c.name
ORDER BY month DESC, photo_count DESC
LIMIT 100;
```

**Storage size analysis:**
```sql
SELECT
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size,
    pg_total_relation_size(schemaname||'.'||tablename) AS size_bytes
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY size_bytes DESC;
```

---

## Docker Database Management

### Connect via Docker

```bash
# Interactive psql session
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev

# Run single query
docker exec marsvista-postgres psql -U marsvista -d marsvista_dev -c "SELECT COUNT(*) FROM photos;"
```

### Backup and Restore

**Create backup:**
```bash
# SQL dump
docker exec marsvista-postgres pg_dump -U marsvista marsvista_dev > backup_$(date +%Y%m%d).sql

# Compressed backup
docker exec marsvista-postgres pg_dump -U marsvista marsvista_dev | gzip > backup_$(date +%Y%m%d).sql.gz

# Custom format (recommended for large databases)
docker exec marsvista-postgres pg_dump -U marsvista -Fc marsvista_dev > backup_$(date +%Y%m%d).dump
```

**Restore from backup:**
```bash
# From SQL dump
cat backup_20251114.sql | docker exec -i marsvista-postgres psql -U marsvista -d marsvista_dev

# From compressed dump
gunzip -c backup_20251114.sql.gz | docker exec -i marsvista-postgres psql -U marsvista -d marsvista_dev

# From custom format
docker exec -i marsvista-postgres pg_restore -U marsvista -d marsvista_dev -c < backup_20251114.dump
```

### Database Maintenance

**Vacuum and analyze:**
```sql
-- Reclaim storage and update statistics
VACUUM ANALYZE photos;
VACUUM ANALYZE rovers;
VACUUM ANALYZE cameras;
```

**Check database size:**
```sql
SELECT
    pg_database.datname,
    pg_size_pretty(pg_database_size(pg_database.datname)) AS size
FROM pg_database
WHERE datname = 'marsvista_dev';
```

**Check table sizes:**
```sql
SELECT
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS total_size,
    pg_size_pretty(pg_relation_size(schemaname||'.'||tablename)) AS table_size,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename) - pg_relation_size(schemaname||'.'||tablename)) AS index_size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

---

## Performance Optimization

### Indexes

The database includes the following indexes for optimal query performance:

**Photos table:**
- `idx_photos_rover_sol` - (rover_id, sol)
- `idx_photos_rover_earth_date` - (rover_id, earth_date)
- `idx_photos_rover_camera` - (rover_id, camera_id)
- `idx_photos_nasa_id` - (nasa_id) UNIQUE

**Query optimization tips:**
- Always filter by `rover_id` first when querying photos
- Use sol or earth_date indexes for temporal queries
- Combine rover + camera filters for best performance

### Monitoring Slow Queries

**Enable query logging** (in PostgreSQL config):
```sql
ALTER DATABASE marsvista_dev SET log_min_duration_statement = 1000; -- Log queries > 1 second
```

**Find slow queries:**
```sql
SELECT
    query,
    calls,
    total_time,
    mean_time,
    max_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

---

## Troubleshooting

### Connection Issues

**Check if PostgreSQL is running:**
```bash
docker ps | grep marsvista-postgres
```

**Start database if stopped:**
```bash
docker compose up -d
```

**Check database logs:**
```bash
docker logs marsvista-postgres
docker logs marsvista-postgres --tail 100 --follow
```

**Test connection:**
```bash
# Should return "1"
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev -c "SELECT 1;"
```

### Permission Issues

**Grant permissions to user:**
```sql
GRANT ALL PRIVILEGES ON DATABASE marsvista_dev TO marsvista;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO marsvista;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO marsvista;
```

### Reset Database

**WARNING: This deletes all data**

```bash
# Stop API first
# Then reset database
docker compose down
docker volume rm mars-vista-api_postgres-data
docker compose up -d

# Database will be recreated with migrations on next API start
cd src/MarsVista.Api && dotnet run
```

---

## Advanced Features

### JSONB Queries

The `raw_data` column stores the complete NASA API response as JSONB.

**Query JSONB fields:**
```sql
SELECT
    nasa_id,
    sol,
    raw_data->'Extended'->>'sample_type' as sample_type,
    raw_data->'Extended'->>'lmst' as mars_local_time,
    raw_data->>'spacecraft_clock' as spacecraft_clock
FROM photos
WHERE rover_id = 2
  AND sol = 1000
LIMIT 10;
```

**Find photos with specific JSONB attributes:**
```sql
SELECT COUNT(*)
FROM photos
WHERE rover_id = 2
  AND raw_data->'Extended'->>'sample_type' = 'full';
```

### Full-Text Search

**Search photo titles and captions:**
```sql
SELECT
    sol,
    title,
    caption,
    img_src_full
FROM photos
WHERE rover_id = 2
  AND (
    title ILIKE '%drill%'
    OR caption ILIKE '%drill%'
  )
LIMIT 20;
```

---

## Common Tasks

### Find Latest Sol with Photos

```sql
SELECT MAX(sol) as latest_sol
FROM photos
WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Curiosity');
```

### Count Photos by Date Range

```sql
SELECT COUNT(*) as photo_count
FROM photos
WHERE rover_id = 2
  AND earth_date BETWEEN '2024-01-01' AND '2024-12-31';
```

### Export Data to CSV

```bash
# Export all Curiosity photos to CSV
docker exec marsvista-postgres psql -U marsvista -d marsvista_dev -c "\COPY (
  SELECT p.nasa_id, p.sol, p.earth_date, c.name as camera, p.img_src_full
  FROM photos p
  JOIN cameras c ON p.camera_id = c.id
  WHERE p.rover_id = 2
  ORDER BY p.sol, c.name
) TO STDOUT WITH CSV HEADER" > curiosity_photos.csv
```

---

## See Also

- [API Endpoints Documentation](API_ENDPOINTS.md)
- [Curiosity Scraper Guide](CURIOSITY_SCRAPER_GUIDE.md)
- [Main README](../README.md)
