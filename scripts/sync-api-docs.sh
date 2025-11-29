#!/bin/bash
# Sync OpenAPI specification from Swagger to LLM documentation
#
# This ensures openapi.json stays in sync with the actual API.
# Run this after making API changes, before committing.
#
# Usage:
#   ./scripts/sync-api-docs.sh              # Sync from local API (localhost:5127)
#   ./scripts/sync-api-docs.sh production   # Sync from production API
#   ./scripts/sync-api-docs.sh --check      # Check if in sync (for CI)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$PROJECT_ROOT/web/app/public/docs/llm"
OUTPUT_FILE="$OUTPUT_DIR/openapi.json"

# Determine API URL
if [[ "$1" == "production" ]]; then
    API_URL="https://api.marsvista.dev"
    SWAGGER_URL="$API_URL/swagger/v2/swagger.json"
elif [[ -n "$API_URL" ]]; then
    SWAGGER_URL="$API_URL/swagger/v2/swagger.json"
else
    API_URL="http://localhost:5127"
    SWAGGER_URL="$API_URL/swagger/v2/swagger.json"
fi

echo "Fetching OpenAPI spec from: $SWAGGER_URL"

# Check if API is reachable
if ! curl -s --connect-timeout 5 "$SWAGGER_URL" > /dev/null 2>&1; then
    echo "Error: Cannot reach API at $API_URL"
    echo "Make sure the API is running: dotnet run --project src/MarsVista.Api"
    exit 1
fi

# Fetch and format the swagger output
SWAGGER_JSON=$(curl -s "$SWAGGER_URL")

if [[ -z "$SWAGGER_JSON" || "$SWAGGER_JSON" == "null" ]]; then
    echo "Error: Empty response from swagger endpoint"
    exit 1
fi

# Check mode
if [[ "$1" == "--check" ]]; then
    # CI mode: compare with existing file
    CURRENT=$(cat "$OUTPUT_FILE" 2>/dev/null || echo "{}")
    NEW=$(echo "$SWAGGER_JSON" | jq -S '.')
    CURRENT_SORTED=$(echo "$CURRENT" | jq -S '.')

    if [[ "$NEW" == "$CURRENT_SORTED" ]]; then
        echo "OpenAPI spec is in sync"
        exit 0
    else
        echo "Error: openapi.json is out of sync with swagger!"
        echo "Run './scripts/sync-api-docs.sh' to update"
        exit 1
    fi
fi

# Customize the output for our LLM docs
# - Update server URL to production
# - Keep the formatting clean
FINAL_JSON=$(echo "$SWAGGER_JSON" | jq '
    .servers = [{"url": "https://api.marsvista.dev", "description": "Production API"}] |
    .info.title = "Mars Vista API" |
    .info.description = "Unified API for NASA Mars rover photos with advanced filtering, location queries, and scientific metadata. Access 680,000+ photos from Curiosity, Perseverance, Opportunity, and Spirit rovers."
')

# Write to file
echo "$FINAL_JSON" | jq '.' > "$OUTPUT_FILE"

echo "Synced openapi.json"
echo "  Source: $SWAGGER_URL"
echo "  Output: $OUTPUT_FILE"
echo "  Paths:  $(echo "$FINAL_JSON" | jq '.paths | keys | length') endpoints"

# Optionally generate TypeScript types if the tool is available
if command -v npx &> /dev/null && [[ -f "$PROJECT_ROOT/web/app/package.json" ]]; then
    if grep -q "openapi-typescript" "$PROJECT_ROOT/web/app/package.json" 2>/dev/null; then
        echo ""
        echo "Generating TypeScript types..."
        cd "$PROJECT_ROOT/web/app"
        npm run generate:types 2>/dev/null || echo "Note: Run 'npm run generate:types' in web/app to update types.ts"
    fi
fi

echo ""
echo "Done! Remember to commit the updated openapi.json"
