#!/bin/bash

# Mars Vista - Sync Local Database to Railway
# Performs a full upsert sync of all tables from local to Railway
# Usage: ./db-sync-to-railway.sh [--dry-run]

# Local database
LOCAL_CONTAINER="marsvista-postgres"
LOCAL_USER="marsvista"
LOCAL_DB="marsvista_dev"
LOCAL_PASSWORD="marsvista_dev_password"

# Railway database
RAILWAY_HOST="maglev.proxy.rlwy.net"
RAILWAY_PORT="38340"
RAILWAY_USER="postgres"
RAILWAY_PASSWORD="OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh"
RAILWAY_DB="railway"

# Temporary files
TEMP_DIR="./temp_sync"
DUMP_FILE="${TEMP_DIR}/local_dump.sql"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
BOLD='\033[1m'

DRY_RUN=false
if [ "$1" == "--dry-run" ]; then
    DRY_RUN=true
fi

echo -e "${BOLD}Mars Vista - Sync Local Database to Railway${NC}"
if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}(DRY RUN MODE - No changes will be made)${NC}"
fi
echo ""

# Check if local container is running
if ! docker ps | grep -q "$LOCAL_CONTAINER"; then
    echo -e "${RED}Error: Local PostgreSQL container '$LOCAL_CONTAINER' is not running${NC}"
    echo -e "${YELLOW}Start it with: docker compose up -d${NC}"
    exit 1
fi

# Test Railway connection
echo -e "${BLUE}Testing Railway database connection...${NC}"
if ! PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -c "SELECT 1;" >/dev/null 2>&1; then
    echo -e "${RED}Error: Cannot connect to Railway database${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Railway connection successful${NC}"
echo ""

# Get current counts
echo -e "${BLUE}Checking current data counts...${NC}"
echo ""

# Function to get count from local
get_local_count() {
    docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_USER" -d "$LOCAL_DB" -t -c "$1" 2>/dev/null | tr -d ' '
}

# Function to get count from Railway
get_railway_count() {
    PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "$1" 2>/dev/null | tr -d ' '
}

# Check each table
printf "%-20s %15s %15s\n" "Table" "Local" "Railway"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

ROVERS_LOCAL=$(get_local_count "SELECT COUNT(*) FROM rovers;")
ROVERS_RAILWAY=$(get_railway_count "SELECT COUNT(*) FROM rovers;")
printf "%-20s %15s %15s\n" "rovers" "$ROVERS_LOCAL" "$ROVERS_RAILWAY"

CAMERAS_LOCAL=$(get_local_count "SELECT COUNT(*) FROM cameras;")
CAMERAS_RAILWAY=$(get_railway_count "SELECT COUNT(*) FROM cameras;")
printf "%-20s %15s %15s\n" "cameras" "$CAMERAS_LOCAL" "$CAMERAS_RAILWAY"

PHOTOS_LOCAL=$(get_local_count "SELECT COUNT(*) FROM photos;")
PHOTOS_RAILWAY=$(get_railway_count "SELECT COUNT(*) FROM photos;")
printf "%-20s %15s %15s\n" "photos" "$PHOTOS_LOCAL" "$PHOTOS_RAILWAY"

echo ""

# Show photo breakdown by rover
echo -e "${BLUE}Photo counts by rover:${NC}"
echo ""
printf "%-20s %15s %15s\n" "Rover" "Local" "Railway"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for rover in "Curiosity" "Perseverance" "Opportunity" "Spirit"; do
    LOCAL_COUNT=$(get_local_count "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = '$rover');")
    RAILWAY_COUNT=$(get_railway_count "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = '$rover');")
    printf "%-20s %15s %15s\n" "$rover" "${LOCAL_COUNT:-0}" "${RAILWAY_COUNT:-0}"
done

echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "${YELLOW}Dry run complete. Use './db-sync-to-railway.sh' to perform actual sync.${NC}"
    exit 0
fi

# Confirm sync
echo -e "${YELLOW}This will sync ALL data from local to Railway using upserts.${NC}"
echo -e "${YELLOW}Existing Railway data will be updated, new data will be inserted.${NC}"
echo ""
read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Aborted.${NC}"
    exit 0
fi
echo ""

# Create temp directory
mkdir -p "$TEMP_DIR"

# Dump local database with INSERT statements and column names
echo -e "${BLUE}Exporting local database...${NC}"
START_TIME=$(date +%s)

docker exec "$LOCAL_CONTAINER" pg_dump \
    -U "$LOCAL_USER" \
    -d "$LOCAL_DB" \
    --data-only \
    --column-inserts \
    --disable-triggers \
    --no-owner \
    --no-privileges \
    -t rovers \
    -t cameras \
    -t photos \
    > "$DUMP_FILE"

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Export failed!${NC}"
    rm -rf "$TEMP_DIR"
    exit 1
fi

DUMP_SIZE=$(du -h "$DUMP_FILE" | cut -f1)
EXPORT_TIME=$(($(date +%s) - START_TIME))

echo -e "${GREEN}✓ Export complete!${NC}"
echo -e "  File size: ${BOLD}${DUMP_SIZE}${NC}"
echo -e "  Duration:  ${BOLD}${EXPORT_TIME}s${NC}"
echo ""

# Convert INSERTs to UPSERTs
echo -e "${BLUE}Converting to upsert statements...${NC}"

# Create upsert version
UPSERT_FILE="${TEMP_DIR}/upsert_dump.sql"

# Add header
cat > "$UPSERT_FILE" << 'EOF'
-- Mars Vista Database Sync
-- Upsert statements to sync data from local to Railway

