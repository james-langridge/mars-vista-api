#!/bin/bash
# Scrape all Spirit volumes sequentially

API_URL="http://localhost:5127"

echo "=========================================="
echo "Spirit Complete Scraper"
echo "=========================================="
echo ""
echo "This will scrape all 5 Spirit camera volumes:"
echo "  - mer2po_0xxx (PANCAM)"
echo "  - mer2no_0xxx (NAVCAM)"
echo "  - mer2ho_0xxx (HAZCAM)"
echo "  - mer2mo_0xxx (MI)"
echo "  - mer2do_0xxx (DESCENT)"
echo ""

curl -X POST "$API_URL/api/scraper/spirit/all"

echo ""
echo "Scrape complete! Check database for results."
