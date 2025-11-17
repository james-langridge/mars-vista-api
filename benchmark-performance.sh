#!/bin/bash

# Performance Benchmark Script for Mars Vista API
# Tests all endpoints with realistic queries and measures response times

set -e

BASE_URL="${1:-https://api.marsvista.dev}"
ITERATIONS="${2:-3}"

echo "======================================================================"
echo "Mars Vista API Performance Benchmark"
echo "======================================================================"
echo ""
echo "Base URL: $BASE_URL"
echo "Iterations per test: $ITERATIONS"
echo "Date: $(date)"
echo ""

# Helper function to benchmark an endpoint
benchmark_endpoint() {
    local name="$1"
    local url="$2"
    local iterations="$3"

    echo "----------------------------------------------------------------------"
    echo "Testing: $name"
    echo "URL: $url"
    echo "----------------------------------------------------------------------"

    local total_time=0
    local min_time=999999
    local max_time=0

    for i in $(seq 1 $iterations); do
        # Use curl with -w to get timing info, -s for silent, -o to discard output
        local time=$(curl -s -o /dev/null -w "%{time_total}" "$url" 2>/dev/null | awk '{print $1 * 1000}')

        total_time=$(echo "$total_time + $time" | bc)

        # Update min/max
        if (( $(echo "$time < $min_time" | bc -l) )); then
            min_time=$time
        fi
        if (( $(echo "$time > $max_time" | bc -l) )); then
            max_time=$time
        fi

        echo "  Run $i: ${time}ms"
    done

    local avg_time=$(echo "scale=2; $total_time / $iterations" | bc)

    echo ""
    echo "  Average: ${avg_time}ms"
    echo "  Min:     ${min_time}ms"
    echo "  Max:     ${max_time}ms"
    echo ""
}

echo "======================================================================"
echo "Consumer-Facing API Benchmarks (/api/v1/*)"
echo "======================================================================"
echo ""

# Critical endpoints (identified as having performance issues)
echo "======================================================================"
echo "ROVER METADATA ENDPOINTS (N+1 and multiple query issues)"
echo "======================================================================"
echo ""

benchmark_endpoint \
    "GET /api/v1/rovers (N+1 query issue)" \
    "$BASE_URL/api/v1/rovers" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity (3 queries for stats)" \
    "$BASE_URL/api/v1/rovers/curiosity" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/perseverance" \
    "$BASE_URL/api/v1/rovers/perseverance" \
    $ITERATIONS

# High-traffic endpoints
echo "======================================================================"
echo "HIGH-TRAFFIC ENDPOINTS"
echo "======================================================================"
echo ""

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity/photos?sol=1000" \
    "$BASE_URL/api/v1/rovers/curiosity/photos?sol=1000" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity/photos?earth_date=2015-01-01" \
    "$BASE_URL/api/v1/rovers/curiosity/photos?earth_date=2015-01-01" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity/photos?sol=1000&camera=MAST" \
    "$BASE_URL/api/v1/rovers/curiosity/photos?sol=1000&camera=MAST" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity/photos?page=1&per_page=100" \
    "$BASE_URL/api/v1/rovers/curiosity/photos?page=1&per_page=100" \
    $ITERATIONS

benchmark_endpoint \
    "GET /api/v1/rovers/curiosity/latest" \
    "$BASE_URL/api/v1/rovers/curiosity/latest" \
    $ITERATIONS

# Already optimized endpoint
echo "======================================================================"
echo "ALREADY OPTIMIZED (Raw SQL)"
echo "======================================================================"
echo ""

benchmark_endpoint \
    "GET /api/v1/manifests/curiosity" \
    "$BASE_URL/api/v1/manifests/curiosity" \
    $ITERATIONS

# Fast endpoints (baseline)
echo "======================================================================"
echo "BASELINE (Should be fast)"
echo "======================================================================"
echo ""

benchmark_endpoint \
    "GET /api/v1/photos/1 (single photo by ID)" \
    "$BASE_URL/api/v1/photos/1" \
    $ITERATIONS

benchmark_endpoint \
    "GET /health (health check)" \
    "$BASE_URL/health" \
    $ITERATIONS

echo "======================================================================"
echo "Benchmark Complete"
echo "======================================================================"
echo ""
echo "Summary of expected issues (consumer-facing APIs only):"
echo "  1. /api/v1/rovers - N+1 query (13 queries for 4 rovers)"
echo "  2. /api/v1/rovers/{name} - 3 separate aggregation queries"
echo "  3. Photo query endpoints - Include() overhead when projecting to DTO"
echo ""
echo "Next steps:"
echo "  1. Save these results as baseline"
echo "  2. Apply optimizations to consumer-facing endpoints"
echo "  3. Re-run benchmark to measure improvements"
echo ""
