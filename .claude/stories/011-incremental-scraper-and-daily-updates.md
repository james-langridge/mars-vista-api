# Story 011: Incremental Photo Scraper and Daily Update Automation

## Background

Currently, we have bulk scrapers that can import entire rover photo collections (4,000+ sols taking 9-10 hours). This is inefficient for daily updates since:
1. Active rovers (Curiosity, Perseverance) add new photos daily
2. Full rescapes waste bandwidth and time on photos we already have
3. Users expect fresh photos to appear within hours of NASA publishing them
4. Photos can be delayed in transmission, requiring lookback windows

We need an incremental scraper that:
- Tracks the last scraped sol per rover
- Only fetches new photos since last successful scrape
- Runs automatically on a daily schedule
- Handles transmission delays (photos appearing days after sol completion)

## Requirements

### Functional Requirements

1. **Track Last Scrape State**
   - Store last successfully scraped sol per rover in database
   - Store last scrape timestamp
   - Track scrape status (success/failure/in_progress)
   - Survive application restarts

2. **Incremental Scrape Endpoint**
   - `POST /api/scraper/{rover}/incremental` - scrape from last_sol to current
   - Optional `lookbackSols` parameter (default: 7) for delayed photos
   - Use existing bulk scraper logic with calculated sol range
   - Update last_scraped_sol on successful completion

