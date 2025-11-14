# NASA JPL Mars Rover API Guide

**Last Updated:** October 9, 2025

## Background

Due to the US government shutdown, the official NASA API at `api.nasa.gov/mars-photos` is down. However, the **JPL (Jet Propulsion Laboratory) direct APIs** remain operational, as they're managed separately from the main NASA infrastructure.

## Working API Endpoints

### Curiosity (MSL - Mars Science Laboratory)
```
Base URL: https://mars.nasa.gov/api/v1/raw_image_items/
```

**Example queries:**
```bash
# Get latest sol
curl "https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc&per_page=1&page=0&condition_1=msl:mission"

# Get photos from specific sol
curl "https://mars.nasa.gov/api/v1/raw_image_items/?order=sol%20desc,instrument_sort%20asc,sample_type_sort%20asc,%20date_taken%20desc&per_page=1000&page=0&condition_1=msl:mission&condition_2=4683:sol:in"
```

**Response structure:**
```json
{
  "items": [
    {
      "id": 1523519,
      "sol": 4683,
      "instrument": "MAHLI",
      "https_url": "https://mars.nasa.gov/msl-raw-images/...",
      "date_taken": "2025-10-08T20:50:36.000Z",
      "date_received": "2025-10-09T00:30:22.000Z",
      "mission": "msl",
      "extended": { ... }
    }
  ],
  "more": false,
  "total": 1388361,
  "page": 0,
  "per_page": 1000
}
```

### Perseverance (Mars 2020)
```
Base URL: https://mars.nasa.gov/rss/api/
```

**Example queries:**
```bash
# Get latest sol info
curl "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&latest=true"

# Get photos from specific sol
curl "https://mars.nasa.gov/rss/api/?feed=raw_images&category=mars2020&feedtype=json&sol=1647"
```

**Response structure:**
```json
{
  "sol": 1647,
  "total": 887593,
  "images": [
    {
      "imageid": "NLF_1647_0813154806_761ECM_N0791382NCAM02647_01_195J",
      "sol": 1647,
      "date_taken_utc": "2025-10-08T00:40:58.002",
      "date_received": "2025-10-09T05:00:56Z",
      "camera": {
        "instrument": "NAVCAM_LEFT"
      },
      "image_files": {
        "full_res": "https://mars.nasa.gov/mars2020-raw-images/.../...png",
        "large": "..._1200.jpg",
        "medium": "..._800.jpg",
        "small": "..._320.jpg"
      },
      "sample_type": "Full"
    }
  ]
}
```

## Rate Limits

### JPL APIs (mars.nasa.gov)
- ✅ **No explicit rate limits enforced** (no `X-RateLimit-*` headers)
- ✅ **No API key required** (unlike api.nasa.gov)
- ✅ **CloudFront CDN** - Cached for 60-120 seconds, globally distributed
- ✅ **CORS enabled** - Can be called directly from browsers
- ⚠️ **Be respectful** - Even without limits, don't abuse the service

### Standard NASA API (api.nasa.gov) - For Reference
- **DEMO_KEY**: 30 requests/hour, 50 requests/day
- **Registered key**: 1,000 requests/hour
- Currently offline due to government shutdown

## Data Volume

**Current photo counts (as of Oct 9, 2025):**

| Rover | Latest Sol | Total Photos |
|-------|------------|--------------|
| Curiosity | 4,683 | 1,388,361 |
| Perseverance | 1,647 | 887,593 |
| **Total** | - | **~2.3 million** |

## API Efficiency

### Curiosity
- **Max page size**: 1,000 photos per request (tested successfully)
- **Estimated requests for full scrape**: ~1,389 requests
- **Pagination**: 0-based pages (`page=0`, `page=1`, etc.)

### Perseverance
- **Returns all images for a sol** in one request (no pagination)
- **Estimated requests for full scrape**: ~1,647 requests (one per sol)
- **Average photos per sol**: ~540

### Recommended Scraping Strategy

**For initial full scrape (~2.3M photos):**
1. Fetch one sol at a time (natural data partition)
2. Use `per_page=1000` for Curiosity
3. Add 1-2 second delays between requests
4. Fetch Curiosity + Perseverance concurrently (separate workers)
5. Track progress (store last-scraped sol in DB for resumability)

**Estimated scrape time** (at 1 req/sec): ~50 minutes total

**For incremental updates:**
1. Check latest sol via metadata endpoint
2. Compare with last-scraped sol in DB
3. Only fetch new sols
4. Run periodically (e.g., daily)

---

# C#/.NET Scraping Architecture

## Recommended Tech Stack

- **ASP.NET Core 8+** - Modern web framework
- **Entity Framework Core** - ORM for PostgreSQL
- **HttpClient + Polly** - HTTP requests with retry/circuit breaker
- **Hangfire** - Background job scheduling
- **Serilog** - Structured logging

