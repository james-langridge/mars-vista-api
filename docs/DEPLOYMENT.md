# Deployment Guide

This guide explains how to deploy your own instance of the Mars Vista API.

## Overview

Mars Vista consists of two services:
- **API** - The REST API that serves Mars rover photo data
- **Scraper** - A scheduled job that fetches new photos from NASA

Both services share the same PostgreSQL database. Redis is optional but recommended for production caching.

## Prerequisites

- Docker and Docker Compose (recommended) or .NET 9.0 SDK
- PostgreSQL 15+
- Redis 7+ (optional, for caching)
- Access to NASA's raw image APIs (no key required)

## Deployment Options

### Option 1: Docker Compose (Recommended)

The easiest way to deploy is using Docker Compose:

```bash
# Clone the repository
git clone https://github.com/james-langridge/mars-vista-api.git
cd mars-vista-api

# Copy and configure environment variables
cp .env.example .env
# Edit .env with your settings

# Start all services
docker compose -f docker-compose.production.yml up -d
```

This starts:
- PostgreSQL database
- Redis cache
- Mars Vista API on port 5000

### Option 2: Railway

Railway provides a simple deployment experience with automatic builds:

1. Fork this repository on GitHub
2. Create a new Railway project
3. Add a PostgreSQL database
4. Add a Redis instance
5. Create a new service from your fork:
   - Root directory: `/`
   - Dockerfile path: `Dockerfile`
6. Set environment variables (see [CONFIGURATION.md](CONFIGURATION.md))
7. Deploy

For the scraper, create a second service:
- Dockerfile path: `Dockerfile.scraper`
- Configure as a cron job (e.g., daily at 2 AM UTC: `0 2 * * *`)

### Option 3: Manual Deployment

Build and run the services directly:

```bash
# Build the API
dotnet publish src/MarsVista.Api -c Release -o ./publish/api

# Build the scraper
dotnet publish src/MarsVista.Scraper -c Release -o ./publish/scraper

# Run the API
cd publish/api
DATABASE_URL="your-connection-string" dotnet MarsVista.Api.dll

# Run the scraper (in another terminal or as a cron job)
cd publish/scraper
DATABASE_URL="your-connection-string" dotnet MarsVista.Scraper.dll
```

## Database Setup

### Initial Migration

After starting PostgreSQL, apply database migrations:

```bash
# Using dotnet CLI
dotnet ef database update --project src/MarsVista.Core

# Or via Docker
docker compose exec api dotnet ef database update --project src/MarsVista.Core
```

### Seeding Data

The database is automatically seeded with rover and camera metadata on first run.

To populate photo data, run the scraper or use the admin API:

```bash
# Scrape Perseverance photos for sol 1000
curl -X POST "http://localhost:5127/api/v1/admin/scraper/perseverance?startSol=1000&endSol=1000" \
  -H "X-API-Key: YOUR_ADMIN_KEY"
```

## Scraper Configuration

The scraper runs as a standalone service that:
1. Queries NASA for the current mission sol
2. Fetches photos from recent sols (14-sol lookback by default)
3. Stores new photos in the database

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DATABASE_URL` | PostgreSQL connection string (Railway format) | - |
| `LOOKBACK_SOLS` | Number of sols to look back for new photos | 14 |

Example:
```bash
LOOKBACK_SOLS=7 dotnet run --project src/MarsVista.Scraper
```

### Manual Scraping

Trigger scrapes via admin endpoints:

```bash
# Single sol
curl -X POST "http://localhost:5127/api/v1/admin/scraper/curiosity?startSol=4000&endSol=4000"

# Bulk scrape
curl -X POST "http://localhost:5127/api/v1/admin/scraper/curiosity/bulk?startSol=1&endSol=100"
```

### Scheduled Scraping

Run the scraper as a cron job for automated updates:

```bash
# Cron entry (daily at 2 AM)
0 2 * * * /path/to/scraper/MarsVista.Scraper.dll >> /var/log/marsvista-scraper.log 2>&1
```

## Health Checks

The API exposes a health endpoint:

```bash
curl http://localhost:5127/health
```

Response:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy"
  }
}
```

## Monitoring

### Logs

Both services output structured JSON logs via Serilog. Configure log aggregation (e.g., Seq, Datadog, CloudWatch) via environment variables.

### Metrics

Admin endpoints provide operational metrics:

```bash
# Scraper status
curl "http://localhost:5127/api/v1/admin/scraper/status"

# Database statistics
curl "http://localhost:5127/api/v1/statistics"
```

## Scaling Considerations

### Database

- Create indexes on frequently queried columns (applied automatically via migrations)
- Consider read replicas for high-traffic deployments
- JSONB columns support flexible queries but monitor performance

### Caching

Redis caching significantly reduces database load:
- Rover/camera data: 24-hour TTL
- Active rover manifests: 1-hour TTL
- Inactive rover manifests: 1-year TTL

Without Redis, the API falls back to in-memory caching (not shared across instances).

### API Rate Limiting

Default limits (configurable):
- 10,000 requests/hour
- 100,000 requests/day

Rate limit state is stored in Redis. Without Redis, limits use in-memory storage (reset on restart).

## Troubleshooting

### API won't start

1. Check DATABASE_URL is set correctly
2. Verify PostgreSQL is running and accessible
3. Check migrations have been applied

### Scraper exits immediately

1. Verify DATABASE_URL environment variable
2. Check logs for connection errors
3. Ensure the database has rover seed data

### Redis connection issues

The API degrades gracefully without Redis. Check logs for:
```
Redis unavailable, falling back to memory cache
```

This is acceptable but not recommended for production.

## Next Steps

- [CONFIGURATION.md](CONFIGURATION.md) - Environment variable reference
- [ARCHITECTURE.md](ARCHITECTURE.md) - System design overview
- [API Reference](https://marsvista.dev/docs) - API documentation
