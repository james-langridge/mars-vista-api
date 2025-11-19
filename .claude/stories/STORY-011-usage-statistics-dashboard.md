# Story 011: Usage Statistics and Analytics Dashboard

## Status
Planning

## Overview
Add comprehensive usage tracking and analytics dashboard to help users monitor their API consumption, understand usage patterns, and make informed decisions about tier upgrades.

## Context
- **Story 010 Completed**: API key authentication and rate limiting deployed
- **Current State**: Users can generate API keys and make authenticated requests
- **Gap**: Users have no visibility into their usage, consumption patterns, or proximity to rate limits
- **Business Need**: Usage visibility encourages engagement and drives tier upgrades

## Goals
1. Track all authenticated API requests with detailed metadata (endpoint, rover, camera, etc.)
2. Display real-time usage statistics in the dashboard (hourly/daily consumption)
3. Show historical usage trends with graphs and visualizations
4. Highlight popular endpoints and rovers to demonstrate API value
5. Provide upgrade prompts when users approach rate limits
6. Create foundation for future usage-based billing

## Technical Approach

### Three-Layer Architecture (Functional)

**1. Data Layer** (`Models/`):
- `ApiUsageEvent` entity - Raw usage events with full request metadata
- `HourlyUsageStats` entity - Aggregated hourly statistics per user
- `DailyUsageStats` entity - Aggregated daily statistics per user
- EF Core migrations for new tables

**2. Calculation Layer** (`Services/`):
- `IUsageTrackingService`: Track requests, aggregate statistics (pure functions)
- `IUsageAnalyticsService`: Calculate trends, popular endpoints, recommendations
- No side effects, testable business logic

**3. Action Layer** (`Middleware/`, `Controllers/`):
- `UsageTrackingMiddleware`: Capture request data after successful authentication
- `UsageController`: Expose usage statistics via API endpoints
- Database writes and HTTP responses

### Storage Strategy

**Two-Tier Storage Approach:**

1. **Raw Events** (detailed, short retention):
   - Store every API request with full metadata
   - Keep for 30 days for detailed analysis
   - Enables debugging and detailed reporting
   - ~1MB per 1000 requests

2. **Aggregated Statistics** (compact, long retention):
   - Hourly and daily rollups per user
   - Keep forever (minimal storage)
   - Fast queries for dashboard
   - ~1KB per day per user

**Why Both?**
- Raw events enable detailed troubleshooting and analytics
- Aggregates provide fast dashboard queries
- Balance between detail and performance

## Database Schema

### Raw Usage Events Table

```sql
CREATE TABLE api_usage_events (
    id BIGSERIAL PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    api_key_hash VARCHAR(64) NOT NULL,

    -- Request metadata
    endpoint VARCHAR(500) NOT NULL,           -- /api/v1/rovers/curiosity/photos
    http_method VARCHAR(10) NOT NULL,         -- GET, POST
    rover VARCHAR(50),                        -- curiosity, perseverance, etc.
    camera VARCHAR(50),                       -- NAVCAM_LEFT, MAST, etc.
    sol INTEGER,                              -- Sol number if applicable

    -- Response metadata
    status_code INTEGER NOT NULL,             -- 200, 404, 429, etc.
    response_time_ms INTEGER NOT NULL,        -- Response time in milliseconds
    photos_returned INTEGER DEFAULT 0,        -- Number of photos in response

    -- Rate limit context
    tier VARCHAR(20) NOT NULL,                -- free, pro, enterprise
    rate_limit_remaining INTEGER,             -- Requests remaining after this one

    -- Timestamps
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    hour_bucket TIMESTAMP NOT NULL,           -- Truncated to hour for aggregation
    day_bucket DATE NOT NULL                  -- Truncated to day for aggregation
);

-- Indexes for fast queries
CREATE INDEX idx_usage_events_user_created ON api_usage_events(user_email, created_at DESC);
CREATE INDEX idx_usage_events_hour_bucket ON api_usage_events(user_email, hour_bucket);
CREATE INDEX idx_usage_events_day_bucket ON api_usage_events(user_email, day_bucket);
CREATE INDEX idx_usage_events_endpoint ON api_usage_events(endpoint);
CREATE INDEX idx_usage_events_rover ON api_usage_events(rover) WHERE rover IS NOT NULL;

-- Cleanup old events (30 days retention)
CREATE INDEX idx_usage_events_cleanup ON api_usage_events(created_at)
WHERE created_at < CURRENT_TIMESTAMP - INTERVAL '30 days';
```

### Hourly Aggregated Statistics