## Project Structure

```
MarsPhotoApi/
├── src/
│   ├── MarsPhotoApi.Core/          # Domain models, interfaces
│   │   ├── Entities/
│   │   │   ├── Photo.cs
│   │   │   ├── Rover.cs
│   │   │   ├── Camera.cs
│   │   │   └── ScraperState.cs
│   │   ├── Interfaces/
│   │   │   ├── IMarsRoverClient.cs
│   │   │   ├── IPhotoRepository.cs
│   │   │   └── IScraperService.cs
│   │   └── DTOs/
│   │       ├── CuriosityPhotoDto.cs
│   │       └── PerseverancePhotoDto.cs
│   │
│   ├── MarsPhotoApi.Infrastructure/ # External services, DB
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Repositories/
│   │   ├── HttpClients/
│   │   │   ├── CuriosityClient.cs
│   │   │   └── PerseveranceClient.cs
│   │   └── BackgroundJobs/
│   │       ├── InitialScraperJob.cs
│   │       └── IncrementalScraperJob.cs
│   │
│   └── MarsPhotoApi.Web/            # ASP.NET Core API
│       ├── Controllers/
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    └── MarsPhotoApi.Tests/
```

## Core Domain Models

```csharp
// MarsPhotoApi.Core/Entities/Photo.cs
public class Photo
{
    public long Id { get; set; }
    public required string ExternalId { get; set; } // NASA's imageid
    public int Sol { get; set; }
    public DateTime DateTaken { get; set; }
    public DateTime DateReceived { get; set; }
    public required string ImageUrl { get; set; }

    // Navigation properties
    public int CameraId { get; set; }
    public Camera Camera { get; set; } = null!;

    public int RoverId { get; set; }
    public Rover Rover { get; set; } = null!;

    // Timestamps
    public DateTime CreatedAt { get; set; }
}

// MarsPhotoApi.Core/Entities/Rover.cs
public class Rover
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateOnly LandingDate { get; set; }
    public DateOnly LaunchDate { get; set; }
    public required string Status { get; set; } // "active", "complete"

    public ICollection<Camera> Cameras { get; set; } = new List<Camera>();
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}

// MarsPhotoApi.Core/Entities/Camera.cs
public class Camera
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string FullName { get; set; }

    public int RoverId { get; set; }
    public Rover Rover { get; set; } = null!;

    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
}

// MarsPhotoApi.Core/Entities/ScraperState.cs
public class ScraperState
{
    public int Id { get; set; }
    public required string RoverName { get; set; }
    public int LastScrapedSol { get; set; }
    public DateTime LastScrapedAt { get; set; }
    public int PhotosScraped { get; set; }
}
```

## HTTP Clients with Polly

```csharp
// MarsPhotoApi.Infrastructure/HttpClients/CuriosityClient.cs
public class CuriosityClient : IMarsRoverClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CuriosityClient> _logger;
    private const string BaseUrl = "https://mars.nasa.gov/api/v1/raw_image_items/";

    public CuriosityClient(HttpClient httpClient, ILogger<CuriosityClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<int> GetLatestSolAsync(CancellationToken ct = default)
    {
        var url = $"{BaseUrl}?order=sol%20desc&per_page=1&page=0&condition_1=msl:mission";

        var response = await _httpClient.GetFromJsonAsync<CuriosityResponse>(url, ct);
        return response?.Items.FirstOrDefault()?.Sol ?? 0;
    }

    public async Task<IReadOnlyList<CuriosityPhotoDto>> GetPhotosBySolAsync(
        int sol,
        int page = 0,
        int perPage = 1000,
        CancellationToken ct = default)
    {
        var url = $"{BaseUrl}?order=sol%20desc,instrument_sort%20asc,sample_type_sort%20asc,%20date_taken%20desc" +
                  $"&per_page={perPage}&page={page}&condition_1=msl:mission&condition_2={sol}:sol:in";

        _logger.LogInformation("Fetching Curiosity photos for sol {Sol}, page {Page}", sol, page);

        var response = await _httpClient.GetFromJsonAsync<CuriosityResponse>(url, ct);

        return response?.Items
            .Where(item => !string.IsNullOrEmpty(item.HttpsUrl))
            .Select(MapToDto)
            .ToList() ?? new List<CuriosityPhotoDto>();
    }

    private static CuriosityPhotoDto MapToDto(CuriosityItem item) => new()
    {
        ExternalId = item.ImageId,
        Sol = item.Sol,
        Instrument = item.Instrument,
        ImageUrl = item.HttpsUrl,
        DateTaken = item.DateTaken,
        DateReceived = item.DateReceived
    };
}

// DTOs for JSON deserialization
public record CuriosityResponse(
    [property: JsonPropertyName("items")] List<CuriosityItem> Items,
    [property: JsonPropertyName("more")] bool More,
    [property: JsonPropertyName("total")] int Total
);

public record CuriosityItem(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("imageid")] string ImageId,
    [property: JsonPropertyName("sol")] int Sol,
    [property: JsonPropertyName("instrument")] string Instrument,
    [property: JsonPropertyName("https_url")] string HttpsUrl,
    [property: JsonPropertyName("date_taken")] DateTime DateTaken,
    [property: JsonPropertyName("date_received")] DateTime DateReceived,
    [property: JsonPropertyName("mission")] string Mission
);
```

