# Story 012: Admin Dashboard - Scraper Job Monitoring

## Overview

Add comprehensive scraper job monitoring to the admin dashboard, providing real-time visibility into scraper performance, success rates, and photo ingestion metrics.

## User Story

**As an** administrator
**I want** to view scraper job history and current status in the admin dashboard
**So that** I can monitor system health, troubleshoot failures, and track photo ingestion progress

## Current State

- Scraper runs as Railway cron job (daily at 2 AM UTC)
- Logs only visible in Railway dashboard (JSON logs)
- No historical visibility of scraper performance
- No way to see scraper health at a glance
- Must query database manually to check scraper state

## Proposed Solution

Create a dedicated "Scraper Monitoring" page in the admin dashboard that displays:

1. **Current Status Panel**
   - Current scraper state per rover
   - Last successful run timestamp
   - Next scheduled run (if cron info available)
   - Quick health indicators (green/yellow/red status)

2. **Recent Job History Table**
   - Last 50 scraper runs
   - Columns: Timestamp, Rover, Status, Photos Added, Duration, Sols Scraped, Errors
   - Color-coded status (success=green, failed=red, partial=yellow)
   - Expandable error details for failed runs
   - Filterable by rover, status, date range

3. **Performance Metrics Cards**
   - Total photos in database per rover
   - Photos added today/this week
   - Average scrape duration
   - Success rate (last 7 days, last 30 days)

4. **Scraper State Details**
   - Last scraped sol per rover
   - Current mission sol (from NASA API)
   - Lookback window size
   - Next expected sol range

## Database Schema

### Existing Tables (Already Available)

```sql
-- Scraper state tracking
CREATE TABLE scraper_states (
    id SERIAL PRIMARY KEY,
    rover_name VARCHAR(50) NOT NULL,
    last_scraped_sol INTEGER NOT NULL,
    last_scrape_timestamp TIMESTAMPTZ NOT NULL,
    last_scrape_status VARCHAR(20) NOT NULL,
    photos_added_last_run INTEGER NOT NULL,
    error_message TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Photos table (for metrics)
CREATE TABLE photos (
    id SERIAL PRIMARY KEY,
    rover_id INTEGER NOT NULL REFERENCES rovers(id),
    sol INTEGER NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    -- ... other columns
);
```

### New Table: Scraper Job History

```sql
CREATE TABLE scraper_job_history (
    id SERIAL PRIMARY KEY,
    job_started_at TIMESTAMPTZ NOT NULL,
    job_completed_at TIMESTAMPTZ,
    total_duration_seconds INTEGER,
    total_rovers_attempted INTEGER NOT NULL,
    total_rovers_succeeded INTEGER NOT NULL,
    total_photos_added INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL, -- 'success', 'failed', 'partial'
    error_summary TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE scraper_job_rover_details (
    id SERIAL PRIMARY KEY,
    job_history_id INTEGER NOT NULL REFERENCES scraper_job_history(id) ON DELETE CASCADE,
    rover_name VARCHAR(50) NOT NULL,
    start_sol INTEGER NOT NULL,
    end_sol INTEGER NOT NULL,
    sols_attempted INTEGER NOT NULL,
    sols_succeeded INTEGER NOT NULL,
    sols_failed INTEGER NOT NULL,
    photos_added INTEGER NOT NULL,
    duration_seconds INTEGER NOT NULL,
    status VARCHAR(20) NOT NULL, -- 'success', 'failed', 'partial'
    error_message TEXT,
    failed_sols TEXT, -- JSON array of failed sol numbers
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_scraper_job_history_started ON scraper_job_history(job_started_at DESC);
CREATE INDEX idx_scraper_job_rover_details_job ON scraper_job_rover_details(job_history_id);
CREATE INDEX idx_scraper_job_rover_details_rover ON scraper_job_rover_details(rover_name);
```

## API Endpoints

### 1. Get Current Scraper Status
```http
GET /api/v1/internal/admin/scraper/status
Authorization: X-Internal-Secret: {shared_secret}

Response 200:
{
  "scrapers": [
    {
      "roverName": "curiosity",
      "lastScrapedSol": 4724,
      "currentMissionSol": 4724,
      "lastRunTimestamp": "2025-11-20T15:57:41Z",
      "lastRunStatus": "success",
      "photosAddedLastRun": 237,
      "errorMessage": null,
      "healthStatus": "healthy", // "healthy", "warning", "error"
      "totalPhotos": 681393
    }
  ],
  "nextScheduledRun": "2025-11-21T02:00:00Z" // Optional
}
```