```sql
CREATE TABLE hourly_usage_stats (
    id BIGSERIAL PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    hour_bucket TIMESTAMP NOT NULL,           -- Start of hour (2025-11-18 14:00:00)
    tier VARCHAR(20) NOT NULL,

    -- Aggregated counts
    total_requests INTEGER NOT NULL DEFAULT 0,
    successful_requests INTEGER NOT NULL DEFAULT 0,  -- 2xx status
    failed_requests INTEGER NOT NULL DEFAULT 0,      -- 4xx, 5xx status
    rate_limited_requests INTEGER NOT NULL DEFAULT 0, -- 429 status

    -- Performance metrics
    avg_response_time_ms INTEGER,
    max_response_time_ms INTEGER,
    total_photos_returned INTEGER DEFAULT 0,

    -- Popular resources
    most_popular_rover VARCHAR(50),           -- Rover queried most this hour
    most_popular_endpoint VARCHAR(500),       -- Endpoint called most this hour

    -- Metadata
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(user_email, hour_bucket)
);

CREATE INDEX idx_hourly_stats_user_hour ON hourly_usage_stats(user_email, hour_bucket DESC);
```

### Daily Aggregated Statistics

```sql
CREATE TABLE daily_usage_stats (
    id BIGSERIAL PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    day_bucket DATE NOT NULL,
    tier VARCHAR(20) NOT NULL,

    -- Aggregated counts
    total_requests INTEGER NOT NULL DEFAULT 0,
    successful_requests INTEGER NOT NULL DEFAULT 0,
    failed_requests INTEGER NOT NULL DEFAULT 0,
    rate_limited_requests INTEGER NOT NULL DEFAULT 0,

    -- Performance metrics
    avg_response_time_ms INTEGER,
    total_photos_returned INTEGER DEFAULT 0,

    -- Popular resources (for the day)
    most_popular_rover VARCHAR(50),
    most_popular_camera VARCHAR(50),
    most_popular_endpoint VARCHAR(500),
    unique_rovers_queried INTEGER DEFAULT 0,  -- How many different rovers
    unique_cameras_queried INTEGER DEFAULT 0, -- How many different cameras

    -- Peak usage
    peak_hour TIMESTAMP,                      -- Hour with most requests
    peak_hour_requests INTEGER,               -- Requests in peak hour

    -- Metadata
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    UNIQUE(user_email, day_bucket)
);

CREATE INDEX idx_daily_stats_user_day ON daily_usage_stats(user_email, day_bucket DESC);
```

### Key Design Decisions

1. **Separate raw events from aggregates** - Balance detail vs performance
2. **Denormalize rover/camera in events** - Avoid joins for analytics queries
3. **Pre-compute hour/day buckets** - Fast aggregation queries
4. **30-day retention on raw events** - Manage storage costs
5. **Keep aggregates forever** - Minimal storage, valuable history

## Implementation Steps

### Phase 1: Backend - Database and Models (Day 1)

#### 1. Create Entity Models

**File**: `src/MarsVista.Api/Models/ApiUsageEvent.cs`

```csharp
public class ApiUsageEvent
{
    public long Id { get; set; }
    public string UserEmail { get; set; } = null!;
    public string ApiKeyHash { get; set; } = null!;

    // Request metadata
    public string Endpoint { get; set; } = null!;
    public string HttpMethod { get; set; } = null!;
    public string? Rover { get; set; }
    public string? Camera { get; set; }
    public int? Sol { get; set; }

    // Response metadata
    public int StatusCode { get; set; }
    public int ResponseTimeMs { get; set; }
    public int PhotosReturned { get; set; }

    // Rate limit context
    public string Tier { get; set; } = null!;
    public int? RateLimitRemaining { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime HourBucket { get; set; }
    public DateTime DayBucket { get; set; }
}
```

**File**: `src/MarsVista.Api/Models/HourlyUsageStats.cs`

```csharp
public class HourlyUsageStats
{
    public long Id { get; set; }
    public string UserEmail { get; set; } = null!;
    public DateTime HourBucket { get; set; }
    public string Tier { get; set; } = null!;

    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitedRequests { get; set; }

    public int? AvgResponseTimeMs { get; set; }
    public int? MaxResponseTimeMs { get; set; }
    public int TotalPhotosReturned { get; set; }

    public string? MostPopularRover { get; set; }
    public string? MostPopularEndpoint { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**File**: `src/MarsVista.Api/Models/DailyUsageStats.cs`

```csharp
public class DailyUsageStats
{
    public long Id { get; set; }
    public string UserEmail { get; set; } = null!;
    public DateTime DayBucket { get; set; }
    public string Tier { get; set; } = null!;

    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitedRequests { get; set; }

