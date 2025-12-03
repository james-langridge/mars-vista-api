using System.Threading.RateLimiting;
using MarsVista.Core.Data;
using MarsVista.Api.Data;
using MarsVista.Api.Middleware;
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

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

// Add HttpContextAccessor for database query timing
builder.Services.AddHttpContextAccessor();

// Add DbContext with snake_case naming convention and query timing interceptor
builder.Services.AddScoped<QueryTimingInterceptor>();
builder.Services.AddDbContext<MarsVistaDbContext>((serviceProvider, options) =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            // Split queries for better performance with Include()
            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

            // Command timeout: 120s for migrations (computed columns on 630K+ photos), 30s for queries
            // Migrations run at startup, so longer timeout is acceptable
            npgsqlOptions.CommandTimeout(120);

            // Batch multiple operations
            npgsqlOptions.MaxBatchSize(100);
        })
    .UseSnakeCaseNamingConvention()
    // Default to no-tracking for read queries (60% faster)
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    // Disable in production for performance
    .EnableSensitiveDataLogging(false)
    .EnableDetailedErrors(false)
    .AddInterceptors(serviceProvider.GetRequiredService<QueryTimingInterceptor>()));

// Redis configuration for two-level caching
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_URL")
    ?? builder.Configuration.GetConnectionString("Redis");

IConnectionMultiplexer? redisMultiplexer = null;

if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        // Parse Railway REDIS_URL format if needed: redis://default:password@host:port
        var redisOptions = redisConnectionString;
        if (redisConnectionString.StartsWith("redis://"))
        {
            var uri = new Uri(redisConnectionString);
            var password = uri.UserInfo.Contains(':') ? uri.UserInfo.Split(':')[1] : uri.UserInfo;
            redisOptions = $"{uri.Host}:{uri.Port},password={password},ssl=false,abortConnect=false,connectTimeout=5000,syncTimeout=5000";
        }

        var configuration = ConfigurationOptions.Parse(redisOptions);
        configuration.AbortOnConnectFail = false; // Graceful degradation
        configuration.ConnectTimeout = 5000; // 5 second timeout
        configuration.SyncTimeout = 5000;

        redisMultiplexer = ConnectionMultiplexer.Connect(configuration);
        Log.Information("Redis connected: {IsConnected}", redisMultiplexer.IsConnected);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis connection failed - falling back to memory-only caching");
        redisMultiplexer = null;
    }
}
else
{
    Log.Warning("Redis not configured - using memory-only caching");
}

// Register Redis multiplexer (nullable for graceful degradation)
builder.Services.AddSingleton<IConnectionMultiplexer?>(redisMultiplexer);

// HTTP client for NASA API with resilience policies
builder.Services.AddHttpClient("NASA", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "MarsVistaAPI/1.0");
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Query services (calculation layer - pure business logic)
// v1 services
builder.Services.AddScoped<IRoverQueryService, RoverQueryService>();
builder.Services.AddScoped<IPhotoQueryService, PhotoQueryService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// v2 services
builder.Services.AddScoped<MarsVista.Api.Services.V2.IPhotoQueryServiceV2, MarsVista.Api.Services.V2.PhotoQueryServiceV2>();
builder.Services.AddScoped<MarsVista.Api.Services.V2.IRoverQueryServiceV2, MarsVista.Api.Services.V2.RoverQueryServiceV2>();
builder.Services.AddSingleton<MarsVista.Api.Services.V2.ICachingServiceV2, MarsVista.Api.Services.V2.CachingServiceV2>();

// Cache warming and metrics services
builder.Services.Configure<MarsVista.Api.Services.V2.CacheWarmingOptions>(
    builder.Configuration.GetSection(MarsVista.Api.Services.V2.CacheWarmingOptions.SectionName));
builder.Services.AddScoped<MarsVista.Api.Services.V2.ICacheWarmingService, MarsVista.Api.Services.V2.CacheWarmingService>();
builder.Services.AddHostedService<MarsVista.Api.Services.V2.CacheWarmingBackgroundService>();
builder.Services.AddHostedService<MarsVista.Api.Services.V2.CacheStatsLoggingService>();

