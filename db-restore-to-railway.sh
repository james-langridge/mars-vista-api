#!/bin/bash

# Mars Vista - Restore Local Backup to Railway
# Replaces Railway database with local backup (simple and fast)
# Usage: ./db-restore-to-railway.sh [backup_file]
#
# If no backup file specified, uses the most recent .dump file in ./backups/

# Find the latest backup if not specified
if [ -z "$1" ]; then
    LATEST_BACKUP=$(ls -t ./backups/*.dump 2>/dev/null | head -n 1)
    if [ -z "$LATEST_BACKUP" ]; then
        echo "Error: No backup files found in ./backups/"
        exit 1
    fi
    BACKUP_FILE="$LATEST_BACKUP"
else
    BACKUP_FILE="$1"
fi

# Railway database
RAILWAY_HOST="maglev.proxy.rlwy.net"
RAILWAY_PORT="38340"
RAILWAY_USER="postgres"
RAILWAY_PASSWORD="OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh"
RAILWAY_DB="railway"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
BOLD='\033[1m'

echo -e "${BOLD}Mars Vista - Restore to Railway${NC}"
echo ""

# Check if backup file exists
if [ ! -f "$BACKUP_FILE" ]; then
    echo -e "${RED}Error: Backup file not found: ${BACKUP_FILE}${NC}"
    echo ""
    echo "Available backups:"
    ls -lh ./backups/*.dump 2>/dev/null || echo "  (none)"
    exit 1
fi

BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
BACKUP_DATE=$(stat -c %y "$BACKUP_FILE" 2>/dev/null | cut -d' ' -f1,2 | cut -d'.' -f1 || stat -f "%Sm" -t "%Y-%m-%d %H:%M:%S" "$BACKUP_FILE" 2>/dev/null)

if [ -z "$1" ]; then
    echo -e "${BLUE}Using latest backup:${NC} ${BOLD}${BACKUP_FILE}${NC}"
else
    echo -e "${BLUE}Backup file:${NC} ${BOLD}${BACKUP_FILE}${NC}"
fi
echo -e "${BLUE}Size:${NC} ${BOLD}${BACKUP_SIZE}${NC}"
echo -e "${BLUE}Created:${NC} ${BOLD}${BACKUP_DATE}${NC}"
echo ""

# Test Railway connection
echo -e "${BLUE}Testing Railway connection...${NC}"
if ! PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -c "SELECT 1;" >/dev/null 2>&1; then
    echo -e "${RED}Error: Cannot connect to Railway database${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Connected${NC}"
echo ""

# Show current Railway data
CURRENT_PHOTOS=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos;" 2>/dev/null | tr -d ' ')

echo -e "${YELLOW}⚠ WARNING: This will REPLACE all data in Railway!${NC}"
echo -e "${YELLOW}Current Railway photos: ${BOLD}${CURRENT_PHOTOS}${NC}"
echo ""

read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Aborted.${NC}"
    exit 0
fi
echo ""

# Restore to Railway
echo -e "${BLUE}Restoring backup to Railway...${NC}"
echo -e "${YELLOW}This may take 2-3 minutes...${NC}"
echo ""

START_TIME=$(date +%s)

# Use pg_restore with --clean to drop existing objects first
cat "$BACKUP_FILE" | PGPASSWORD="$RAILWAY_PASSWORD" pg_restore \
    -h "$RAILWAY_HOST" \
    -U "$RAILWAY_USER" \
    -p "$RAILWAY_PORT" \
    -d "$RAILWAY_DB" \
    --clean \
    --if-exists \
    --no-owner \
    --no-privileges \
    2>&1 | grep -v "^pg_restore: warning" | grep -v "does not exist, skipping" || true

RESTORE_RESULT=${PIPESTATUS[1]}

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
MINUTES=$((DURATION / 60))
SECONDS=$((DURATION % 60))

if [ -n "$RESTORE_RESULT" ] && [ "$RESTORE_RESULT" -ne 0 ] && [ "$RESTORE_RESULT" -ne 1 ]; then
    echo -e "${RED}✗ Restore may have encountered errors${NC}"
    echo -e "${YELLOW}Check output above for details${NC}"
fi

echo -e "${GREEN}✓ Restore complete!${NC}"
echo -e "  Duration: ${BOLD}${MINUTES}m ${SECONDS}s${NC}"
echo ""

# Verify restore
echo -e "${BLUE}Verifying restored data...${NC}"
echo ""

printf "%-20s %15s\n" "Rover" "Photos"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for rover in "Curiosity" "Perseverance" "Opportunity" "Spirit"; do
    COUNT=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos WHERE rover_id = (SELECT id FROM rovers WHERE name = '$rover');" 2>/dev/null | tr -d ' ')
    printf "%-20s %15s\n" "$rover" "${COUNT:-0}"
done

TOTAL=$(PGPASSWORD="$RAILWAY_PASSWORD" psql -h "$RAILWAY_HOST" -U "$RAILWAY_USER" -p "$RAILWAY_PORT" -d "$RAILWAY_DB" -t -c "SELECT COUNT(*) FROM photos;" 2>/dev/null | tr -d ' ')

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
printf "%-20s %15s\n" "TOTAL" "$TOTAL"
echo ""

echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Railway Database Restored!${NC}"
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
