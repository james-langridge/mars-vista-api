# Monitoring & Error Tracking

Production monitoring setup for the Mars Vista API using Serilog, Sentry, and health checks.

## Overview

The API includes comprehensive monitoring capabilities:

- **Structured Logging**: JSON-formatted logs with Serilog
- **Error Tracking**: Sentry integration for production error monitoring
- **Request Tracing**: Correlation IDs for tracking requests across logs
- **Health Checks**: Enhanced endpoint with database connectivity checks
- **Performance Metrics**: Request duration and throughput tracking via Sentry

## Structured Logging with Serilog

### Features

- **Compact JSON Format**: Machine-readable structured logs
- **Request Logging**: HTTP method, path, status code, duration
- **Context Enrichment**:
  - Environment name (Development/Production)
  - Machine name
  - Thread ID
  - Correlation ID
  - Remote IP address
  - User agent
  - Request host and scheme

### Log Outputs

**Console**: Structured JSON logs written to stdout (captured by Railway)
**File**: Rolling daily log files in `logs/marsvista-YYYYMMDD.json` (7 day retention)

### Configuration

Configured in `Program.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/marsvista-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();
```

### Example Log Entry

```json
{
  "@t": "2025-11-17T06:09:45.3496832Z",
  "@mt": "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms",
  "@r": ["414.2295"],
  "RequestHost": "localhost:5127",
  "RequestScheme": "http",
  "RemoteIP": "::1",
  "UserAgent": "curl/8.5.0",
  "CorrelationId": "test-123-456",
  "RequestMethod": "GET",
  "RequestPath": "/api/v1/rovers",
  "StatusCode": 200,
  "Elapsed": 414.229465,
  "SourceContext": "Serilog.AspNetCore.RequestLoggingMiddleware",
  "EnvironmentName": "Development",
  "MachineName": "juliett-lima",
  "ThreadId": 12
}
```

## Correlation IDs

### Purpose

Track individual requests across distributed logs and services.

### Usage

**Automatic Generation**: If no correlation ID is provided, one is auto-generated
**Custom ID**: Send `X-Correlation-ID` header to use your own ID
**Response Header**: Correlation ID is returned in response headers

### Example

```bash
# Send request with custom correlation ID
curl -H "X-Correlation-ID: my-request-123" \
     -H "X-API-Key: your_key" \
     https://api.marsvista.dev/api/v1/rovers

# Response includes the same correlation ID
X-Correlation-ID: my-request-123
```

### Log Filtering

Use correlation ID to track a request through all logs:

```bash
# Railway logs
railway logs | grep "my-request-123"

# Local log files
cat logs/marsvista-20251117.json | grep "my-request-123"
```

## Error Tracking with Sentry

### Setup (Production Only)

Sentry is configured conditionally - it only runs when a DSN is provided (production).

**Railway Environment Variable**:
```bash
SENTRY_DSN=https://your-sentry-dsn@o123456.ingest.sentry.io/7890123
```

### Features

- Automatic exception capture with stack traces
- Performance monitoring (10% sample rate in production)
- Request context (no PII sent)
- Breadcrumbs (last 50 events leading to error)
- Environment tagging (Development/Production)

### Configuration

```csharp
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = 0.1; // 10% sampling in production
        options.AttachStacktrace = true;
        options.SendDefaultPii = false; // Don't send PII
        options.MaxBreadcrumbs = 50;
    });
}
```

### Getting a Sentry DSN

