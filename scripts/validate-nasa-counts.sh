#!/bin/bash
# Validate photo counts against NASA API
# Usage: ./scripts/validate-nasa-counts.sh [production|local] [rover] [start_sol] [end_sol]
#
# Examples:
#   ./scripts/validate-nasa-counts.sh production perseverance 1700 1720
#   ./scripts/validate-nasa-counts.sh production curiosity 4700 4752

set -e

ENV=${1:-production}
ROVER=${2:-perseverance}
START_SOL=${3:-}
END_SOL=${4:-}

# Database connection
if [ "$ENV" = "production" ]; then
    export PGPASSWORD=OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh
    DB_HOST="maglev.proxy.rlwy.net"
    DB_PORT="38340"
    DB_USER="postgres"
    DB_NAME="railway"
    echo "Using PRODUCTION database"
else
    export PGPASSWORD=marsvista_dev_password
    DB_HOST="localhost"
    DB_PORT="5432"
    DB_USER="marsvista"
    DB_NAME="marsvista_dev"
    echo "Using LOCAL database"
fi

# Get rover info from database
ROVER_INFO=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "
SELECT r.id, MIN(p.sol) as min_sol, MAX(p.sol) as max_sol, COUNT(p.id) as total
FROM rovers r
LEFT JOIN photos p ON p.rover_id = r.id
WHERE LOWER(r.name) = LOWER('$ROVER')
GROUP BY r.id;
")

if [ -z "$ROVER_INFO" ]; then
    echo "Error: Rover '$ROVER' not found in database"
    exit 1
fi

ROVER_ID=$(echo "$ROVER_INFO" | cut -d'|' -f1)
DB_MIN_SOL=$(echo "$ROVER_INFO" | cut -d'|' -f2)
DB_MAX_SOL=$(echo "$ROVER_INFO" | cut -d'|' -f3)
DB_TOTAL=$(echo "$ROVER_INFO" | cut -d'|' -f4)

echo ""
echo "=== $ROVER Validation ==="
echo "Database: $DB_TOTAL photos, sols $DB_MIN_SOL - $DB_MAX_SOL"

# Set sol range
START_SOL=${START_SOL:-$DB_MIN_SOL}
END_SOL=${END_SOL:-$DB_MAX_SOL}

echo "Checking sols $START_SOL to $END_SOL"
echo ""

# Function to get NASA count for a sol
# Note: Perseverance API returns fallback data if sol has no photos, so we check first image sol
get_nasa_count() {
    local sol=$1

    if [ "$ROVER" = "perseverance" ]; then
        # Perseverance RSS API - check if returned images match requested sol
        # (API returns fallback data for empty sols instead of empty array)
        local response=$(curl -s --max-time 90 "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol=$sol&num=100")
        local first_sol=$(echo "$response" | jq -r '.images[0].sol // -1')

        if [ "$first_sol" != "$sol" ]; then
            # API returned fallback data - this sol has no photos
            echo "0"
        else
            # Count actual images for this sol
            echo "$response" | jq -r '.images | length'
        fi
    elif [ "$ROVER" = "curiosity" ]; then
        # Curiosity raw_image_items API - returns accurate total
        curl -s --max-time 30 "https://mars.nasa.gov/api/v1/raw_image_items/?per_page=1&condition_1=msl:mission&condition_2=$sol:sol:in" | \
            jq -r '.total // 0'
    else
        echo "0"
    fi
}

# Get our counts per sol
echo "Sol,DB_Count,NASA_Count,Diff,Status" > /tmp/validation_results.csv

MISSING_SOLS=()
TOTAL_MISSING=0
SOLS_CHECKED=0

for ((sol=START_SOL; sol<=END_SOL; sol++)); do
    # Get our count
    DB_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -t -A -c "
        SELECT COUNT(*) FROM photos WHERE rover_id = $ROVER_ID AND sol = $sol;
    ")

    # Get NASA count
    NASA_COUNT=$(get_nasa_count $sol)

    if [ -z "$NASA_COUNT" ] || [ "$NASA_COUNT" = "null" ]; then
        NASA_COUNT=0
    fi

    # For Curiosity, NASA count includes thumbnails (~50%)
    if [ "$ROVER" = "curiosity" ] && [ "$NASA_COUNT" -gt 0 ]; then
        NASA_EXPECTED=$((NASA_COUNT / 2))
    else
        NASA_EXPECTED=$NASA_COUNT
    fi

    DIFF=$((NASA_EXPECTED - DB_COUNT))

    if [ "$DIFF" -gt 0 ]; then
        STATUS="MISSING"
        MISSING_SOLS+=("$sol:$DIFF")
        TOTAL_MISSING=$((TOTAL_MISSING + DIFF))
    elif [ "$DIFF" -lt 0 ]; then
        STATUS="EXTRA"
    else
        STATUS="OK"
    fi

    echo "$sol,$DB_COUNT,$NASA_COUNT,$DIFF,$STATUS" >> /tmp/validation_results.csv

    # Progress indicator
    if [ "$STATUS" = "MISSING" ]; then
        echo "Sol $sol: DB=$DB_COUNT, NASA=$NASA_COUNT (expected ~$NASA_EXPECTED), MISSING $DIFF"
    fi

    SOLS_CHECKED=$((SOLS_CHECKED + 1))

    # Rate limiting for NASA API
    sleep 0.5
done

echo ""
echo "=== Summary ==="
echo "Sols checked: $SOLS_CHECKED"
echo "Total missing photos: $TOTAL_MISSING"

if [ ${#MISSING_SOLS[@]} -gt 0 ]; then
    echo ""
    echo "Sols with missing photos:"
    for item in "${MISSING_SOLS[@]}"; do
        echo "  Sol ${item%:*}: missing ~${item#*:} photos"
    done
fi

echo ""
echo "Full results saved to /tmp/validation_results.csv"
