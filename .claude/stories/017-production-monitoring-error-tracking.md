# Story 017: Production Monitoring & Error Tracking

**Status:** TODO
**Priority:** CRITICAL (Pre-Launch Blocker)
**Estimated Effort:** Medium (4-6 hours)

## Problem Statement

The API is currently running blind in production with no visibility into:
- Runtime errors and exceptions
- Performance degradation
- Failed requests
- Slow queries
- System health trends

Without monitoring, we cannot:
- Diagnose issues when users report problems
- Detect failures before users notice
- Track performance regressions
- Make data-driven optimization decisions
- Meet production SLA expectations

## Success Criteria

1. **Error Tracking:** All unhandled exceptions captured with full stack traces and context
2. **Structured Logging:** JSON-formatted logs with request tracing and correlation IDs
3. **Performance Metrics:** Request duration, throughput, and error rates tracked
4. **Health Monitoring:** Automated uptime checks with alerting
5. **Dashboards:** Visibility into system health and performance trends
6. **Alerting:** Notifications for critical errors and downtime

## Technical Approach

### 1. Structured Logging with Serilog

**Why Serilog?**
- Industry standard for .NET
- Structured JSON logging
- Multiple sink support (console, file, cloud)
- Request correlation and enrichment
- Performance-optimized

**Implementation:**
```csharp
// Add packages
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread

// Configure in Program.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        "logs/marsvista-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();
```

**Log Context:**
- Request ID (correlation)
- User IP address
- HTTP method and path
- Response status code
- Duration
- Error details
- Query parameters (sanitized)

### 2. Error Tracking with Sentry

**Why Sentry?**
- Free tier sufficient for moderate traffic
- Excellent .NET integration
- Automatic error grouping and deduplication
- Stack trace analysis
- Performance monitoring built-in
- Alert integrations (email, Slack, etc.)
- Release tracking

**Implementation:**
```csharp
// Add package
dotnet add package Sentry.AspNetCore

// Configure in Program.cs
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 0.1; // 10% of requests for performance monitoring
    options.EnableTracing = true;
    options.AttachStacktrace = true;
    options.SendDefaultPii = false; // Don't send PII
});
```

**Error Context:**
- User information (hashed IP, not PII)
- Request details
- Database query (if applicable)
- Custom tags (rover, endpoint type, etc.)
- Breadcrumbs (sequence of events leading to error)

### 3. Application Metrics

**Metrics to Track:**
- Request count (by endpoint, status code)
- Request duration (p50, p95, p99)
- Error rate (4xx, 5xx)
- Database query duration
- Active connections
- Memory usage
- Exception count (by type)

**Implementation Options:**

**Option A: Sentry Performance Monitoring (Recommended for MVP)**
- Already included with Sentry
- No additional setup
- Good enough for initial launch

**Option B: Prometheus + Grafana (For Scale)**
- More granular metrics
- Self-hosted dashboards
- Better for high-traffic scenarios
- Overkill for MVP

**Decision:** Start with Sentry, migrate to Prometheus if needed

### 4. Health Monitoring & Uptime

**Enhanced Health Check:**
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsJsonAsync(result);
    }
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database")
    .AddCheck("data_freshness", () =>
    {
        // Check if latest photo is within 7 days of NASA's latest
        // Implementation TBD
        return HealthCheckResult.Healthy();
    });
```

**External Uptime Monitoring:**
- **Option 1:** UptimeRobot (free, simple, reliable)
- **Option 2:** Railway built-in health checks
- **Option 3:** Pingdom (paid, more features)

**Decision:** Use UptimeRobot (free tier: 50 monitors, 5-min checks)

**Setup:**
1. Create UptimeRobot account
2. Add HTTP monitor for `https://mars-vista-api-production.up.railway.app/health`
3. Set check interval: 5 minutes
4. Configure alert contacts (email, Slack)
5. Set alert threshold: 2 failures (avoid false positives)

### 5. Request Tracing & Correlation

**Correlation ID Middleware:**
```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers.Add("X-Correlation-ID", correlationId);

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
```

**Benefits:**
- Track single request across logs
- Debug multi-step operations
- Correlate errors with user reports

## Implementation Steps

### Phase 1: Serilog Setup (1-2 hours)
1. ✅ Add Serilog NuGet packages
2. ✅ Configure structured JSON logging
3. ✅ Add request logging middleware
4. ✅ Add correlation ID middleware
5. ✅ Configure log enrichers
6. ✅ Test locally and verify JSON output
7. ✅ Update .gitignore to exclude logs/

