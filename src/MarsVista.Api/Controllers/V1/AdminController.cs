using MarsVista.Core.Data;
using MarsVista.Api.Filters;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace MarsVista.Api.Controllers.V1;

/// <summary>
/// Admin endpoints for system oversight, monitoring, and analytics.
/// Protected by AdminAuthorization filter - requires admin role.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[AdminAuthorization]
public class AdminController : ControllerBase
{
    private readonly MarsVistaDbContext _db;
    private readonly WaypointImportService _waypointImportService;
    private readonly ILogger<AdminController> _logger;
    private static readonly DateTime _processStartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    public AdminController(
        MarsVistaDbContext db,
        WaypointImportService waypointImportService,
        ILogger<AdminController> logger)
    {
        _db = db;
        _waypointImportService = waypointImportService;
        _logger = logger;
    }

    /// <summary>
    /// Get system statistics overview
    /// GET /api/v1/admin/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var totalUsers = await _db.ApiKeys
            .Where(k => k.Role != "admin") // Exclude admin keys from user count
            .Select(k => k.UserEmail)
            .Distinct()
            .CountAsync();

        var activeApiKeys = await _db.ApiKeys
            .Where(k => k.IsActive && k.Role != "admin")
            .CountAsync();

        var totalApiCalls = await _db.UsageEvents.CountAsync();

        var totalPhotos = await _db.UsageEvents.SumAsync(e => (long?)e.PhotosReturned) ?? 0;

        var last24h = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        var last7d = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        var avgResponseTime = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .AverageAsync(e => (int?)e.ResponseTimeMs) ?? 0;

        var uptime = DateTime.UtcNow - _processStartTime;
        var systemUptime = $"{uptime.Days} days, {uptime.Hours} hours";

