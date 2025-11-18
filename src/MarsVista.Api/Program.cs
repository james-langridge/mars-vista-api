using System.Threading.RateLimiting;
using MarsVista.Api.Data;
using MarsVista.Api.Middleware;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

// Configure Serilog before building the application
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(
        new CompactJsonFormatter(),
        "logs/marsvista-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    Log.Information("Starting Mars Vista API");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// Configure Sentry for error tracking (only if DSN is provided)
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrEmpty(sentryDsn))
{
    Log.Information("Configuring Sentry error tracking");
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = builder.Environment.IsProduction() ? 0.1 : 1.0; // 10% sampling in production, 100% in dev
        options.AttachStacktrace = true;
        options.SendDefaultPii = false; // Don't send PII
        options.MaxBreadcrumbs = 50;
        options.Debug = builder.Environment.IsDevelopment();
    });
}
else
{
    Log.Warning("Sentry DSN not configured - error tracking disabled");
}

// Add services to the container.

// Railway provides DATABASE_URL in PostgreSQL URL format, convert to connection string
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse Railway DATABASE_URL format: postgresql://user:pass@host:port/dbname
    var uri = new Uri(databaseUrl);
    var password = uri.UserInfo.Split(':')[1];
    var username = uri.UserInfo.Split(':')[0];
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    // Fall back to appsettings.json for local development
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

// Add DbContext with snake_case naming convention
builder.Services.AddDbContext<MarsVistaDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    )
    .UseSnakeCaseNamingConvention());

// HTTP client for NASA API with resilience policies
builder.Services.AddHttpClient("NASA", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaAPI/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Query services (calculation layer - pure business logic)
builder.Services.AddScoped<IRoverQueryService, RoverQueryService>();
builder.Services.AddScoped<IPhotoQueryService, PhotoQueryService>();

// Scraper services (action layer - side effects)
// Register scrapers by rover name for dynamic resolution
builder.Services.AddKeyedScoped<IScraperService, PerseveranceScraper>("perseverance");
builder.Services.AddKeyedScoped<IScraperService, CuriosityScraper>("curiosity");
builder.Services.AddKeyedScoped<IScraperService, OpportunityScraper>("opportunity");
builder.Services.AddKeyedScoped<IScraperService, SpiritScraper>("spirit");

// Also register non-keyed for IEnumerable<IScraperService> injection
builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
builder.Services.AddScoped<IScraperService, CuriosityScraper>();
builder.Services.AddScoped<IScraperService, OpportunityScraper>();
builder.Services.AddScoped<IScraperService, SpiritScraper>();

// Register PDS index parser (used by MER scrapers: Opportunity and Spirit)
builder.Services.AddScoped<PdsIndexParser>();

// Register database seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Enable CORS for public API access
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add rate limiting to protect against spam/DDoS even without valid API key
// Configure JSON serialization
// Default: snake_case (via JsonPropertyName attributes on DTOs)
// This makes the API backward compatible with the original NASA Mars Photo API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Keep property names as defined in DTOs (respects JsonPropertyName attributes)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        // Get IP address for rate limiting
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ipAddress,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,                          // 100 requests
                Window = TimeSpan.FromMinutes(1),           // per minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0                              // no queueing
            });
    });

    // Customize response when rate limit is exceeded
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too Many Requests",
            message = "Rate limit exceeded. Maximum 100 requests per minute per IP address.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.TotalSeconds
                : 60
        }, cancellationToken);
    };
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Configure OpenAPI to only include public API endpoints (exclude internal/admin endpoints)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Remove Health and Scraper endpoints from public documentation
        var pathsToRemove = document.Paths
            .Where(p => p.Key.StartsWith("/api/Health") || p.Key.StartsWith("/api/Scraper"))
            .Select(p => p.Key)
            .ToList();

        foreach (var path in pathsToRemove)
        {
            document.Paths.Remove(path);
        }

        return Task.CompletedTask;
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MarsVistaDbContext>("database")
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" });

var app = builder.Build();

// Apply pending migrations on startup (both development and production)
// This ensures Railway automatically applies migrations when new code is deployed
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Seed database on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable CORS middleware
app.UseCors();

// Request logging with Serilog (logs HTTP requests with timing)
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());

        // Add correlation ID if present
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// Correlation ID middleware (must come before request logging to be captured)
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();

    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    context.Items["CorrelationId"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});

// JSON format middleware (supports ?format=camelCase for modern JavaScript apps)
app.UseMiddleware<JsonFormatMiddleware>();

// Rate limiting (first defense - stops spam before API key check)
app.UseRateLimiter();

// API key authentication (second defense - protects all endpoints except /health)
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Enhanced health check endpoint with detailed diagnostics
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsJsonAsync(result);
    }
});

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Retry policy with exponential backoff
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx, 408, network failures
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Request failed. Waiting {timespan.TotalSeconds}s before retry {retryCount}...");
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
