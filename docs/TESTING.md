# Testing Guide

Comprehensive guide for running and understanding tests in the Mars Vista API project.

## Table of Contents

- [Test Types](#test-types)
- [Running Tests Locally](#running-tests-locally)
- [Test Database Setup](#test-database-setup)
- [CI/CD Pipeline](#cicd-pipeline)
- [Writing New Tests](#writing-new-tests)
- [Troubleshooting](#troubleshooting)

## Test Types

The Mars Vista API uses two types of tests:

### Unit Tests

**Location**: `tests/MarsVista.Api.Tests/Controllers/`

**What they test**: Individual controllers and their logic in isolation using mocked dependencies.

**Characteristics**:
- Fast execution (< 1 second)
- No database or external dependencies
- Test controller behavior, validation, and response formatting

**Example**: Testing that the PhotosController returns proper error responses for invalid parameters.

### Integration Tests

**Location**: `tests/MarsVista.Api.Tests/Integration/`

**What they test**: Complete service layer behavior with real PostgreSQL database, EF Core queries, and data integrity.

**Characteristics**:
- Slower execution (~10-15 seconds for full suite)
- Use real PostgreSQL database
- Test actual database queries, relationships, and complex filtering
- Each test class creates a unique temporary database

**Example**: Testing that photo queries with multiple filters return correct results from the database.

## Running Tests Locally

### Prerequisites

1. **PostgreSQL Running**: Integration tests require PostgreSQL to be running
   ```bash
   docker compose up -d  # Start PostgreSQL
   ```

2. **.NET 9.0 SDK**: Ensure you have .NET 9.0 installed
   ```bash
   dotnet --version  # Should show 9.0.x
   ```

### Run All Tests

```bash
dotnet test
```

### Run Only Unit Tests

```bash
dotnet test --filter "FullyQualifiedName~Controllers"
```

### Run Only Integration Tests

```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

### Run Tests with Detailed Output

```bash
dotnet test --verbosity detailed
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~PhotoQueryIntegrationTests.QueryPhotos_WithMultipleRovers"
```

## Test Database Setup

### How Integration Tests Use the Database

Integration tests use a **unique temporary database per test run** to ensure complete isolation:

1. **Test starts**: Creates database named `marsvista_test_{unique-guid}`
2. **Migrations run**: Applies all EF Core migrations to the test database
3. **Seed data**: Adds rovers, cameras, and test photos
4. **Tests execute**: Run queries against the real database
5. **Cleanup**: Drops the test database automatically

**Your development database (`marsvista_dev`) is never touched by tests.**

### Configuration

Test database configuration is in `tests/MarsVista.Api.Tests/appsettings.Test.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password"
  }
}
```

**Note**: This connects to `marsvista_dev` only to execute `CREATE DATABASE` commands for test databases.

### Test Data

Each integration test class seeds required data:

- **Base data** (from `IntegrationTestBase`):
  - 2 Rovers: Curiosity, Perseverance
  - 3 Cameras: FHAZ, MAST, NAVCAM

- **Test-specific data** (from individual test classes):
  - Photos with various sols, dates, cameras for testing filters

## CI/CD Pipeline

### GitHub Actions Workflow

**Location**: `.github/workflows/ci.yml`

**Triggers**:
- Push to `main` branch
- Pull requests to `main` branch

**What it does**:
1. Spins up PostgreSQL 15 service container
2. Checks out code
3. Sets up .NET 9.0 SDK
4. Restores dependencies
5. Builds the solution
6. Runs all tests (unit + integration)
7. Uploads test results as artifacts

**PostgreSQL Service**:
```yaml
services:
  postgres:
    image: postgres:15-alpine
    env:
      POSTGRES_USER: marsvista
      POSTGRES_PASSWORD: marsvista_dev_password
      POSTGRES_DB: marsvista_dev
    ports:
      - 5432:5432
```

### Viewing Test Results

1. Navigate to **Actions** tab in GitHub repository
2. Click on the workflow run
3. View test output in the "Run tests" step
4. Download test results artifacts if needed

## Writing New Tests

### Adding Unit Tests

1. Create test class in `tests/MarsVista.Api.Tests/Controllers/V{version}/`
2. Use Moq to mock dependencies
3. Test controller methods in isolation

**Example**:
```csharp
[Fact]
public async Task GetPhoto_WithInvalidId_ReturnsNotFound()
{
    // Arrange
    var mockService = new Mock<IPhotoQueryServiceV2>();
    mockService.Setup(s => s.GetPhotoByIdAsync(999, It.IsAny<PhotoQueryParameters>(), default))
        .ReturnsAsync((PhotoResource?)null);

    var controller = new PhotosController(mockService.Object);

    // Act
    var result = await controller.GetPhoto(999);

    // Assert
    result.Should().BeOfType<NotFoundResult>();
}
```

### Adding Integration Tests

1. Create test class in `tests/MarsVista.Api.Tests/Integration/V{version}/`
2. Inherit from `IntegrationTestBase`
3. Override `ConfigureServices` to register required services
4. Override `SeedAdditionalDataAsync` to add test-specific data
5. **Important**: Manually populate `IncludeList` when testing relationships

**Example**:
```csharp
public class NewIntegrationTests : IntegrationTestBase
{
    private IPhotoQueryServiceV2 _service = null!;

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPhotoQueryServiceV2, PhotoQueryServiceV2>();
    }

    protected override async Task SeedAdditionalDataAsync()
    {
        _service = ServiceProvider.GetRequiredService<IPhotoQueryServiceV2>();

        // Add test photos
        var photo = new Photo
        {
            NasaId = "TEST_001",
            ImgSrcFull = "https://example.com/photo.jpg",
            Sol = 100,
            EarthDate = DateTime.UtcNow,
            DateTakenUtc = DateTime.UtcNow,
            RoverId = 1,
            CameraId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Photos.Add(photo);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task TestName()
    {
        // Arrange
        var parameters = new PhotoQueryParameters
        {
            Rovers = "curiosity",
            Include = "rover",
            IncludeList = new List<string> { "rover" } // Required for relationships
        };

        // Act
        var result = await _service.QueryPhotosAsync(parameters, default);

        // Assert
        result.Data.Should().NotBeEmpty();
    }
}
```

## Troubleshooting

### PostgreSQL Not Running

**Error**: `could not connect to server`

**Solution**:
```bash
docker compose up -d
docker compose ps  # Verify postgres is running
```

### Database Permission Errors

**Error**: `permission denied to create database`

**Solution**: Verify PostgreSQL user has `CREATEDB` privilege:
```sql
ALTER USER marsvista CREATEDB;
```

### Test Database Cleanup Issues

**Error**: `database "marsvista_test_xxx" already exists`

**Solution**: The test base class automatically cleans up, but if tests crash:
```bash
psql -h localhost -U marsvista -d postgres -c "DROP DATABASE IF EXISTS marsvista_test_xxx"
```

Or drop all test databases:
```bash
psql -h localhost -U marsvista -d postgres -c "SELECT 'DROP DATABASE IF EXISTS ' || datname || ';' FROM pg_database WHERE datname LIKE 'marsvista_test_%'"
```

### DateTime UTC Errors

**Error**: `Cannot write DateTime with Kind=Unspecified to PostgreSQL`

**Solution**: Always use `DateTimeKind.Utc` for DateTime values in tests:
```csharp
EarthDate = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc)
CreatedAt = DateTime.UtcNow
```

### NullReferenceException in Relationships

**Error**: `Object reference not set to an instance of an object` when accessing `Relationships.Rover`

**Solution**: Manually populate `IncludeList` in test parameters:
```csharp
var parameters = new PhotoQueryParameters
{
    Include = "rover,camera",
    IncludeList = new List<string> { "rover", "camera" }  // Add this
};
```

### Migrations Out of Sync

**Error**: `The model for context 'MarsVistaDbContext' has pending changes`

**Solution**: This warning is suppressed in tests, but if you see it:
```bash
dotnet ef migrations add MigrationName --project src/MarsVista.Api
dotnet ef database update --project src/MarsVista.Api
```

## Test Coverage Summary

**Current Status** (as of Story 013 completion):

| Test Type | Total | Passing | Status |
|-----------|-------|---------|--------|
| Unit Tests | 38 | 38 | ✅ 100% |
| Integration Tests | 14 | 6+ | 🟡 In Progress |
| **Total** | **52+** | **44+** | **~85%** |

**Note**: Integration tests are functional with real PostgreSQL. Remaining failures are test assertion refinements being addressed incrementally.

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [EF Core Testing](https://learn.microsoft.com/en-us/ef/core/testing/)
- [GitHub Actions](https://docs.github.com/en/actions)