### 2. Get Recent Job History
```http
GET /api/v1/internal/admin/scraper/history?limit=50&rover=curiosity&status=failed
Authorization: X-Internal-Secret: {shared_secret}

Query Parameters:
- limit: 10-100 (default 50)
- offset: pagination
- rover: filter by rover name (optional)
- status: filter by status (optional)
- startDate: ISO 8601 date (optional)
- endDate: ISO 8601 date (optional)

Response 200:
{
  "jobs": [
    {
      "id": 123,
      "startedAt": "2025-11-20T02:00:00Z",
      "completedAt": "2025-11-20T02:05:45Z",
      "durationSeconds": 345,
      "totalRoversAttempted": 2,
      "totalRoversSucceeded": 2,
      "totalPhotosAdded": 450,
      "status": "success",
      "roverDetails": [
        {
          "roverName": "curiosity",
          "startSol": 4717,
          "endSol": 4724,
          "solsAttempted": 8,
          "solsSucceeded": 8,
          "solsFailed": 0,
          "photosAdded": 237,
          "durationSeconds": 180,
          "status": "success",
          "errorMessage": null,
          "failedSols": []
        },
        {
          "roverName": "perseverance",
          "startSol": 1682,
          "endSol": 1689,
          "solsAttempted": 8,
          "solsSucceeded": 8,
          "solsFailed": 0,
          "photosAdded": 213,
          "durationSeconds": 165,
          "status": "success",
          "errorMessage": null,
          "failedSols": []
        }
      ]
    }
  ],
  "total": 145,
  "limit": 50,
  "offset": 0
}
```

### 3. Get Performance Metrics
```http
GET /api/v1/internal/admin/scraper/metrics?period=7d
Authorization: X-Internal-Secret: {shared_secret}

Query Parameters:
- period: "24h", "7d", "30d" (default "7d")

Response 200:
{
  "period": "7d",
  "metrics": {
    "totalJobs": 7,
    "successfulJobs": 6,
    "failedJobs": 1,
    "successRate": 85.7,
    "totalPhotosAdded": 3145,
    "averageDurationSeconds": 42,
    "roverBreakdown": [
      {
        "roverName": "curiosity",
        "totalPhotos": 681393,
        "photosAddedPeriod": 1650,
        "successfulRuns": 6,
        "failedRuns": 1,
        "averageDurationSeconds": 25
      }
    ]
  }
}
```

## Implementation Steps

### Phase 1: Database & API (C# Backend)

1. **Create EF Core Migrations**
   - Add `ScraperJobHistory` and `ScraperJobRoverDetails` entities
   - Run migration to create tables

2. **Update Scraper to Log Job History**
   - Modify `MarsVista.Scraper/Program.cs` to create job history records
   - Log start time, end time, duration, results per rover
   - Store in database after each run

3. **Create Admin Scraper Controller**
   - `AdminScraperController.cs` with internal auth
   - Implement three endpoints above
   - Query scraper_states, scraper_job_history, photos tables
   - Return formatted JSON responses

4. **Add Repository Layer** (Optional but Recommended)
   - `IScraperJobHistoryRepository`
   - Encapsulate complex queries
   - Easier to test and maintain

### Phase 2: Admin Dashboard UI (Next.js)

1. **Create Scraper Monitoring Page**
   - New route: `/admin/scraper` (protected by auth)
   - Layout with status cards + history table

2. **API Client Functions**
   ```typescript
   // lib/api/scraper-admin.ts
   export async function getScraperStatus(): Promise<ScraperStatus>
   export async function getScraperHistory(params: HistoryParams): Promise<JobHistory>
   export async function getScraperMetrics(period: string): Promise<Metrics>
   ```

3. **React Components**
   ```
   /admin/scraper/page.tsx              # Main page
   /components/admin/ScraperStatusCard.tsx
   /components/admin/ScraperMetricsCard.tsx
   /components/admin/ScraperJobHistoryTable.tsx
   /components/admin/ScraperJobDetailsModal.tsx
   ```

