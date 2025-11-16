# Deployment Guide

Complete guide for deploying the Mars Vista API to Railway.

## Prerequisites

- Railway account (Pro plan recommended)
- Railway CLI installed: `npm install -g @railway/cli`
- Git repository connected to Railway
- Production PostgreSQL database already deployed on Railway

## Environment Variables

Required environment variables in Railway:

| Variable | Value | Notes |
|----------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Enables production settings |
| `DATABASE_URL` | Auto-injected | Linked from PostgreSQL service |

Railway automatically injects `DATABASE_URL` when the API service is linked to the PostgreSQL service.

## Initial Deployment

### 1. Link to Railway Project

```bash
# Navigate to project directory
cd /home/james/git/mars-vista-api

# Link to your Railway project
railway link

# Select your project (calm-bravery)
```

### 2. Set Environment Variables

```bash
# Set production environment
railway variables set ASPNETCORE_ENVIRONMENT=Production

# Verify variables are set
railway variables
```

### 3. Link PostgreSQL Service

The API needs to connect to your existing PostgreSQL database:

```bash
# Link to the Postgres service
railway service link Postgres
```

This automatically injects the `DATABASE_URL` environment variable.

### 4. Deploy the API

**Option A: Deploy from Local (Faster for initial deploy)**

```bash
# Deploy current code
railway up
```

**Option B: Connect GitHub Repository (Recommended for production)**

1. In the Railway dashboard, go to your project
2. Click "New Service" > "GitHub Repo"
3. Select the `mars-vista-api` repository
4. Railway will automatically detect it's a .NET project and build

**Build Configuration** (if needed):
- Root Directory: `/`
- Build Command: `dotnet publish src/MarsVista.Api/MarsVista.Api.csproj -c Release -o /app`
- Start Command: `dotnet /app/MarsVista.Api.dll`

### 5. Get Public URL

```bash
# Get the public URL for your API
railway domain
```

Railway provides a URL like: `https://mars-vista-api-production.up.railway.app`

## Monitoring Deployment

### View Logs

```bash
# Watch deployment logs in real-time
railway logs

# Filter for errors
railway logs --filter "error"

# Filter for database-related logs
railway logs --filter "database"
```

### Check Service Status

```bash
# Check service status
railway status

# Test health endpoint
curl "$(railway domain)/health"
```

Expected health response:
```json
{
  "status": "healthy",
  "timestamp": "2025-11-16T18:30:00Z",
  "database": "connected",
  "rovers": 4,
  "photos": 1977520
}
```

## Updating the API

### Deploy New Code

**If using Railway CLI:**
```bash
# Deploy latest code
railway up
```

**If using GitHub integration:**
```bash
# Push to main branch
git push origin main

# Railway will automatically build and deploy
```

### Monitor the Update

```bash
# Watch deployment progress
railway logs --deployment

# Verify health after deployment
curl "$(railway domain)/health"
```

## Verification Tests

After deployment, run these tests to verify everything works:

```bash
# Set Railway URL for easy testing
RAILWAY_URL=$(railway domain)

# Test 1: Health check
curl "$RAILWAY_URL/health" | jq '.status'
# Expected: "healthy"

# Test 2: Get all rovers
curl "$RAILWAY_URL/api/v1/rovers" | jq '.rovers | length'
# Expected: 4

# Test 3: Query photos
curl "$RAILWAY_URL/api/v1/rovers/perseverance/photos?sol=1&per_page=5" | jq '.photos | length'
# Expected: 5

# Test 4: Pagination
curl "$RAILWAY_URL/api/v1/rovers/curiosity/photos?sol=1000&page=2" | jq '.pagination.page'
# Expected: 2

# Test 5: Camera filter
curl "$RAILWAY_URL/api/v1/rovers/perseverance/photos?sol=1000&camera=NAVCAM_LEFT&per_page=1" | jq '.photos[0].camera.name'
# Expected: "NAVCAM_LEFT"
```

