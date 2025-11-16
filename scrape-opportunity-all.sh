#!/bin/bash
# Start complete Opportunity scrape for all 5 camera volumes
# This will take approximately 90 minutes for ~1 million photos

set -e

API_URL="http://localhost:5127"

echo "=========================================="
echo " Opportunity Complete Scrape"
echo "=========================================="
echo ""
echo "📷 Cameras to scrape:"
echo "  - PANCAM (mer1po_0xxx): ~366,510 photos"
echo "  - NAVCAM (mer1no_0xxx): ~500,000 photos"
echo "  - HAZCAM (mer1ho_0xxx): ~100,000 photos"
echo "  - MI (mer1mo_0xxx): ~50,000 photos"
echo "  - DESCENT (mer1do_0xxx): ~20,000 photos"
echo ""
echo "📊 Total: ~1,000,000 photos across all cameras"
echo "⏱️  Expected duration: ~90 minutes"
echo ""

# Check current progress
echo "Current status:"
echo "---------------"
PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev -c \
  "SELECT
     COUNT(*) as total_photos,
     MIN(sol) as min_sol,
     MAX(sol) as max_sol,
     COUNT(DISTINCT sol) as sols_covered
   FROM photos WHERE rover_id = 3;" 2>/dev/null || echo "Could not connect to database"

echo ""
echo "🚀 Starting scrape for ALL volumes..."
echo ""
echo "This will run in the background. Monitor progress with:"
echo "  ./scrape-monitor.sh opportunity"
echo ""
echo "Or check status with:"
echo "  ./scrape-opportunity-status.sh"
echo ""
echo "Press Ctrl+C to cancel, or wait 5 seconds to continue..."
sleep 5

# Start the complete scrape
echo ""
echo "📡 Sending scrape request..."
curl -X POST "$API_URL/api/scraper/opportunity/all" \
  -H "Content-Type: application/json" &

CURL_PID=$!

echo "Request sent (PID: $CURL_PID)"
echo ""
echo "✅ Scrape started! This will process all 5 camera volumes sequentially."
echo ""
echo "📊 Monitor progress:"
echo "   ./scrape-monitor.sh opportunity"
echo ""
echo "📈 Or check database:"
echo "   watch -n 5 './scrape-opportunity-status.sh'"
echo ""
echo "The scrape will continue in the background even if you close this terminal."
echo "Check logs at: /tmp/marsvista3.log"