    public int? AvgResponseTimeMs { get; set; }
    public int TotalPhotosReturned { get; set; }

    public string? MostPopularRover { get; set; }
    public string? MostPopularCamera { get; set; }
    public string? MostPopularEndpoint { get; set; }
    public int UniqueRoversQueried { get; set; }
    public int UniqueCamerasQueried { get; set; }

    public DateTime? PeakHour { get; set; }
    public int? PeakHourRequests { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 2. Add to DbContext

**File**: `src/MarsVista.Api/Data/MarsVistaDbContext.cs`

```csharp
public DbSet<ApiUsageEvent> UsageEvents { get; set; } = null!;
public DbSet<HourlyUsageStats> HourlyUsageStats { get; set; } = null!;
public DbSet<DailyUsageStats> DailyUsageStats { get; set; } = null!;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configure usage events
    modelBuilder.Entity<ApiUsageEvent>(entity =>
    {
        entity.ToTable("api_usage_events");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserEmail, e.CreatedAt });
        entity.HasIndex(e => new { e.UserEmail, e.HourBucket });
        entity.HasIndex(e => new { e.UserEmail, e.DayBucket });
        entity.HasIndex(e => e.Endpoint);
        entity.HasIndex(e => e.Rover).HasFilter("rover IS NOT NULL");
    });

    // Configure hourly stats
    modelBuilder.Entity<HourlyUsageStats>(entity =>
    {
        entity.ToTable("hourly_usage_stats");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserEmail, e.HourBucket }).IsUnique();
    });

    // Configure daily stats
    modelBuilder.Entity<DailyUsageStats>(entity =>
    {
        entity.ToTable("daily_usage_stats");
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.UserEmail, e.DayBucket }).IsUnique();
    });
}
```

#### 3. Create and Run Migration

```bash
dotnet ef migrations add AddUsageTracking --project src/MarsVista.Api
dotnet ef database update --project src/MarsVista.Api
```

### Phase 2: Backend - Services (Day 1-2)

#### 4. Create Usage Tracking Service Interface

**File**: `src/MarsVista.Api/Services/IUsageTrackingService.cs`

```csharp
public interface IUsageTrackingService
{
    /// <summary>
    /// Track an API request with full metadata
    /// </summary>
    Task TrackRequestAsync(UsageEventData eventData);

    /// <summary>
    /// Get current period usage for a user
    /// </summary>
    Task<CurrentUsageStats> GetCurrentUsageAsync(string userEmail);

    /// <summary>
    /// Get hourly usage for the last N hours
    /// </summary>
    Task<IEnumerable<HourlyUsageStats>> GetHourlyUsageAsync(string userEmail, int hours = 24);

    /// <summary>
    /// Get daily usage for the last N days
    /// </summary>
    Task<IEnumerable<DailyUsageStats>> GetDailyUsageAsync(string userEmail, int days = 30);

    /// <summary>
    /// Get usage summary for dashboard
    /// </summary>
    Task<UsageSummary> GetUsageSummaryAsync(string userEmail);
}

public record UsageEventData(
    string UserEmail,
    string ApiKeyHash,
    string Endpoint,
    string HttpMethod,
    string? Rover,
    string? Camera,
    int? Sol,
    int StatusCode,
    int ResponseTimeMs,
    int PhotosReturned,
    string Tier,
    int? RateLimitRemaining
);

public record CurrentUsageStats(
    int HourlyRequests,
    int HourlyLimit,
    int DailyRequests,
    int DailyLimit,
    string Tier,
    DateTime HourResetAt,
    DateTime DayResetAt
);

public record UsageSummary(
    CurrentUsageStats Current,
    int TotalRequestsLast24Hours,
    int TotalRequestsLast30Days,
    int TotalPhotosRetrieved,
    string? MostPopularRover,
    string? MostPopularEndpoint,
    List<HourlyTrend> HourlyTrends,
    List<DailyTrend> DailyTrends
);

public record HourlyTrend(DateTime Hour, int Requests);
public record DailyTrend(DateTime Day, int Requests);
```

#### 5. Implement Usage Tracking Service

**File**: `src/MarsVista.Api/Services/UsageTrackingService.cs`

```csharp
public class UsageTrackingService : IUsageTrackingService
{
    private readonly MarsVistaDbContext _db;
    private readonly ILogger<UsageTrackingService> _logger;

    public UsageTrackingService(MarsVistaDbContext db, ILogger<UsageTrackingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task TrackRequestAsync(UsageEventData eventData)
    {
        var now = DateTime.UtcNow;
        var hourBucket = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var dayBucket = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        // Store raw event
        var usageEvent = new ApiUsageEvent
        {
            UserEmail = eventData.UserEmail,
            ApiKeyHash = eventData.ApiKeyHash,
            Endpoint = eventData.Endpoint,
            HttpMethod = eventData.HttpMethod,
            Rover = eventData.Rover,
            Camera = eventData.Camera,
            Sol = eventData.Sol,
            StatusCode = eventData.StatusCode,
            ResponseTimeMs = eventData.ResponseTimeMs,
            PhotosReturned = eventData.PhotosReturned,
            Tier = eventData.Tier,
            RateLimitRemaining = eventData.RateLimitRemaining,
            CreatedAt = now,
            HourBucket = hourBucket,
            DayBucket = dayBucket
        };

        _db.UsageEvents.Add(usageEvent);

        // Update hourly aggregates
        await UpdateHourlyStatsAsync(eventData, hourBucket);

        // Update daily aggregates
        await UpdateDailyStatsAsync(eventData, dayBucket);

        await _db.SaveChangesAsync();
    }

    private async Task UpdateHourlyStatsAsync(UsageEventData eventData, DateTime hourBucket)
    {
        var stats = await _db.HourlyUsageStats
            .FirstOrDefaultAsync(s => s.UserEmail == eventData.UserEmail && s.HourBucket == hourBucket);

        if (stats == null)
        {
            stats = new HourlyUsageStats
            {
                UserEmail = eventData.UserEmail,
                HourBucket = hourBucket,
                Tier = eventData.Tier,
                CreatedAt = DateTime.UtcNow
            };
            _db.HourlyUsageStats.Add(stats);
        }

        stats.TotalRequests++;

        if (eventData.StatusCode >= 200 && eventData.StatusCode < 300)
            stats.SuccessfulRequests++;
        else if (eventData.StatusCode == 429)
            stats.RateLimitedRequests++;
        else if (eventData.StatusCode >= 400)
            stats.FailedRequests++;

        stats.TotalPhotosReturned += eventData.PhotosReturned;
        stats.UpdatedAt = DateTime.UtcNow;

        // Update average response time
        stats.AvgResponseTimeMs = stats.AvgResponseTimeMs.HasValue
            ? (stats.AvgResponseTimeMs.Value + eventData.ResponseTimeMs) / 2
            : eventData.ResponseTimeMs;

        // Update max response time
        stats.MaxResponseTimeMs = Math.Max(stats.MaxResponseTimeMs ?? 0, eventData.ResponseTimeMs);

        // Track most popular rover (simple approach - could be more sophisticated)
        if (eventData.Rover != null)
            stats.MostPopularRover = eventData.Rover;

        stats.MostPopularEndpoint = eventData.Endpoint;
    }

    private async Task UpdateDailyStatsAsync(UsageEventData eventData, DateTime dayBucket)
    {
        // Similar logic to UpdateHourlyStatsAsync but for daily aggregates
        // Implementation omitted for brevity - follows same pattern
    }

    public async Task<CurrentUsageStats> GetCurrentUsageAsync(string userEmail)
    {
        var now = DateTime.UtcNow;
        var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        var hourlyStats = await _db.HourlyUsageStats
            .Where(s => s.UserEmail == userEmail && s.HourBucket == hourStart)
            .FirstOrDefaultAsync();

        var dailyStats = await _db.DailyUsageStats
            .Where(s => s.UserEmail == userEmail && s.DayBucket == dayStart)
            .FirstOrDefaultAsync();

        // Get user's tier from API key
        var apiKey = await _db.ApiKeys
            .Where(k => k.UserEmail == userEmail && k.IsActive)
            .FirstOrDefaultAsync();

        var tier = apiKey?.Tier ?? "free";
        var (hourlyLimit, dailyLimit) = GetLimitsForTier(tier);

        return new CurrentUsageStats(
            HourlyRequests: hourlyStats?.TotalRequests ?? 0,
            HourlyLimit: hourlyLimit,
            DailyRequests: dailyStats?.TotalRequests ?? 0,
            DailyLimit: dailyLimit,
            Tier: tier,
            HourResetAt: hourStart.AddHours(1),
            DayResetAt: dayStart.AddDays(1)
        );
    }

    public async Task<UsageSummary> GetUsageSummaryAsync(string userEmail)
    {
        var current = await GetCurrentUsageAsync(userEmail);

        var last24Hours = await GetHourlyUsageAsync(userEmail, 24);
        var last30Days = await GetDailyUsageAsync(userEmail, 30);

        var totalLast24Hours = last24Hours.Sum(h => h.TotalRequests);
        var totalLast30Days = last30Days.Sum(d => d.TotalRequests);
        var totalPhotos = last30Days.Sum(d => d.TotalPhotosReturned);

        var mostPopularRover = last30Days
            .Where(d => d.MostPopularRover != null)
            .GroupBy(d => d.MostPopularRover)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return new UsageSummary(
            Current: current,
            TotalRequestsLast24Hours: totalLast24Hours,
            TotalRequestsLast30Days: totalLast30Days,
            TotalPhotosRetrieved: totalPhotos,
            MostPopularRover: mostPopularRover,
            MostPopularEndpoint: last24Hours.FirstOrDefault()?.MostPopularEndpoint,
            HourlyTrends: last24Hours.Select(h => new HourlyTrend(h.HourBucket, h.TotalRequests)).ToList(),
            DailyTrends: last30Days.Select(d => new DailyTrend(d.DayBucket, d.TotalRequests)).ToList()
        );
    }

    // Helper methods
    private (int hourly, int daily) GetLimitsForTier(string tier) =>
        tier.ToLowerInvariant() switch
        {
            "pro" => (5000, 100000),
            "enterprise" => (100000, -1),
            _ => (60, 500)
        };
}
```

#### 6. Register Service in Program.cs

```csharp
builder.Services.AddScoped<IUsageTrackingService, UsageTrackingService>();
```

### Phase 3: Backend - Middleware and Endpoints (Day 2)

#### 7. Create Usage Tracking Middleware

**File**: `src/MarsVista.Api/Middleware/UsageTrackingMiddleware.cs`

```csharp
public class UsageTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsageTrackingMiddleware> _logger;

    public UsageTrackingMiddleware(RequestDelegate next, ILogger<UsageTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUsageTrackingService usageTracking)
    {
        // Only track authenticated API requests (has user email claim)
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Capture response for metadata
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Track the request
            await TrackRequestAsync(context, stopwatch.ElapsedMilliseconds, usageTracking);

            // Copy response back to original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task TrackRequestAsync(HttpContext context, long responseTimeMs, IUsageTrackingService usageTracking)
    {
        try
        {
            var userEmail = context.User.FindFirst("email")?.Value;
            var apiKeyHash = context.User.FindFirst("api_key_hash")?.Value;
            var tier = context.User.FindFirst("tier")?.Value ?? "free";

            if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(apiKeyHash))
                return;

            // Parse request metadata
            var endpoint = context.Request.Path.Value ?? "";
            var httpMethod = context.Request.Method;
            var rover = ExtractRover(context);
            var camera = context.Request.Query["camera"].FirstOrDefault();
            var sol = TryParseSol(context);

            // Get rate limit remaining from response headers
            var rateLimitRemaining = context.Response.Headers.TryGetValue("X-RateLimit-Remaining", out var remaining)
                ? int.TryParse(remaining, out var val) ? val : null
                : null;

            // Estimate photos returned (simple heuristic)
            var photosReturned = EstimatePhotosReturned(context);

            var eventData = new UsageEventData(
                UserEmail: userEmail,
                ApiKeyHash: apiKeyHash,
                Endpoint: endpoint,
                HttpMethod: httpMethod,
                Rover: rover,
                Camera: camera,
                Sol: sol,
                StatusCode: context.Response.StatusCode,
                ResponseTimeMs: (int)responseTimeMs,
                PhotosReturned: photosReturned,
                Tier: tier,
                RateLimitRemaining: rateLimitRemaining
            );

            await usageTracking.TrackRequestAsync(eventData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track usage for request {Path}", context.Request.Path);
            // Don't fail the request if tracking fails
        }
    }

    private string? ExtractRover(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var match = System.Text.RegularExpressions.Regex.Match(path, @"/rovers/([^/]+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private int? TryParseSol(HttpContext context)
    {
        var solParam = context.Request.Query["sol"].FirstOrDefault();
        return int.TryParse(solParam, out var sol) ? sol : null;
    }

    private int EstimatePhotosReturned(HttpContext context)
    {
        // Simple heuristic: check if response is successful and endpoint is photos
        if (context.Response.StatusCode == 200 && context.Request.Path.Value?.Contains("/photos") == true)
        {
            // Could parse response body here, but that's expensive
            // For now, use a reasonable estimate
            return 25; // Default page size
        }
        return 0;
    }
}
```

#### 8. Create Usage API Controller

**File**: `src/MarsVista.Api/Controllers/UsageController.cs`

```csharp
[ApiController]
[Route("api/v1/usage")]
public class UsageController : ControllerBase
{
    private readonly IUsageTrackingService _usageTracking;

    public UsageController(IUsageTrackingService usageTracking)
    {
        _usageTracking = usageTracking;
    }

    /// <summary>
    /// Get current usage statistics for authenticated user
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentUsage()
    {
        var userEmail = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var stats = await _usageTracking.GetCurrentUsageAsync(userEmail);
        return Ok(stats);
    }

    /// <summary>
    /// Get comprehensive usage summary for dashboard
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetUsageSummary()
    {
        var userEmail = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var summary = await _usageTracking.GetUsageSummaryAsync(userEmail);
        return Ok(summary);
    }

    /// <summary>
    /// Get hourly usage trends
    /// </summary>
    [HttpGet("hourly")]
    public async Task<IActionResult> GetHourlyUsage([FromQuery] int hours = 24)
    {
        var userEmail = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var hourlyUsage = await _usageTracking.GetHourlyUsageAsync(userEmail, hours);
        return Ok(hourlyUsage);
    }

    /// <summary>
    /// Get daily usage trends
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyUsage([FromQuery] int days = 30)
    {
        var userEmail = User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var dailyUsage = await _usageTracking.GetDailyUsageAsync(userEmail, days);
        return Ok(dailyUsage);
    }
}
```

#### 9. Register Middleware in Program.cs

```csharp
// After authentication but before endpoints
app.UseAuthentication();
app.UseMiddleware<UsageTrackingMiddleware>(); // Add this
app.UseAuthorization();
```

### Phase 4: Frontend - Dashboard UI (Day 3)

#### 10. Create Next.js API Route for Usage Stats

**File**: `web/app/api/usage/summary/route.ts`

```typescript
import { auth } from '@/auth';
import { NextResponse } from 'next/server';

export async function GET() {
  const session = await auth();

  if (!session?.user?.email) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  try {
    // Get user's API key to make authenticated request to C# API
    const keysResponse = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/api/v1/internal/keys/current?user_email=${session.user.email}`,
      {
        headers: {
          'X-Internal-Secret': process.env.INTERNAL_API_SECRET!,
        },
      }
    );

