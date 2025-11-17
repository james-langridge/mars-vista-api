using System.Threading.RateLimiting;
using MarsVista.Api.Data;
using MarsVista.Api.Middleware;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddOpenApi();

var app = builder.Build();

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

// JSON format middleware (supports ?format=camelCase for modern JavaScript apps)
app.UseMiddleware<JsonFormatMiddleware>();

// Rate limiting (first defense - stops spam before API key check)
app.UseRateLimiter();

// API key authentication (second defense - protects all endpoints except /health)
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Railway and monitoring
app.MapGet("/health", async (MarsVistaDbContext db) =>
{
    try
    {
        // Verify database connection
        await db.Database.CanConnectAsync();

        var roverCount = await db.Rovers.CountAsync();
        var photoCount = await db.Photos.CountAsync();

        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            database = "connected",
            rovers = roverCount,
            photos = photoCount
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Health check failed",
            detail: ex.Message,
            statusCode: 503
        );
    }
});

app.Run();

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
