using MarsVista.Core.Data;
using MarsVista.Core.Options;
using MarsVista.Core.Repositories;
using MarsVista.Scraper.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Formatting.Compact;

// Configure Serilog
var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    // Silence EF Core's per-command logging (matches the API config) so scrape
    // and backfill progress is not buried under a flood of SQL command logs.
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter());

// Error-level events go to Sentry so scrape failures, retention failures, and
// data-quality alarms are actually seen - Railway console logs are not
// actively watched. Without SENTRY_DSN (e.g. local runs) logging is
// console-only, matching the API's opt-in behavior.
var sentryDsn = Environment.GetEnvironmentVariable("SENTRY_DSN");
if (!string.IsNullOrEmpty(sentryDsn))
{
    loggerConfiguration = loggerConfiguration.WriteTo.Sentry(options =>
    {
        options.Dsn = sentryDsn;
        options.MinimumEventLevel = Serilog.Events.LogEventLevel.Error;
        options.MinimumBreadcrumbLevel = Serilog.Events.LogEventLevel.Information;
        options.Environment = Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT_NAME") ?? "production";
    });
}

Log.Logger = loggerConfiguration.CreateLogger();

try
{
    Log.Information("Mars Vista Scraper starting");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog();

    // Parse DATABASE_URL environment variable (Railway format)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    string connectionString;

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        var uri = new Uri(databaseUrl);
        var password = uri.UserInfo.Split(':')[1];
        var username = uri.UserInfo.Split(':')[0];
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        Log.Information("Using Railway DATABASE_URL for database connection");
    }
    else
    {
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
        Log.Information("Using appsettings.json for database connection");
    }

    // Add DbContext
    builder.Services.AddDbContext<MarsVistaDbContext>(options =>
        options.UseNpgsql(connectionString,
            npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
        .UseSnakeCaseNamingConvention());

    // HTTP client for NASA API with resilience policies
    // 45s timeout: Fail fast - if NASA doesn't respond in 45s, retry at sol level
    // Note: Circuit breaker removed - not suitable for batch jobs with sol-level retry
    builder.Services.AddHttpClient("NASA", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(45);
        client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaScraper/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy());

    // Register scrapers (only active rovers)
    builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
    builder.Services.AddScoped<IScraperService, CuriosityScraper>();

    // Register incremental scraper services
    builder.Services.AddScoped<IScraperStateRepository, ScraperStateRepository>();
    builder.Services.AddScoped<ISolCompletenessRepository, SolCompletenessRepository>();
    builder.Services.AddScoped<IIncrementalScraperService, IncrementalScraperService>();

    // Daily maintenance: prune usage_events past the retention window into the
    // monthly rollup. Runs after the scrape as a best-effort step.
    builder.Services.AddScoped<MarsVista.Core.Services.IUsageEventRetentionService,
        MarsVista.Core.Services.UsageEventRetentionService>();

    // Panorama pre-compute services (backfill mode + daily incremental refresh)
    builder.Services.AddScoped<MarsVista.Core.Services.PanoramaDetector>();
    builder.Services.AddScoped<MarsVista.Core.Services.IPanoramaTableBuilder,
        MarsVista.Core.Services.PanoramaTableBuilder>();
    builder.Services.AddScoped<MarsVista.Core.Services.PanoramaBackfillRunner>();

    // Configure scraper schedule options
    builder.Services.Configure<ScraperScheduleOptions>(
        builder.Configuration.GetSection(ScraperScheduleOptions.SectionName));

    var host = builder.Build();

    // Apply pending migrations on startup
    // This ensures the scraper can run independently of API deployment order
    using (var migrationScope = host.Services.CreateScope())
    {
        var dbContext = migrationScope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
        Log.Information("Applying pending database migrations...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations complete");
    }

    // Panorama backfill mode: populate the panoramas table for every existing
    // (rover, sol) and exit without scraping. Run as a one-off job.
    var backfillMode = Environment.GetEnvironmentVariable("PANORAMA_BACKFILL");
    if (string.Equals(backfillMode, "true", StringComparison.OrdinalIgnoreCase))
    {
        List<int>? backfillRoverIds = null;
        var roverIdsEnv = Environment.GetEnvironmentVariable("BACKFILL_ROVER_IDS");
        if (!string.IsNullOrWhiteSpace(roverIdsEnv))
        {
            backfillRoverIds = roverIdsEnv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }

        Log.Information("PANORAMA_BACKFILL=true - running panorama table backfill (rovers: {Rovers})",
            backfillRoverIds != null ? string.Join(", ", backfillRoverIds) : "all");

        using var backfillScope = host.Services.CreateScope();
        var runner = backfillScope.ServiceProvider
            .GetRequiredService<MarsVista.Core.Services.PanoramaBackfillRunner>();
        var summary = await runner.RunAsync(backfillRoverIds);

        Log.Information(
            "Panorama backfill finished: {Processed}/{Total} sols processed, {Panoramas} panoramas written, {Failures} failures",
            summary.Processed, summary.TotalPairs, summary.PanoramasWritten, summary.Failures);
        Environment.ExitCode = summary.Failures == 0 ? 0 : 1;
        return;
    }

    // Get configuration
    var config = host.Services.GetRequiredService<IOptions<ScraperScheduleOptions>>().Value;

    // Override lookback from environment variable if set
    var lookbackEnv = Environment.GetEnvironmentVariable("LOOKBACK_SOLS");
    if (!string.IsNullOrEmpty(lookbackEnv) && int.TryParse(lookbackEnv, out var lookbackSols))
    {
        config.LookbackSols = lookbackSols;
        Log.Information("Using LOOKBACK_SOLS environment variable: {LookbackSols}", lookbackSols);
    }

    var activeRovers = config.ActiveRovers.Count > 0
        ? config.ActiveRovers
        : new List<string> { "perseverance", "curiosity" };

    Log.Information("Scraper configuration: Rovers={Rovers}, LookbackSols={Lookback}",
        string.Join(", ", activeRovers), config.LookbackSols);

    // Run incremental scrape for all active rovers
    using var scope = host.Services.CreateScope();
    var incrementalScraper = scope.ServiceProvider.GetRequiredService<IIncrementalScraperService>();

    var result = await incrementalScraper.ScrapeAllRoversAsync();

    // Summary
    if (result.Success)
    {
        Log.Information(
            "Scraper completed successfully: {Photos} photos added across {Rovers} rovers in {Duration}s",
            result.TotalPhotosAdded, result.RoverResults.Count, result.DurationSeconds);
        Environment.ExitCode = 0;
    }
    else
    {
        var failed = result.RoverResults.Where(r => !r.Success).Select(r => r.RoverName).ToList();
        var succeeded = result.RoverResults.Where(r => r.Success).Select(r => r.RoverName).ToList();

        Log.Warning(
            "Scraper completed with failures: {Photos} photos added, {Succeeded} succeeded, {Failed} failed",
            result.TotalPhotosAdded,
            string.Join(", ", succeeded),
            string.Join(", ", failed));
        Environment.ExitCode = 1; // Non-zero exit code for Railway monitoring
    }

    // Best-effort: refresh the panoramas table for the sols each rover scraped,
    // so new photos are reflected in pre-computed panoramas. Own try/catch so a
    // refresh failure is logged but never changes the scrape's exit code.
    try
    {
        using var panoramaScope = host.Services.CreateScope();
        var panoramaContext = panoramaScope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
        var panoramaBuilder = panoramaScope.ServiceProvider
            .GetRequiredService<MarsVista.Core.Services.IPanoramaTableBuilder>();

        var totalRefreshed = 0;
        foreach (var roverResult in result.RoverResults.Where(r => r.Success))
        {
            var rover = await panoramaContext.Rovers
                .FirstOrDefaultAsync(r => r.Name.ToLower() == roverResult.RoverName.ToLower());
            if (rover == null)
            {
                Log.Warning("Panorama refresh: unknown rover {Rover}, skipping", roverResult.RoverName);
                continue;
            }

            totalRefreshed += await panoramaBuilder.RebuildSolRangeAsync(
                rover.Id, roverResult.StartSol, roverResult.EndSol);
        }

        Log.Information("Panorama refresh complete: {Count} panoramas rebuilt across scraped sols", totalRefreshed);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Panorama refresh failed (scrape result unaffected)");
    }

    // Best-effort daily maintenance. Wrapped so a retention failure is logged
    // but never changes the scrape's exit code - the scrape is the job that
    // Railway monitors, not the cleanup.
    try
    {
        var retentionDays = 90;
        var retentionEnv = Environment.GetEnvironmentVariable("USAGE_EVENTS_RETENTION_DAYS");
        if (!string.IsNullOrEmpty(retentionEnv) && int.TryParse(retentionEnv, out var configuredDays))
        {
            retentionDays = configuredDays;
        }

        using var retentionScope = host.Services.CreateScope();
        var retentionService = retentionScope.ServiceProvider
            .GetRequiredService<MarsVista.Core.Services.IUsageEventRetentionService>();
        var purged = await retentionService.PurgeAndRollUpAsync(retentionDays);
        Log.Information(
            "Usage-event retention complete: purged {Purged} events older than {RetentionDays} days into monthly rollup",
            purged, retentionDays);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Usage-event retention failed (scrape result unaffected)");
    }

    Log.Information("Mars Vista Scraper finished");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Scraper terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

// Retry policy with exponential backoff
// Handles: transient HTTP errors, 429 Too Many Requests, and timeout exceptions
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .Or<TimeoutException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Request failed. Waiting {Seconds}s before retry {RetryCount}...",
                    timespan.TotalSeconds, retryCount);
            });
}

