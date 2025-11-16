# Story 011: Deploy Mars Vista API to Railway Production

## Status
- **State**: Ready for Implementation
- **Priority**: High
- **Estimated Effort**: 2-3 hours
- **Dependencies**:
  - Database deployed to Railway Pro ✅
  - All 4 rover scrapers implemented ✅
  - Query API endpoints functional ✅

## Story
As a developer building Mars rover applications, I need a publicly accessible Mars Vista API so that I can integrate Mars rover photos into my projects without running a local server.

## Context

### Current State
- **Local Development**: API runs on `http://localhost:5127` only
- **Database**: Production PostgreSQL deployed on Railway Pro (4.6GB, 1.98M photos)
- **Data Complete**: All 4 rovers fully scraped (Perseverance, Curiosity, Opportunity, Spirit)
- **Features Working**: Query API, scraper endpoints, progress monitoring

### Why Deploy Now?
1. **Database is already deployed** - Natural next step after successful Railway migration
2. **Can't validate product-market fit** - Need real users to test demand
3. **Enables future stories** - Documentation, features, monetization require live API
4. **Railway Pro ready** - Upgraded plan, plenty of resources available
5. **Zero friction deployment** - Railway CLI already configured and linked

### What Deployment Unlocks
- Share with space enthusiast communities (Reddit r/space, r/mars)
- List on API marketplaces (RapidAPI, API Layer)
- Get real usage data to inform feature priorities
- Test performance/scaling with real traffic
- Build credibility ("production API" vs "local prototype")

## Acceptance Criteria

### Functional Requirements
- [ ] .NET API deployed to Railway and publicly accessible
- [ ] Production database connection configured via environment variables
- [ ] Health check endpoint responding at `/health`
- [ ] All query API endpoints functional (`/api/v1/rovers/*`)
- [ ] CORS configured to allow browser access from any origin
- [ ] API returns correct data from production database (spot checks)

### Non-Functional Requirements
- [ ] HTTPS enabled by default (Railway provides this automatically)
- [ ] Response times acceptable (<500ms for typical queries)
- [ ] Error handling works correctly in production
- [ ] Logging enabled for monitoring and debugging
- [ ] Connection pooling configured for database efficiency

### Documentation Requirements
- [ ] README updated with production API URL
- [ ] Environment variables documented
- [ ] Deployment guide created for future updates
- [ ] Quick start examples use production URL
- [ ] Troubleshooting section added

## Technical Decisions

### Decision 1: Deployment Platform
**Decision**: Deploy to Railway (same platform as database)
**Rationale**:
- Database already on Railway Pro
- Zero network latency between API and database (internal network)
- Simple deployment via Railway CLI or GitHub integration
- Automatic HTTPS, health checks, logging
- No need to manage separate infrastructure

**Alternative Considered**: Deploy API separately (e.g., Azure App Service, AWS Elastic Beanstalk)
**Rejected Because**:
- Adds complexity (two platforms to manage)
- Higher latency (external database connection)
- Additional cost (two services)

### Decision 2: Configuration Strategy
**Decision**: Use environment variables for all configuration
**Rationale**:
- Railway provides built-in environment variable management
- Keeps secrets out of source code
- Easy to update without redeployment
- Follows 12-factor app principles

**Configuration Required**:
```bash
DATABASE_URL=postgresql://postgres:PASSWORD@HOST:PORT/railway
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

### Decision 3: CORS Policy
**Decision**: Enable CORS for all origins in production (for now)
**Rationale**:
- Public API designed to be called from browsers
- No user authentication yet (no sensitive data)
- NASA data is public domain
- Can restrict later when API keys are implemented

**Implementation**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Decision 4: Scraper Endpoints in Production
**Decision**: Keep scraper endpoints enabled but document as admin-only
**Rationale**:
- Useful for updating data when new photos are available
- Can be restricted with API keys in future story
- No harm in enabling (NASA APIs are public, rate-limited)
- Simplifies initial deployment

**Future Enhancement**: Add API key authentication for scraper endpoints (Story 015)

### Decision 5: Logging Strategy
**Decision**: Use ASP.NET Core built-in logging to stdout
**Rationale**:
- Railway captures stdout/stderr automatically
- No need for external logging service initially
- Can view logs via Railway dashboard or CLI
- Simple, zero-config solution

**Log Levels**:
- Production: Information and above
- Database queries: Warning and above (reduce noise)

## Implementation Steps

### Phase 1: Prepare Application for Production (30 minutes)

#### 1.1 Update appsettings.json for Production

**File**: `src/MarsVista.Api/appsettings.Production.json`

Create production-specific settings:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Notes**:
- No connection string in file (uses environment variable)
- Reduced EF Core logging to Warning (less noise)
- AllowedHosts set to * (Railway proxy handles host validation)

#### 1.2 Update Program.cs for Railway Compatibility

**File**: `src/MarsVista.Api/Program.cs`

Add Railway-specific configurations:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Railway provides DATABASE_URL, convert to connection string
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse Railway DATABASE_URL format: postgresql://user:pass@host:port/dbname
    var uri = new Uri(databaseUrl);
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={uri.UserInfo.Split(':')[0]};Password={uri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    // Fall back to appsettings.json for local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Configure database with connection string
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(connectionString));

// Enable CORS for public API access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ... rest of existing configuration ...

var app = builder.Build();

// Always use CORS in production
app.UseCors();

// ... rest of existing middleware ...
```

