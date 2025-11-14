# Decision 006: Scraper Service Pattern

**Status:** Active
**Date:** 2025-11-13
**Story:** 006 - NASA API Scraper Service

## Context

We need to scrape photos from NASA APIs for multiple rovers (Perseverance, Curiosity, Opportunity, Spirit). Each rover has a different API format and data structure. How should we structure the scraper services?

## Options Considered

### Option 1: One Scraper Service Per Rover (Recommended)

Create separate scraper classes implementing a common interface:

```csharp
public interface IScraperService
{
    Task<int> ScrapeAsync(CancellationToken cancellationToken);
    Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken);
    string RoverName { get; }
}

public class PerseveranceScraper : IScraperService { }
public class CuriosityScraper : IScraperService { }
```

**Pros:**
- Clean separation of concerns
- Each rover's unique logic isolated
- Easy to test individually
- Can have different scraping strategies per rover
- Register all with DI: `IEnumerable<IScraperService>`
- Simple to add new rovers

**Cons:**
- More classes to maintain
- Some code duplication (extraction helpers)
- Slightly more setup code

### Option 2: Single Scraper with Rover Parameter

One scraper class with switch/if statements:

```csharp
public class RoverScraper
{
    public async Task<int> ScrapeAsync(string roverName)
    {
        switch (roverName)
        {
            case "Perseverance": return await ScrapePerseverance();
            case "Curiosity": return await ScrapeCuriosity();
            // ...
        }
    }
}
```

**Pros:**
- Single class to maintain
- Shared helper methods
- Less code overall

**Cons:**
- God class anti-pattern (too many responsibilities)
- Hard to test specific rover logic
- Switch statements fragile
- Violates Single Responsibility Principle
- Mixing different JSON structures in one class
- Harder to parallelize scraping

### Option 3: Hosted Background Service

Background service that scrapes automatically:

```csharp
public class ScraperBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScrapeAllRovers();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

**Pros:**
- Fully automated
- No manual triggers needed
- Runs in background

**Cons:**
- Can't trigger manually for testing
- Harder to debug
- No visibility into scrape status
- Runs even when not needed
- Should be Story 010 (after manual scraping works)

### Option 4: Static Methods / Functional Approach

No classes, just functions:

```csharp
public static class Scrapers
{
    public static async Task<int> ScrapePerseverance(HttpClient client, MarsVistaDbContext context) { }
    public static async Task<int> ScrapeCuriosity(HttpClient client, MarsVistaDbContext context) { }
}
```

**Pros:**
- Simple and lightweight
- No DI complexity
- Easy to call

**Cons:**
- No polymorphism
- Hard to mock for testing
- No shared state management
- Doesn't integrate well with ASP.NET Core patterns

## Decision

**Use Option 1: One Scraper Service Per Rover**

## Reasoning

### Why This Choice?

1. **Clean Architecture:**
   - Each rover's scraper is a self-contained service
   - Easy to understand and maintain
   - Follows Single Responsibility Principle

2. **Different API Formats:**
   - Perseverance: Modern JSON API (30+ fields)
   - Curiosity: Different JSON structure (38+ fields)
   - Spirit/Opportunity: May require HTML scraping
   - Each needs custom logic - don't force into one class

3. **Testability:**
   ```csharp
   // Easy to mock and test
   var mockScraper = new Mock<IScraperService>();
   mockScraper.Setup(s => s.ScrapeAsync()).ReturnsAsync(10);
   ```

4. **Dependency Injection:**
   ```csharp
   // Register all scrapers
   builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
   builder.Services.AddScoped<IScraperService, CuriosityScraper>();

   // Inject all scrapers
   public ScraperController(IEnumerable<IScraperService> scrapers) { }
   ```

5. **Parallel Execution:**
   ```csharp
   // Scrape all rovers in parallel
   var tasks = scrapers.Select(s => s.ScrapeAsync());
   await Task.WhenAll(tasks);
   ```

6. **Future Extensibility:**
   - Add new rover: create new class
   - No changes to existing scrapers
   - Open/Closed Principle

## Implementation

### Interface

```csharp
public interface IScraperService
{
    /// <summary>
    /// Scrapes latest photos from NASA API
    /// </summary>
    Task<int> ScrapeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scrapes photos for a specific sol
    /// </summary>
    Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rover name this scraper handles
    /// </summary>
    string RoverName { get; }
}
```

### Registration

```csharp
// In Program.cs
builder.Services.AddScoped<IScraperService, PerseveranceScraper>();
builder.Services.AddScoped<IScraperService, CuriosityScraper>();
// More scrapers as we implement them
```

### Usage

```csharp
// In controller
public class ScraperController : ControllerBase
{
    private readonly IEnumerable<IScraperService> _scrapers;

    public ScraperController(IEnumerable<IScraperService> scrapers)
    {
        _scrapers = scrapers;
    }

    [HttpPost("{roverName}")]
    public async Task<IActionResult> ScrapeRover(string roverName)
    {
        var scraper = _scrapers.FirstOrDefault(s =>
            s.RoverName.Equals(roverName, StringComparison.OrdinalIgnoreCase));

        if (scraper == null)
            return NotFound();

        var count = await scraper.ScrapeAsync();
        return Ok(new { photosScraped = count });
    }
}
```

## Trade-offs Accepted

### More Classes
- **Accepted:** 4 scraper classes instead of 1
- **Why it's OK:** Each is focused and maintainable
- **Mitigation:** Shared helpers in base class or utility

### Some Code Duplication
- **Accepted:** JSON extraction helpers duplicated
- **Why it's OK:** Rover-specific extraction logic differs
- **Mitigation:** Extract truly common code to utility class

## Alternatives Rejected

### Why Not Single Scraper? (Option 2)
- Becomes god class (1000+ lines)
- Hard to test specific rover
- Switch statements fragile
- Violates SRP

### Why Not Background Service? (Option 3)
- That's Story 010 (automated scheduling)
- For now, need manual control for testing
- Background service will USE these scrapers

### Why Not Static Functions? (Option 4)
- Doesn't leverage DI and ASP.NET Core patterns
- Hard to test
- No polymorphism

## Validation

This pattern is validated by:
- ✅ Each scraper can be tested independently
- ✅ Easy to add new rovers without modifying existing code
- ✅ Controller can work with any scraper generically
- ✅ Can scrape all rovers in parallel
- ✅ DI handles lifetime management

## Related Decisions

- [Decision 003: ORM Selection](003-orm-selection.md) - EF Core used in scrapers
- [Decision 006A: HTTP Resilience Strategy](006a-http-resilience.md) - Retry/circuit breaker
- Future: Story 010 will add background service that uses these scrapers

## References

- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [Strategy Pattern](https://refactoring.guru/design-patterns/strategy)
- [Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)
