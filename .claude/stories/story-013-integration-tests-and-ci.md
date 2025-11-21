# Story 013: Integration Tests with Real PostgreSQL and CI/CD Pipeline

**Status**: Not Started
**Priority**: High
**Estimated Effort**: Medium
**Dependencies**: Story 012 (API v2 Redesign) - Completed

## Overview

Currently, 9 out of 14 integration tests fail due to EF Core in-memory database limitations with `JsonDocument` properties and complex queries. While the 38 unit tests provide excellent controller coverage, we need working integration tests to validate real database behavior, EF Core queries, transactions, and data integrity.

This story implements:
1. Real PostgreSQL database for integration tests (local and CI)
2. GitHub Actions CI/CD pipeline with automated testing
3. Database setup automation (migrations + seed data)
4. Test isolation and cleanup strategies
5. Comprehensive documentation for running tests

## Current Situation

**Test Results** (as of Story 012 completion):
- ✅ 38/38 Unit Tests Passing (100%)
- ⚠️ 5/14 Integration Tests Passing (36%)
- ❌ 9/14 Integration Tests Failing

**Failing Integration Tests**:
```
QueryPhotos_WithCameraFilter_ReturnsOnlyMatchingCameras
QueryPhotos_WithSolRange_FiltersCorrectly
QueryPhotos_WithDateRange_FiltersCorrectly
QueryPhotos_WithIncludeRelationships_IncludesRelatedData
GetStatistics_GroupByCamera_ReturnsCorrectStats
QueryPhotos_WithMultipleCameras_ReturnsAllMatchingCameras
QueryPhotos_WithMultipleRovers_ReturnsPhotoFromBothRovers
QueryPhotos_WithPagination_ReturnsCorrectPage
QueryPhotos_CombinedFilters_AppliesAllFilters
```

**Root Cause**: EF Core in-memory provider doesn't support:
- `JsonDocument` property mapping (our `Photo.RawData` column)
- Complex LINQ queries with joins and filtering
- PostgreSQL-specific features (JSONB, indexes)

## Requirements

### Functional Requirements

**FR1**: Integration tests must use real PostgreSQL database
- Tests should validate actual database behavior
- Support all EF Core features (joins, includes, complex queries)
- Properly test JSONB column usage

**FR2**: Test database isolation
- Each test run uses a clean database state
- Tests don't interfere with development database
- Parallel test execution support (if needed)

**FR3**: Automated CI/CD pipeline
- GitHub Actions workflow for all PRs
- Automatic PostgreSQL service container setup
- Run migrations and seed data before tests
- Report test results in PR checks

**FR4**: Local test execution
- Developers can run integration tests locally
- Use existing Docker Compose PostgreSQL
- Simple setup instructions

### Non-Functional Requirements

**NFR1**: Fast test execution
- Test database setup < 5 seconds
- Full test suite < 30 seconds
- Efficient cleanup between tests

**NFR2**: Reliable and reproducible
- Tests produce consistent results
- No flaky tests due to timing or state
- Deterministic test data

**NFR3**: Maintainable
- Clear separation between test and production configs
- Easy to add new integration tests
- Well-documented setup process

## Technical Approach

### 1. Test Database Strategy

**Database Per Test Run** (Recommended):
```
marsvista_test_{guid}  // Unique DB per test run
```

Pros:
- Complete isolation between test runs
- Can run tests in parallel
- No cleanup needed (drop DB after tests)

Alternative: **Single Test Database with Transactions**:
```
marsvista_test  // Single DB, rollback after each test
```

### 2. Test Configuration

**appsettings.Test.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=marsvista_test;Username=postgres;Password=test_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

**Environment Detection**:
```csharp
// Use environment variable to detect test environment
var isTestEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Test";
```

### 3. Integration Test Base Class

Create `IntegrationTestBase` for common setup:

```csharp
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected MarsVistaDbContext DbContext { get; private set; } = null!;
    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // 1. Create unique test database
        var connectionString = GetTestConnectionString();

        // 2. Run EF Core migrations
        await ApplyMigrationsAsync(connectionString);

        // 3. Seed required data (rovers, cameras)
        await SeedTestDataAsync();

        // 4. Set up services
        ServiceProvider = CreateServiceProvider(connectionString);
        DbContext = ServiceProvider.GetRequiredService<MarsVistaDbContext>();
    }

    public async Task DisposeAsync()
    {
        // Clean up: Drop test database or rollback transaction
        await CleanupAsync();
        await DbContext.DisposeAsync();
        await ServiceProvider.DisposeAsync();
    }

    protected abstract Task SeedTestDataAsync();
}
```