## Troubleshooting

### Database Connection Errors

**Problem**: API can't connect to database

**Solution**:
```bash
# Check DATABASE_URL is set
railway variables | grep DATABASE_URL

# Verify PostgreSQL service is linked
railway service

# Check database is running
railway status
```

### Deployment Fails

**Problem**: Build or deployment fails

**Solution**:
```bash
# Check build logs
railway logs --deployment

# Verify .NET SDK version
railway logs | grep "dotnet"

# Try local build first
dotnet build src/MarsVista.Api/MarsVista.Api.csproj -c Release
```

### Slow Response Times

**Problem**: API responses are slower than expected

**Possible causes**:
- Cold start (Railway may sleep service after inactivity - not on Pro plan)
- Database connection pooling issues
- Complex queries without proper indexes

**Solutions**:
```bash
# Check database connection pooling in logs
railway logs | grep "connection"

# Monitor Railway metrics dashboard
# Visit: https://railway.app/project/[your-project]/metrics

# Consider upgrading Railway plan if consistently slow
```

### CORS Errors

**Problem**: Browser-based requests fail with CORS errors

**Solution**: CORS is enabled in Program.cs for all origins. If you're still seeing errors:
```bash
# Check CORS middleware is running
railway logs | grep -i cors

# Verify Program.cs has:
# - builder.Services.AddCors(...) in service registration
# - app.UseCors() in middleware pipeline
```

## Rollback

If a deployment causes critical issues:

```bash
# View deployment history
railway deployments

# Rollback to previous deployment
railway rollback <deployment-id>
```

The database is independent of the API, so rollback won't affect data.

## Performance Expectations

Based on Railway Pro plan and production database (1.98M photos):

| Endpoint | Expected Response Time | Notes |
|----------|----------------------|-------|
| GET /health | < 100ms | Database count query |
| GET /api/v1/rovers | < 200ms | 4 rovers with stats |
| GET /api/v1/rovers/{name} | < 200ms | Single rover details |
| GET /api/v1/rovers/{name}/photos | < 500ms | Paginated query (25 results) |
| GET /api/v1/rovers/{name}/photos (filtered) | < 800ms | Complex query with filters |

**Note**: First request after idle may be slower due to Railway cold start (~1-2 seconds on Starter plan, not on Pro).

## Security Notes

### Current Limitations
1. **No Authentication**: API is completely open (will be addressed in future story)
2. **No Rate Limiting**: Relies on fair use (will be addressed in future story)
3. **No API Keys**: Anyone can use scraper endpoints (will be addressed in future story)

### Best Practices
- Keep `DATABASE_URL` secret (never commit to git)
- Monitor logs for unusual activity
- Consider enabling Railway's security features
- Plan to implement authentication before sharing publicly

## Cost Estimate

Railway Pro plan: **$20/month**
- Includes API service
- Includes PostgreSQL database
- 10GB database storage allocation
- Unlimited bandwidth (fair use)

**Total Monthly Cost**: $20 (all-inclusive)

## Monitoring

### Railway Dashboard
- URL: https://railway.app/dashboard
- View logs, metrics, deployment history
- Monitor resource usage (CPU, memory, network)
- Check health check status

### Manual Health Checks
```bash
# Check every 5 minutes
watch -n 300 'curl -s $(railway domain)/health | jq .status'
```

### Future Enhancements
- Set up automated monitoring (Uptime Robot, Pingdom)
- Configure alerts for downtime
- Add custom metrics and telemetry

## Related Resources

- [Railway Documentation](https://docs.railway.app/)
- [Railway .NET Guide](https://docs.railway.app/guides/dotnet)
- [Railway Environment Variables](https://docs.railway.app/develop/variables)
- [Railway Deployment](https://docs.railway.app/deploy/deployments)
- [API Endpoints Documentation](./API_ENDPOINTS.md)
- [Database Access Guide](./DATABASE_ACCESS.md)
