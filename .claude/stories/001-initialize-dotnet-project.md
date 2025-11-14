# Story 001: Initialize .NET Project Structure

## Story
As a developer, I need to set up the initial .NET Web API project structure so that I have a foundation to build the Mars Vista API.

## Acceptance Criteria
- [ ] Solution file created for the project
- [ ] ASP.NET Core Web API project initialized
- [ ] Project builds successfully with `dotnet build`
- [ ] Project runs successfully with `dotnet run`
- [ ] Swagger/OpenAPI documentation is accessible at /swagger
- [ ] Basic health check endpoint returns 200 OK
- [ ] .gitignore properly configured for .NET (bin/, obj/, etc.)

## Context
This is the foundational step for the Mars Vista API. We're creating a C#/.NET alternative to the Ruby on Rails NASA Mars Photo API. The project will eventually provide endpoints to query Mars rover photos with enhanced data storage using PostgreSQL with JSONB support.

For this story, we're just getting the basic ASP.NET Core Web API scaffold in place.

## Implementation Steps

### 1. Create the Solution
Create a new solution file to organize the project(s).

```bash
dotnet new sln -n MarsVista
```

**Documentation:**
- [dotnet new sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates#sln)

### 2. Create the Web API Project
Create an ASP.NET Core Web API project with controllers.

```bash
dotnet new webapi -n MarsVista.Api -o src/MarsVista.Api --use-controllers
```

**IMPORTANT:** The `--use-controllers` flag is required in .NET 8+. Without it, the template creates a minimal API (endpoints defined in Program.cs with lambda functions) instead of a controller-based API.

**Key decisions:**
- Using controllers-based approach (not minimal APIs) for better structure as the API grows
- Project will be in `src/` directory for clean organization
- See DECISIONS.md for detailed analysis of this choice

**Documentation:**
- [ASP.NET Core Web API](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [dotnet new webapi](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates#webapi)

### 3. Add Project to Solution
Link the Web API project to the solution.

```bash
dotnet sln add src/MarsVista.Api/MarsVista.Api.csproj
```

### 4. Verify the Project Structure
Your directory should look like:
```
mars-vista-api/
├── .claude/
├── .git/
├── .gitignore
├── CLAUDE.md
├── README.md
├── MarsVista.sln
└── src/
    └── MarsVista.Api/
        ├── Controllers/
        ├── MarsVista.Api.csproj
        ├── Program.cs
        ├── appsettings.json
        └── ...
```

### 5. Configure Basic Settings (Optional for Story 001)
Review and understand:
- `Program.cs` - Application entry point and service configuration
- `appsettings.json` - Configuration settings
- `launchSettings.json` - Development server settings

**Documentation:**
- [Program.cs in ASP.NET Core 6+](https://learn.microsoft.com/en-us/aspnet/core/migration/50-to-60#new-hosting-model)
- [Configuration in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

### 6. Build the Project
```bash
dotnet build
```

Should complete without errors.

### 7. Run the Project
```bash
dotnet run --project src/MarsVista.Api
```

Should start the development server (typically on https://localhost:5001 and http://localhost:5000).

### 8. Test Swagger UI
Navigate to `https://localhost:5001/swagger` in your browser. You should see the Swagger UI with the default WeatherForecast endpoint.

**Documentation:**
- [Swagger/OpenAPI in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)

## Testing
- Run `dotnet build` - should succeed with no errors
- Run `dotnet run --project src/MarsVista.Api` - should start server
- Access `http://localhost:5000/swagger` - should show Swagger UI
- Try the WeatherForecast GET endpoint - should return sample data

## Technical Notes

### Project Template Defaults
The `dotnet new webapi` template includes:
- Swagger/OpenAPI support (Swashbuckle)
- HTTPS redirection
- A sample WeatherForecast controller
- Development and production configuration files

### .gitignore
The existing .gitignore should already exclude:
- `bin/` and `obj/` directories
- User-specific files (`.vs/`, `*.user`, etc.)

Verify this is working correctly.

### Next Steps
After completing this story, you'll be ready for:
- Story 002: Set up PostgreSQL with Docker
- Story 003: Configure Entity Framework Core
- Story 004: Design the database schema

## Key Documentation Links

**Essential Reading:**
1. [ASP.NET Core Web API Tutorial](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
2. [.NET CLI Overview](https://learn.microsoft.com/en-us/dotnet/core/tools/)
3. [Project File (csproj) Reference](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)
4. [ASP.NET Core Fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/)

**Helpful for Understanding:**
5. [Dependency Injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
6. [Routing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)
7. [Environments (Development, Staging, Production)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/environments)

## Success Criteria
✅ You have a running ASP.NET Core Web API
✅ Swagger documentation is accessible
✅ You understand the basic project structure
✅ The project builds and runs without errors
✅ You're ready to add database support in the next story