### 4. GitHub Actions Workflow

**.github/workflows/ci.yml**:
```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_DB: marsvista_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: test_password
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v4

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Run migrations
      env:
        ConnectionStrings__DefaultConnection: "Host=localhost;Database=marsvista_test;Username=postgres;Password=test_password"
      run: dotnet ef database update --project src/MarsVista.Api

    - name: Run tests
      env:
        ASPNETCORE_ENVIRONMENT: Test
        ConnectionStrings__DefaultConnection: "Host=localhost;Database=marsvista_test;Username=postgres;Password=test_password"
      run: dotnet test --no-build --verbosity normal

    - name: Upload test results
      if: always()
      uses: actions/upload-artifact@v4
      with:
        name: test-results
        path: '**/TestResults/*.trx'
```

## Implementation Steps

### Phase 1: Local Integration Test Setup

**Step 1.1**: Create test configuration
- [ ] Add `appsettings.Test.json` with test database connection string
- [ ] Create test-specific Docker Compose service (optional)
- [ ] Document connection string format

**Step 1.2**: Create `IntegrationTestBase` class
- [ ] File: `tests/MarsVista.Api.Tests/Integration/IntegrationTestBase.cs`
- [ ] Implement `IAsyncLifetime` for setup/teardown
- [ ] Add method to create test database
- [ ] Add method to run EF Core migrations
- [ ] Add method to seed rovers and cameras
- [ ] Add cleanup logic (drop database or transaction rollback)

**Step 1.3**: Update `PhotoQueryIntegrationTests`
- [ ] Inherit from `IntegrationTestBase`
- [ ] Remove `InMemoryModelCustomizer` (no longer needed)
- [ ] Remove in-memory database configuration
- [ ] Keep existing test seed data logic
- [ ] Verify all 14 tests pass with real PostgreSQL

**Step 1.4**: Test locally
- [ ] Start PostgreSQL with Docker Compose: `docker compose up -d`
- [ ] Run integration tests: `dotnet test --filter "FullyQualifiedName~Integration"`
- [ ] Verify all tests pass
- [ ] Check test execution time

### Phase 2: GitHub Actions CI/CD Pipeline

**Step 2.1**: Create GitHub Actions workflow
- [ ] File: `.github/workflows/ci.yml`
- [ ] Configure PostgreSQL service container
- [ ] Add health checks for database readiness
- [ ] Set appropriate timeout values

**Step 2.2**: Configure workflow steps
- [ ] Checkout code
- [ ] Setup .NET 9.0
- [ ] Restore NuGet packages
- [ ] Build solution
- [ ] Run EF Core migrations
- [ ] Execute all tests (unit + integration)
- [ ] Upload test results as artifacts

**Step 2.3**: Environment variables
- [ ] Set `ASPNETCORE_ENVIRONMENT=Test`
- [ ] Configure `ConnectionStrings__DefaultConnection`
- [ ] Pass to both migration and test steps

**Step 2.4**: Test result reporting
- [ ] Configure test logger to output TRX files
- [ ] Upload results as GitHub Actions artifacts
- [ ] Display pass/fail status in PR checks

### Phase 3: Documentation

**Step 3.1**: Update test documentation
- [ ] File: `docs/TESTING.md` (new file)
- [ ] Document test types (unit vs integration)
- [ ] Explain test database setup
- [ ] Provide local test execution commands
- [ ] Document CI/CD pipeline

**Step 3.2**: Update README.md
- [ ] Add "Running Tests" section
- [ ] Link to detailed testing documentation
- [ ] Add CI badge from GitHub Actions

**Step 3.3**: Update CLAUDE.md
- [ ] Add note about integration test requirements
- [ ] Document test database connection string format
- [ ] Add troubleshooting tips

### Phase 4: Verification and Cleanup

**Step 4.1**: Verify locally
- [ ] Run all tests: `dotnet test`
- [ ] Verify 79/79 tests passing
- [ ] Check test execution time (target: < 30s)
- [ ] Verify test database cleanup

**Step 4.2**: Test CI/CD pipeline
- [ ] Create test PR to trigger workflow
- [ ] Verify PostgreSQL container starts
- [ ] Verify migrations run successfully
- [ ] Verify all tests pass in CI
- [ ] Check test results in GitHub Actions UI

**Step 4.3**: Clean up old code
- [ ] Remove `InMemoryModelCustomizer` class
- [ ] Remove in-memory database configuration
- [ ] Update any outdated comments

**Step 4.4**: Final verification
- [ ] Run full test suite locally: `dotnet test`
- [ ] Run full test suite in CI (via PR)
- [ ] Verify test coverage report
- [ ] Review and merge PR

