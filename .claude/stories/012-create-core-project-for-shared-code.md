# Story 012: Create MarsVista.Core Project for Shared Code

## Background

Currently, the MarsVista.Scraper console application references the entire MarsVista.Api project to access shared entities, repositories, and services. This creates several issues:

1. **Unnecessary dependencies**: The console app pulls in ASP.NET Core packages, Swashbuckle (Swagger), and other web-specific dependencies it doesn't need
2. **Larger deployment**: Docker image for the scraper includes web middleware and controller code
3. **Architectural coupling**: Console app is coupled to web layer implementation details
4. **Limited reusability**: Difficult to share business logic with other non-web applications

The solution is to extract shared code into a `MarsVista.Core` class library that both `MarsVista.Api` and `MarsVista.Scraper` can reference.

## Requirements

### Functional Requirements

1. **Shared Core Library**
   - Create new `MarsVista.Core` class library project (.NET 9.0)
   - Contains entities, repositories, services, database context, and options
   - No dependencies on ASP.NET Core or web-specific packages
   - References only: EF Core, Npgsql, Polly, Serilog (if needed)

2. **API Project Refactoring**
   - Reference `MarsVista.Core` instead of containing business logic
   - Keep only web-specific code: Controllers, Middleware, Program.cs, Swagger config
   - Update namespaces from `MarsVista.Api.*` to `MarsVista.Core.*` where applicable

3. **Scraper Project Refactoring**
   - Change reference from `MarsVista.Api` to `MarsVista.Core`
   - No other changes needed (already minimal)

### Non-Functional Requirements

1. **Minimal Breaking Changes**
   - Maintain existing public API contracts
   - No database schema changes
   - No runtime behavior changes

2. **Build and Test**
   - All projects must build without errors
   - Existing functionality must work identically
   - Deployment process unchanged

## Proposed Architecture

### Before (Current)
```
MarsVista.Api (ASP.NET Core Web API)
├── Entities/
├── Repositories/
├── Services/
├── Data/
├── Options/
├── Controllers/
├── Middleware/
└── Program.cs

MarsVista.Scraper (Console App)
└── References: MarsVista.Api
    (pulls in ASP.NET Core, Swashbuckle, controllers, etc.)
```

### After (Proposed)
```
MarsVista.Core (Class Library)
├── Entities/
│   ├── Photo.cs
│   ├── Rover.cs
│   ├── Camera.cs
│   ├── ScraperState.cs
│   └── ApiKey.cs
├── Repositories/
│   ├── IScraperStateRepository.cs
│   ├── ScraperStateRepository.cs
│   └── (other repositories)
├── Services/
│   ├── IScraperService.cs
│   ├── IncrementalScraperService.cs
│   ├── PerseveranceScraper.cs
│   ├── CuriosityScraper.cs
│   ├── OpportunityScraper.cs
│   └── SpiritScraper.cs
├── Data/
│   ├── MarsVistaDbContext.cs
│   └── Migrations/ (EF Core migrations)
├── Options/
│   ├── ScraperScheduleOptions.cs
│   └── (other options)
└── Extensions/
    └── JsonElementExtensions.cs

MarsVista.Api (ASP.NET Core Web API)
├── Controllers/
│   ├── PhotosController.cs
│   ├── RoversController.cs
│   ├── ScraperController.cs
│   └── ApiKeysController.cs
├── Middleware/
│   ├── ApiKeyAuthenticationMiddleware.cs
│   └── RateLimitMiddleware.cs
├── Program.cs
├── appsettings.json
└── References: MarsVista.Core

MarsVista.Scraper (Console App)
├── Program.cs
├── appsettings.json
├── Dockerfile
└── References: MarsVista.Core (NOT MarsVista.Api)
```

## Implementation Steps

