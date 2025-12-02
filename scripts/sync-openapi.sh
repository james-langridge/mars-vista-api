#!/bin/bash
# Sync OpenAPI specification from Swagger to repository root
#
# Usage:
#   ./scripts/sync-openapi.sh              # Sync from local API (localhost:5127)
#   ./scripts/sync-openapi.sh production   # Sync from production API
#   ./scripts/sync-openapi.sh --check      # Check if in sync (for CI)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
OUTPUT_FILE="$PROJECT_ROOT/openapi.json"

# Check for jq
if ! command -v jq &> /dev/null; then
    echo "Error: jq is required but not installed"
    echo "Install with: sudo apt install jq"
    exit 1
fi

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

# Fetch swagger output
SWAGGER_JSON=$(curl -s "$SWAGGER_URL")

if [[ -z "$SWAGGER_JSON" || "$SWAGGER_JSON" == "null" ]]; then
    echo "Error: Empty response from swagger endpoint"
    exit 1
fi

# Check mode
if [[ "$1" == "--check" ]]; then
    CURRENT=$(cat "$OUTPUT_FILE" 2>/dev/null || echo "{}")
    NEW=$(echo "$SWAGGER_JSON" | jq -S '.')
    CURRENT_SORTED=$(echo "$CURRENT" | jq -S '.')

    if [[ "$NEW" == "$CURRENT_SORTED" ]]; then
        echo "OpenAPI spec is in sync"
        exit 0
    else
        echo "Error: openapi.json is out of sync with swagger!"
        echo "Run './scripts/sync-openapi.sh' to update"
        exit 1
    fi
fi

# Customize the output
FINAL_JSON=$(echo "$SWAGGER_JSON" | jq '
    .servers = [{"url": "https://api.marsvista.dev", "description": "Production API"}] |
    .info.title = "Mars Vista API" |
    .info.description = "Unified API for NASA Mars rover photos with advanced filtering, location queries, and scientific metadata."
')

# Write to file
echo "$FINAL_JSON" | jq '.' > "$OUTPUT_FILE"

echo "Synced openapi.json"
echo "  Source: $SWAGGER_URL"
echo "  Output: $OUTPUT_FILE"
echo "  Paths:  $(echo "$FINAL_JSON" | jq '.paths | keys | length') endpoints"
echo ""
echo "Done! Commit the updated openapi.json"
