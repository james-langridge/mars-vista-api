# Database Access Guide

Guide for accessing and querying the Mars Vista PostgreSQL database.

## Connection Details

### From Command Line (psql)

```bash
# Using environment variable for password
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev

# Or with connection string
psql "postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev"
```

### Connection Parameters

- **Host:** localhost
- **Port:** 5432
- **Database:** marsvista_dev
- **Username:** marsvista
- **Password:** marsvista_dev_password

### Application Connection String

```
Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password
```

---

## Database Schema

### Core Tables

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