**Why These Changes**:
- Railway provides `DATABASE_URL` in PostgreSQL format, need to convert
- SSL required for Railway PostgreSQL connections
- CORS enables browser-based API calls
- Graceful fallback to local config for development

#### 1.3 Add Health Check Endpoint

**File**: `src/MarsVista.Api/Program.cs`

Add before `app.Run()`:

```csharp
// Health check endpoint for Railway
app.MapGet("/health", async (MarsVistaDbContext db) =>
{
    try
    {
        // Verify database connection
        await db.Database.CanConnectAsync();

        var roverCount = await db.Rovers.CountAsync();
        var photoCount = await db.Photos.CountAsync();

        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected",
            rovers = roverCount,
            photos = photoCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health check failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
});
```

**Purpose**:
- Railway uses health checks to monitor service status
- Verifies database connectivity
- Provides basic stats for monitoring
- Returns 503 if database is unreachable

### Phase 2: Deploy to Railway (30 minutes)

#### 2.1 Create Railway Service via CLI

```bash
# Make sure you're in the project directory
cd /home/james/git/mars-vista-api

# Create new Railway service (or use existing if already created)
railway service create mars-vista-api

# Link to existing project (calm-bravery)
railway link
```

#### 2.2 Configure Environment Variables

```bash
# Set production environment
railway variables set ASPNETCORE_ENVIRONMENT=Production

# Link to existing PostgreSQL database
# Railway will automatically inject DATABASE_URL when services are linked
railway service link Postgres

# Verify variables are set
railway variables
```

**Expected Variables**:
```
ASPNETCORE_ENVIRONMENT=Production
DATABASE_URL=postgresql://postgres:OcyvqZhqygoJCtAksWaNwdnuKIbTGQPh@...
PGDATABASE=railway
PGHOST=postgres.railway.internal
PGUSER=postgres
```

#### 2.3 Deploy the API

**Option A: Deploy from Local (Faster for first deploy)**

```bash
# Deploy current code
railway up
```

**Option B: Deploy from GitHub (Better for long-term)**

```bash
# Connect Railway to GitHub repo
railway link github

# Configure build settings in Railway dashboard:
# - Root Directory: /
# - Build Command: dotnet publish src/MarsVista.Api/MarsVista.Api.csproj -c Release -o /app
# - Start Command: dotnet /app/MarsVista.Api.dll

# Push to GitHub to trigger deployment
git push origin main
```

**Recommendation**: Use Option A for initial deploy, then set up Option B for automated deployments.

#### 2.4 Monitor Deployment

```bash
# Watch deployment logs
railway logs

# Check service status
railway status

# Get public URL
railway domain
```

**Expected Output**:
```
Service: mars-vista-api
Status: Deployed
URL: https://mars-vista-api-production.up.railway.app
```

### Phase 3: Verify Production Deployment (30 minutes)

#### 3.1 Test Health Check

```bash
# Get your Railway URL
RAILWAY_URL=$(railway domain)

# Test health endpoint
curl "$RAILWAY_URL/health" | jq

# Expected response:
{
  "status": "healthy",
  "timestamp": "2025-11-16T18:30:00Z",
  "database": "connected",
  "rovers": 4,
  "photos": 1977520
}
```

#### 3.2 Test Query API Endpoints

```bash
# Get all rovers
curl "$RAILWAY_URL/api/v1/rovers" | jq '.rovers[].name'
# Expected: ["Curiosity", "Opportunity", "Perseverance", "Spirit"]

# Get specific rover
curl "$RAILWAY_URL/api/v1/rovers/perseverance" | jq

# Query photos
curl "$RAILWAY_URL/api/v1/rovers/curiosity/photos?sol=1000&per_page=5" | jq

# Test pagination
curl "$RAILWAY_URL/api/v1/rovers/opportunity/photos?sol=1&page=1&per_page=10" | jq '.pagination'
```

#### 3.3 Performance Baseline

```bash
# Measure response times
time curl -s "$RAILWAY_URL/api/v1/rovers/perseverance/photos?sol=1000" > /dev/null

# Should be < 500ms for typical query
```

