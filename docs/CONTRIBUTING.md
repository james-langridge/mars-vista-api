# Contributing to Mars Vista API

Thank you for your interest in contributing to Mars Vista API! This document provides guidelines and instructions for contributing.

## Code of Conduct

Be respectful and constructive. We're all here to build something useful.

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- Git
- A code editor (VS Code, Rider, Visual Studio)

### Setting Up Development Environment

```bash
# Clone the repository
git clone https://github.com/james-langridge/mars-vista-api.git
cd mars-vista-api

# Start dependencies
docker compose up -d

# Apply database migrations
dotnet ef database update --project src/MarsVista.Core

# Run the API
dotnet run --project src/MarsVista.Api

# Run tests
dotnet test
```

The API runs at `http://localhost:5127`.

## Project Structure

```
src/
├── MarsVista.Api/       # REST API controllers, services, middleware
├── MarsVista.Core/      # Shared entities, DbContext, repositories
└── MarsVista.Scraper/   # NASA data ingestion service

tests/
├── MarsVista.Api.Tests/
└── MarsVista.Scraper.Tests/
```

## Making Changes

### Branch Naming

Use descriptive branch names:
- `feature/add-mars-time-filter`
- `fix/rate-limit-calculation`
- `docs/update-api-reference`

### Commit Messages

Write clear, concise commit messages:

```
Add Mars time filtering to v2 photos endpoint

Implement local solar time parsing and filtering for photo queries.
Supports golden hour detection for optimal lighting conditions.
```

**Guidelines:**
- Use imperative mood ("Add" not "Added")
- First line: 50 characters or less
- Blank line before body
- Body: explain what and why, not how

### Code Style

- Follow existing code patterns
- Use meaningful variable names
- Keep functions focused and small
- Add XML documentation for public APIs

### Testing

All changes should include tests:

```bash
# Run all tests
dotnet test

# Run specific project tests
dotnet test tests/MarsVista.Api.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Guidelines:**
- Unit tests for business logic
- Integration tests for API endpoints
- Test edge cases and error conditions

## Pull Request Process

### Before Submitting

1. **Update documentation** - If your change affects the API, update:
   - OpenAPI spec: `./scripts/sync-openapi.sh`
   - README if needed

2. **Run tests** - Ensure all tests pass:
   ```bash
   dotnet test
   ```

3. **Build successfully** - Verify the build:
   ```bash
   dotnet build --configuration Release
   ```

### Submitting a PR

1. Push your branch to GitHub
2. Create a pull request against `main`
3. Fill out the PR template:
   - Describe your changes
   - Link related issues
   - Note any breaking changes

### PR Review

- A maintainer will review your PR
- Address any feedback
- Once approved, it will be merged

## Types of Contributions

### Bug Reports

Found a bug? Open an issue with:
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details (OS, .NET version)

### Feature Requests

Have an idea? Open an issue with:
- Use case description
- Proposed solution
- Alternatives considered

### Documentation

Improvements to documentation are always welcome:
- Fix typos
- Clarify explanations
- Add examples
- Translate content

### Code Contributions

Areas where contributions are especially welcome:
- New scraper implementations
- Performance optimizations
- Additional filters/query options
- Test coverage improvements

## Architecture Decisions

For significant changes, please open an issue first to discuss:
- Database schema changes
- New API endpoints
- Breaking changes
- New dependencies

This ensures alignment before you invest significant time.

## Local Development Tips

### Seed Test Data

```bash
# Scrape a few sols for testing
curl -X POST "http://localhost:5127/api/v1/admin/scraper/perseverance?startSol=100&endSol=105"
```

### Reset Database

```bash
# Drop and recreate
docker compose down -v
docker compose up -d
dotnet ef database update --project src/MarsVista.Core
```

### View Logs

```bash
# Follow API logs
dotnet run --project src/MarsVista.Api | jq
```

### Debug in VS Code

`.vscode/launch.json` is configured for debugging the API.

## Questions?

- Open a GitHub issue for questions
- Check existing issues first

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
