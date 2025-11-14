#!/bin/bash

# Mars Vista Scraper Progress Monitor
# Usage: ./scrape-monitor.sh [rover_name] [api_url]

ROVER="${1:-perseverance}"
API_URL="${2:-http://localhost:5127}"
REFRESH_INTERVAL=2

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color
BOLD='\033[1m'

# Clear screen and hide cursor
clear
tput civis

# Trap to show cursor on exit
trap 'tput cnorm; exit' INT TERM

echo -e "${BOLD}Mars Vista - Scraper Progress Monitor${NC}"
echo -e "Rover: ${BLUE}${ROVER}${NC}"
echo -e "Refresh: ${REFRESH_INTERVAL}s"
echo ""

LAST_PHOTO_COUNT=0
LAST_CHECK_TIME=$(date +%s)

while true; do
    # Fetch progress data
    RESPONSE=$(curl -s "${API_URL}/api/scraper/${ROVER}/progress")

    if [ $? -ne 0 ]; then
        echo -e "${RED}✗ Failed to connect to API${NC}"
        sleep $REFRESH_INTERVAL
        continue
    fi

    # Parse JSON (requires jq)
    TOTAL_PHOTOS=$(echo "$RESPONSE" | jq -r '.totalPhotos // 0')
    SOLS_SCRAPED=$(echo "$RESPONSE" | jq -r '.solsScraped // 0')
    EXPECTED_SOLS=$(echo "$RESPONSE" | jq -r '.expectedTotalSols // 0')
    PERCENT=$(echo "$RESPONSE" | jq -r '.percentComplete // 0')
    OLDEST_SOL=$(echo "$RESPONSE" | jq -r '.oldestSol // 0')
    LATEST_SOL=$(echo "$RESPONSE" | jq -r '.latestSol // 0')
    LAST_SCRAPED=$(echo "$RESPONSE" | jq -r '.lastPhotoScraped // "never"')
    STATUS=$(echo "$RESPONSE" | jq -r '.status // "unknown"')
    STATUS_MSG=$(echo "$RESPONSE" | jq -r '.statusMessage // "Unknown status"')
    MINUTES_IDLE=$(echo "$RESPONSE" | jq -r '.minutesSinceLastUpdate // 0')

    # Calculate speed
    CURRENT_TIME=$(date +%s)
    TIME_DIFF=$((CURRENT_TIME - LAST_CHECK_TIME))
    PHOTO_DIFF=$((TOTAL_PHOTOS - LAST_PHOTO_COUNT))

    if [ $TIME_DIFF -gt 0 ]; then
        PHOTOS_PER_SEC=$(echo "scale=2; $PHOTO_DIFF / $TIME_DIFF" | bc)
    else
        PHOTOS_PER_SEC="0.00"
    fi

    # Estimate remaining time
    REMAINING_SOLS=$((EXPECTED_SOLS - SOLS_SCRAPED))
    if [ $(echo "$PHOTOS_PER_SEC > 0" | bc) -eq 1 ]; then
        # Rough estimate: average 500 photos per sol, 1 second delay between sols
        AVG_PHOTOS_PER_SOL=500
        SECONDS_PER_SOL=$(echo "scale=0; $AVG_PHOTOS_PER_SOL / $PHOTOS_PER_SEC + 1" | bc)
        ESTIMATED_SECONDS=$((REMAINING_SOLS * SECONDS_PER_SOL))
        ESTIMATED_HOURS=$((ESTIMATED_SECONDS / 3600))
        ESTIMATED_MINUTES=$(( (ESTIMATED_SECONDS % 3600) / 60 ))
        ETA="${ESTIMATED_HOURS}h ${ESTIMATED_MINUTES}m"
    else
        ETA="calculating..."
    fi

    # Clear previous output (move cursor up)
    tput cup 4 0
    tput ed

    # Display progress
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${GREEN}✓${NC} Total Photos:    ${BOLD}$(printf "%'d" $TOTAL_PHOTOS)${NC}"
    echo -e "${GREEN}✓${NC} Sols Scraped:    ${BOLD}${SOLS_SCRAPED}${NC} / ${EXPECTED_SOLS}"
    echo ""

    # Progress bar
    BAR_WIDTH=40
    FILLED=$(echo "scale=0; $PERCENT * $BAR_WIDTH / 100" | bc)
    EMPTY=$((BAR_WIDTH - FILLED))

    printf "${BLUE}Progress:${NC}        ["
    printf "%${FILLED}s" | tr ' ' '█'
    printf "%${EMPTY}s" | tr ' ' '░'
    printf "] ${BOLD}${PERCENT}%%${NC}\n"

    echo ""

    # Status indicator with color
    case "$STATUS" in
        "active")
            STATUS_COLOR="${GREEN}"
            STATUS_ICON="✓"
            ;;
        "slow")
            STATUS_COLOR="${YELLOW}"
            STATUS_ICON="⚠"
            ;;
        "stalled"|"stopped")
            STATUS_COLOR="${RED}"
            STATUS_ICON="✗"
            ;;
        "complete")
            STATUS_COLOR="${GREEN}"
            STATUS_ICON="✓"
            ;;
        *)
            STATUS_COLOR="${BLUE}"
            STATUS_ICON="○"
            ;;
    esac

    echo -e "${STATUS_COLOR}${STATUS_ICON} Status:${NC}          ${BOLD}${STATUS_MSG}${NC}"
    if [ "$MINUTES_IDLE" != "0" ] && [ "$MINUTES_IDLE" != "null" ]; then
        echo -e "  ${STATUS_COLOR}Idle time:       ${BOLD}$(printf "%.1f" $MINUTES_IDLE)${NC}${STATUS_COLOR} minutes${NC}"
    fi
    echo ""
    echo -e "${YELLOW}⚡${NC} Speed:           ${BOLD}${PHOTOS_PER_SEC}${NC} photos/sec"
    echo -e "${YELLOW}⏱${NC}  ETA:             ${BOLD}${ETA}${NC}"
    echo ""
    echo -e "${BLUE}📊${NC} Sol Range:       ${OLDEST_SOL} → ${LATEST_SOL}"
    echo -e "${BLUE}🕐${NC} Last Update:     $(date -d "$LAST_SCRAPED" '+%Y-%m-%d %H:%M:%S' 2>/dev/null || echo "$LAST_SCRAPED")"
    echo -e "${BOLD}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    echo -e "${BLUE}Press Ctrl+C to exit${NC}"

    # Update for next iteration
    LAST_PHOTO_COUNT=$TOTAL_PHOTOS
    LAST_CHECK_TIME=$CURRENT_TIME

    sleep $REFRESH_INTERVAL
done