        return Ok(new
        {
            totalUsers,
            activeApiKeys,
            totalApiCalls,
            totalPhotosRetrieved = totalPhotos,
            apiCallsLast24h = last24h,
            apiCallsLast7d = last7d,
            averageResponseTime = (int)avgResponseTime,
            systemUptime
        });
    }

    /// <summary>
    /// Get list of users with usage statistics
    /// GET /api/v1/admin/users
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var apiKeys = await _db.ApiKeys
            .Where(k => k.Role != "admin") // Exclude admin keys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        var usersList = new List<object>();

        foreach (var key in apiKeys)
        {
            var today = DateTime.UtcNow.Date;
            var hourStart = DateTime.UtcNow.AddHours(-1);

            var totalRequests = await _db.UsageEvents
                .Where(e => e.UserEmail == key.UserEmail)
                .CountAsync();

            var requestsToday = await _db.UsageEvents
                .Where(e => e.UserEmail == key.UserEmail && e.CreatedAt >= today)
                .CountAsync();

            var requestsThisHour = await _db.UsageEvents
                .Where(e => e.UserEmail == key.UserEmail && e.CreatedAt >= hourStart)
                .CountAsync();

            usersList.Add(new
            {
                email = key.UserEmail,
                tier = key.Tier,
                apiKeyCreated = key.CreatedAt,
                lastUsed = key.LastUsedAt,
                totalRequests,
                requestsToday,
                requestsThisHour,
                isActive = key.IsActive
            });
        }

        return Ok(new { users = usersList });
    }

    /// <summary>
    /// Get recent API activity log
    /// GET /api/v1/admin/activity?limit=50
    /// </summary>
    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 50)
    {
        var events = await _db.UsageEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Min(limit, 200)) // Cap at 200
            .Select(e => new
            {
                timestamp = e.CreatedAt,
                userEmail = e.UserEmail,
                endpoint = e.Endpoint,
                statusCode = e.StatusCode,
                responseTime = e.ResponseTimeMs,
                photosReturned = e.PhotosReturned
            })
            .ToListAsync();

        return Ok(new { events });
    }

    /// <summary>
    /// Get rate limit violations
    /// GET /api/v1/admin/rate-limit-violations?limit=50
    /// </summary>
    [HttpGet("rate-limit-violations")]
    public async Task<IActionResult> GetRateLimitViolations([FromQuery] int limit = 50)
    {
        var violations = await _db.UsageEvents
            .Where(e => e.StatusCode == 429)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Min(limit, 200)) // Cap at 200
            .Select(e => new
            {
                timestamp = e.CreatedAt,
                userEmail = e.UserEmail,
                tier = e.Tier,
                violationType = "rate_limit",
                endpoint = e.Endpoint
            })
            .ToListAsync();

        return Ok(new { violations });
    }

    /// <summary>
    /// Get API performance metrics
    /// GET /api/v1/admin/metrics/performance
    /// </summary>
    [HttpGet("metrics/performance")]
    public async Task<IActionResult> GetPerformanceMetrics()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);
        var oneDayAgo = DateTime.UtcNow.AddHours(-24);

        // Get recent events for calculations
        var recentEvents = await _db.UsageEvents
            .Where(e => e.CreatedAt >= oneHourAgo)
            .Select(e => new { e.ResponseTimeMs, e.StatusCode, e.CreatedAt })
            .ToListAsync();

        var responseTimes = recentEvents.Select(e => e.ResponseTimeMs).OrderBy(x => x).ToList();

        var currentMetrics = new
        {
            averageResponseTime = responseTimes.Any() ? (int)responseTimes.Average() : 0,
            p50ResponseTime = responseTimes.Any() ? GetPercentile(responseTimes, 0.50) : 0,
            p95ResponseTime = responseTimes.Any() ? GetPercentile(responseTimes, 0.95) : 0,
            p99ResponseTime = responseTimes.Any() ? GetPercentile(responseTimes, 0.99) : 0,
            requestsPerMinute = recentEvents.Count > 0 ? recentEvents.Count / 60.0 : 0,
            errorRate = recentEvents.Count > 0
                ? (recentEvents.Count(e => e.StatusCode >= 400) / (double)recentEvents.Count) * 100
                : 0,
            successRate = recentEvents.Count > 0
                ? (recentEvents.Count(e => e.StatusCode < 400) / (double)recentEvents.Count) * 100
                : 100
        };

        // Get hourly stats for last 24 hours
        var last24Hours = new List<object>();
        for (int i = 23; i >= 0; i--)
        {
            var hourStart = DateTime.UtcNow.AddHours(-i).Date.AddHours(DateTime.UtcNow.AddHours(-i).Hour);
            var hourEnd = hourStart.AddHours(1);

            var hourEvents = await _db.UsageEvents
                .Where(e => e.CreatedAt >= hourStart && e.CreatedAt < hourEnd)
                .ToListAsync();

            if (hourEvents.Any())
            {
                last24Hours.Add(new
                {
                    hour = hourStart,
                    avgResponseTime = (int)hourEvents.Average(e => e.ResponseTimeMs),
                    requests = hourEvents.Count,
                    errors = hourEvents.Count(e => e.StatusCode >= 400),
                    successRate = (hourEvents.Count(e => e.StatusCode < 400) / (double)hourEvents.Count) * 100
                });
            }
        }

        // Find slow queries (>1000ms)
        var slowQueries = await _db.UsageEvents
            .Where(e => e.ResponseTimeMs > 1000 && e.CreatedAt >= oneDayAgo)
            .GroupBy(e => e.Endpoint)
            .Select(g => new
            {
                endpoint = g.Key,
                avgResponseTime = (int)g.Average(e => e.ResponseTimeMs),
                maxResponseTime = g.Max(e => e.ResponseTimeMs),
                count = g.Count(),
                lastOccurrence = g.Max(e => e.CreatedAt)
            })
            .OrderByDescending(x => x.avgResponseTime)
            .Take(10)
            .ToListAsync();

        return Ok(new
        {
            currentMetrics,
            last24Hours,
            slowQueries
        });
    }

    /// <summary>
    /// Get endpoint usage statistics
    /// GET /api/v1/admin/metrics/endpoints
    /// </summary>
    [HttpGet("metrics/endpoints")]
    public async Task<IActionResult> GetEndpointUsage()
    {
        var oneDayAgo = DateTime.UtcNow.AddHours(-24);

        // Top endpoints overall
        var topEndpoints = await _db.UsageEvents
            .GroupBy(e => e.Endpoint)
            .Select(g => new
            {
                endpoint = g.Key,
                calls = g.Count(),
                avgResponseTime = (int)g.Average(e => e.ResponseTimeMs),
                errorRate = (g.Count(e => e.StatusCode >= 400) / (double)g.Count()) * 100,
                successRate = (g.Count(e => e.StatusCode < 400) / (double)g.Count()) * 100,
                last24h = g.Count(e => e.CreatedAt >= oneDayAgo)
            })
            .OrderByDescending(x => x.calls)
            .Take(20)
            .ToListAsync();

        // Rover usage statistics (extract rover name from endpoint)
        var roverUsage = await _db.UsageEvents
            .Where(e => e.Endpoint.Contains("/rovers/"))
            .ToListAsync();

        var roverStats = roverUsage
            .Select(e =>
            {
                var parts = e.Endpoint.Split('/');
                var roverIndex = Array.IndexOf(parts, "rovers");
                return roverIndex >= 0 && roverIndex + 1 < parts.Length
                    ? parts[roverIndex + 1]
                    : "unknown";
            })
            .Where(r => r != "unknown")
            .GroupBy(r => r)
            .ToDictionary(g => g.Key, g => g.Count());

        // Camera usage statistics (extract from query parameters or endpoint)
        var cameraUsage = new Dictionary<string, int>
        {
            { "ALL", roverUsage.Count }
        };

        return Ok(new
        {
            topEndpoints,
            roverUsage = roverStats,
            cameraUsage
        });
    }

    /// <summary>
    /// Get error tracking data
    /// GET /api/v1/admin/metrics/errors?limit=50
    /// </summary>
    [HttpGet("metrics/errors")]
    public async Task<IActionResult> GetErrors([FromQuery] int limit = 50)
    {
        var oneDayAgo = DateTime.UtcNow.AddHours(-24);

        var errorEvents = await _db.UsageEvents
            .Where(e => e.StatusCode >= 400)
            .ToListAsync();

        var errorSummary = new
        {
            total = errorEvents.Count,
            last24h = errorEvents.Count(e => e.CreatedAt >= oneDayAgo),
            byStatusCode = errorEvents
                .GroupBy(e => e.StatusCode.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };

        var recentErrors = await _db.UsageEvents
            .Where(e => e.StatusCode >= 400)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(e => new
            {
                timestamp = e.CreatedAt,
                userEmail = e.UserEmail,
                endpoint = e.Endpoint,
                statusCode = e.StatusCode,
                errorMessage = GetErrorMessage(e.StatusCode),
                responseTime = e.ResponseTimeMs
            })
            .ToListAsync();

        var errorsByEndpoint = errorEvents
            .GroupBy(e => e.Endpoint)
            .Select(g => new
            {
                endpoint = g.Key,
                errorCount = g.Count(),
                errorRate = 100.0, // Would need total requests per endpoint to calculate properly
                mostCommonError = g.GroupBy(e => e.StatusCode)
                    .OrderByDescending(sg => sg.Count())
                    .First()
                    .Key
                    .ToString()
            })
            .OrderByDescending(x => x.errorCount)
            .Take(10)
            .ToList();

        return Ok(new
        {
            errorSummary,
            recentErrors,
            errorsByEndpoint
        });
    }

    /// <summary>
    /// Get performance trends over time
    /// GET /api/v1/admin/metrics/trends?period=24h
    /// </summary>
    [HttpGet("metrics/trends")]
    public async Task<IActionResult> GetPerformanceTrends([FromQuery] string period = "24h")
    {
        var (startTime, interval) = period switch
        {
            "1h" => (DateTime.UtcNow.AddHours(-1), TimeSpan.FromMinutes(5)),
            "7d" => (DateTime.UtcNow.AddDays(-7), TimeSpan.FromHours(6)),
            "30d" => (DateTime.UtcNow.AddDays(-30), TimeSpan.FromDays(1)),
            _ => (DateTime.UtcNow.AddHours(-24), TimeSpan.FromHours(1))
        };

        var events = await _db.UsageEvents
            .Where(e => e.CreatedAt >= startTime)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();

        var responseTimeTrend = new List<object>();
        var throughputTrend = new List<object>();
        var errorRateTrend = new List<object>();

        var currentTime = startTime;
        while (currentTime < DateTime.UtcNow)
        {
            var windowEnd = currentTime.Add(interval);
            var windowEvents = events.Where(e => e.CreatedAt >= currentTime && e.CreatedAt < windowEnd).ToList();

            if (windowEvents.Any())
            {
                var responseTimes = windowEvents.Select(e => e.ResponseTimeMs).OrderBy(x => x).ToList();

                responseTimeTrend.Add(new
                {
                    timestamp = currentTime,
                    avgMs = (int)responseTimes.Average(),
                    p95Ms = GetPercentile(responseTimes, 0.95)
                });

                throughputTrend.Add(new
                {
                    timestamp = currentTime,
                    requestsPerMinute = windowEvents.Count / interval.TotalMinutes
                });

                errorRateTrend.Add(new
                {
                    timestamp = currentTime,
                    errorRate = (windowEvents.Count(e => e.StatusCode >= 400) / (double)windowEvents.Count) * 100
                });
            }

            currentTime = windowEnd;
        }

        return Ok(new
        {
            responseTimeTrend,
            throughputTrend,
            errorRateTrend
        });
    }

    /// <summary>
    /// Import rover waypoints from NASA PDS localization data
    /// POST /api/v1/admin/waypoints/import/{rover}
    /// </summary>
    /// <remarks>
    /// Imports official NASA waypoint positions from the Planetary Data System.
    /// These coordinates are relative to the landing site and used for accurate traverse calculations.
    /// Source: https://pds-geosciences.wustl.edu/m2020/urn-nasa-pds-mars2020_rover_places/data_localizations/
    /// </remarks>
    [HttpPost("waypoints/import/{rover}")]
    public async Task<IActionResult> ImportWaypoints(string rover)
    {
        try
        {
            var result = await _waypointImportService.ImportWaypointsAsync(rover);
            return Ok(new
            {
                success = true,
                rover = result.Rover,
                totalRows = result.TotalRows,
                imported = result.Imported,
                updated = result.Updated,
                skipped = result.Skipped,
                totalDistanceKm = result.TotalDistanceKm,
                maxSol = result.MaxSol
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import waypoints for {Rover}", rover);
            return StatusCode(500, new { success = false, error = "Failed to import waypoints" });
        }
    }

    /// <summary>
    /// Get waypoint statistics for a rover
    /// GET /api/v1/admin/waypoints/{rover}
    /// </summary>
    [HttpGet("waypoints/{rover}")]
    public async Task<IActionResult> GetWaypointStats(string rover)
    {
        var roverLower = rover.ToLowerInvariant();
        var roverEntity = await _db.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverLower);

        if (roverEntity == null)
        {
            return NotFound(new { error = $"Rover not found: {rover}" });
        }

        var waypoints = await _db.RoverWaypoints
            .Where(w => w.RoverId == roverEntity.Id)
            .OrderBy(w => w.Sol ?? 0)
            .ThenBy(w => w.Site)
            .ThenBy(w => w.Drive ?? 0)
            .ToListAsync();

        if (!waypoints.Any())
        {
            return Ok(new
            {
                rover = roverLower,
                waypointCount = 0,
                message = "No waypoints imported. Use POST /api/v1/admin/waypoints/import/{rover} to import."
            });
        }

        // Calculate total distance
        float totalDistance = 0;
        for (int i = 1; i < waypoints.Count; i++)
        {
            var prev = waypoints[i - 1];
            var curr = waypoints[i];
            totalDistance += MathF.Sqrt(
                MathF.Pow(curr.LandingX - prev.LandingX, 2) +
                MathF.Pow(curr.LandingY - prev.LandingY, 2) +
                MathF.Pow(curr.LandingZ - prev.LandingZ, 2));
        }

        var siteCount = waypoints.Where(w => w.Frame == "SITE").Count();
        var roverCount = waypoints.Where(w => w.Frame == "ROVER").Count();
        var maxSol = waypoints.Where(w => w.Sol.HasValue).Max(w => w.Sol) ?? 0;
        var lastUpdated = waypoints.Max(w => w.UpdatedAt);

        return Ok(new
        {
            rover = roverLower,
            waypointCount = waypoints.Count,
            siteFrames = siteCount,
            roverFrames = roverCount,
            totalDistanceKm = totalDistance / 1000,
            maxSol,
            lastUpdated
        });
    }

    #region Helper Methods

    private static int GetPercentile(List<int> sortedValues, double percentile)
    {
        if (!sortedValues.Any()) return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));
        return sortedValues[index];
    }

    private static string GetErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            429 => "Rate Limit Exceeded",
            500 => "Internal Server Error",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => $"HTTP {statusCode}"
        };
    }

    #endregion
}
