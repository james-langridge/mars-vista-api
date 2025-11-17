# Scaling and Performance Guide

Comprehensive analysis of the Mars Vista API's scaling capacity, bottlenecks, and optimization strategies.

## Current Production Metrics

- **Deployment**: Railway Pro plan
- **URL**: https://mars-vista-api-production.up.railway.app
- **Database**: Railway PostgreSQL Pro (1.98M photos)
- **Response Time**: 557ms for typical photo queries (25 results)
- **Health Check**: < 100ms

## Estimated Capacity

### Current Configuration (Default Resources)

**Resources**:
- ~2 vCPU (Railway default allocation)
- ~2 GB RAM
- PostgreSQL: ~100-200 concurrent connections

**Capacity**:
- **Simple queries** (GET /health, GET /api/v1/rovers): ~1,000-2,000 req/s
- **Photo queries** (with filters, pagination): ~50-100 req/s
- **Mixed workload** (realistic): **~100 req/s sustained**, 200 req/s burst
- **Concurrent users**: 50-100

**Cost**: ~$60/month (estimated)

### With Basic Optimizations

**Changes**:
- Connection pooling configured
- Memory cache for rovers/manifests
- Response compression enabled
- 4 vCPU, 4GB RAM

**Capacity**:
- **Photo queries**: ~100-200 req/s sustained, 400 req/s burst
- **Mixed workload**: **~500 req/s sustained**, 1,000 req/s burst
- **Concurrent users**: 200-500

**Cost**: ~$120-150/month

### With Redis Caching

**Changes**:
- Redis cache for frequently accessed data
- 80% cache hit rate
- 4 vCPU, 4GB RAM + Redis instance

**Capacity**:
- **Cache hits**: ~2,000-3,000 req/s
- **Cache misses**: ~100-200 req/s
- **Mixed workload**: **~500-800 req/s sustained**, 1,000+ req/s burst
- **Concurrent users**: 1,000-2,000

**Cost**: ~$200-300/month

### Maxed Out Railway Pro (Single Instance)

**Resources**:
- 8 vCPU, 8GB RAM
- Redis cache
- Optimized connection pooling
- Response caching

**Capacity**:
- **~1,500-2,500 req/s sustained**
- **3,000+ req/s burst**
- **Concurrent users**: 5,000-10,000

**Cost**: ~$400-500/month

### Horizontal Scaling (Multiple Replicas)

**Resources**:
- 3 replicas × 8 vCPU = 24 vCPU total
- Railway automatic load balancing
- Shared Redis cache
- Shared PostgreSQL (read replicas recommended)

**Capacity**:
- **~5,000-10,000 req/s sustained**
- **15,000+ req/s burst**
- **Concurrent users**: 20,000-50,000

**Cost**: ~$720-1,000/month

## Architecture Bottleneck Analysis

### 1. Database Connection Pooling ❌ Not Configured

**Current State**:
```csharp
// Program.cs - No connection pool limits
options.UseNpgsql(connectionString)
```

**Impact**:
- Default EF Core pool: 1024 connections
- Railway PostgreSQL limit: ~100-200 connections
- **Bottleneck**: Database exhausts connections at ~100-150 concurrent requests

**Solution**:
```csharp
var connectionString = baseConnectionString +
    ";Maximum Pool Size=50;Min Pool Size=10;Connection Idle Lifetime=300;Pooling=true";
```

**Improvement**: 2x capacity increase

### 2. No Caching ❌ Major Bottleneck

**Current State**:
- Every request hits PostgreSQL
- No in-memory cache
- No distributed cache (Redis)

**Impact**:
- Database becomes bottleneck at ~100-200 req/s
- Repeated queries for same data (rovers list, manifests)
- 80% of queries could be cached

**Solutions**:

**Level 1: Memory Cache** (quick win)
```csharp
// Cache rovers list for 1 hour
builder.Services.AddMemoryCache();

// In controller:
_cache.GetOrCreate("rovers", entry => {
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
    return _db.Rovers.Include(r => r.Cameras).ToList();
});
```

**Improvement**: 5x capacity for cached endpoints

**Level 2: Redis Cache** (production)
```csharp
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = "redis:6379";
    options.InstanceName = "MarsVista:";
});
```

**Improvement**: 10x capacity for cached data

### 3. DbContext Scoping ✅ Good

**Current State**:
```csharp
builder.Services.AddDbContext<MarsVistaDbContext>() // Scoped by default
```

**Status**: ✅ Optimal
- Each request gets its own DbContext
- Properly disposed after request
- No connection leaks
- Thread-safe

### 4. Query Efficiency ✅ Good