1. Create free account at [sentry.io](https://sentry.io)
2. Create new project (select ASP.NET Core)
3. Copy DSN from project settings
4. Add to Railway environment variables

### Sentry Dashboard

Access your Sentry dashboard to view:
- Real-time error tracking
- Performance metrics (p50, p95, p99 latency)
- Request throughput
- Error rates by endpoint
- Stack traces with source context

## Health Checks

### Endpoint

```
GET /health
```

### Features

- Database connectivity check (DbContext)
- PostgreSQL connection check
- Response time measurement
- JSON format with detailed diagnostics

### Response Format

```json
{
  "status": "Healthy",
  "timestamp": "2025-11-17T06:09:39.3464496Z",
  "duration": 129.9851,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 118.6259,
      "description": null,
      "error": null
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "duration": 10.3798,
      "description": null,
      "error": null
    }
  ]
}
```

### Status Codes

- `200 OK`: All checks passed (Healthy)
- `503 Service Unavailable`: One or more checks failed (Unhealthy/Degraded)

### Health Check Configuration

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MarsVistaDbContext>("database")
    .AddNpgSql(connectionString, name: "postgresql",
               failureStatus: HealthStatus.Unhealthy);
```

## Uptime Monitoring

### Recommended: UptimeRobot

Free tier includes:
- 50 monitors
- 5-minute check intervals
- Email and Slack alerts
- Public status pages

### Setup

1. Create account at [uptimerobot.com](https://uptimerobot.com)
2. Add new HTTP monitor:
   - **URL**: `https://api.marsvista.dev/health`
   - **Check interval**: 5 minutes
   - **Alert threshold**: 2 consecutive failures
3. Configure alert contacts (email, Slack, etc.)

### Railway Built-in Health Checks

Railway can also monitor the `/health` endpoint automatically. Configure in `railway.toml` or service settings.

## Viewing Logs

### Railway Production Logs

```bash
# Real-time logs
railway logs

# Follow logs (like tail -f)
railway logs --follow

# Filter by time
railway logs --since 1h
railway logs --since 2025-11-17

# Search logs
railway logs | grep "error"
railway logs | grep "CorrelationId.*abc-123"
```

### Local Development Logs

**Console output**: Structured JSON logs in terminal
**Log files**: `logs/marsvista-YYYYMMDD.json`

```bash
# View today's logs
cat logs/marsvista-$(date +%Y%m%d).json

# Pretty print JSON
cat logs/marsvista-20251117.json | jq

# Filter by level
cat logs/marsvista-20251117.json | jq 'select(."@l" == "Error")'

# Filter by correlation ID
cat logs/marsvista-20251117.json | jq 'select(.CorrelationId == "test-123")'
```

## Monitoring Best Practices

### 1. Use Correlation IDs

Always include correlation IDs when debugging production issues:

```bash
curl -H "X-Correlation-ID: debug-$(date +%s)" \
     -H "X-API-Key: your_key" \
     https://api.marsvista.dev/api/v1/rovers
```

### 2. Monitor Health Check Endpoint

Set up external monitoring (UptimeRobot) to alert on downtime.

### 3. Review Sentry Regularly

- Check for new error types weekly
- Monitor performance trends
- Set up alert rules for critical errors

### 4. Log Retention

- **Railway**: 7 days of logs (free tier)
- **Local files**: 7 days (auto-rotation)
- **Sentry**: 30 days (free tier)

### 5. Debug Workflow

When investigating an issue:

1. Check Sentry for exception details
2. Use correlation ID to filter logs
3. Review request logs for timing/status
4. Check health endpoint for system status

## Performance Metrics

### Available Metrics (via Sentry)

- **Request duration**: p50, p95, p99 percentiles
- **Throughput**: Requests per second
- **Error rate**: 4xx and 5xx by endpoint
- **Database query duration**: Via EF Core logging

### Example Queries

**Slow requests** (Sentry):
```
transaction.duration:>1000 AND transaction.op:http.server
```

**Error rate by endpoint** (Sentry):
```
event.type:error AND transaction:/api/v1/rovers
```

## Troubleshooting

### Logs Not Appearing in Railway

1. Check that application is writing to stdout
2. Verify Railway logs retention (free tier: 7 days)
3. Use `railway logs --follow` to see real-time output

### Sentry Not Capturing Errors

1. Verify `SENTRY_DSN` environment variable is set
2. Check Sentry quota (free tier limits)
3. Look for "Configuring Sentry error tracking" in startup logs
4. Test with a deliberate exception in dev environment

### Health Check Failing

1. Check database connection string
2. Verify PostgreSQL is running
3. Check database credentials
4. Review health check response for specific failure

### Missing Correlation IDs

1. Ensure middleware is before request logging
2. Check that `Serilog.Context` is imported
3. Verify correlation ID appears in response headers

## Security Considerations

- **No PII in logs**: IP addresses and sensitive data not logged
- **Sentry PII**: `SendDefaultPii = false` prevents PII transmission
- **API keys**: Never logged or sent to Sentry
- **Database credentials**: Not included in error messages

## Cost Considerations

### Free Tier Limits

- **Sentry**: 5,000 errors/month, 10,000 transactions/month
- **UptimeRobot**: 50 monitors, 5-minute intervals
- **Railway logs**: 7 days retention

### Upgrade Triggers

- Sentry: >5,000 errors/month or need longer retention
- UptimeRobot: Need faster check intervals (<5 min)
- Railway: Need longer log retention

## Related Documentation

- [API Endpoints](API_ENDPOINTS.md) - API reference
- [Database Access](DATABASE_ACCESS.md) - Database queries and management
- [Railway Deployment](../README.md#deployment) - Production deployment guide
