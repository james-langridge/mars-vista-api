# Story 007: Public Query API Endpoints

## Story
As a developer, I need REST API endpoints to query rover photos by sol, earth date, and camera so that I can build Mars rover photo applications.

## Acceptance Criteria
- [ ] GET /api/v1/rovers - List all rovers with metadata
- [ ] GET /api/v1/rovers/:name - Show single rover details
- [ ] GET /api/v1/rovers/:name/photos - Query photos with filtering
- [ ] GET /api/v1/rovers/:name/latest - Get latest sol photos
- [ ] GET /api/v1/photos/:id - Get specific photo by ID
- [ ] GET /api/v1/manifests/:name - Get rover photo manifest
- [ ] Support filtering by sol, earth_date, and camera
- [ ] Pagination with page and per_page parameters
- [ ] JSON serialization matches NASA API format
- [ ] Query parameters are case-insensitive where appropriate
- [ ] Proper HTTP status codes (200, 400, 404)
- [ ] API versioning via URL path (/api/v1/)

## Context

The original NASA Mars Photo API provides a REST interface for querying photos. We need to implement compatible endpoints so existing applications can use our enhanced API.

### Original Rails API Endpoints

```
GET /api/v1/rovers                         # List all rovers
GET /api/v1/rovers/:id                     # Show rover details
GET /api/v1/rovers/:rover_id/photos        # Query photos
GET /api/v1/rovers/:rover_id/latest_photos # Latest sol photos
GET /api/v1/photos/:id                     # Show specific photo
GET /api/v1/manifests/:id                  # Rover manifest
```

### Query Parameters
- `sol`: Martian day (e.g., `?sol=1000`)
- `earth_date`: Earth date in YYYY-MM-DD format (e.g., `?earth_date=2024-06-15`)
- `camera`: Camera abbreviation (e.g., `?camera=NAVCAM`)
- `page`: Page number for pagination (default: 1)
- `per_page`: Results per page (default: 25, max: 100)

### Why This Story?

1. **Enable consumption**: Photos are being scraped but can't be queried yet
2. **NASA API compatibility**: Match existing API patterns for easy migration
3. **Foundation for features**: All future features build on these endpoints
4. **Demonstrate value**: Show that 450K+ photos are accessible

## Implementation Steps

### 1. Create DTOs (Data Transfer Objects)

We need clean, structured JSON responses that match the NASA API format.

**File:** `src/MarsVista.Api/DTOs/RoverDto.cs`

```csharp
namespace MarsVista.Api.DTOs;

public record RoverDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MaxSol { get; init; }
    public string MaxDate { get; init; } = string.Empty;
    public int TotalPhotos { get; init; }
    public List<CameraDto> Cameras { get; init; } = new();
}

public record CameraDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
}

public record PhotoDto
{
    public int Id { get; init; }
    public int Sol { get; init; }
    public CameraDto Camera { get; init; } = new();
    public string ImgSrc { get; init; } = string.Empty;
    public string EarthDate { get; init; } = string.Empty;
    public RoverSummaryDto Rover { get; init; } = new();
}

public record RoverSummaryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

public record PhotoManifestDto
{
    public string Name { get; init; } = string.Empty;
    public string LandingDate { get; init; } = string.Empty;
    public string LaunchDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MaxSol { get; init; }
    public string MaxDate { get; init; } = string.Empty;
    public int TotalPhotos { get; init; }
    public List<PhotosBySolDto> Photos { get; init; } = new();
}

public record PhotosBySolDto
{
    public int Sol { get; init; }
    public int TotalPhotos { get; init; }
    public List<string> Cameras { get; init; } = new();
}
```

**Why DTOs?**
- Decouple API response format from database entities
- Control exactly what gets serialized
- Easy to add/remove fields without changing entities
- Match NASA API format precisely

### 2. Create Photo Query Service

Centralize photo querying logic in a dedicated service.

**File:** `src/MarsVista.Api/Services/IPhotoQueryService.cs`

