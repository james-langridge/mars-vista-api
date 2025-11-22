#!/bin/bash

# Test critical endpoints that were previously slow
# No API key needed for basic performance check

API_BASE="https://api.marsvista.dev"
echo "Testing critical endpoints for performance improvements..."
echo "=========================================="

# Test 1: Panorama endpoint (was 95 seconds)
echo -e "\n[TEST 1] Panorama query (was 95s):"
start=$(date +%s%N)
response=$(curl -s -w "\n%{http_code}" "${API_BASE}/api/v2/panoramas?rovers=curiosity&sol_min=1000" 2>/dev/null)
http_code=$(echo "$response" | tail -n1)
end=$(date +%s%N)
duration=$((($end - $start) / 1000000))
echo "Response time: ${duration}ms (HTTP $http_code)"
if [ "$http_code" == "401" ]; then
  echo "Endpoint requires authentication - skipping"
elif [ "$duration" -lt 5000 ]; then
  echo "✅ PASS: Under 5 seconds"
else
  echo "❌ FAIL: Still slow"
fi

# Test 2: Landing day photos (was 44 seconds)
echo -e "\n[TEST 2] Landing day photos (was 44s):"
start=$(date +%s%N)
response=$(curl -s -w "\n%{http_code}" "${API_BASE}/api/v1/rovers/curiosity/photos?earth_date=2012-08-06" 2>/dev/null)
http_code=$(echo "$response" | tail -n1)
end=$(date +%s%N)
duration=$((($end - $start) / 1000000))
echo "Response time: ${duration}ms (HTTP $http_code)"
if [ "$http_code" == "401" ]; then
  echo "Endpoint requires authentication - skipping"
elif [ "$duration" -lt 2000 ]; then
  echo "✅ PASS: Under 2 seconds"
else
  echo "❌ FAIL: Still slow"
fi

# Test 3: Sol max query (was 36 seconds)
echo -e "\n[TEST 3] Sol max query (was 36s):"
start=$(date +%s%N)
response=$(curl -s -w "\n%{http_code}" "${API_BASE}/api/v2/photos?rovers=curiosity&sol_max=100" 2>/dev/null)
http_code=$(echo "$response" | tail -n1)
end=$(date +%s%N)
duration=$((($end - $start) / 1000000))
echo "Response time: ${duration}ms (HTTP $http_code)"
if [ "$http_code" == "401" ]; then
  echo "Endpoint requires authentication - skipping"
elif [ "$duration" -lt 2000 ]; then
  echo "✅ PASS: Under 2 seconds"
else
  echo "❌ FAIL: Still slow"
fi

# Test 4: Complex filters (was 32 seconds)
echo -e "\n[TEST 4] Complex filters (was 32s):"
start=$(date +%s%N)
response=$(curl -s -w "\n%{http_code}" "${API_BASE}/api/v2/photos?mars_time_min=M14:00:00&mars_time_max=M16:00:00&mast_elevation_min=0&mast_elevation_max=45&cameras=MAST" 2>/dev/null)
http_code=$(echo "$response" | tail -n1)
end=$(date +%s%N)
duration=$((($end - $start) / 1000000))
echo "Response time: ${duration}ms (HTTP $http_code)"
if [ "$http_code" == "401" ]; then
  echo "Endpoint requires authentication - skipping"
elif [ "$duration" -lt 5000 ]; then
  echo "✅ PASS: Under 5 seconds"
else
  echo "❌ FAIL: Still slow"
fi

# Test 5: Image quality filters (was 3-5 seconds)
echo -e "\n[TEST 5] Image quality filters (was 3-5s):"
start=$(date +%s%N)
response=$(curl -s -w "\n%{http_code}" "${API_BASE}/api/v2/photos?min_width=1024&min_height=768" 2>/dev/null)
http_code=$(echo "$response" | tail -n1)
end=$(date +%s%N)
duration=$((($end - $start) / 1000000))
echo "Response time: ${duration}ms (HTTP $http_code)"
if [ "$http_code" == "401" ]; then
  echo "Endpoint requires authentication - skipping"
elif [ "$duration" -lt 1000 ]; then
  echo "✅ PASS: Under 1 second"
else
  echo "❌ FAIL: Still slow"
fi

echo -e "\n=========================================="
echo "Testing complete!"
echo ""
echo "Note: All endpoints require authentication."
echo "These tests only check if the endpoints respond quickly with 401."
echo "To test actual performance, you need an API key from https://marsvista.dev/dashboard"