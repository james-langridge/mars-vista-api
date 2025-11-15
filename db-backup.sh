#!/bin/bash

# Mars Vista - Database Backup Script
# Creates a compressed backup of the PostgreSQL database
# Usage: ./db-backup.sh [backup_name]

BACKUP_DIR="./backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="${1:-marsvista_${TIMESTAMP}}"
CONTAINER_NAME="marsvista-postgres"
DB_USER="marsvista"
DB_NAME="marsvista_dev"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
BOLD='\033[1m'

echo -e "${BOLD}Mars Vista - Database Backup${NC}"
echo ""

# Check if Docker container is running
if ! docker ps | grep -q "$CONTAINER_NAME"; then
    echo -e "${RED}Error: PostgreSQL container '$CONTAINER_NAME' is not running${NC}"
    echo -e "${YELLOW}Start it with: docker compose up -d${NC}"
    exit 1
fi

# Create backup directory if it doesn't exist
if [ ! -d "$BACKUP_DIR" ]; then
    echo -e "${BLUE}Creating backup directory: ${BACKUP_DIR}${NC}"
    mkdir -p "$BACKUP_DIR"
fi

# Full backup path
BACKUP_PATH="${BACKUP_DIR}/${BACKUP_NAME}.dump"

echo -e "${BLUE}Starting backup...${NC}"
echo -e "  Database:   ${BOLD}${DB_NAME}${NC}"
echo -e "  Container:  ${BOLD}${CONTAINER_NAME}${NC}"
echo -e "  Output:     ${BOLD}${BACKUP_PATH}${NC}"
echo ""

# Create backup using custom format (recommended for large databases)
START_TIME=$(date +%s)

docker exec "$CONTAINER_NAME" pg_dump -U "$DB_USER" -Fc "$DB_NAME" > "$BACKUP_PATH"

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Backup failed!${NC}"
    exit 1
fi

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

# Get backup size
BACKUP_SIZE=$(du -h "$BACKUP_PATH" | cut -f1)

echo -e "${GREEN}✓ Backup complete!${NC}"
echo ""
echo -e "  File:       ${BOLD}${BACKUP_PATH}${NC}"
echo -e "  Size:       ${BOLD}${BACKUP_SIZE}${NC}"
echo -e "  Duration:   ${BOLD}${DURATION}s${NC}"
echo ""

# Count total backups
BACKUP_COUNT=$(ls -1 "$BACKUP_DIR"/*.dump 2>/dev/null | wc -l)
echo -e "${BLUE}Total backups in ${BACKUP_DIR}: ${BOLD}${BACKUP_COUNT}${NC}"

# Show restore command
echo ""
echo -e "${YELLOW}To restore this backup:${NC}"
echo -e "  cat ${BACKUP_PATH} | docker exec -i ${CONTAINER_NAME} pg_restore -U ${DB_USER} -d ${DB_NAME} -c"
echo ""