```csharp
using MarsVista.Api.DTOs;

namespace MarsVista.Api.Services;

public interface IPhotoQueryService
{
    Task<(List<PhotoDto> Photos, int TotalCount)> QueryPhotosAsync(
        string roverName,
        int? sol = null,
        DateTime? earthDate = null,
        string? camera = null,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default);

    Task<(List<PhotoDto> Photos, int TotalCount)> GetLatestPhotosAsync(
        string roverName,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default);

    Task<PhotoDto?> GetPhotoByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
```

**File:** `src/MarsVista.Api/Services/PhotoQueryService.cs`

```csharp
using MarsVista.Api.Data;
using MarsVista.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class PhotoQueryService : IPhotoQueryService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PhotoQueryService> _logger;

    public PhotoQueryService(
        MarsVistaDbContext context,
        ILogger<PhotoQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<PhotoDto> Photos, int TotalCount)> QueryPhotosAsync(
        string roverName,
        int? sol = null,
        DateTime? earthDate = null,
        string? camera = null,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination
        page = Math.Max(1, page);
        perPage = Math.Clamp(perPage, 1, 100);

        // Start with base query
        var query = _context.Photos
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .Where(p => p.Rover.Name.ToLower() == roverName.ToLower());

        // Apply filters
        if (sol.HasValue)
        {
            query = query.Where(p => p.Sol == sol.Value);
        }

        if (earthDate.HasValue)
        {
            var date = DateOnly.FromDateTime(earthDate.Value);
            query = query.Where(p => p.EarthDate == date);
        }

        if (!string.IsNullOrWhiteSpace(camera))
        {
            query = query.Where(p => p.Camera.Name.ToLower() == camera.ToLower());
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Order by camera and ID for consistent results
        query = query
            .OrderBy(p => p.CameraId)
            .ThenBy(p => p.Id);

        // Apply pagination
        var photos = await query
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Sol = p.Sol,
                ImgSrc = p.ImgSrcMedium ?? p.ImgSrcLarge ?? p.ImgSrcFull ?? "",
                EarthDate = p.EarthDate.ToString("yyyy-MM-dd"),
                Camera = new CameraDto
                {
                    Id = p.Camera.Id,
                    Name = p.Camera.Name,
                    FullName = p.Camera.FullName
                },
                Rover = new RoverSummaryDto
                {
                    Id = p.Rover.Id,
                    Name = p.Rover.Name,
                    LandingDate = p.Rover.LandingDate.ToString("yyyy-MM-dd"),
                    LaunchDate = p.Rover.LaunchDate.ToString("yyyy-MM-dd"),
                    Status = p.Rover.Status
                }
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Queried {Count} photos for {Rover} (sol: {Sol}, date: {Date}, camera: {Camera}, page: {Page})",
            photos.Count, roverName, sol, earthDate?.ToString("yyyy-MM-dd"), camera, page);

        return (photos, totalCount);
    }

    public async Task<(List<PhotoDto> Photos, int TotalCount)> GetLatestPhotosAsync(
        string roverName,
        int page = 1,
        int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Find the maximum sol for this rover
        var maxSol = await _context.Photos
            .Where(p => p.Rover.Name.ToLower() == roverName.ToLower())
            .MaxAsync(p => (int?)p.Sol, cancellationToken);

        if (!maxSol.HasValue)
        {
            return (new List<PhotoDto>(), 0);
        }

        // Query photos for the latest sol
        return await QueryPhotosAsync(
            roverName,
            sol: maxSol.Value,
            page: page,
            perPage: perPage,
            cancellationToken: cancellationToken);
    }

    public async Task<PhotoDto?> GetPhotoByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var photo = await _context.Photos
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .Where(p => p.Id == id)
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                Sol = p.Sol,
                ImgSrc = p.ImgSrcMedium ?? p.ImgSrcLarge ?? p.ImgSrcFull ?? "",
                EarthDate = p.EarthDate.ToString("yyyy-MM-dd"),
                Camera = new CameraDto
                {
                    Id = p.Camera.Id,
                    Name = p.Camera.Name,
                    FullName = p.Camera.FullName
                },
                Rover = new RoverSummaryDto
                {
                    Id = p.Rover.Id,
                    Name = p.Rover.Name,
                    LandingDate = p.Rover.LandingDate.ToString("yyyy-MM-dd"),
                    LaunchDate = p.Rover.LaunchDate.ToString("yyyy-MM-dd"),
                    Status = p.Rover.Status
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return photo;
    }
}
```

