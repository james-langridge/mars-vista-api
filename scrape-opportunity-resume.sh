#!/bin/bash
# Resume Opportunity PANCAM scraping from where it left off
# The scraper automatically skips duplicates, so just re-run the same volume

set -e

VOLUME="${1:-mer1po_0xxx}"  # Default to PANCAM if no arg provided
API_URL="http://localhost:5127"

echo "=========================================="
echo "Opportunity Scraper Resume Tool"
echo "=========================================="
echo ""
echo "Volume: $VOLUME"
echo ""

# Check current progress
echo "📊 Current Progress:"
echo "-------------------"
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev -c \
  "SELECT
     COUNT(*) as photos,
     MIN(sol) as min_sol,
     MAX(sol) as max_sol,
     COUNT(DISTINCT sol) as sols_covered
   FROM photos WHERE rover_id = 3;" 2>/dev/null || echo "Could not connect to database"

echo ""
echo "🚀 Resuming scrape for volume: $VOLUME"
echo ""
echo "The scraper will automatically skip already-inserted photos."
echo "Press Ctrl+C to cancel, or wait 5 seconds to continue..."
echo ""

sleep 5

# Start the scrape
echo "Starting scrape..."
curl -X POST "$API_URL/api/scraper/opportunity/volume/$VOLUME" \
  -H "Content-Type: application/json" \
  -w "\n\n✅ Scrape request sent!\n\n" || {
    echo "❌ Failed to start scrape. Is the API running?"
    exit 1
  }

echo "📈 Monitor progress with:"
echo "  ./scrape-monitor.sh opportunity"
echo ""
echo "Or check database directly:"
echo "  PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev -c \\"
echo "    \"SELECT COUNT(*) as photos, MAX(sol) as max_sol FROM photos WHERE rover_id = 3;\""