SET session_replication_role = replica; -- Disable triggers during import
SET CONSTRAINTS ALL DEFERRED; -- Defer constraint checks

EOF

# Process the dump file to convert INSERTs to UPSERTs
# This handles INSERT INTO table (columns) VALUES (values);
python3 << 'PYTHON_SCRIPT' >> "$UPSERT_FILE"
import re
import sys

dump_file = "./temp_sync/local_dump.sql"

# Define primary keys for each table
pk_constraints = {
    'rovers': 'id',
    'cameras': 'id',
    'photos': 'nasa_id'  # Using nasa_id unique constraint
}

# Define which columns to update on conflict (all except PK)
update_columns = {
    'rovers': ['name', 'landing_date', 'status', 'max_sol', 'total_photos'],
    'cameras': ['rover_id', 'name', 'full_name'],
    'photos': [
        'rover_id', 'camera_id', 'sol', 'img_src_full', 'img_src_small',
        'earth_date', 'date_taken_utc', 'date_taken_mars', 'date_received',
        'instrument', 'sample_type', 'filter', 'size_bytes', 'credits',
        'title', 'caption', 'link', 'extended_properties',
        'attitude', 'site', 'drive', 'product_id', 'spacecraft_clock',
        'scale_factor', 'dimension_width', 'dimension_height',
        'xyz_rmc', 'xyz_rover', 'subframe_rect', 'downsample_method',
        'compression_class', 'color_space', 'interpolation_type',
        'lut_type', 'raw_data'
    ]
}

with open(dump_file, 'r') as f:
    for line in f:
        # Skip comments and empty lines
        if line.strip().startswith('--') or not line.strip():
            continue

        # Check if this is an INSERT statement
        if line.strip().startswith('INSERT INTO'):
            # Extract table name
            match = re.match(r'INSERT INTO (\w+)', line)
            if match:
                table = match.group(1)
                if table in pk_constraints:
                    # Remove the semicolon and add ON CONFLICT clause
                    line = line.rstrip().rstrip(';')

                    # Build the ON CONFLICT DO UPDATE clause
                    pk = pk_constraints[table]
                    updates = update_columns[table]

                    if pk == 'nasa_id':
                        # Use unique constraint name for nasa_id
                        conflict_target = f'ON CONFLICT (nasa_id)'
                    else:
                        conflict_target = f'ON CONFLICT ({pk})'

                    # Build SET clause
                    set_clause = ', '.join([f'{col} = EXCLUDED.{col}' for col in updates])

                    upsert = f'{line} {conflict_target} DO UPDATE SET {set_clause};\n'
                    print(upsert, end='')
                    continue

        # Output line as-is if not an INSERT
        print(line, end='')

PYTHON_SCRIPT

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Conversion failed!${NC}"
    rm -rf "$TEMP_DIR"
    exit 1
fi

# Add footer
cat >> "$UPSERT_FILE" << 'EOF'

SET session_replication_role = DEFAULT; -- Re-enable triggers
EOF

UPSERT_SIZE=$(du -h "$UPSERT_FILE" | cut -f1)
echo -e "${GREEN}✓ Conversion complete!${NC}"
echo -e "  File size: ${BOLD}${UPSERT_SIZE}${NC}"
echo ""

# Import to Railway
echo -e "${BLUE}Importing to Railway...${NC}"
echo -e "${YELLOW}This may take several minutes for large datasets...${NC}"
echo ""

START_TIME=$(date +%s)

PGPASSWORD="$RAILWAY_PASSWORD" psql \
    -h "$RAILWAY_HOST" \
    -U "$RAILWAY_USER" \
    -p "$RAILWAY_PORT" \
    -d "$RAILWAY_DB" \
    -f "$UPSERT_FILE" \
    -v ON_ERROR_STOP=1 \
    2>&1 | grep -v "^INSERT"

IMPORT_RESULT=$?

IMPORT_TIME=$(($(date +%s) - START_TIME))
IMPORT_MINUTES=$((IMPORT_TIME / 60))
IMPORT_SECONDS=$((IMPORT_TIME % 60))

if [ $IMPORT_RESULT -ne 0 ]; then
    echo -e "${RED}✗ Import failed!${NC}"
    echo -e "${YELLOW}Check errors above. Temp files kept in ${TEMP_DIR}${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Import complete!${NC}"
echo -e "  Duration: ${BOLD}${IMPORT_MINUTES}m ${IMPORT_SECONDS}s${NC}"
echo ""

# Verify sync
echo -e "${BLUE}Verifying sync...${NC}"
echo ""

printf "%-20s %15s %15s\n" "Rover" "Local" "Railway (After)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for rover in "Curiosity" "Perseverance" "Opportunity" "Spirit"; do
    LOCAL_COUNT=$(get_local_count "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = '$rover');")
    RAILWAY_COUNT=$(get_railway_count "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = '$rover');")
    printf "%-20s %15s %15s\n" "$rover" "${LOCAL_COUNT:-0}" "${RAILWAY_COUNT:-0}"
done

echo ""

TOTAL_RAILWAY=$(get_railway_count "SELECT COUNT(*) FROM photos;")
echo -e "${BLUE}Railway database total photos: ${BOLD}${TOTAL_RAILWAY}${NC}"
echo ""

# Cleanup
echo -e "${BLUE}Cleaning up temporary files...${NC}"
rm -rf "$TEMP_DIR"
echo -e "${GREEN}✓ Done!${NC}"
echo ""

echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Database Sync Complete!${NC}"
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