**Why a Service Layer?**
- Keeps controllers thin (calculation layer, not action layer)
- Reusable query logic
- Easier to test business logic
- Consistent error handling

### 3. Create Rover Query Service

**File:** `src/MarsVista.Api/Services/IRoverQueryService.cs`

```csharp
using MarsVista.Api.DTOs;

namespace MarsVista.Api.Services;

public interface IRoverQueryService
{
    Task<List<RoverDto>> GetAllRoversAsync(CancellationToken cancellationToken = default);
    Task<RoverDto?> GetRoverByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PhotoManifestDto?> GetManifestAsync(string roverName, CancellationToken cancellationToken = default);
}
```

**File:** `src/MarsVista.Api/Services/RoverQueryService.cs`

```csharp
using MarsVista.Api.Data;
using MarsVista.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

public class RoverQueryService : IRoverQueryService
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<RoverQueryService> _logger;

    public RoverQueryService(
        MarsVistaDbContext context,
        ILogger<RoverQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RoverDto>> GetAllRoversAsync(CancellationToken cancellationToken = default)
    {
        var rovers = await _context.Rovers
            .Include(r => r.Cameras)
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);

        var roverDtos = new List<RoverDto>();

        foreach (var rover in rovers)
        {
            var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

            roverDtos.Add(new RoverDto
            {
                Id = rover.Id,
                Name = rover.Name,
                LandingDate = rover.LandingDate.ToString("yyyy-MM-dd"),
                LaunchDate = rover.LaunchDate.ToString("yyyy-MM-dd"),
                Status = rover.Status,
                MaxSol = stats.MaxSol,
                MaxDate = stats.MaxDate,
                TotalPhotos = stats.TotalPhotos,
                Cameras = rover.Cameras
                    .Select(c => new CameraDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        FullName = c.FullName
                    })
                    .ToList()
            });
        }

        return roverDtos;
    }

    public async Task<RoverDto?> GetRoverByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

        if (rover == null)
        {
            return null;
        }

        var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

        return new RoverDto
        {
            Id = rover.Id,
            Name = rover.Name,
            LandingDate = rover.LandingDate.ToString("yyyy-MM-dd"),
            LaunchDate = rover.LaunchDate.ToString("yyyy-MM-dd"),
            Status = rover.Status,
            MaxSol = stats.MaxSol,
            MaxDate = stats.MaxDate,
            TotalPhotos = stats.TotalPhotos,
            Cameras = rover.Cameras
                .Select(c => new CameraDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    FullName = c.FullName
                })
                .ToList()
        };
    }

    public async Task<PhotoManifestDto?> GetManifestAsync(
        string roverName,
        CancellationToken cancellationToken = default)
    {
        var rover = await _context.Rovers
            .FirstOrDefaultAsync(r => r.Name.ToLower() == roverName.ToLower(), cancellationToken);

        if (rover == null)
        {
            return null;
        }

        var stats = await GetRoverStatsAsync(rover.Id, cancellationToken);

        // Get photos grouped by sol
        var photosBySol = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .Include(p => p.Camera)
            .GroupBy(p => p.Sol)
            .Select(g => new PhotosBySolDto
            {
                Sol = g.Key,
                TotalPhotos = g.Count(),
                Cameras = g.Select(p => p.Camera.Name).Distinct().ToList()
            })
            .OrderBy(p => p.Sol)
            .ToListAsync(cancellationToken);

        return new PhotoManifestDto
        {
            Name = rover.Name,
            LandingDate = rover.LandingDate.ToString("yyyy-MM-dd"),
            LaunchDate = rover.LaunchDate.ToString("yyyy-MM-dd"),
            Status = rover.Status,
            MaxSol = stats.MaxSol,
            MaxDate = stats.MaxDate,
            TotalPhotos = stats.TotalPhotos,
            Photos = photosBySol
        };
    }

    private async Task<(int MaxSol, string MaxDate, int TotalPhotos)> GetRoverStatsAsync(
        int roverId,
        CancellationToken cancellationToken)
    {
        var photos = await _context.Photos
            .Where(p => p.RoverId == roverId)
            .ToListAsync(cancellationToken);

        if (!photos.Any())
        {
            return (0, "", 0);
        }

        var maxSol = photos.Max(p => p.Sol);
        var maxDate = photos.Max(p => p.EarthDate).ToString("yyyy-MM-dd");
        var totalPhotos = photos.Count;

        return (maxSol, maxDate, totalPhotos);
    }
}
```

