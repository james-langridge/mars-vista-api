#!/bin/bash

# Mars Vista Scraper Resume Tool
# Usage: ./scrape-resume.sh [rover_name] [api_url]

ROVER="${1:-perseverance}"
API_URL="${2:-http://localhost:5127}"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color
BOLD='\033[1m'

echo -e "${BOLD}Mars Vista - Scraper Resume Tool${NC}"
echo -e "Rover: ${BLUE}${ROVER}${NC}"
echo ""

# Check current progress
echo -e "${YELLOW}Checking current progress...${NC}"
PROGRESS=$(curl -s "${API_URL}/api/scraper/${ROVER}/progress")

if [ $? -ne 0 ]; then
    echo -e "${RED}✗ Failed to connect to API at ${API_URL}${NC}"
    exit 1
fi

TOTAL_PHOTOS=$(echo "$PROGRESS" | jq -r '.totalPhotos // 0')
SOLS_SCRAPED=$(echo "$PROGRESS" | jq -r '.solsScraped // 0')
EXPECTED_SOLS=$(echo "$PROGRESS" | jq -r '.expectedTotalSols // 0')
LATEST_SOL=$(echo "$PROGRESS" | jq -r '.latestSol // 0')
PERCENT=$(echo "$PROGRESS" | jq -r '.percentComplete // 0')

echo -e "${GREEN}✓${NC} Current Status:"
echo -e "  Photos scraped:  ${BOLD}$(printf "%'d" $TOTAL_PHOTOS)${NC}"
echo -e "  Sols completed:  ${BOLD}${SOLS_SCRAPED}${NC} / ${EXPECTED_SOLS} (${PERCENT}%)"
echo -e "  Highest sol:     ${BOLD}${LATEST_SOL}${NC}"
echo ""

REMAINING_SOLS=$((EXPECTED_SOLS - LATEST_SOL))

if [ $REMAINING_SOLS -le 0 ]; then
    echo -e "${GREEN}✓ Already caught up! All sols have been scraped.${NC}"
    exit 0
fi

echo -e "${YELLOW}Will resume from sol ${BOLD}$((LATEST_SOL + 1))${NC}${YELLOW} to ${BOLD}${EXPECTED_SOLS}${NC}"
echo -e "${YELLOW}Remaining: ${BOLD}${REMAINING_SOLS}${NC}${YELLOW} sols${NC}"
echo ""

# Ask for confirmation
read -p "Start resume scrape? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Aborted.${NC}"
    exit 0
fi

echo ""
echo -e "${GREEN}Starting resume scrape...${NC}"
echo -e "${BLUE}Tip: Open another terminal and run './scrape-monitor.sh ${ROVER}' to monitor progress${NC}"
echo ""

# Start the resume scrape
curl -X POST "${API_URL}/api/scraper/${ROVER}/bulk/resume?delayMs=1000" \
    -H "accept: application/json" \
    -w "\n\n${GREEN}✓ Scrape completed!${NC}\n" \
    | jq . 2>/dev/null || cat
