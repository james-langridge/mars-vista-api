#!/bin/bash
# Quick status check for Opportunity scraping progress

PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev << 'SQL'
\x
SELECT
  COUNT(*) as total_photos,
  MIN(sol) as min_sol,
  MAX(sol) as max_sol,
  COUNT(DISTINCT sol) as sols_scraped,
  ROUND(COUNT(DISTINCT sol)::numeric / 5111 * 100, 2) as percent_sols,
  MAX(created_at) as last_photo_at,
  EXTRACT(EPOCH FROM (NOW() - MAX(created_at)))::int as seconds_since_last
FROM photos
WHERE rover_id = 3;
SQL
