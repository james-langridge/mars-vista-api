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

Get a free API key at [marsvista.dev/signin](https://marsvista.dev/signin), then send it in the `X-API-Key` header:

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol=1000"
```

The base URL is `https://api.marsvista.dev`. Every request needs a valid API key.

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
  "https://api.marsvista.dev/api/v2/photos?date_min=2024-01-01&date_max=2024-01-31"

# Golden hour photos
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&mars_time_golden_hour=true"
```

### Include Related Data

```bash
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?include=rover,camera&per_page=10"
```

See the [API Reference](https://marsvista.dev/docs) for the full list of endpoints, query parameters, and response fields.

## Tech Stack

- **.NET 9** - ASP.NET Core, Entity Framework Core
- **PostgreSQL 15** - JSONB for metadata preservation
- **Redis** - Two-level caching (L1 memory + L2 distributed)

## Contributing

Contributions are welcome. To work on the project locally, run `docker compose up -d` to start PostgreSQL and Redis, apply migrations with `dotnet ef database update --project src/MarsVista.Core`, and start the API with `dotnet run --project src/MarsVista.Api`. Please open an issue to discuss significant changes before submitting a pull request against `main`, and make sure `dotnet test` passes. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for an overview of the codebase.

## License

MIT License - see [LICENSE](LICENSE).

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
