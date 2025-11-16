#!/bin/bash
# Quick status check for Spirit photos

PGPASSWORD=marsvista_dev_password psql -h localhost -U marsvista -d marsvista_dev << 'SQL'
SELECT
  c.name as camera,
  COUNT(p.id) as photos,
  MIN(p.sol) as min_sol,
  MAX(p.sol) as max_sol
FROM cameras c
LEFT JOIN photos p ON c.id = p.camera_id
WHERE c.rover_id = 4
GROUP BY c.id, c.name
ORDER BY c.id;

SELECT COUNT(*) as total_spirit_photos FROM photos WHERE rover_id = 4;
SQL
