# Mars Vista API

A modern REST API for Mars rover imagery, providing unified access to over 1.5 million photos from NASA's Perseverance, Curiosity, Opportunity, and Spirit missions.

[![CI](https://github.com/james-langridge/mars-vista-api/actions/workflows/ci.yml/badge.svg)](https://github.com/james-langridge/mars-vista-api/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- **Unified API** - One interface for all four Mars rovers
- **Complete NASA Data** - 100% metadata preservation (vs ~5% in other APIs)
- **Mars Time Queries** - Filter by sol, local solar time, golden hour
- **Location Search** - Query by site, drive, or proximity
- **Multiple Image Sizes** - Thumbnails to full resolution
- **Production Ready** - Rate limiting, caching, comprehensive documentation

## Quick Start

### Using the Public API

Get a free API key at [marsvista.dev/signin](https://marsvista.dev/signin), then:

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol=1000"
```

### Self-Hosting

```bash
# Clone and start
git clone https://github.com/james-langridge/mars-vista-api.git
cd mars-vista-api

# Start dependencies
docker compose up -d

# Apply migrations
dotnet ef database update --project src/MarsVista.Core

# Run the API
dotnet run --project src/MarsVista.Api
```

API runs at `http://localhost:5127`. See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for production setup.

## API Documentation

| Resource | Description |
|----------|-------------|
| [API Reference](https://marsvista.dev/docs) | Complete documentation |
| [Swagger UI](https://api.marsvista.dev/swagger) | Interactive explorer |
| [OpenAPI Spec](openapi.json) | Machine-readable specification |

### For AI Agents

LLM-optimized documentation:

| Resource | URL |
|----------|-----|
| Discovery | [marsvista.dev/llms.txt](https://marsvista.dev/llms.txt) |
| TypeScript Types | [docs/llm/types.ts](https://marsvista.dev/docs/llm/types.ts) |
| Reference | [docs/llm/reference.md](https://marsvista.dev/docs/llm/reference.md) |

## API Examples

### Get Rovers

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/rovers"
```

### Query Photos

```bash
# By rover and sol
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100"

# By date range
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?earth_date_min=2024-01-01&earth_date_max=2024-01-31"

# Golden hour photos
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&is_golden_hour=true"
```

### Include Related Data

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?include=rover,camera&per_page=10"
```

## Project Structure

```
mars-vista-api/
├── src/
│   ├── MarsVista.Api/         # REST API service
│   ├── MarsVista.Core/        # Shared library (entities, DbContext)
│   └── MarsVista.Scraper/     # NASA data ingestion
├── tests/                     # Unit and integration tests
├── docs/                      # Documentation
│   ├── DEPLOYMENT.md          # Deployment guide
│   ├── CONFIGURATION.md       # Environment variables
│   ├── ARCHITECTURE.md        # System design
│   └── CONTRIBUTING.md        # Contribution guidelines
├── examples/                  # API collection examples
├── scripts/                   # Utility scripts
├── openapi.json              # OpenAPI specification
└── docker-compose.yml        # Local development
```

## Tech Stack

- **.NET 9** - ASP.NET Core, Entity Framework Core
- **PostgreSQL 15** - JSONB for metadata preservation
- **Redis** - Two-level caching (L1 memory + L2 distributed)
- **Docker** - Containerized deployment

## Development

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- PostgreSQL 15+ (or use Docker)
- Redis 7+ (optional, for caching)

### Setup

```bash
# Clone repository
git clone https://github.com/james-langridge/mars-vista-api.git
cd mars-vista-api

# Start PostgreSQL and Redis
docker compose up -d

# Apply database migrations
dotnet ef database update --project src/MarsVista.Core

# Run the API
dotnet run --project src/MarsVista.Api

# Run tests
dotnet test
```

### Populating Data

```bash
# Scrape photos for a sol range
curl -X POST "http://localhost:5127/api/v1/admin/scraper/perseverance?startSol=1000&endSol=1010" \
  -H "X-API-Key: YOUR_ADMIN_KEY"
```

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for bulk data ingestion.

## Deployment

### Docker

```bash
docker compose -f docker-compose.production.yml up -d
```

### Railway

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for Railway deployment instructions.

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `DATABASE_URL` | Yes | PostgreSQL connection string |
| `REDIS_URL` | No | Redis connection (falls back to memory) |
| `INTERNAL_API_SECRET` | No | For dashboard integration |
| `ADMIN_API_KEY` | No | For scraper control |

See [docs/CONFIGURATION.md](docs/CONFIGURATION.md) for complete reference.

## Contributing

Contributions are welcome! Please read [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](LICENSE).

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