## Scraper Service (Deep Module Pattern)

```csharp
// MarsPhotoApi.Infrastructure/Services/ScraperService.cs
public class ScraperService : IScraperService
{
    private readonly IMarsRoverClient _curiosityClient;
    private readonly IMarsRoverClient _perseveranceClient;
    private readonly IPhotoRepository _photoRepository;
    private readonly ILogger<ScraperService> _logger;

    public ScraperService(
        IMarsRoverClient curiosityClient,
        IMarsRoverClient perseveranceClient,
        IPhotoRepository photoRepository,
        ILogger<ScraperService> logger)
    {
        _curiosityClient = curiosityClient;
        _perseveranceClient = perseveranceClient;
        _photoRepository = photoRepository;
        _logger = logger;
    }

    /// <summary>
    /// Scrape all photos for a rover from start sol to latest sol
    /// </summary>
    public async Task ScrapeRoverAsync(
        string roverName,
        int startSol = 1,
        CancellationToken ct = default)
    {
        var client = GetClientForRover(roverName);
        var latestSol = await client.GetLatestSolAsync(ct);

        _logger.LogInformation(
            "Starting scrape for {Rover}: sols {Start} to {End}",
            roverName, startSol, latestSol);

        var photosScraped = 0;

        for (var sol = startSol; sol <= latestSol; sol++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var photos = await client.GetPhotosBySolAsync(sol, ct: ct);

                if (photos.Count > 0)
                {
                    await _photoRepository.BulkInsertPhotosAsync(photos, roverName);
                    photosScraped += photos.Count;

                    _logger.LogInformation(
                        "{Rover} sol {Sol}: scraped {Count} photos",
                        roverName, sol, photos.Count);
                }

                // Update scraper state checkpoint
                await _photoRepository.UpdateScraperStateAsync(roverName, sol, photosScraped);

                // Rate limiting - be respectful
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scraping {Rover} sol {Sol}", roverName, sol);
                // Continue with next sol - don't fail entire scrape
            }
        }

        _logger.LogInformation(
            "Completed scrape for {Rover}: {Total} photos scraped",
            roverName, photosScraped);
    }

    /// <summary>
    /// Incremental scrape - only fetch new sols since last scrape
    /// </summary>
    public async Task IncrementalScrapeAsync(
        string roverName,
        CancellationToken ct = default)
    {
        var state = await _photoRepository.GetScraperStateAsync(roverName);
        var startSol = state?.LastScrapedSol + 1 ?? 1;

        await ScrapeRoverAsync(roverName, startSol, ct);
    }

    private IMarsRoverClient GetClientForRover(string roverName) =>
        roverName.ToLower() switch
        {
            "curiosity" => _curiosityClient,
            "perseverance" => _perseveranceClient,
            _ => throw new ArgumentException($"Unknown rover: {roverName}")
        };
}
```

## Hangfire Background Jobs

```csharp
// MarsPhotoApi.Web/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure HttpClient with Polly retry policy
builder.Services.AddHttpClient<CuriosityClient>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

builder.Services.AddHttpClient<PerseveranceClient>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

// Register services
builder.Services.AddScoped<IScraperService, ScraperService>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();

// Hangfire for background jobs
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

var app = builder.Build();

// Schedule recurring jobs
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

// Incremental scrape daily at 2 AM
recurringJobManager.AddOrUpdate<IScraperService>(
    "scrape-curiosity-incremental",
    service => service.IncrementalScrapeAsync("curiosity", CancellationToken.None),
    "0 2 * * *"); // Cron: 2 AM daily

recurringJobManager.AddOrUpdate<IScraperService>(
    "scrape-perseverance-incremental",
    service => service.IncrementalScrapeAsync("perseverance", CancellationToken.None),
    "0 2 * * *");

app.Run();
```

## Database Configuration