### Phase 2: Sentry Integration (1-2 hours)
1. ✅ Create Sentry account (free tier)
2. ✅ Add Sentry NuGet package
3. ✅ Configure Sentry DSN (environment variable)
4. ✅ Add Sentry middleware
5. ✅ Test error capture locally
6. ✅ Configure release tracking
7. ✅ Set up alert rules

### Phase 3: Enhanced Health Checks (1 hour)
1. ✅ Add health check packages
2. ✅ Implement database health check
3. ✅ Add data freshness check (basic version)
4. ✅ Customize health check response format
5. ✅ Test health endpoint

### Phase 4: Uptime Monitoring (30 minutes)
1. ✅ Create UptimeRobot account
2. ✅ Configure HTTP monitor
3. ✅ Set up alert contacts
4. ✅ Test alerting (force failure)

### Phase 5: Documentation & Deployment (30 minutes)
1. ✅ Document monitoring setup in docs/
2. ✅ Add monitoring section to README
3. ✅ Deploy to Railway
4. ✅ Verify Sentry capturing errors
5. ✅ Verify UptimeRobot monitoring

## Configuration

### Environment Variables (Railway)

```bash
# Sentry
SENTRY_DSN=https://xxx@xxx.ingest.sentry.io/xxx
SENTRY_ENVIRONMENT=production

# Logging
LOGGING_LEVEL=Information

# Health Check (future)
DATA_FRESHNESS_THRESHOLD_DAYS=7
```

### Secrets Management

- Sentry DSN: Railway environment variable (not committed)
- UptimeRobot: External service, no secrets needed in code

## Acceptance Criteria

- [ ] All unhandled exceptions appear in Sentry within 30 seconds
- [ ] Request logs include correlation ID, duration, status code
- [ ] Health check returns JSON with database status
- [ ] UptimeRobot sends alert when /health fails
- [ ] Sentry shows performance metrics for top endpoints
- [ ] Logs are JSON-formatted and structured
- [ ] No PII logged (IP addresses hashed if logged)

## Testing Checklist

**Local Testing:**
- [ ] Trigger 500 error, verify Sentry captures it
- [ ] Check logs directory contains JSON files
- [ ] Verify correlation ID in response headers
- [ ] Health check returns 200 when healthy
- [ ] Health check returns 503 when database down

**Production Testing:**
- [ ] Deploy to Railway
- [ ] Verify Sentry captures production errors
- [ ] Check Railway logs for structured JSON
- [ ] UptimeRobot shows green status
- [ ] Trigger test alert (temporary health failure)

## Monitoring Dashboard Goals

**Sentry Dashboard:**
- Error rate by endpoint
- p95 latency by endpoint
- Top 10 errors
- Release comparison

**UptimeRobot Dashboard:**
- Uptime percentage (target: 99.9%)
- Response time trends
- Incident history

## Alerting Rules

**Sentry Alerts:**
- New error type (immediate)
- Error rate > 5% (5-minute window)
- p95 latency > 1000ms (15-minute window)

**UptimeRobot Alerts:**
- 2 consecutive failures (10 minutes)
- Response time > 2000ms (3 checks)

## Future Enhancements (Post-Story)

- Add database query performance tracking
- Implement distributed tracing (OpenTelemetry)
- Add custom business metrics (photos scraped/day, etc.)
- Set up log aggregation (Seq, Elasticsearch, or Datadog)
- Add performance regression alerts

## Dependencies

- Railway deployment access
- Sentry account (free tier)
- UptimeRobot account (free tier)

## Risks & Mitigations

**Risk:** Logging too verbose, high costs
**Mitigation:** Set minimum log level to Warning in production, sample traces

**Risk:** Sentry rate limits on free tier
**Mitigation:** Monitor quota usage, upgrade if needed (or implement sampling)

**Risk:** Health check false positives
**Mitigation:** Require 2+ consecutive failures before alerting

## Success Metrics

- **MTTR (Mean Time To Recovery):** < 15 minutes (from alert to fix deployed)
- **Error Detection:** 100% of unhandled exceptions captured
- **Uptime:** 99.9% measured by UptimeRobot
- **Alert Accuracy:** < 5% false positive rate

## References

- Serilog Documentation: https://serilog.net/
- Sentry .NET SDK: https://docs.sentry.io/platforms/dotnet/
- UptimeRobot: https://uptimerobot.com/
- ASP.NET Health Checks: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