### 4. Create V1 API Controllers

**File:** `src/MarsVista.Api/Controllers/V1/RoversController.cs`

```csharp
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/rovers")]
public class RoversController : ControllerBase
{
    private readonly IRoverQueryService _roverQueryService;
    private readonly ILogger<RoversController> _logger;

    public RoversController(
        IRoverQueryService roverQueryService,
        ILogger<RoversController> logger)
    {
        _roverQueryService = roverQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rovers with their metadata
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRovers(CancellationToken cancellationToken)
    {
        var rovers = await _roverQueryService.GetAllRoversAsync(cancellationToken);

        return Ok(new { rovers });
    }

    /// <summary>
    /// Get a specific rover by name
    /// </summary>
    /// <param name="name">Rover name (e.g., "perseverance")</param>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetRover(string name, CancellationToken cancellationToken)
    {
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);

        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        return Ok(new { rover });
    }

    /// <summary>
    /// Query photos for a specific rover
    /// </summary>
    /// <param name="name">Rover name</param>
    /// <param name="sol">Martian sol (optional)</param>
    /// <param name="earthDate">Earth date YYYY-MM-DD (optional)</param>
    /// <param name="camera">Camera name (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="perPage">Results per page (default: 25, max: 100)</param>
    [HttpGet("{name}/photos")]
    public async Task<IActionResult> GetPhotos(
        string name,
        [FromQuery] int? sol,
        [FromQuery(Name = "earth_date")] string? earthDate,
        [FromQuery] string? camera,
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate rover exists
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        // Parse earth_date if provided
        DateTime? parsedEarthDate = null;
        if (!string.IsNullOrWhiteSpace(earthDate))
        {
            if (!DateTime.TryParse(earthDate, out var date))
            {
                return BadRequest(new { error = "Invalid earth_date format. Use YYYY-MM-DD." });
            }
            parsedEarthDate = date;
        }

        var photoQueryService = HttpContext.RequestServices
            .GetRequiredService<IPhotoQueryService>();

        var (photos, totalCount) = await photoQueryService.QueryPhotosAsync(
            name,
            sol,
            parsedEarthDate,
            camera,
            page,
            perPage,
            cancellationToken);

        return Ok(new
        {
            photos,
            pagination = new
            {
                total_count = totalCount,
                page,
                per_page = perPage,
                total_pages = (int)Math.Ceiling(totalCount / (double)perPage)
            }
        });
    }

    /// <summary>
    /// Get the latest photos for a rover (highest sol)
    /// </summary>
    /// <param name="name">Rover name</param>
    /// <param name="page">Page number</param>
    /// <param name="perPage">Results per page</param>
    [HttpGet("{name}/latest")]
    public async Task<IActionResult> GetLatestPhotos(
        string name,
        [FromQuery] int page = 1,
        [FromQuery(Name = "per_page")] int perPage = 25,
        CancellationToken cancellationToken = default)
    {
        // Validate rover exists
        var rover = await _roverQueryService.GetRoverByNameAsync(name, cancellationToken);
        if (rover == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        var photoQueryService = HttpContext.RequestServices
            .GetRequiredService<IPhotoQueryService>();

        var (photos, totalCount) = await photoQueryService.GetLatestPhotosAsync(
            name,
            page,
            perPage,
            cancellationToken);

        return Ok(new
        {
            photos,
            pagination = new
            {
                total_count = totalCount,
                page,
                per_page = perPage,
                total_pages = (int)Math.Ceiling(totalCount / (double)perPage)
            }
        });
    }
}
```

**File:** `src/MarsVista.Api/Controllers/V1/PhotosController.cs`

