# Mars Vista Scraper - Railway Cron Deployment Guide

This guide explains how to deploy the MarsVista.Scraper service to Railway as a cron job for automated daily photo scraping.

## Overview

The scraper is a standalone .NET console application that:
- Runs incrementally to fetch only new photos
- Tracks last scraped sol per rover in the database
- Uses a 7-sol lookback window to handle delayed NASA photo transmissions
- Exits with status code 0 (success) or 1 (failure) for Railway monitoring
- Logs structured JSON output with Serilog

## Architecture

The scraper service is completely decoupled from the API:
- **Direct database access**: Connects to PostgreSQL via DATABASE_URL
- **No API dependency**: Doesn't call API endpoints
- **Shared codebase**: References MarsVista.Api project for shared entities/services
- **Cron-based execution**: Railway runs it on a schedule

## Railway Deployment Steps

### 1. Create New Service in Railway

1. Open your Railway project (mars-vista-api)
2. Click "+ New" and select "Empty Service"
3. Name it "mars-vista-scraper"

### 2. Connect to GitHub Repository

1. In the scraper service settings, click "Connect to GitHub"
2. Select your `mars-vista-api` repository
3. Set the **Root Directory** to: `src/MarsVista.Scraper`
4. Set the **Dockerfile Path** to: `Dockerfile`

### 3. Configure Environment Variables

Add the following environment variables to the scraper service:

```bash
# Database connection (same as your main database)
DATABASE_URL=${{MarsVistaDatabase.DATABASE_URL}}

# Optional: Override lookback window (default: 7)
SCRAPER_LOOKBACK_SOLS=7

# Optional: Override active rovers (default: curiosity,perseverance)
SCRAPER_ACTIVE_ROVERS=curiosity,perseverance
```

**Important**: Use Railway's variable referencing syntax (`${{ServiceName.VARIABLE}}`) to link to your existing PostgreSQL database.

### 4. Configure as Cron Job

1. In the scraper service settings, scroll to **"Service"**
2. Change **"Type"** from "Web Service" to **"Cron Job"**
3. Set the **Cron Schedule**: `0 2 * * *` (runs daily at 2 AM UTC)
4. **Cron Timezone**: Select "UTC"

### 5. Deploy

1. Click "Deploy" or push changes to trigger deployment
2. Railway will build the Docker image
3. The service will run according to the cron schedule

## Cron Schedule Examples

```bash
# Every day at 2 AM UTC
0 2 * * *

# Every day at midnight UTC
0 0 * * *

# Every 6 hours
0 */6 * * *

# Every hour
0 * * * *

# Twice daily (6 AM and 6 PM UTC)
0 6,18 * * *
```

## Monitoring and Logs

### View Execution Logs

1. Go to the scraper service in Railway
2. Click "Logs" tab
3. Logs are structured JSON (Serilog CompactJsonFormatter)

### Success Indicators

Look for these log messages:

```json
{"@mt":"Scraper completed: {Successful}/{Total} rovers succeeded, {Photos} photos added, {Failed} failed","Successful":2,"Total":2,"Photos":450,"Failed":0}
{"@mt":"Mars Vista Scraper finished"}
```

### Failure Detection

Railway monitors exit codes:
- **Exit code 0**: Success (all rovers scraped successfully)
- **Exit code 1**: Failure (one or more rovers failed)

Failed runs will show in Railway's deployment history with a red status.

## Database Configuration

The scraper reads configuration from:

### 1. DATABASE_URL (Railway)

Parsed automatically in Railway:

```csharp
postgresql://user:password@host:port/database
```

### 2. appsettings.json (Local Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password"
  },
  "ScraperSchedule": {
    "LookbackSols": 7,
    "ActiveRovers": ["curiosity", "perseverance"]
  }
}
```

## Incremental Scraping Logic

### State Tracking

The `scraper_states` table tracks progress per rover:

```sql
SELECT * FROM scraper_states;

 id | rover_name     | last_scraped_sol | last_scrape_timestamp | last_scrape_status | photos_added_last_run
----+----------------+------------------+-----------------------+--------------------+----------------------
  1 | curiosity      | 4683            | 2025-11-20 02:00:15   | success            | 245
  2 | perseverance   | 1682            | 2025-11-20 02:00:45   | success            | 436
```

### Sol Range Calculation

For each rover:

```
startSol = max(0, lastScrapedSol - lookbackSols)
endSol = Current mission sol from NASA API (with fallback)