#### 3.4 Test CORS from Browser

Create test HTML file:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Mars Vista API Test</title>
</head>
<body>
    <h1>Mars Vista API Test</h1>
    <button onclick="testAPI()">Test API</button>
    <pre id="result"></pre>

    <script>
        async function testAPI() {
            const url = 'https://mars-vista-api-production.up.railway.app/api/v1/rovers';
            try {
                const response = await fetch(url);
                const data = await response.json();
                document.getElementById('result').textContent = JSON.stringify(data, null, 2);
            } catch (error) {
                document.getElementById('result').textContent = 'Error: ' + error.message;
            }
        }
    </script>
</body>
</html>
```

Open in browser and click "Test API" - should work without CORS errors.

#### 3.5 Verify Database Connectivity

```bash
# Check Railway logs for database connection
railway logs --filter "database"

# Should see successful connection logs, no errors
```

### Phase 4: Update Documentation (30 minutes)

#### 4.1 Update README.md

**File**: `README.md`

Add production API section:

```markdown
## Production API

The Mars Vista API is publicly available at:

**Base URL**: `https://mars-vista-api-production.up.railway.app`

### Quick Start

Get all rovers:
```bash
curl "https://mars-vista-api-production.up.railway.app/api/v1/rovers"
```

Query Perseverance photos from Sol 1000:
```bash
curl "https://mars-vista-api-production.up.railway.app/api/v1/rovers/perseverance/photos?sol=1000&per_page=10"
```

### API Documentation

See [API_ENDPOINTS.md](docs/API_ENDPOINTS.md) for complete endpoint reference.

### Rate Limits

Currently no rate limits. Please be respectful:
- No more than 10 requests per second
- Cache responses when possible
- Use pagination for large datasets

### Support

- Issues: https://github.com/yourusername/mars-vista-api/issues
- Email: your@email.com
```

#### 4.2 Create Deployment Guide

**File**: `docs/DEPLOYMENT.md`

```markdown
# Deployment Guide

This guide covers deploying the Mars Vista API to Railway.

## Prerequisites

- Railway account (Pro plan recommended)
- Railway CLI installed: `npm install -g @railway/cli`
- Git repository connected to Railway

## Environment Variables

Required environment variables:

| Variable | Value | Notes |
|----------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Enables production settings |
| `DATABASE_URL` | Auto-injected | Linked from PostgreSQL service |

## Deployment Steps

### Initial Deployment

1. Link to Railway project:
   ```bash
   railway link
   ```

2. Set environment:
   ```bash
   railway variables set ASPNETCORE_ENVIRONMENT=Production
   ```

3. Link PostgreSQL service:
   ```bash
   railway service link Postgres
   ```

4. Deploy:
   ```bash
   railway up
   ```

5. Get public URL:
   ```bash
   railway domain
   ```

### Updates

For subsequent deployments:

```bash
# Deploy latest code
railway up

# Or push to GitHub if using GitHub integration
git push origin main
```

### Monitoring

View logs:
```bash
railway logs

# Filter for errors
railway logs --filter "error"
```

Check health:
```bash
curl "$(railway domain)/health"
```

## Troubleshooting

### Database Connection Errors

Check DATABASE_URL is set:
```bash
railway variables | grep DATABASE_URL
```

Verify PostgreSQL service is linked:
```bash
railway service
```

### Deployment Fails

Check build logs:
```bash
railway logs --deployment
```

Verify .NET SDK version in build:
```bash
railway logs | grep "dotnet"
```

### Slow Response Times

Check database connection pooling in logs
Monitor Railway metrics dashboard
Consider upgrading Railway plan if consistently slow

## Rollback

If deployment fails:

```bash
# View deployment history
railway deployments

# Rollback to previous deployment
railway rollback <deployment-id>
```
```

#### 4.3 Update API_ENDPOINTS.md

**File**: `docs/API_ENDPOINTS.md`

Update the "Base URL" section:

```markdown
# API Endpoints Documentation

Complete reference for all Mars Vista API endpoints.

## Base URLs

- **Production**: `https://mars-vista-api-production.up.railway.app`
- **Local Development**: `http://localhost:5127`

All examples below use the production URL. Replace with `http://localhost:5127` for local development.

...
```

Update all curl examples to use production URL:

```bash
# Before:
curl "http://localhost:5127/api/v1/rovers"