```csharp
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/photos")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoQueryService _photoQueryService;
    private readonly ILogger<PhotosController> _logger;

    public PhotosController(
        IPhotoQueryService photoQueryService,
        ILogger<PhotosController> logger)
    {
        _photoQueryService = photoQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get a specific photo by ID
    /// </summary>
    /// <param name="id">Photo ID</param>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPhoto(int id, CancellationToken cancellationToken)
    {
        var photo = await _photoQueryService.GetPhotoByIdAsync(id, cancellationToken);

        if (photo == null)
        {
            return NotFound(new { error = $"Photo {id} not found" });
        }

        return Ok(new { photo });
    }
}
```

**File:** `src/MarsVista.Api/Controllers/V1/ManifestsController.cs`

```csharp
using MarsVista.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarsVista.Api.Controllers.V1;

[ApiController]
[Route("api/v1/manifests")]
public class ManifestsController : ControllerBase
{
    private readonly IRoverQueryService _roverQueryService;
    private readonly ILogger<ManifestsController> _logger;

    public ManifestsController(
        IRoverQueryService roverQueryService,
        ILogger<ManifestsController> logger)
    {
        _roverQueryService = roverQueryService;
        _logger = logger;
    }

    /// <summary>
    /// Get photo manifest for a rover (photos grouped by sol)
    /// </summary>
    /// <param name="name">Rover name</param>
    [HttpGet("{name}")]
    public async Task<IActionResult> GetManifest(string name, CancellationToken cancellationToken)
    {
        var manifest = await _roverQueryService.GetManifestAsync(name, cancellationToken);

        if (manifest == null)
        {
            return NotFound(new { error = $"Rover '{name}' not found" });
        }

        return Ok(new { photo_manifest = manifest });
    }
}
```

### 5. Register Services in DI Container

**File:** `src/MarsVista.Api/Program.cs`

Update the service registration section:

```csharp
// Query services (pure calculation layer)
builder.Services.AddScoped<IRoverQueryService, RoverQueryService>();
builder.Services.AddScoped<IPhotoQueryService, PhotoQueryService>();

// Scraper services (action layer)
builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
builder.Services.AddScoped<DatabaseSeeder>();
```

### 6. Test the API Endpoints

Start the application:

```bash
cd src/MarsVista.Api
dotnet run
```

Test each endpoint:

```bash
# List all rovers
curl http://localhost:5000/api/v1/rovers | jq

# Get specific rover
curl http://localhost:5000/api/v1/rovers/perseverance | jq

# Query photos by sol
curl "http://localhost:5000/api/v1/rovers/perseverance/photos?sol=1000" | jq

# Query photos by earth date
curl "http://localhost:5000/api/v1/rovers/perseverance/photos?earth_date=2024-06-15" | jq

# Query photos by camera
curl "http://localhost:5000/api/v1/rovers/perseverance/photos?camera=NAVCAM" | jq

# Query with pagination
curl "http://localhost:5000/api/v1/rovers/perseverance/photos?sol=1000&page=2&per_page=10" | jq

# Get latest photos
curl "http://localhost:5000/api/v1/rovers/perseverance/latest" | jq

# Get specific photo
curl http://localhost:5000/api/v1/photos/12345 | jq

# Get rover manifest
curl http://localhost:5000/api/v1/manifests/perseverance | jq
```

Expected response format:

```json
{
  "photos": [
    {
      "id": 12345,
      "sol": 1000,
      "camera": {
        "id": 5,
        "name": "NAVCAM_LEFT",
        "full_name": "Navigation Camera - Left"
      },
      "img_src": "https://mars.nasa.gov/...",
      "earth_date": "2024-06-15",
      "rover": {
        "id": 1,
        "name": "Perseverance",
        "landing_date": "2021-02-18",
        "launch_date": "2020-07-30",
        "status": "active"
      }
    }
  ],
  "pagination": {
    "total_count": 127,
    "page": 1,
    "per_page": 25,
    "total_pages": 6
  }
}
```

## Technical Decisions

### Decision 007: DTO vs Entity Serialization
**File:** `.claude/decisions/007-dto-pattern.md`

**Context:** Should we serialize entities directly or use DTOs?

**Recommendation:** Use dedicated DTOs for API responses

**Reasoning:**
- Control exact JSON structure independent of database schema
- Prevent over-posting vulnerabilities
- Hide internal fields (created_at, updated_at, raw_data)
- Match NASA API format precisely
- Easy to version API (v1, v2) without breaking entities

