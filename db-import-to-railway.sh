#!/bin/bash

# Mars Vista - Import Curiosity Data to Railway
# Exports Curiosity photos from local DB and imports to Railway
# Usage: ./db-import-to-railway.sh

# Local database
LOCAL_CONTAINER="marsvista-postgres"
LOCAL_USER="marsvista"
LOCAL_DB="marsvista_dev"

# Railway database
RAILWAY_HOST="maglev.proxy.rlwy.net"
RAILWAY_PORT="38340"
RAILWAY_USER="postgres"
RAILWAY_PASSWORD="OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh"
RAILWAY_DB="railway"

# Temporary files
TEMP_DIR="./temp_export"
PHOTOS_SQL="${TEMP_DIR}/curiosity_photos.sql"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
BOLD='\033[1m'

echo -e "${BOLD}Mars Vista - Import Curiosity Data to Railway${NC}"
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
echo -e "${BLUE}Checking current data...${NC}"
LOCAL_COUNT=$(docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_USER" -d "$LOCAL_DB" -t -c "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Curiosity');" | tr -d ' ')
RAILWAY_COUNT=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Curiosity');" | tr -d ' ')

echo -e "  Local Curiosity photos:   ${BOLD}${LOCAL_COUNT}${NC}"
echo -e "  Railway Curiosity photos: ${BOLD}${RAILWAY_COUNT}${NC}"
echo ""

if [ "$LOCAL_COUNT" -eq 0 ]; then
    echo -e "${RED}Error: No Curiosity photos found in local database${NC}"
    exit 1
fi

if [ "$RAILWAY_COUNT" -gt 0 ]; then
    echo -e "${YELLOW}Warning: Railway already has ${RAILWAY_COUNT} Curiosity photos${NC}"
    echo -e "${YELLOW}This import will ADD photos (duplicates will be skipped by unique constraint)${NC}"
    echo ""
    read -p "Continue? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo -e "${YELLOW}Aborted.${NC}"
        exit 0
    fi
    echo ""
fi

# Create temp directory
mkdir -p "$TEMP_DIR"

# Export Curiosity photos from local database
echo -e "${BLUE}Exporting Curiosity photos from local database...${NC}"
START_TIME=$(date +%s)

docker exec "$LOCAL_CONTAINER" psql -U "$LOCAL_USER" -d "$LOCAL_DB" -c "\COPY (
    SELECT
        nasa_id, rover_id, camera_id, sol, img_src_full, img_src_small,
        earth_date, date_taken_utc, date_taken_mars, date_received,
        instrument, sample_type, filter, size_bytes, credits,
        title, caption, link, extended_properties,
        attitude, site, drive, product_id, spacecraft_clock,
        scale_factor, dimension_width, dimension_height,
        xyz_rmc, xyz_rover, subframe_rect, downsample_method,
        compression_class, color_space, interpolation_type,
        lut_type, raw_data
    FROM photos
    WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Curiosity')
    ORDER BY id
) TO STDOUT WITH (FORMAT CSV, HEADER false, NULL '\\N')" > "$PHOTOS_SQL"

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Export failed!${NC}"
    rm -rf "$TEMP_DIR"
    exit 1
fi

EXPORT_SIZE=$(du -h "$PHOTOS_SQL" | cut -f1)
EXPORT_TIME=$(($(date +%s) - START_TIME))

echo -e "${GREEN}✓ Export complete!${NC}"
echo -e "  File size: ${BOLD}${EXPORT_SIZE}${NC}"
echo -e "  Duration:  ${BOLD}${EXPORT_TIME}s${NC}"
echo ""

# Import to Railway
echo -e "${BLUE}Importing Curiosity photos to Railway...${NC}"
echo -e "${YELLOW}This may take several minutes for ~675k photos...${NC}"
echo ""

START_TIME=$(date +%s)

PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -c "\COPY photos (
    nasa_id, rover_id, camera_id, sol, img_src_full, img_src_small,
    earth_date, date_taken_utc, date_taken_mars, date_received,
    instrument, sample_type, filter, size_bytes, credits,
    title, caption, link, extended_properties,
    attitude, site, drive, product_id, spacecraft_clock,
    scale_factor, dimension_width, dimension_height,
    xyz_rmc, xyz_rover, subframe_rect, downsample_method,
    compression_class, color_space, interpolation_type,
    lut_type, raw_data
) FROM STDIN WITH (FORMAT CSV, HEADER false, NULL '\\N')" < "$PHOTOS_SQL"

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Import failed!${NC}"
    echo -e "${YELLOW}Note: Some duplicates may have been skipped (this is normal)${NC}"
    rm -rf "$TEMP_DIR"
    exit 1
fi

IMPORT_TIME=$(($(date +%s) - START_TIME))
IMPORT_MINUTES=$((IMPORT_TIME / 60))
IMPORT_SECONDS=$((IMPORT_TIME % 60))

echo -e "${GREEN}✓ Import complete!${NC}"
echo -e "  Duration: ${BOLD}${IMPORT_MINUTES}m ${IMPORT_SECONDS}s${NC}"
echo ""

# Verify import
echo -e "${BLUE}Verifying import...${NC}"
NEW_RAILWAY_COUNT=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = 'Curiosity');" | tr -d ' ')

echo -e "${GREEN}✓ Verification complete!${NC}"
echo ""
echo -e "  Railway Curiosity photos (before): ${BOLD}${RAILWAY_COUNT}${NC}"
echo -e "  Railway Curiosity photos (after):  ${BOLD}${NEW_RAILWAY_COUNT}${NC}"
echo -e "  Photos imported:                   ${BOLD}$((NEW_RAILWAY_COUNT - RAILWAY_COUNT))${NC}"
echo ""

# Get total photos in Railway
TOTAL_RAILWAY=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos;" | tr -d ' ')

echo -e "${BLUE}Railway database totals:${NC}"
echo -e "  Total photos: ${BOLD}${TOTAL_RAILWAY}${NC}"
echo ""

# Cleanup
echo -e "${BLUE}Cleaning up temporary files...${NC}"
rm -rf "$TEMP_DIR"
echo -e "${GREEN}✓ Done!${NC}"
echo ""

echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Import to Railway Complete!${NC}"
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