# Example with 7-sol lookback:
# Last scraped: sol 4683
# Current mission sol (from NASA): sol 4723
# Range: 4676 - 4723 (7 sols back + all new sols)
```

**NASA API Query**:
- Curiosity: Queries `/api/v1/raw_image_items/?order=sol desc&per_page=1&condition_1=msl:mission`
- Perseverance: Falls back to hardcoded value (NASA's `/rss/api/` endpoint less reliable for metadata)
- Inactive rovers (Opportunity, Spirit): Use hardcoded max sol values
- If NASA API fails: Falls back to hardcoded values as safety measure

### Why Lookback Window?

NASA doesn't transmit photos immediately. A 7-sol lookback ensures:
- Recently transmitted photos from past sols are captured
- No photos are missed due to transmission delays
- Duplicate detection prevents re-adding existing photos

## Local Testing

Test the scraper locally before deploying:

```bash
cd src/MarsVista.Scraper

# Build
dotnet build

# Run
dotnet run

# Check exit code
echo $?
```

Expected output:

```
{"@t":"2025-11-20T02:00:00Z","@mt":"Mars Vista Scraper starting"}
{"@t":"2025-11-20T02:00:01Z","@mt":"Using appsettings.json for database connection"}
{"@t":"2025-11-20T02:00:01Z","@mt":"Scraper configuration: Rovers={Rovers}, LookbackSols={Lookback}","Rovers":"curiosity, perseverance","Lookback":7}
{"@t":"2025-11-20T02:00:02Z","@mt":"Starting incremental scrape for {Rover} with {Lookback}-sol lookback","Rover":"curiosity","Lookback":7}
...
{"@t":"2025-11-20T02:05:30Z","@mt":"✓ {Rover}: {Photos} photos added (sols {StartSol}-{EndSol}, {Duration}s)","Rover":"curiosity","Photos":245,"StartSol":4676,"EndSol":4684,"Duration":9}
...
{"@t":"2025-11-20T02:10:15Z","@mt":"Scraper completed: {Successful}/{Total} rovers succeeded, {Photos} photos added, {Failed} failed","Successful":2,"Total":2,"Photos":681,"Failed":0}
{"@t":"2025-11-20T02:10:15Z","@mt":"Mars Vista Scraper finished"}
```

## Troubleshooting

### Issue: Scraper fails with exit code 1

**Check logs for error messages:**

```json
{"@mt":"✗ {Rover}: Scrape failed - {Error}","Rover":"curiosity","Error":"Connection refused"}
{"@mt":"Failed rovers: {FailedRovers}","FailedRovers":"curiosity"}
```

**Common causes:**
- Database connection failure (check DATABASE_URL)
- NASA API timeout or downtime
- Network issues in Railway

### Issue: No new photos added

**Expected behavior** if:
- All photos already scraped
- NASA hasn't released new photos
- Lookback window covers already-scraped sols

**Check scraper state:**

```sql
SELECT * FROM scraper_states WHERE rover_name = 'curiosity';
```

### Issue: Duplicate rover entries in config

The configuration shows rovers duplicated:

```json
"Rovers":"curiosity, perseverance, curiosity, perseverance"
```

This is benign - the scraper processes each rover and updates state correctly. The duplication doesn't cause issues due to idempotent scraping.

## Best Practices

1. **Monitor first few runs**: Watch logs for the first week to ensure correct operation
2. **Check database growth**: Monitor photo table size to ensure scraping is working
3. **Set up alerts**: Use Railway's webhook notifications for failed deployments
4. **Keep lookback window at 7 sols**: Balances completeness with efficiency
5. **Run daily at off-peak hours**: 2 AM UTC minimizes NASA API load

## Manual Trigger (Optional)

If you need to run the scraper manually between scheduled runs:

### Option 1: Railway Dashboard

1. Go to scraper service
2. Click "Deployments" tab
3. Click "Redeploy" on latest deployment

### Option 2: Via API (if using incremental endpoints)

The API also exposes incremental scraping endpoints (disabled by default in scraper):

```bash
curl -X POST \
  -H "X-Api-Key: your_admin_key" \
  "https://your-api.railway.app/api/scraper/curiosity/incremental?lookbackSols=7"
```

## Performance Metrics

Expected performance per run:

- **Curiosity**: 0-500 photos, 5-30 seconds
- **Perseverance**: 0-600 photos, 10-180 seconds
- **Total runtime**: 15 seconds - 5 minutes (depending on new photo count)
- **Database writes**: Minimal (only new photos + state updates)
- **NASA API calls**: ~2-20 requests per rover (1 per sol range)

## Next Steps

After deployment:
1. Monitor first run in Railway logs
2. Verify photos appearing in database
3. Check API query endpoints return new photos
4. Set up any additional monitoring/alerting

## Related Documentation

- [API Endpoints](./API_ENDPOINTS.md) - Incremental scraper API endpoints
- [Database Access](./DATABASE_ACCESS.md) - Querying scraper state
- [Story 011](./../.claude/stories/011-incremental-scraper-and-daily-updates.md) - Original implementation spec
