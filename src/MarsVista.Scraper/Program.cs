using MarsVista.Core.Data;
using MarsVista.Core.Options;
using MarsVista.Core.Repositories;
using MarsVista.Scraper.Services;
using Microsoft.EntityFrameworkCore;
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

    // Register scrapers (only active rovers)
    builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
    builder.Services.AddScoped<IScraperService, CuriosityScraper>();

    // Register incremental scraper services
    builder.Services.AddScoped<IScraperStateRepository, ScraperStateRepository>();
    builder.Services.AddScoped<IIncrementalScraperService, IncrementalScraperService>();

    // Configure scraper schedule options
    builder.Services.Configure<ScraperScheduleOptions>(
        builder.Configuration.GetSection(ScraperScheduleOptions.SectionName));

    var host = builder.Build();

    // Get configuration
    var config = host.Services.GetRequiredService<IOptions<ScraperScheduleOptions>>().Value;

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