# After:
curl "https://mars-vista-api-production.up.railway.app/api/v1/rovers"
```

## Testing Checklist

### Pre-Deployment
- [ ] All tests pass locally
- [ ] Database migrations applied to production DB
- [ ] Environment variables configured in Railway
- [ ] CORS policy reviewed and approved
- [ ] Health check endpoint implemented

### Post-Deployment
- [ ] Health check returns 200 OK
- [ ] GET /api/v1/rovers returns all 4 rovers
- [ ] Photo queries return correct data
- [ ] Pagination works correctly
- [ ] CORS headers present in responses
- [ ] Response times < 500ms for typical queries
- [ ] No errors in Railway logs
- [ ] Database queries execute successfully

### Documentation
- [ ] README updated with production URL
- [ ] API_ENDPOINTS.md updated
- [ ] DEPLOYMENT.md created
- [ ] Examples use production URL
- [ ] Troubleshooting guide included

### Smoke Tests

Run these after deployment:

```bash
# Set Railway URL
RAILWAY_URL="https://mars-vista-api-production.up.railway.app"

# Test 1: Health check
curl "$RAILWAY_URL/health" | jq '.status'
# Expected: "healthy"

# Test 2: Get rovers
curl "$RAILWAY_URL/api/v1/rovers" | jq '.rovers | length'
# Expected: 4

# Test 3: Query photos
curl "$RAILWAY_URL/api/v1/rovers/perseverance/photos?sol=1" | jq '.photos | length'
# Expected: > 0

# Test 4: Pagination
curl "$RAILWAY_URL/api/v1/rovers/curiosity/photos?sol=1000&page=2" | jq '.pagination.page'
# Expected: 2

# Test 5: Camera filter
curl "$RAILWAY_URL/api/v1/rovers/perseverance/photos?sol=1000&camera=NAVCAM_LEFT" | jq '.photos[0].camera.name'
# Expected: "NAVCAM_LEFT"
```

## Success Criteria

### Deployment Success
✅ API accessible at public Railway URL
✅ HTTPS enabled automatically
✅ Health check endpoint returns healthy status
✅ Database connection successful (internal Railway network)
✅ Zero deployment errors in logs

### Functionality Success
✅ All query endpoints return correct data
✅ Pagination works across all endpoints
✅ Camera filtering works
✅ Sol filtering works
✅ Earth date filtering works
✅ Response times acceptable (< 500ms typical)

### Documentation Success
✅ README has production URL and examples
✅ Deployment guide complete
✅ API documentation updated
✅ Troubleshooting guide created
✅ Environment variables documented

## Performance Expectations

Based on Railway Pro plan and database size (1.98M photos):

| Endpoint | Expected Response Time | Notes |
|----------|----------------------|-------|
| GET /health | < 100ms | Database count query |
| GET /api/v1/rovers | < 200ms | 4 rovers with stats |
| GET /api/v1/rovers/{name} | < 200ms | Single rover details |
| GET /api/v1/rovers/{name}/photos | < 500ms | Paginated query (25 results) |
| GET /api/v1/rovers/{name}/photos (filtered) | < 800ms | Complex query with filters |

**Note**: First request after idle may be slower due to Railway cold start (~1-2 seconds).

## Known Limitations

1. **No Authentication**: API is completely open (addressed in Story 015)
2. **No Rate Limiting**: Relies on fair use (addressed in Story 013)
3. **No CDN**: Images served directly from NASA URLs
4. **Cold Starts**: Railway may sleep service after inactivity on free tier (not applicable to Pro)

## Next Steps

After successful deployment:

1. **Story 012**: Create API documentation website (Swagger/OpenAPI)
2. **Story 013**: Add usage analytics and basic rate limiting
3. **Story 014**: Implement advanced search features
4. **Story 015**: Add API key system for freemium tiers

## Rollback Plan

If critical issues arise post-deployment:

1. **Quick Fix**: Rollback via Railway dashboard
   ```bash
   railway rollback <previous-deployment-id>
   ```

2. **Database Issues**: Database is independent, API rollback won't affect data

3. **Communication**: Update README with maintenance notice if needed

## Monitoring

**Railway Dashboard**: https://railway.app/dashboard
- View logs, metrics, deployment history
- Monitor resource usage (CPU, memory, network)
- Check health check status

**Manual Health Checks**:
```bash
# Every 5 minutes
watch -n 300 'curl -s https://mars-vista-api-production.up.railway.app/health | jq .status'
```

**Future Enhancement**: Set up automated monitoring (Uptime Robot, Pingdom) in Story 013.

## Cost Estimate

Railway Pro plan: $20/month
- Includes API service
- Includes PostgreSQL database (10GB allocation)
- Includes 250GB total storage pool
- Unlimited bandwidth (fair use)

**Total Monthly Cost**: $20 (all-inclusive)

## Related Documentation

- Railway Docs: https://docs.railway.app/
- Railway .NET Guide: https://docs.railway.app/guides/dotnet
- Railway Environment Variables: https://docs.railway.app/develop/variables
- Railway Deployment: https://docs.railway.app/deploy/deployments