### Step 1: Create MarsVista.Core Project
- [ ] Create new Class Library project: `dotnet new classlib -n MarsVista.Core -f net9.0`
- [ ] Add to solution: `dotnet sln add src/MarsVista.Core/MarsVista.Core.csproj`
- [ ] Add NuGet packages:
  - `Microsoft.EntityFrameworkCore`
  - `Microsoft.EntityFrameworkCore.Design`
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.Extensions.Options`
  - `Microsoft.Extensions.Http.Polly`
  - `Microsoft.Extensions.Logging.Abstractions`
  - `Polly`

### Step 2: Move Entities to Core
- [ ] Move `Entities/` directory from `MarsVista.Api` to `MarsVista.Core`
- [ ] Update namespace from `MarsVista.Api.Entities` to `MarsVista.Core.Entities`
- [ ] Files to move:
  - Photo.cs
  - Rover.cs
  - Camera.cs
  - ScraperState.cs
  - ApiKey.cs

### Step 3: Move Data Layer to Core
- [ ] Move `Data/MarsVistaDbContext.cs` to `MarsVista.Core/Data/`
- [ ] Move `Migrations/` directory to `MarsVista.Core/Data/Migrations/`
- [ ] Update namespaces to `MarsVista.Core.Data`
- [ ] Update DbContext to use new entity namespaces

### Step 4: Move Repositories to Core
- [ ] Move `Repositories/` directory to `MarsVista.Core/`
- [ ] Update namespaces to `MarsVista.Core.Repositories`
- [ ] Files to move:
  - IScraperStateRepository.cs
  - ScraperStateRepository.cs
  - (any other repositories)

### Step 5: Move Services to Core
- [ ] Move scraper services to `MarsVista.Core/Services/`:
  - IScraperService.cs
  - IncrementalScraperService.cs
  - PerseveranceScraper.cs
  - CuriosityScraper.cs
  - OpportunityScraper.cs
  - SpiritScraper.cs
- [ ] Update namespaces to `MarsVista.Core.Services`
- [ ] Keep API-specific services in MarsVista.Api (if any)

### Step 6: Move Options and Extensions
- [ ] Move `Options/` directory to `MarsVista.Core/`
- [ ] Move `Extensions/` directory to `MarsVista.Core/`
- [ ] Update namespaces to `MarsVista.Core.Options` and `MarsVista.Core.Extensions`

### Step 7: Update MarsVista.Api References
- [ ] Add project reference: `<ProjectReference Include="..\MarsVista.Core\MarsVista.Core.csproj" />`
- [ ] Update all `using` statements to use `MarsVista.Core.*` namespaces
- [ ] Remove duplicate NuGet packages now provided by Core
- [ ] Update Program.cs dependency injection to use Core types
- [ ] Build and fix any compilation errors

### Step 8: Update MarsVista.Scraper References
- [ ] Change project reference from `MarsVista.Api` to `MarsVista.Core`
- [ ] Update `using` statements to use `MarsVista.Core.*` namespaces
- [ ] Update Dockerfile COPY commands to include Core project
- [ ] Build and fix any compilation errors

### Step 9: Update Dockerfiles
- [ ] Update `src/MarsVista.Scraper/Dockerfile`:
  ```dockerfile
  # Copy project files
  COPY ["src/MarsVista.Core/MarsVista.Core.csproj", "MarsVista.Core/"]
  COPY ["src/MarsVista.Scraper/MarsVista.Scraper.csproj", "MarsVista.Scraper/"]

  # Restore dependencies
  RUN dotnet restore "MarsVista.Scraper/MarsVista.Scraper.csproj"

  # Copy source code
  COPY src/MarsVista.Core/ MarsVista.Core/
  COPY src/MarsVista.Scraper/ MarsVista.Scraper/
  ```
- [ ] Test Docker build locally

### Step 10: Testing
- [ ] Build all projects: `dotnet build`
- [ ] Run API locally and verify endpoints work
- [ ] Run scraper console app locally and verify it works
- [ ] Test incremental scraper via API endpoint
- [ ] Run scraper Docker build and test container
- [ ] Verify no functionality regressions

### Step 11: Documentation Updates
- [ ] Update `CLAUDE.md` with new project structure
- [ ] Update `README.md` with Core project description
- [ ] Update `docs/SCRAPER_DEPLOYMENT_GUIDE.md` with new Dockerfile paths
- [ ] Create architecture diagram showing three-project structure

## Technical Decisions

### What Goes in Core vs API?

**MarsVista.Core (Business Logic & Data Access):**
- ✅ Entities (domain models)
- ✅ Database context and migrations
- ✅ Repositories (data access interfaces and implementations)
- ✅ Business services (scrapers, incremental scraper)
- ✅ Configuration options (strongly-typed classes)
- ✅ Extension methods (JSON helpers, etc.)

**MarsVista.Api (Web Layer):**
- ✅ Controllers (HTTP endpoints)
- ✅ Middleware (authentication, rate limiting)
- ✅ Program.cs (web host configuration)
- ✅ API-specific DTOs (if any)
- ✅ Swagger/OpenAPI configuration
- ✅ CORS policies

**MarsVista.Scraper (Console Application):**
- ✅ Program.cs only (entry point, DI setup, execution logic)
- ✅ appsettings.json (configuration)
- ✅ Dockerfile (deployment)

### Namespace Strategy

- Core: `MarsVista.Core.*` (e.g., `MarsVista.Core.Entities`, `MarsVista.Core.Services`)
- API: `MarsVista.Api.*` (e.g., `MarsVista.Api.Controllers`, `MarsVista.Api.Middleware`)
- Scraper: `MarsVista.Scraper` (single namespace, minimal code)

### Migration Strategy

EF Core migrations stay in `MarsVista.Core`:
- Design-time context factory may need adjustment
- Migration commands run from Core project directory
- Both API and Scraper apply migrations using Core's DbContext

## Benefits

### Immediate Benefits
1. **Cleaner separation of concerns** - Business logic isolated from web layer
2. **Smaller scraper deployments** - No ASP.NET Core dependencies
3. **Better testability** - Core can be tested independently
4. **Reusability** - Easy to create new console apps or services

### Future Benefits
1. **Microservices readiness** - Core can be shared across multiple services
2. **Easier testing** - Unit test business logic without web infrastructure
3. **gRPC/Blazor support** - Core can be reused in different app types
4. **Library publishing** - Could publish Core as NuGet package if needed

## Risks and Mitigations

### Risk 1: Breaking Changes During Refactor
**Mitigation**:
- Work in a feature branch
- Test thoroughly before merging
- Keep namespace changes minimal

### Risk 2: EF Core Migrations Issues
**Mitigation**:
- Test migration generation/application in development
- Document new migration workflow
- Keep migrations in Core project

### Risk 3: Circular Dependencies
**Mitigation**:
- Ensure Core has no dependencies on API or Scraper
- API and Scraper only reference Core, never each other
- Review dependency graph before committing

### Risk 4: Deployment Issues
**Mitigation**:
- Test Docker builds locally before deploying
- Update Railway configuration if needed
- Keep rollback plan ready

## Success Criteria

- [ ] All three projects (Core, API, Scraper) build without errors
- [ ] API endpoints function identically to before refactor
- [ ] Scraper console app runs successfully (local and Docker)
- [ ] Docker image for scraper is smaller than before
- [ ] No runtime behavior changes detected
- [ ] All existing tests pass (if any)
- [ ] Documentation updated to reflect new structure

## Estimated Effort

- **Complexity**: Medium (file moves, namespace updates, testing)
- **Time**: 2-3 hours (careful refactoring, testing, documentation)
- **Risk**: Low (no business logic changes, only structural refactoring)

## Dependencies

- Requires working knowledge of:
  - .NET project references
  - EF Core migrations
  - Docker multi-stage builds
  - Namespace refactoring

## Notes

- This is a pure refactoring story - no new features or behavior changes
- All functionality must work identically after the refactor
- The goal is better architecture, not new capabilities
- Consider doing this during a low-activity period to minimize risk
- Can be done incrementally if needed (move one layer at a time)

## Future Enhancements (Out of Scope)

- Add unit tests for Core services
- Create integration test project
- Add XML documentation comments to public APIs
- Consider CQRS pattern with MediatR
- Add domain events for better decoupling
