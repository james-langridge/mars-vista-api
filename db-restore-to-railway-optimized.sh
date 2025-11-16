#!/bin/bash

# Mars Vista - Optimized Railway Restore
# Restores in stages to minimize temporary disk usage

BACKUP_FILE="${1:-$(ls -t ./backups/*.dump 2>/dev/null | head -n 1)}"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: Backup file not found"
    exit 1
fi

# Railway credentials
RAILWAY_HOST="maglev.proxy.rlwy.net"
RAILWAY_PORT="38340"
RAILWAY_USER="postgres"
RAILWAY_PASSWORD="OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh"
RAILWAY_DB="railway"

export PGPASSWORD="$RAILWAY_PASSWORD"

echo "Mars Vista - Optimized Railway Restore"
echo ""
echo "Backup: $BACKUP_FILE"
echo ""

# Step 1: Schema only
echo "[1/3] Restoring schema (tables, no data)..."
pg_restore \
    -h "$RAILWAY_HOST" \
    -U "$RAILWAY_USER" \
    -p "$RAILWAY_PORT" \
    -d "$RAILWAY_DB" \
    --schema-only \
    --no-owner \
    --no-privileges \
    "$BACKUP_FILE" 2>&1 | grep -v "^pg_restore: warning" | grep -v "does not exist, skipping" || true

echo "✓ Schema restored"
echo ""

# Step 2: Data only (without indexes for now)
echo "[2/3] Restoring data (this will take 3-5 minutes)..."
pg_restore \
    -h "$RAILWAY_HOST" \
    -U "$RAILWAY_USER" \
    -p "$RAILWAY_PORT" \
    -d "$RAILWAY_DB" \
    --data-only \
    --disable-triggers \
    --no-owner \
    --no-privileges \
    "$BACKUP_FILE" 2>&1 | grep -v "^pg_restore: warning" || true

echo "✓ Data restored"
echo ""

# Step 3: Create indexes (this is where we might run out of space)
echo "[3/3] Building indexes (this may take 2-3 minutes)..."
psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" <<'SQL'
-- Create indexes one at a time to monitor progress
CREATE UNIQUE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_nasa_id ON photos (nasa_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_rover_id_sol ON photos (rover_id, sol);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_camera_id ON photos (camera_id);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_sol ON photos (sol);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_earth_date ON photos (earth_date);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_date_taken_utc ON photos (date_taken_utc);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_rover_id_camera_id_sol ON photos (rover_id, camera_id, sol);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_site_drive ON photos (site, drive);
CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_mast_az_mast_el ON photos (mast_az, mast_el);

-- Verify
SELECT 'Total photos:', COUNT(*) FROM photos;
SQL

echo ""
echo "✓ Restore complete!"
echo ""

# Verify
psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -c "
    SELECT r.name, COUNT(p.id) as photos
    FROM rovers r
    LEFT JOIN photos p ON r.id = p.rover_id
    GROUP BY r.name
    ORDER BY r.name;
"