// v2 Phase 2 advanced features services
builder.Services.AddScoped<MarsVista.Api.Services.V2.IPanoramaService, MarsVista.Api.Services.V2.PanoramaService>();
builder.Services.AddScoped<MarsVista.Api.Services.V2.ILocationService, MarsVista.Api.Services.V2.LocationService>();
builder.Services.AddScoped<MarsVista.Api.Services.V2.IJourneyService, MarsVista.Api.Services.V2.JourneyService>();
builder.Services.AddScoped<MarsVista.Api.Services.V2.ITraverseService, MarsVista.Api.Services.V2.TraverseService>();

// API key and rate limiting services
builder.Services.AddMemoryCache(); // Required for rate limiting fallback and L1 cache
builder.Services.AddSingleton<IApiKeyService, ApiKeyService>();
builder.Services.AddSingleton<IRateLimitService, RedisRateLimitService>();

// Response compression for 30-50% payload size reduction
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable for HTTPS (important!)
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/json" });
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

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

// Configure Swashbuckle for enhanced OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mars Vista API v1",
        Version = "v1.0.0",
        Description = "Drop-in replacement for the archived original NASA Mars Rover API.",
        Contact = new OpenApiContact
        {
            Name = "Mars Vista API",
            Url = new Uri("https://marsvista.dev")
        }
    });

    options.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "Mars Vista API v2",
        Version = "v2.0.0",
        Description = "Modern REST API for Mars Rover Photos with powerful filtering, field selection, HTTP caching, and comprehensive error handling.",
        Contact = new OpenApiContact
        {
            Name = "Mars Vista API",
            Url = new Uri("https://marsvista.dev")
        }
    });

    // Group by API version tags
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (docName == "v1")
        {
            // Include v1 endpoints and exclude internal/admin/statistics endpoints from public docs
            return apiDesc.RelativePath?.StartsWith("api/v1/") == true &&
                   !apiDesc.RelativePath.Contains("/internal/") &&
                   !apiDesc.RelativePath.Contains("/admin/") &&
                   !apiDesc.RelativePath.Contains("/statistics");
        }
        else if (docName == "v2")
        {
            // Include v2 endpoints
            return apiDesc.RelativePath?.StartsWith("api/v2/") == true;
        }
        return false;
    });

    // Add XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add API key authentication scheme
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Description = "API key authentication. Get your key from the dashboard after signing in."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add health checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<MarsVistaDbContext>("database")
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" });

// Add Redis health check if configured
if (redisMultiplexer != null)
{
    healthChecksBuilder.AddRedis(
        redisMultiplexer,
        name: "redis",
        failureStatus: HealthStatus.Degraded, // Degraded, not Unhealthy - app works without Redis
        tags: new[] { "cache", "redis" });
}

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
// Enable Swagger in both development and production for API documentation
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "Mars Vista API v2");
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mars Vista API v1");
    options.RoutePrefix = "swagger"; // Swagger UI at /swagger
    options.DocumentTitle = "Mars Vista API Documentation";
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
});

app.UseHttpsRedirection();

// Response compression (MUST come before UseCors and other middleware)
app.UseResponseCompression();

// Enable CORS middleware
app.UseCors();

// Response timing middleware (must come early to measure full pipeline processing time)
app.UseMiddleware<ResponseTimingMiddleware>();

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

// Internal API authentication (protects /api/v1/internal/* endpoints)
app.UseMiddleware<InternalApiMiddleware>();

// Simple API key authentication for scraper endpoints (admin access)
app.UseMiddleware<ApiKeyMiddleware>();

// User API key authentication (protects /api/v1/* public endpoints)
app.UseMiddleware<UserApiKeyAuthenticationMiddleware>();

// Usage tracking for admin dashboard analytics
app.UseMiddleware<UsageTrackingMiddleware>();

app.UseAuthorization();

app.MapControllers();

// Enhanced health check endpoint with detailed diagnostics
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        // Get cache stats from the caching service
        var cachingService = context.RequestServices.GetService<MarsVista.Api.Services.V2.ICachingServiceV2>();
        var cacheStats = cachingService?.GetCacheStats();

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
            }),
            cache = cacheStats == null ? null : new
            {
                l1_hits = cacheStats.L1Hits,
                l2_hits = cacheStats.L2Hits,
                misses = cacheStats.Misses,
                hit_rate = cacheStats.HitRateFormatted,
                sets = cacheStats.Sets,
                invalidations = cacheStats.Invalidations,
                redis_connected = cachingService?.IsRedisConnected ?? false
            }
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