**Current State**:
- Indexed columns: rover_id, sol, camera_id, earth_date
- Composite indexes: (rover_id, sol), (rover_id, camera_id)
- JSONB data not in WHERE clauses (doesn't slow queries)
- Pagination limits result size (per_page max: 100)

**Status**: ✅ Well optimized

**Potential Improvements**:
- Add covering indexes for common query patterns
- Consider materialized views for complex aggregations
- Query result caching (2-5 minute TTL)

### 5. No Rate Limiting ❌ Risk

**Current State**:
- No per-IP limits
- No per-endpoint limits
- Single client can overwhelm API

**Impact**:
- Vulnerable to abuse
- No protection against traffic spikes
- Fair use policy unenforced

**Solution**:
```csharp
// Add rate limiting middleware
builder.Services.AddRateLimiter(options => {
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

**Improvement**: Protects against abuse, ensures fair resource allocation

### 6. Response Compression ❌ Not Configured

**Current State**:
- No compression middleware
- JSON responses sent uncompressed
- Average response: 5-50 KB

**Impact**:
- Higher bandwidth costs
- Slower response times (especially mobile)
- 70-80% size reduction possible with gzip

**Solution**:
```csharp
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

**Improvement**: 3-5x bandwidth reduction, faster response times

### 7. No CDN ⚠️ Consider for Global Traffic

**Current State**:
- Single Railway region (europe-west4)
- No edge caching
- No geographic distribution

**Impact**:
- Higher latency for users outside Europe
- All traffic hits origin server

**Solution** (future):
- Cloudflare in front of Railway
- Cache popular endpoints at edge
- ~50-100ms latency reduction globally

## Railway Pro Plan Specifications

### Resource Limits

- **CPU**: Up to 32 vCPU per service
- **Memory**: Up to 32 GB RAM per service
- **Storage**: 100 GB ephemeral disk, 250 GB volume
- **Database**: Separate PostgreSQL service (shared or dedicated)

### Scaling Options

**Vertical Scaling**:
- Adjust vCPU/RAM in Railway dashboard
- Takes effect on next deployment
- Costs scale linearly with resources

**Horizontal Scaling**:
- Add replicas (multiple instances)
- Railway automatic load balancing
- Shared database (consider read replicas)
- Session affinity optional

### Pricing Model

**Pay-per-use**:
- CPU: $0.00000772 per vCPU-second
- Memory: $0.00000386 per GB-second
- Database: Included in PostgreSQL service

**Example Costs**:

| Configuration | vCPU | RAM | Monthly Cost (24/7) |
|--------------|------|-----|---------------------|
| Default | 2 | 2 GB | ~$60 |
| Optimized | 4 | 4 GB | ~$120 |
| High Traffic | 8 | 8 GB | ~$240 |
| 3 Replicas | 24 | 24 GB | ~$720 |

Add PostgreSQL cost: ~$10-50/month depending on size

## Optimization Roadmap

### Phase 1: Quick Wins (1-2 hours)

**Immediate capacity boost with minimal code changes**

1. **Configure connection pooling** (15 min)
   - Add pool size limits to connection string
   - Impact: 2x capacity

2. **Add memory cache for rovers** (30 min)
   - Cache GET /api/v1/rovers response
   - 1-hour TTL
   - Impact: 10x improvement for this endpoint

3. **Enable response compression** (15 min)
   - Add Gzip middleware
   - Impact: 70% bandwidth reduction

4. **Add basic rate limiting** (30 min)
   - 100 requests/minute per IP
   - Impact: Protection against abuse

**Result**: 100 req/s → 300-400 req/s

**Cost**: Same ($60/month)

### Phase 2: Production Hardening (1 day)

**Production-ready optimizations**

1. **Redis cache** (2 hours)
   - Add Redis service on Railway
   - Cache rovers, manifests, popular queries
   - 5-15 minute TTL
   - Impact: 5-10x improvement on cached data

2. **Query result caching** (2 hours)
   - Cache photo query results
   - 2-5 minute TTL
   - Impact: 80% cache hit rate

3. **Monitoring and alerting** (2 hours)
   - Application Insights / Sentry
   - Track request rates, errors, latency
   - Alert on anomalies

4. **Increase resources** (5 min)
   - 2 vCPU → 4 vCPU
   - 2 GB → 4 GB RAM

**Result**: 300-400 req/s → 800-1,000 req/s

**Cost**: $60/month → $150-200/month

### Phase 3: High Scale (1 week)

**For significant traffic growth**

1. **Database read replicas** (1 day)
   - Separate read/write connections
   - Route queries to replicas
   - Impact: 3-5x database capacity

2. **Advanced caching strategy** (2 days)
   - Cache invalidation on data updates
   - Warm cache on startup
   - Cache preloading for popular queries

3. **Horizontal scaling** (1 day)
   - Add 2-3 replicas
   - Configure load balancing
   - Session state management

4. **CDN integration** (1 day)
   - Cloudflare in front of Railway
   - Cache at edge
   - DDoS protection

**Result**: 1,000 req/s → 5,000-10,000 req/s

**Cost**: $200/month → $500-1,000/month

## Realistic Usage Scenarios

### Scenario 1: Launch / Small Scale

**Traffic**: 10-50 req/s average, 100 req/s peak
**Users**: Hundreds per day
**Configuration**: Default (2 vCPU, 2GB RAM)
**Optimizations**: None required yet
**Cost**: $50-100/month
**Use Cases**: Personal projects, demos, API marketplace listing

### Scenario 2: Growing / Medium Scale

**Traffic**: 100-200 req/s average, 500 req/s peak
**Users**: Thousands per day
**Configuration**: 4 vCPU, 4GB RAM, Redis cache
**Optimizations**: Phase 1 + Phase 2
**Cost**: $150-300/month
**Use Cases**: Production apps, developer community adoption

### Scenario 3: Popular / High Scale

**Traffic**: 500-1,000 req/s average, 2,000 req/s peak
**Users**: Tens of thousands per day
**Configuration**: 3 replicas, 8 vCPU each, Redis, read replicas
**Optimizations**: Phase 1 + Phase 2 + Phase 3
**Cost**: $500-1,000/month
**Use Cases**: Popular apps, featured on Product Hunt, high traffic sites

### Scenario 4: Viral / Enterprise Scale

**Traffic**: 5,000+ req/s sustained
**Users**: Hundreds of thousands per day
**Configuration**: CDN + multiple replicas + read replicas + advanced caching
**Optimizations**: All phases + custom optimizations
**Cost**: $1,000-5,000/month
**Use Cases**: Viral success, enterprise clients, major partnerships

## Monitoring and Metrics

### Key Metrics to Track

1. **Request Rate**
   - Requests per second
   - Peak vs average
   - Endpoint breakdown

2. **Response Times**
   - P50, P95, P99 latency
   - Slow query identification
   - Database query times

3. **Error Rates**
   - 4xx vs 5xx errors
   - Database connection errors
   - Timeout errors

4. **Resource Usage**
   - CPU utilization
   - Memory usage
   - Database connections active
   - Cache hit rates

5. **Database Performance**
   - Query execution time
   - Connection pool exhaustion
   - Slow query log
   - Index usage

### Recommended Tools

**Application Performance Monitoring**:
- Sentry (errors and performance)
- Application Insights (Microsoft, native .NET)
- Datadog (comprehensive, expensive)

**Infrastructure Monitoring**:
- Railway dashboard (built-in metrics)
- Uptime monitoring (Uptime Robot, Pingdom)

**Database Monitoring**:
- Railway PostgreSQL metrics
- pg_stat_statements (query analysis)

## Load Testing

### Before Load Testing

**Important**: Coordinate with Railway before running load tests:
- Notify support of planned test
- Start small and ramp up gradually
- Monitor costs during test
- Have rollback plan ready

### Recommended Tools

1. **k6** (modern, developer-friendly)
   ```javascript
   import http from 'k6/http';
   export let options = {
     stages: [
       { duration: '2m', target: 100 }, // Ramp up to 100 users
       { duration: '5m', target: 100 }, // Stay at 100 users
       { duration: '2m', target: 0 },   // Ramp down
     ],
   };
   export default function() {
     http.get('https://mars-vista-api-production.up.railway.app/api/v1/rovers');
   }
   ```

2. **Artillery** (scenario-based)
3. **JMeter** (traditional, GUI-based)

### Test Scenarios

1. **Baseline Test**: Verify current capacity (50-100 users)
2. **Stress Test**: Find breaking point (ramp to 500 users)
3. **Spike Test**: Handle traffic bursts (0 → 200 → 0 quickly)
4. **Soak Test**: Stability over time (100 users for 1 hour)

## Comparison: NASA Mars Photo API

The original NASA Mars Photo API likely handles similar or lower traffic:

**NASA API Characteristics**:
- Public, free, no authentication
- Niche audience (space enthusiasts, developers)
- Estimated traffic: 10-100 req/s average
- Probably hosted on AWS/Azure with caching

**Your API Advantages**:
- Complete data (all 55 fields vs NASA's 10-15)
- Better query performance (optimized indexes)
- Modern architecture (.NET 9 vs Ruby on Rails)
- Hybrid storage enables future advanced features

**Realistic Expectations**:
- Initial traffic: 1-10 req/s (demos, personal projects)
- With promotion: 10-50 req/s (API marketplace, Reddit/HN post)
- Viral success: 100-500 req/s (Product Hunt, major feature)
- Enterprise adoption: 500+ req/s (commercial partnerships)

## Conclusion

**Current State**:
- Ready for launch at small-medium scale
- Can handle 100 req/s sustained without changes
- Cost-effective for initial adoption ($60/month)

**Growth Path**:
- Phase 1 optimizations: 3-4x capacity for same cost
- Phase 2 optimizations: 10x capacity for 2-3x cost
- Phase 3 optimizations: 50x+ capacity with horizontal scaling

**Bottom Line**:
The API is production-ready for launch. Current capacity (100 req/s) is sufficient for initial traction. You'll have clear signals (response times increasing, CPU hitting limits) before needing to scale. Railway makes scaling straightforward when needed.

Start with current configuration, monitor metrics, and optimize based on actual usage patterns.