### Decision 007A: Service Layer for Queries
**File:** `.claude/decisions/007a-query-service-layer.md`

**Context:** Should query logic live in controllers or services?

**Recommendation:** Dedicated query service layer

**Reasoning:**
- Controllers are action layer (thin, HTTP concerns only)
- Services are calculation layer (pure business logic)
- Reusable across controllers
- Easier to test (mock services, not controllers)
- Follows functional architecture: Data → Calculation → Action

### Decision 007B: Pagination Strategy
**File:** `.claude/decisions/007b-pagination.md`

**Context:** How to handle pagination for large photo collections?

**Recommendation:** Offset-based pagination with page/per_page params

**Reasoning:**
- Matches Rails API behavior (compatibility)
- Simple and predictable for API consumers
- Works well for random access
- Default 25 per page, max 100 (prevent abuse)
- Include total_count for client UI

**Trade-offs:**
- Offset pagination can be slow for deep pages (>10K offset)
- Alternative: Cursor-based pagination (for future v2 API)
- For now, offset is fine (most queries filtered by sol/date)

### Decision 007C: Image URL Selection
**File:** `.claude/decisions/007c-image-url-priority.md`

**Context:** Which image URL to return in img_src field?

**Recommendation:** Prefer medium → large → full in that order

**Reasoning:**
- Medium size (800px) is best for gallery views
- Falls back to large if medium unavailable
- Falls back to full_res as last resort
- Clients can query raw_data for all URL options
- Balances image quality with bandwidth

## Testing Checklist

- [ ] DTOs created and compile successfully
- [ ] Query services implemented and registered
- [ ] All V1 controllers created
- [ ] Services registered in Program.cs
- [ ] Application builds without errors
- [ ] GET /api/v1/rovers returns all rovers
- [ ] GET /api/v1/rovers/:name returns single rover
- [ ] GET /api/v1/rovers/:name/photos returns photos
- [ ] Filtering by sol works correctly
- [ ] Filtering by earth_date works correctly
- [ ] Filtering by camera works correctly
- [ ] Pagination works (page, per_page)
- [ ] GET /api/v1/rovers/:name/latest returns latest sol
- [ ] GET /api/v1/photos/:id returns specific photo
- [ ] GET /api/v1/manifests/:name returns manifest
- [ ] 404 returned for invalid rover names
- [ ] 404 returned for invalid photo IDs
- [ ] 400 returned for invalid earth_date format
- [ ] Case-insensitive rover name lookup
- [ ] Case-insensitive camera name lookup
- [ ] JSON response matches expected format

## Success Criteria

✅ All 6 API endpoints implemented
✅ Query filtering by sol, earth_date, camera
✅ Pagination with page/per_page parameters
✅ Proper HTTP status codes (200, 400, 404)
✅ JSON responses match NASA API format
✅ Case-insensitive rover and camera lookups
✅ Query services follow functional architecture
✅ DTOs decouple API from database entities
✅ All endpoints tested and verified

## Next Steps

After completing this story, you'll be ready for:
- **Story 008:** Add caching layer (Redis) for performance
- **Story 009:** Implement Curiosity, Opportunity, Spirit scrapers
- **Story 010:** Add extended photo details endpoint (all 30+ fields)
- **Story 011:** Background job scheduler for automatic scraping

## Notes

### API Versioning

Using URL-based versioning (`/api/v1/`) allows us to:
- Maintain backward compatibility
- Ship breaking changes in v2
- Support multiple versions simultaneously
- Clearly communicate API evolution

### Performance Considerations

Current implementation is simple and works for moderate load:
- No caching yet (Story 008)
- Offset pagination (acceptable for filtered queries)
- Include() for eager loading (prevents N+1 queries)

For high-traffic scenarios:
- Add Redis caching (coming in Story 008)
- Consider cursor-based pagination for deep pages
- Add database indexes on query columns (already have them)

### JSON Naming Convention

Using snake_case for JSON keys (earth_date, per_page) to match:
- NASA API convention
- Rails API convention
- Common REST API patterns

This differs from C# PascalCase but provides better compatibility.