4. **Styling & UX**
   - Use Tailwind for consistent styling
   - Color-coded status badges (green/yellow/red)
   - Loading states with skeleton loaders
   - Error boundaries for failed API calls
   - Auto-refresh every 30 seconds (optional)

### Phase 3: Enhanced Features (Optional)

1. **Real-time Updates**
   - WebSocket or Server-Sent Events for live scraper status
   - Show "Scraper Running" indicator when job in progress

2. **Manual Trigger**
   - Button to manually trigger scraper for specific rover
   - Requires admin API key authentication
   - Shows progress in real-time

3. **Alerting Configuration**
   - Set thresholds for failures (e.g., 3 consecutive failures)
   - Email/webhook notifications
   - Integration with Railway alerts

4. **Sol-Level Drill-Down**
   - Click rover to see per-sol results
   - Show which sols failed and why
   - Link to photos added for specific sol

## UI Mockup (Text Format)

```
┌─────────────────────────────────────────────────────────────┐
│ Scraper Monitoring                                          │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│ │ Curiosity   │ │Perseverance │ │ Opportunity │          │
│ │ ✓ Healthy   │ │ ✓ Healthy   │ │ ✓ Healthy   │          │
│ │ Sol: 4724   │ │ Sol: 1689   │ │ Sol: 5111   │          │
│ │ 2h ago      │ │ 2h ago      │ │ Complete    │          │
│ └─────────────┘ └─────────────┘ └─────────────┘          │
│                                                             │
│ ┌─────────────────────────────────────────────────────┐   │
│ │ Performance Metrics (Last 7 Days)                    │   │
│ │ Total Jobs: 7  Success: 6  Failed: 1  Rate: 85.7%  │   │
│ │ Photos Added: 3,145  Avg Duration: 42s              │   │
│ └─────────────────────────────────────────────────────┘   │
│                                                             │
│ Recent Job History                                          │
│ ┌───────────────────────────────────────────────────────┐ │
│ │ Time       Rover         Status  Photos  Duration     │ │
│ ├───────────────────────────────────────────────────────┤ │
│ │ 2h ago     Curiosity     ✓       237     3m 0s       │ │
│ │ 2h ago     Perseverance  ✓       213     2m 45s      │ │
│ │ 1d ago     Curiosity     ✓       145     2m 15s      │ │
│ │ 1d ago     Perseverance  ✗       0       0s  [View]  │ │
│ └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Success Criteria

### Must Have
- ✅ Display current scraper state for all rovers
- ✅ Show last 50 job runs with success/failure status
- ✅ Display photos added per run
- ✅ Show error messages for failed runs
- ✅ Protected by authentication (admin only)

### Nice to Have
- ✅ Performance metrics (success rate, avg duration)
- ✅ Filterable job history (by rover, status, date)
- ✅ Auto-refresh status every 30 seconds
- ✅ Export job history to CSV

### Future Enhancements
- Real-time job progress updates
- Manual scraper trigger from dashboard
- Email alerts for failures
- Sol-level drill-down details

## Technical Considerations

### Authentication
- Use existing Auth.js session for dashboard access
- Admin role check (add `role` field to User model if needed)
- OR: Simple email allowlist for admin users

### Performance
- Index on `job_started_at DESC` for fast history queries
- Limit history queries to last 3 months (paginated)
- Cache metrics for 5 minutes (reduce DB load)

### Data Retention
- Keep job history for 90 days
- Archive or delete older records
- Add cleanup job (daily)

### Error Handling
- Graceful degradation if scraper data unavailable
- Show "No data available" instead of errors
- Log API failures for debugging

## Testing Checklist

- [ ] C# integration tests for admin endpoints
- [ ] Repository unit tests for complex queries
- [ ] Scraper job history logging works correctly
- [ ] UI displays correct data from API
- [ ] Filtering and pagination work correctly
- [ ] Authentication prevents unauthorized access
- [ ] Error states display gracefully
- [ ] Mobile responsive layout

## Related Stories

- Story 011: Incremental scraper and daily updates (foundation)
- Future: Real-time scraper progress monitoring
- Future: Admin user management and roles

## Notes

- Consider using React Query for data fetching/caching
- ShadcnUI components for consistent admin UI
- May want to add charts (success rate over time) using Recharts
- Job history can grow large - implement pagination early
