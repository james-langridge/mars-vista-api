#!/bin/bash

# Compare baseline and optimized benchmark results

echo "======================================================================"
echo "Performance Benchmark Comparison"
echo "======================================================================"
echo ""

cat << 'EOF'
| Endpoint                           | Baseline | Optimized | Diff    | Change |
|------------------------------------|----------|-----------|---------|--------|
| GET /api/v1/rovers                 | 489ms    | 577ms     | +88ms   | -18%   |
| GET /api/v1/rovers/curiosity       | 435ms    | 432ms     | -3ms    | +1%    |
| GET /api/v1/rovers/perseverance    | 464ms    | 418ms     | -46ms   | +10%   |
| Photos (sol=1000)                  | 319ms    | 362ms     | +43ms   | -13%   |
| Photos (earth_date=2015-01-01)     | 300ms    | 394ms     | +94ms   | -31%   |
| Photos (sol=1000&camera=MAST)      | 316ms    | 368ms     | +52ms   | -16%   |
| Photos (page=1&per_page=100)       | 383ms    | 336ms     | -47ms   | +12%   |
| Latest photos                      | 349ms    | 355ms     | +6ms    | -2%    |
| Manifests                          | 317ms    | 407ms     | +90ms   | -28%   |
| Photo by ID                        | 406ms    | 366ms     | -40ms   | +10%   |
| Health check                       | 595ms    | 893ms     | +298ms  | -50%   |

Network Latency Analysis:
- Baseline health check (minimal processing): 595ms
- Optimized health check: 893ms (extreme variance: 423ms-1810ms)
- This suggests significant network instability during optimized test
- True API improvement is likely masked by network variance

Conclusions:
1. Network variance is too high for reliable comparison (~300-400ms swings)
2. Some endpoints show improvement (rovers/perseverance: +10%, photo by ID: +10%)
3. Regression in /api/v1/rovers needs investigation
4. Should run multiple test iterations to average out network noise
5. Consider testing from Railway's region or use internal metrics

Next Steps:
1. Check Railway logs for actual database query times
2. Run benchmark with more iterations (5-10) to reduce variance
3. Consider migrating to US West region for lower latency
4. Add application-level timing metrics to measure pure query performance
EOF
