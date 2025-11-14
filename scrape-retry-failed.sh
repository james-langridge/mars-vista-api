#!/bin/bash

# Mars Vista - Retry Failed Sols
# Usage: ./scrape-retry-failed.sh <failed_sols_list> [rover_name] [api_url] [delay_ms]
#
# Example:
#   ./scrape-retry-failed.sh "22,59,60,61" perseverance http://localhost:5127 1000
#
# Or paste the JSON response and it will extract failed sols:
#   ./scrape-retry-failed.sh '{"failedSols":[22,59,60]}' perseverance

FAILED_SOLS_INPUT="$1"
ROVER="${2:-perseverance}"
API_URL="${3:-http://localhost:5127}"
DELAY_MS="${4:-1000}"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
BOLD='\033[1m'

echo -e "${BOLD}Mars Vista - Retry Failed Sols${NC}"
echo -e "Rover: ${BLUE}${ROVER}${NC}"
echo ""

if [ -z "$FAILED_SOLS_INPUT" ]; then
    echo -e "${RED}Error: No failed sols provided${NC}"
    echo ""
    echo "Usage:"
    echo "  1. Copy the failedSols array from the bulk scrape response"
    echo "  2. Run: ./scrape-retry-failed.sh \"22,59,60,61\" perseverance"
    echo ""
    echo "Or paste the entire JSON response as the first argument"
    exit 1
fi

# Try to extract failedSols from JSON if full response was pasted
if echo "$FAILED_SOLS_INPUT" | jq -e '.failedSols' >/dev/null 2>&1; then
    echo -e "${BLUE}Extracting failed sols from JSON response...${NC}"
    FAILED_SOLS=$(echo "$FAILED_SOLS_INPUT" | jq -r '.failedSols | join(",")')
else
    FAILED_SOLS="$FAILED_SOLS_INPUT"
fi

# Convert comma-separated list to array
IFS=',' read -ra SOLS_ARRAY <<< "$FAILED_SOLS"
TOTAL_FAILED=${#SOLS_ARRAY[@]}

if [ $TOTAL_FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ No failed sols to retry!${NC}"
    exit 0
fi

echo -e "${YELLOW}Found ${BOLD}${TOTAL_FAILED}${NC}${YELLOW} failed sols to retry${NC}"
echo -e "${BLUE}Delay between requests: ${BOLD}${DELAY_MS}ms${NC}"
echo ""

# Estimate time
SECONDS_PER_SOL=$(echo "scale=0; $DELAY_MS / 1000 + 20" | bc)
ESTIMATED_SECONDS=$((TOTAL_FAILED * SECONDS_PER_SOL))
ESTIMATED_HOURS=$((ESTIMATED_SECONDS / 3600))
ESTIMATED_MINUTES=$(( (ESTIMATED_SECONDS % 3600) / 60 ))

echo -e "${YELLOW}Estimated time: ${BOLD}${ESTIMATED_HOURS}h ${ESTIMATED_MINUTES}m${NC}"
echo ""

read -p "Start retry? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}Aborted.${NC}"
    exit 0
fi

echo ""
echo -e "${GREEN}Starting retry of failed sols...${NC}"
echo -e "${BLUE}Tip: Open another terminal and run './scrape-monitor.sh ${ROVER}' to monitor progress${NC}"
echo ""

# Track results
SUCCESSFUL=0
FAILED=0
STILL_FAILING=()

START_TIME=$(date +%s)

for i in "${!SOLS_ARRAY[@]}"; do
    SOL="${SOLS_ARRAY[$i]}"
    PROGRESS=$((i + 1))

    echo -ne "${BLUE}[${PROGRESS}/${TOTAL_FAILED}]${NC} Retrying sol ${BOLD}${SOL}${NC}... "

    # Make the request
    RESPONSE=$(curl -s -X POST "${API_URL}/api/scraper/${ROVER}/sol/${SOL}" -H "accept: application/json")

    # Check if successful
    PHOTOS_SCRAPED=$(echo "$RESPONSE" | jq -r '.photosScraped // 0')

    if [ $? -eq 0 ] && [ "$PHOTOS_SCRAPED" != "null" ]; then
        if [ "$PHOTOS_SCRAPED" -gt 0 ]; then
            echo -e "${GREEN}✓ ${PHOTOS_SCRAPED} photos${NC}"
            SUCCESSFUL=$((SUCCESSFUL + 1))
        else
            echo -e "${YELLOW}⊘ 0 photos (no data or already scraped)${NC}"
            SUCCESSFUL=$((SUCCESSFUL + 1))
        fi
    else
        echo -e "${RED}✗ Failed${NC}"
        FAILED=$((FAILED + 1))
        STILL_FAILING+=("$SOL")
    fi

    # Add delay between requests (except for last one)
    if [ $i -lt $((TOTAL_FAILED - 1)) ] && [ $DELAY_MS -gt 0 ]; then
        sleep $(echo "scale=3; $DELAY_MS / 1000" | bc)
    fi
done

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
DURATION_MINUTES=$((DURATION / 60))

echo ""
echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}✓ Retry Complete!${NC}"
echo ""
echo -e "  Total attempted:  ${BOLD}${TOTAL_FAILED}${NC}"
echo -e "  ${GREEN}Successful:       ${BOLD}${SUCCESSFUL}${NC}"
echo -e "  ${RED}Still failing:    ${BOLD}${FAILED}${NC}"
echo -e "  Duration:         ${BOLD}${DURATION_MINUTES}m ${DURATION}s${NC}"
echo ""

if [ $FAILED -gt 0 ]; then
    echo -e "${YELLOW}Still failing sols:${NC}"
    echo -e "${RED}${STILL_FAILING[*]}${NC}"
    echo ""
    echo -e "${YELLOW}To retry these again, run:${NC}"
    echo -e "  ./scrape-retry-failed.sh \"$(IFS=,; echo "${STILL_FAILING[*]}")\" ${ROVER}"
else
    echo -e "${GREEN}✓ All sols successfully retried!${NC}"
fi

echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
