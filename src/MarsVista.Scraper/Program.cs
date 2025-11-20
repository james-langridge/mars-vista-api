using MarsVista.Api.Data;
using MarsVista.Api.Options;
using MarsVista.Api.Repositories;
using MarsVista.Api.Services;
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
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateLogger();

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
    builder.Services.AddHttpClient("NASA", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaScraper/1.0");
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

    // Register scrapers
    builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
    builder.Services.AddScoped<IScraperService, CuriosityScraper>();
    builder.Services.AddScoped<IScraperService, OpportunityScraper>();
    builder.Services.AddScoped<IScraperService, SpiritScraper>();
    builder.Services.AddScoped<PdsIndexParser>();

    // Register incremental scraper services
    builder.Services.AddScoped<IScraperStateRepository, ScraperStateRepository>();
    builder.Services.AddScoped<IIncrementalScraperService, IncrementalScraperService>();

    // Configure scraper schedule options
    builder.Services.Configure<ScraperScheduleOptions>(
        builder.Configuration.GetSection(ScraperScheduleOptions.SectionName));

    var host = builder.Build();

    // Get configuration
    var config = host.Services.GetRequiredService<IOptions<ScraperScheduleOptions>>().Value;

    Log.Information("Scraper configuration: Rovers={Rovers}, LookbackSols={Lookback}",
        string.Join(", ", config.ActiveRovers), config.LookbackSols);

    // Run incremental scrape for each active rover
    using var scope = host.Services.CreateScope();
    var incrementalScraper = scope.ServiceProvider.GetRequiredService<IIncrementalScraperService>();

    var jobStartTime = DateTime.UtcNow;
    var totalPhotos = 0;
    var successfulRovers = 0;
    var failedRovers = new List<string>();

    // Create job history record for this multi-rover scrape run
    var jobHistory = await incrementalScraper.CreateJobHistoryAsync(config.ActiveRovers.Count);
    Log.Information("Created job history {JobId} for multi-rover scrape", jobHistory.Id);

    foreach (var roverName in config.ActiveRovers)
    {
        try
        {
            Log.Information("Starting incremental scrape for {Rover} with {Lookback}-sol lookback",
                roverName, config.LookbackSols);

            var result = await incrementalScraper.ScrapeIncrementalWithJobHistoryAsync(roverName, jobHistory.Id, config.LookbackSols);

            if (result.Success)
            {
                successfulRovers++;
                totalPhotos += result.PhotosAdded;
                Log.Information(
                    "✓ {Rover}: {Photos} photos added (sols {StartSol}-{EndSol}, {Duration}s)",
                    roverName, result.PhotosAdded, result.StartSol, result.EndSol,
                    (int)result.Duration.TotalSeconds);
            }
            else
            {
                failedRovers.Add(roverName);
                Log.Error("✗ {Rover}: Scrape failed - {Error}", roverName, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            failedRovers.Add(roverName);
            Log.Error(ex, "Failed to scrape {Rover}", roverName);
        }
    }

    // Update job history with final results
    jobHistory.JobCompletedAt = DateTime.UtcNow;
    jobHistory.TotalDurationSeconds = (int)(jobHistory.JobCompletedAt.Value - jobStartTime).TotalSeconds;
    jobHistory.TotalRoversSucceeded = successfulRovers;
    jobHistory.TotalPhotosAdded = totalPhotos;
    jobHistory.Status = failedRovers.Count == 0 ? "success" :
                        (successfulRovers > 0 ? "partial" : "failed");
    jobHistory.ErrorSummary = failedRovers.Count > 0
        ? $"Failed rovers: {string.Join(", ", failedRovers)}"
        : null;

    await incrementalScraper.UpdateJobHistoryAsync(jobHistory);

    // Summary
    Log.Information(
        "Scraper completed: {Successful}/{Total} rovers succeeded, {Photos} photos added, {Failed} failed",
        successfulRovers, config.ActiveRovers.Count, totalPhotos, failedRovers.Count);

    if (failedRovers.Count > 0)
    {
        Log.Warning("Failed rovers: {FailedRovers}", string.Join(", ", failedRovers));
        Environment.ExitCode = 1; // Non-zero exit code for Railway monitoring
    }
    else
    {
        Environment.ExitCode = 0; // Success
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
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Request failed. Waiting {Seconds}s before retry {RetryCount}...",
                    timespan.TotalSeconds, retryCount);
            });
}

// Circuit breaker - stop hitting NASA API if it's down
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromMinutes(1));
}