3. **Daily Automated Scraping**
   - Background service running on schedule (configurable interval)
   - Default: Run at 2:00 AM UTC daily
   - Only scrape active rovers (Perseverance, Curiosity)
   - Log all scrape attempts and results
   - Continue on failure (don't crash the service)

4. **Manual Override**
   - Admin can trigger incremental scrape via API
   - Admin can reset last_scraped_sol to force re-scrape
   - `GET /api/scraper/{rover}/status` - view last scrape info

### Non-Functional Requirements

1. **Reliability**
   - Must handle NASA API outages gracefully
   - Must not duplicate photos (idempotent)
   - Must not lose scrape state on crash
   - Must recover from partial failures

2. **Performance**
   - Daily scrapes should complete in <5 minutes (typical: 1-3 sols)
   - Lookback scrapes (7 sols) should complete in <15 minutes
   - Minimal database queries (batch updates)

3. **Observability**
   - Log scrape start/end times
   - Log photos added per sol
   - Log errors with context for debugging
   - Expose metrics via status endpoint

## Technical Approach

### Database Schema Changes

Add new table `scraper_state`:

```sql
CREATE TABLE scraper_state (
    rover_name VARCHAR(50) PRIMARY KEY,
    last_scraped_sol INTEGER NOT NULL,
    last_scrape_timestamp TIMESTAMPTZ NOT NULL,
    last_scrape_status VARCHAR(20) NOT NULL, -- 'success', 'failed', 'in_progress'
    photos_added_last_run INTEGER DEFAULT 0,
    error_message TEXT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Initialize with current max sols
INSERT INTO scraper_state (rover_name, last_scraped_sol, last_scrape_timestamp, last_scrape_status)
VALUES
    ('curiosity', 4683, NOW(), 'success'),
    ('perseverance', 1000, NOW(), 'success');
```

### C# Implementation Components

1. **ScraperState Entity** (Models/ScraperState.cs)
   - EF Core entity for scraper_state table
   - Repository pattern for state management

2. **IncrementalScraperService** (Services/IncrementalScraperService.cs)
   - Calculates sol range: `(last_scraped_sol - lookback, current_sol)`
   - Delegates to existing bulk scraper
   - Updates scraper_state on completion
   - Handles errors and state rollback

3. **DailyScraperBackgroundService** (Services/DailyScraperBackgroundService.cs)
   - Inherits from `BackgroundService`
   - Uses `IHostedService` for startup/shutdown
   - Configurable schedule via appsettings.json
   - Loops through active rovers

4. **ScraperController Updates** (Controllers/ScraperController.cs)
   - `POST /api/scraper/{rover}/incremental?lookbackSols=7`
   - `GET /api/scraper/{rover}/status`
   - `POST /api/scraper/{rover}/reset-state?sol=X`

### Configuration

Add to `appsettings.json`:

```json
{
  "ScraperSchedule": {
    "Enabled": true,
    "IntervalHours": 24,
    "RunAtUtcHour": 2,
    "LookbackSols": 7,
    "ActiveRovers": ["curiosity", "perseverance"]
  }
}
```

### How It Works

**Daily Automation Flow:**
```
2:00 AM UTC - Background service triggers
  ↓
For each active rover:
  1. Query scraper_state for last_scraped_sol
  2. Query NASA API for rover's current max sol
  3. Calculate range: (last_scraped_sol - 7, current_sol)
  4. Call bulk scraper with calculated range
  5. Update scraper_state with results
  ↓
Log summary: "Scraped 2 rovers, added 47 photos, 0 errors"
  ↓
Sleep until next scheduled run
```

**Lookback Window Rationale:**
- NASA photos can arrive 1-7 days after sol completion
- Default 7-sol lookback ensures we catch delayed transmissions
- Idempotent scraper prevents duplicates
- Trade-off: Slightly more API calls vs missing photos

## Implementation Steps

### Step 1: Database Migration
- [ ] Create EF Core migration for `scraper_state` table
- [ ] Run migration locally
- [ ] Seed initial state with current max sols from database
- [ ] Test migration rollback

### Step 2: ScraperState Entity and Repository
- [ ] Create `Models/ScraperState.cs` entity
- [ ] Add `DbSet<ScraperState>` to MarsVistaDbContext
- [ ] Create `IScraperStateRepository` interface
- [ ] Implement repository with Get/Update methods
- [ ] Add repository to DI container

### Step 3: IncrementalScraperService
- [ ] Create `Services/IncrementalScraperService.cs`
- [ ] Implement `ScrapeIncrementalAsync(rover, lookbackSols)` method
- [ ] Query scraper_state for last_scraped_sol
- [ ] Query database for current max sol in photos table
- [ ] Calculate sol range with lookback
- [ ] Delegate to existing BulkScraperService
- [ ] Update scraper_state on success/failure
- [ ] Add comprehensive error handling
- [ ] Add unit tests for sol range calculation

### Step 4: API Endpoints
- [ ] Add `POST /api/scraper/{rover}/incremental` endpoint
- [ ] Add `GET /api/scraper/{rover}/status` endpoint
- [ ] Add `POST /api/scraper/{rover}/reset-state` endpoint (admin only)
- [ ] Add request validation (rover exists, sol ranges)
- [ ] Add response DTOs (ScraperStatusDto)
- [ ] Test endpoints with Postman/curl

### Step 5: Background Service
- [ ] Create `Services/DailyScraperBackgroundService.cs`
- [ ] Inherit from `BackgroundService`
- [ ] Implement `ExecuteAsync` with scheduled loop
- [ ] Read configuration from appsettings.json
- [ ] Calculate next run time (2:00 AM UTC)
- [ ] Loop through active rovers and call incremental scraper
- [ ] Add structured logging (start/end, results, errors)
- [ ] Handle exceptions gracefully (log and continue)
- [ ] Add cancellation token support for graceful shutdown
- [ ] Register service in Program.cs with `AddHostedService`

### Step 6: Configuration
- [ ] Add `ScraperSchedule` section to appsettings.json
- [ ] Add `ScraperSchedule` section to appsettings.Development.json
- [ ] Create strongly-typed configuration class `ScraperScheduleOptions`
- [ ] Bind configuration in Program.cs
- [ ] Add validation for configuration values

### Step 7: Testing
- [ ] Test incremental scrape with small lookback (1-2 sols)
- [ ] Test incremental scrape with no new photos (idempotent)
- [ ] Test incremental scrape with delayed photos (7-sol lookback)
- [ ] Test background service startup/shutdown
- [ ] Test background service error handling (NASA API down)
- [ ] Test manual trigger endpoints
- [ ] Test state persistence across app restarts
- [ ] Load test: Simulate 30 days of automated scraping

### Step 8: Documentation
- [ ] Update `docs/API_ENDPOINTS.md` with new endpoints
- [ ] Create `docs/INCREMENTAL_SCRAPER_GUIDE.md` with usage examples
- [ ] Update `README.md` with daily automation feature
- [ ] Document configuration options in guide
- [ ] Add troubleshooting section (stuck state, missed photos)
- [ ] Update CLAUDE.md with completed story

## Testing Plan

### Unit Tests
- `ScraperStateRepository` - CRUD operations
- `IncrementalScraperService` - Sol range calculation logic
- `IncrementalScraperService` - State update logic
- Configuration binding and validation

### Integration Tests
- Incremental scrape with real NASA API (small range)
- State persistence across database restarts
- Background service execution (reduce interval to 1 minute for testing)
- Concurrent scrapes (should prevent duplicate work)

### Manual Testing Scenarios

1. **First Incremental Scrape**
   ```bash
   # Reset state to 7 days ago
   curl -X POST "http://localhost:5127/api/scraper/curiosity/reset-state?sol=4676"

   # Run incremental scrape
   curl -X POST "http://localhost:5127/api/scraper/curiosity/incremental?lookbackSols=3"

   # Check status
   curl "http://localhost:5127/api/scraper/curiosity/status"
   ```

2. **No New Photos (Idempotent)**
   ```bash
   # Run twice in a row - second should find 0 photos
   curl -X POST "http://localhost:5127/api/scraper/perseverance/incremental"
   curl -X POST "http://localhost:5127/api/scraper/perseverance/incremental"
   ```

3. **Background Service**
   ```bash
   # Set interval to 2 minutes in appsettings.Development.json
   # Start API and watch logs
   dotnet run

   # Should see scheduled scrapes every 2 minutes
   # Verify photos are added to database
   ```

4. **Error Recovery**
   ```bash
   # Simulate NASA API failure (network disconnect)
   # Verify state shows 'failed' status
   # Verify error message is logged
   # Verify service continues running (doesn't crash)
   ```

## Edge Cases and Gotchas

1. **No Photos for Current Sol**
   - Rover hasn't taken photos today
   - NASA hasn't published today's photos yet
   - Solution: This is expected, scraper should succeed with 0 photos added

2. **State Corruption**
   - Database manually modified
   - Migration failed mid-way
   - Solution: Add state validation on service startup, reset to safe defaults if corrupt

3. **Clock Skew**
   - Server time differs from NASA API time
   - Solution: Use UTC everywhere, add ±1 day tolerance

4. **Concurrent Scrapes**
   - Manual trigger while background service running
   - Solution: Add database-level locking or check `last_scrape_status == 'in_progress'`

5. **Rover Max Sol Decreasing**
   - NASA API returns lower max sol than before (data correction)
   - Solution: Log warning, don't update last_scraped_sol

## Success Criteria

- [ ] Incremental scraper can detect and import new photos within 5 minutes
- [ ] Background service runs daily without manual intervention for 7+ days
- [ ] No duplicate photos are created (idempotent)
- [ ] State persists across application restarts
- [ ] Errors are logged and service recovers gracefully
- [ ] API endpoints return correct scraper status
- [ ] Documentation is complete and accurate

## Future Enhancements (Out of Scope)

- Webhook notifications when new photos arrive
- Slack/Discord integration for scrape summaries
- Metrics dashboard (photos per day, scrape latency)
- Multi-instance coordination (distributed locking)
- Backfill missing photos from historical gaps
- Smart lookback windows based on rover activity patterns

## Estimated Complexity

- **Database**: Simple (1 table, straightforward schema)
- **Business Logic**: Medium (state management, scheduling)
- **Testing**: Medium (background service testing, timing edge cases)
- **Overall**: ~4-6 hours of focused implementation

## Dependencies

- Existing bulk scraper service (working)
- EF Core migrations (working)
- appsettings.json configuration (working)
- IHostedService infrastructure (built into .NET)

## Risks

1. **Background service memory leaks** - Use proper disposal patterns, test long-running execution
2. **NASA API rate limiting** - Already have Polly policies, should be fine for daily scraping
3. **Time zone confusion** - Use UTC everywhere, document clearly
4. **State management bugs** - Comprehensive unit tests, add validation

## Notes

- Consider using Quartz.NET for more advanced scheduling (cron expressions), but built-in `BackgroundService` + timer is sufficient for daily execution
- The lookback window (default 7 sols) is conservative and can be tuned based on observed NASA publishing patterns
- Active rover list should be updated if new rovers are added or existing ones go offline
- The background service can be disabled in production via configuration if external cron (e.g., Railway cron jobs, GitHub Actions) is preferred