## Testing Strategy

### Test Scenarios

**Local Development**:
1. Run unit tests only: `dotnet test --filter "FullyQualifiedName~Controllers"`
2. Run integration tests only: `dotnet test --filter "FullyQualifiedName~Integration"`
3. Run all tests: `dotnet test`

**CI/CD Pipeline**:
1. Every PR triggers full test suite
2. Tests must pass before merge
3. Test results displayed in PR checks

### Expected Outcomes

After implementation:
- ✅ 79/79 tests passing (100%)
- ✅ All integration tests validate real database behavior
- ✅ CI/CD pipeline prevents breaking changes
- ✅ Fast feedback loop for developers
- ✅ Confidence in v2 API correctness

## Success Criteria

1. **All Integration Tests Pass**
   - 14/14 integration tests passing with real PostgreSQL
   - No flaky or intermittent failures
   - Test execution time < 30 seconds

2. **CI/CD Pipeline Functional**
   - GitHub Actions workflow triggers on all PRs
   - PostgreSQL service container starts successfully
   - Migrations run without errors
   - All tests execute and report results
   - PR shows green check or red X appropriately

3. **Local Development Works**
   - Developers can run integration tests locally
   - Clear setup instructions (< 5 steps)
   - Tests use existing Docker Compose PostgreSQL
   - No manual database cleanup required

4. **Documentation Complete**
   - Testing guide explains unit vs integration tests
   - Local test execution documented
   - CI/CD pipeline explained
   - Troubleshooting section included

5. **Code Quality**
   - No hardcoded connection strings
   - Proper use of configuration and environment variables
   - Clean separation of test and production configs
   - Well-structured test base classes

## Technical Decisions to Document

Create decision documents for:

1. **Test Database Strategy**: Why database-per-run vs single database with transactions
2. **CI/CD Tool Choice**: Why GitHub Actions vs alternatives (CircleCI, Azure Pipelines)
3. **Test Isolation Approach**: How we ensure tests don't interfere with each other
4. **Performance Optimization**: Strategies for fast test execution

## Related Files

### Existing Files to Modify
- `tests/MarsVista.Api.Tests/Integration/V2/PhotoQueryIntegrationTests.cs`
- `README.md`
- `.claude/CLAUDE.md`

### New Files to Create
- `.github/workflows/ci.yml`
- `tests/MarsVista.Api.Tests/Integration/IntegrationTestBase.cs`
- `tests/MarsVista.Api.Tests/appsettings.Test.json`
- `docs/TESTING.md`
- `.claude/decisions/021-integration-test-database-strategy.md`
- `.claude/decisions/022-ci-cd-pipeline-design.md`

## Dependencies

- Docker (for local PostgreSQL)
- Docker Compose (for local development)
- GitHub Actions (for CI/CD)
- PostgreSQL 15
- .NET 9.0 SDK
- EF Core CLI tools

## Resources

### Documentation
- [GitHub Actions PostgreSQL Service](https://docs.github.com/en/actions/using-containerized-services/creating-postgresql-service-containers)
- [EF Core Testing Guidance](https://learn.microsoft.com/en-us/ef/core/testing/)
- [xUnit IAsyncLifetime](https://xunit.net/docs/shared-context#async-lifetime)

### Example Workflows
- [ASP.NET Core CI/CD Example](https://github.com/dotnet/aspnetcore/blob/main/.github/workflows/ci.yml)
- [EF Core Test Setup](https://github.com/dotnet/efcore/tree/main/test)

## Notes

- Integration tests should test the PhotoQueryServiceV2 service layer, not just the controllers
- Consider adding performance benchmarks for query execution time
- May want to add test coverage reporting in future story
- Consider adding database seeding scripts for common test scenarios
- Test data should be realistic (actual Mars rover photo data samples)

## Estimated Timeline

- Phase 1 (Local Tests): 2-3 hours
- Phase 2 (CI/CD): 1-2 hours
- Phase 3 (Documentation): 1 hour
- Phase 4 (Verification): 1 hour

**Total**: 5-7 hours

## Definition of Done

- [ ] All 79 tests passing (38 unit + 41 integration)
- [ ] GitHub Actions workflow file created and working
- [ ] Tests run successfully in CI for a test PR
- [ ] Documentation updated (README, TESTING.md, CLAUDE.md)
- [ ] Code committed and pushed to main
- [ ] CI badge added to README
- [ ] Integration test base class created and used
- [ ] Test database cleanup verified
- [ ] Performance metrics documented (test execution time)