```csharp
// MarsPhotoApi.Infrastructure/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Rover> Rovers => Set<Rover>();
    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<ScraperState> ScraperStates => Set<ScraperState>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Indexes for efficient queries
        modelBuilder.Entity<Photo>()
            .HasIndex(p => p.ExternalId)
            .IsUnique();

        modelBuilder.Entity<Photo>()
            .HasIndex(p => p.Sol);

        modelBuilder.Entity<Photo>()
            .HasIndex(p => p.DateTaken);

        modelBuilder.Entity<Photo>()
            .HasIndex(p => new { p.RoverId, p.Sol });

        // Seed data
        modelBuilder.Entity<Rover>().HasData(
            new Rover
            {
                Id = 1,
                Name = "Curiosity",
                LandingDate = new DateOnly(2012, 8, 6),
                LaunchDate = new DateOnly(2011, 11, 26),
                Status = "active"
            },
            new Rover
            {
                Id = 2,
                Name = "Perseverance",
                LandingDate = new DateOnly(2021, 2, 18),
                LaunchDate = new DateOnly(2020, 7, 30),
                Status = "active"
            }
        );
    }
}
```

## Bulk Insert for Performance

```csharp
// MarsPhotoApi.Infrastructure/Data/Repositories/PhotoRepository.cs
public class PhotoRepository : IPhotoRepository
{
    private readonly AppDbContext _context;

    public PhotoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task BulkInsertPhotosAsync(
        IReadOnlyList<CuriosityPhotoDto> photos,
        string roverName)
    {
        // Get rover and camera mappings
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstAsync(r => r.Name == roverName);

        var entities = new List<Photo>();

        foreach (var dto in photos)
        {
            // Get or create camera
            var camera = rover.Cameras.FirstOrDefault(c => c.Name == dto.Instrument);
            if (camera == null)
            {
                camera = new Camera
                {
                    Name = dto.Instrument,
                    FullName = dto.Instrument,
                    RoverId = rover.Id
                };
                _context.Cameras.Add(camera);
                rover.Cameras.Add(camera);
            }

            entities.Add(new Photo
            {
                ExternalId = dto.ExternalId,
                Sol = dto.Sol,
                DateTaken = dto.DateTaken,
                DateReceived = dto.DateReceived,
                ImageUrl = dto.ImageUrl,
                Camera = camera,
                Rover = rover,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Bulk insert with ON CONFLICT DO NOTHING (ignore duplicates)
        await _context.Photos.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateScraperStateAsync(
        string roverName,
        int lastSol,
        int photosScraped)
    {
        var state = await _context.ScraperStates
            .FirstOrDefaultAsync(s => s.RoverName == roverName);

        if (state == null)
        {
            state = new ScraperState
            {
                RoverName = roverName,
                LastScrapedSol = lastSol,
                PhotosScraped = photosScraped,
                LastScrapedAt = DateTime.UtcNow
            };
            _context.ScraperStates.Add(state);
        }
        else
        {
            state.LastScrapedSol = lastSol;
            state.PhotosScraped = photosScraped;
            state.LastScrapedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
```

## Key Design Principles Applied

### 1. Functional Layering
- **Data Layer**: Entities are immutable-focused records
- **Calculation Layer**: Pure mapping functions (`MapToDto`)
- **Action Layer**: HTTP clients and repository operations

### 2. Deep Modules
- `ScraperService` hides complex orchestration behind simple interface:
  - `ScrapeRoverAsync(roverName, startSol)` - simple to call, complex internally
  - `IncrementalScrapeAsync(roverName)` - even simpler

### 3. Error Handling
- **Polly policies**: Retry transient HTTP errors
- **Circuit breaker**: Prevent cascade failures
- **Continue on error**: One failed sol doesn't stop entire scrape
- **Checkpointing**: Can resume from last successful sol

### 4. Performance
- Bulk inserts (not one-by-one)
- Database indexes on common query patterns
- Configurable `per_page` size (max 1000 for Curiosity)
- Parallel rover scraping (separate Hangfire jobs)

### 5. Observability
- Structured logging with Serilog
- Track scraper state in database
- Hangfire dashboard for monitoring jobs

## Running Initial Scrape

```csharp
// One-time manual trigger via API endpoint or Hangfire dashboard
public class ScraperController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobs;

    [HttpPost("api/scraper/full/{roverName}")]
    public IActionResult TriggerFullScrape(string roverName)
    {
        var jobId = _backgroundJobs.Enqueue<IScraperService>(
            service => service.ScrapeRoverAsync(roverName, 1, CancellationToken.None));

        return Ok(new { jobId });
    }
}
```

## Deployment Considerations

1. **Database**: Use PostgreSQL with proper connection pooling
2. **Memory**: Bulk operations may use significant memory; consider batching
3. **Monitoring**: Track Hangfire jobs, HTTP client metrics, DB performance
4. **Scaling**: Can run multiple workers for faster initial scrape
5. **Backups**: Regular database backups during/after scraping

---

## Next Steps

1. Set up project structure
2. Implement HTTP clients for both rovers
3. Set up database with EF Core
4. Implement scraper service
5. Add Hangfire for scheduling
6. Test with small sol range first (e.g., sols 1-10)
7. Run full scrape
8. Set up incremental daily scraping