    if (!keysResponse.ok) {
      return NextResponse.json({ error: 'No API key found' }, { status: 404 });
    }

    const { api_key_hash } = await keysResponse.json();

    // Fetch usage summary from C# API
    const usageResponse = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/api/v1/usage/summary`,
      {
        headers: {
          'X-API-Key-Hash': api_key_hash, // Use hash to authenticate
        },
      }
    );

    if (!usageResponse.ok) {
      throw new Error('Failed to fetch usage summary');
    }

    const summary = await usageResponse.json();
    return NextResponse.json(summary);
  } catch (error) {
    console.error('Error fetching usage summary:', error);
    return NextResponse.json(
      { error: 'Failed to fetch usage statistics' },
      { status: 500 }
    );
  }
}
```

#### 11. Create Usage Dashboard Component

**File**: `web/app/components/UsageDashboard.tsx`

```typescript
'use client';

import { useEffect, useState } from 'react';
import { Line } from 'react-chartjs-2';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface UsageSummary {
  current: {
    hourlyRequests: number;
    hourlyLimit: number;
    dailyRequests: number;
    dailyLimit: number;
    tier: string;
  };
  totalRequestsLast24Hours: number;
  totalRequestsLast30Days: number;
  totalPhotosRetrieved: number;
  mostPopularRover: string | null;
  hourlyTrends: Array<{ hour: string; requests: number }>;
  dailyTrends: Array<{ day: string; requests: number }>;
}

export default function UsageDashboard() {
  const [summary, setSummary] = useState<UsageSummary | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchUsageSummary();
  }, []);

  async function fetchUsageSummary() {
    try {
      const response = await fetch('/api/usage/summary');
      if (response.ok) {
        const data = await response.json();
        setSummary(data);
      }
    } catch (error) {
      console.error('Failed to fetch usage summary:', error);
    } finally {
      setLoading(false);
    }
  }

  if (loading) {
    return <div className="animate-pulse">Loading usage statistics...</div>;
  }

  if (!summary) {
    return <div>No usage data available</div>;
  }

  const hourlyPercentage = (summary.current.hourlyRequests / summary.current.hourlyLimit) * 100;
  const dailyPercentage = (summary.current.dailyRequests / summary.current.dailyLimit) * 100;

  const hourlyChartData = {
    labels: summary.hourlyTrends.map(t => new Date(t.hour).toLocaleTimeString()),
    datasets: [
      {
        label: 'Requests per Hour',
        data: summary.hourlyTrends.map(t => t.requests),
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.2)',
      },
    ],
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Usage Statistics</h2>

      {/* Current Period Usage */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="border rounded-lg p-4">
          <h3 className="text-lg font-semibold mb-2">Hourly Usage</h3>
          <div className="text-3xl font-bold">
            {summary.current.hourlyRequests} / {summary.current.hourlyLimit}
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2.5 mt-2">
            <div
              className={`h-2.5 rounded-full ${
                hourlyPercentage > 80 ? 'bg-red-600' : 'bg-blue-600'
              }`}
              style={{ width: `${hourlyPercentage}%` }}
            />
          </div>
          <p className="text-sm text-gray-600 mt-2">
            {summary.current.hourlyLimit - summary.current.hourlyRequests} requests remaining this hour
          </p>
        </div>

        <div className="border rounded-lg p-4">
          <h3 className="text-lg font-semibold mb-2">Daily Usage</h3>
          <div className="text-3xl font-bold">
            {summary.current.dailyRequests} / {summary.current.dailyLimit}
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2.5 mt-2">
            <div
              className={`h-2.5 rounded-full ${
                dailyPercentage > 80 ? 'bg-red-600' : 'bg-blue-600'
              }`}
              style={{ width: `${dailyPercentage}%` }}
            />
          </div>
          <p className="text-sm text-gray-600 mt-2">
            {summary.current.dailyLimit - summary.current.dailyRequests} requests remaining today
          </p>
        </div>
      </div>

      {/* Summary Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="border rounded-lg p-4">
          <div className="text-sm text-gray-600">Last 24 Hours</div>
          <div className="text-2xl font-bold">{summary.totalRequestsLast24Hours}</div>
          <div className="text-sm">API Requests</div>
        </div>

        <div className="border rounded-lg p-4">
          <div className="text-sm text-gray-600">Last 30 Days</div>
          <div className="text-2xl font-bold">{summary.totalRequestsLast30Days}</div>
          <div className="text-sm">API Requests</div>
        </div>

        <div className="border rounded-lg p-4">
          <div className="text-sm text-gray-600">Photos Retrieved</div>
          <div className="text-2xl font-bold">{summary.totalPhotosRetrieved.toLocaleString()}</div>
          <div className="text-sm">Total</div>
        </div>
      </div>

      {/* Hourly Trends Chart */}
      <div className="border rounded-lg p-4">
        <h3 className="text-lg font-semibold mb-4">Hourly Trends (Last 24 Hours)</h3>
        <Line data={hourlyChartData} options={{ responsive: true }} />
      </div>

      {/* Popular Resources */}
      {summary.mostPopularRover && (
        <div className="border rounded-lg p-4">
          <h3 className="text-lg font-semibold mb-2">Most Popular Rover</h3>
          <div className="text-2xl font-bold capitalize">{summary.mostPopularRover}</div>
        </div>
      )}

      {/* Upgrade Prompt */}
      {(hourlyPercentage > 70 || dailyPercentage > 70) && summary.current.tier === 'free' && (
        <div className="border-2 border-orange-500 rounded-lg p-4 bg-orange-50">
          <h3 className="text-lg font-semibold mb-2">Approaching Rate Limit</h3>
          <p className="mb-2">
            You're using {Math.max(hourlyPercentage, dailyPercentage).toFixed(0)}% of your {summary.current.tier} tier limits.
          </p>
          <a
            href="/pricing"
            className="inline-block bg-orange-600 text-white px-4 py-2 rounded hover:bg-orange-700"
          >
            Upgrade to Pro
          </a>
        </div>
      )}
    </div>
  );
}
```

#### 12. Add Usage Tab to Dashboard

**File**: `web/app/app/dashboard/page.tsx`

Update to include usage dashboard:

```typescript
import UsageDashboard from '@/components/UsageDashboard';

export default function DashboardPage() {
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold mb-8">Dashboard</h1>

      <div className="space-y-8">
        {/* API Key Section (existing) */}
        <section>
          <ApiKeyManager />
        </section>

        {/* Usage Statistics Section (new) */}
        <section>
          <UsageDashboard />
        </section>
      </div>
    </div>
  );
}
```

### Phase 5: Testing and Deployment (Day 4)

#### 13. Create Unit Tests for Usage Services

**File**: `tests/MarsVista.Api.Tests/Services/UsageTrackingServiceTests.cs`

```csharp
public class UsageTrackingServiceTests
{
    [Fact]
    public async Task TrackRequestAsync_ShouldCreateUsageEvent()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<MarsVistaDbContext>()
            .UseInMemoryDatabase("UsageTest")
            .Options;

        using var context = new MarsVistaDbContext(options);
        var logger = new Mock<ILogger<UsageTrackingService>>();
        var service = new UsageTrackingService(context, logger.Object);

        var eventData = new UsageEventData(
            UserEmail: "test@example.com",
            ApiKeyHash: "hash123",
            Endpoint: "/api/v1/rovers/curiosity/photos",
            HttpMethod: "GET",
            Rover: "curiosity",
            Camera: "NAVCAM",
            Sol: 1000,
            StatusCode: 200,
            ResponseTimeMs: 150,
            PhotosReturned: 25,
            Tier: "free",
            RateLimitRemaining: 59
        );

        // Act
        await service.TrackRequestAsync(eventData);

        // Assert
        var events = await context.UsageEvents.ToListAsync();
        events.Should().HaveCount(1);
        events[0].UserEmail.Should().Be("test@example.com");
        events[0].Rover.Should().Be("curiosity");
        events[0].PhotosReturned.Should().Be(25);
    }

    [Fact]
    public async Task GetCurrentUsageAsync_ShouldReturnCorrectStats()
    {
        // Test implementation
    }

    // More tests...
}
```

#### 14. Manual Testing Checklist

- [ ] Make several authenticated API requests
- [ ] Verify usage events are created in database
- [ ] Check hourly stats are aggregated correctly
- [ ] Check daily stats are aggregated correctly
- [ ] View usage summary in dashboard
- [ ] Verify graphs display correctly
- [ ] Test upgrade prompt appears at 70% usage
- [ ] Verify most popular rover is calculated correctly

#### 15. Deploy to Railway

```bash
# Run migration on production database
railway run dotnet ef database update --project src/MarsVista.Api

# Deploy C# API
railway up

# Deploy Next.js frontend
cd web && npm run build && railway up
```

## Success Criteria

### Backend
- ✅ All API requests tracked with complete metadata
- ✅ Hourly and daily aggregates calculated correctly
- ✅ Usage API endpoints return accurate statistics
- ✅ Performance overhead < 10ms per request
- ✅ No failed requests due to tracking errors

### Frontend
- ✅ Usage dashboard displays current period usage
- ✅ Graphs show hourly and daily trends
- ✅ Upgrade prompts appear when approaching limits
- ✅ Popular rover/endpoint statistics displayed
- ✅ Real-time updates when making API calls

### Data Quality
- ✅ No duplicate tracking events
- ✅ Aggregates match raw events
- ✅ Old events cleaned up (30-day retention)
- ✅ No performance degradation with large datasets

## Testing Checklist

### Unit Tests
- [ ] UsageTrackingService.TrackRequestAsync creates events
- [ ] Hourly aggregation calculates correctly
- [ ] Daily aggregation calculates correctly
- [ ] GetCurrentUsageAsync returns accurate stats
- [ ] GetUsageSummaryAsync includes all required data

### Integration Tests
- [ ] Middleware captures request metadata correctly
- [ ] Usage API endpoints require authentication
- [ ] Dashboard API routes validate session
- [ ] Rate limit headers included in tracking

### Manual Tests
- [ ] Make 10 API requests and verify all tracked
- [ ] Check dashboard shows correct usage
- [ ] Trigger hourly limit and verify tracking
- [ ] Regenerate API key and verify new usage tracked
- [ ] View usage across multiple days

## Future Enhancements (Not in This Story)

- [ ] Export usage data to CSV/JSON
- [ ] Email alerts when approaching rate limits
- [ ] Webhooks for usage milestones
- [ ] Team/organization usage rollups
- [ ] Custom date range selection
- [ ] Compare usage across time periods
- [ ] Usage-based billing integration
- [ ] API endpoint performance analytics
- [ ] Error rate tracking and alerts
- [ ] Geographic usage distribution

## Technical Decisions to Document

**Decision 1: Store Raw Events + Aggregates**
- **Why**: Balance between detail and performance
- **Trade-off**: 2x storage but 10x faster queries
- **30-day retention**: Manage costs while keeping useful history

**Decision 2: Track After Request Completion**
- **Why**: Need response metadata (status, time, photos returned)
- **Trade-off**: Can't track failed middleware, but captures most important data
- **Async tracking**: Don't slow down API responses

**Decision 3: Simple Photo Count Heuristic**
- **Why**: Parsing response body is expensive
- **Trade-off**: Estimate vs exact count
- **Good enough**: 90% accurate for dashboard purposes

**Decision 4: In-Memory Aggregation**
- **Why**: Fast updates, no distributed locking
- **Limitation**: Single-instance only (like rate limiting)
- **Future**: Migrate to background job when scaling

## Estimated Effort
3-4 days for complete implementation and testing

## Dependencies
- Story 010 (API key authentication) deployed and working
