# Mars Vista API

A modern REST API for Mars rover imagery, serving nearly 2 million photos from NASA's Perseverance, Curiosity, Opportunity, and Spirit missions.

[![Status](https://img.shields.io/badge/status-operational-brightgreen)](https://status.marsvista.dev)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

## Why Mars Vista?

NASA's raw image APIs are undocumented and inconsistent across missions. Mars Vista provides:

- **Unified interface** - One API for all four rovers
- **Enhanced metadata** - Access 100% of NASA's data fields (vs ~5% in other APIs)
- **Mars time queries** - Filter by sol, local solar time, golden hour
- **Location search** - Query by site, drive, or proximity
- **Multiple image sizes** - Thumbnails to full resolution
- **Production ready** - Rate limiting, caching, comprehensive docs

## Quick Start

```bash
# Get your free API key at marsvista.dev/signin
curl -H "X-API-Key: YOUR_KEY" \
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol=1000"
```

**[View full documentation →](https://marsvista.dev/docs)**

## Links

| Resource | URL |
|----------|-----|
| Documentation | [marsvista.dev/docs](https://marsvista.dev/docs) |
| API Playground | [api.marsvista.dev/swagger](https://api.marsvista.dev/swagger) |
| Status Page | [status.marsvista.dev](https://status.marsvista.dev) |
| OpenAPI Spec | [marsvista.dev/docs/llm/openapi.json](https://marsvista.dev/docs/llm/openapi.json) |

## Project Structure

```
├── src/
│   ├── MarsVista.Api/       # REST API (controllers, services, middleware)
│   ├── MarsVista.Core/      # Shared library (entities, repositories, DbContext)
│   └── MarsVista.Scraper/   # Daily NASA data ingestion service
├── web/
│   ├── app/                 # Next.js frontend (docs, dashboard, auth)
│   └── status-site/         # Status page (Vite + React)
```

## Tech Stack

- **API**: .NET 9, ASP.NET Core, Entity Framework Core
- **Database**: PostgreSQL 15 with JSONB for metadata preservation
- **Caching**: Redis (two-level: memory + distributed)
- **Frontend**: Next.js 15, Auth.js, Tailwind CSS
- **Infrastructure**: Railway, Vercel

## Local Development

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose
- Node.js 20+ (for frontend)

### API

```bash
docker compose up -d          # Start PostgreSQL + Redis
dotnet ef database update --project src/MarsVista.Core
dotnet run --project src/MarsVista.Api
```

API runs at `http://localhost:5127`

### Frontend

```bash
cd web/app
npm install
npm run dev
```

Frontend runs at `http://localhost:3000`

## License

MIT License - see [LICENSE](LICENSE).

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
