# Mars Vista API

A C#/.NET API providing access to Mars rover photo data from Perseverance, Curiosity, Opportunity, and Spirit.

## Production API

**Base URL**: `https://api.marsvista.dev`

### Authentication

1. Sign in at [marsvista.dev/signin](https://marsvista.dev/signin)
2. Generate an API key from your dashboard
3. Include the key in all requests via the `X-API-Key` header

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers"
```

### Documentation

- **Interactive API docs**: https://api.marsvista.dev/swagger/index.html
- **User documentation**: https://marsvista.dev/docs

### Health Check (no auth required)

```bash
curl "https://api.marsvista.dev/health"
```

## Local Development

### Prerequisites

- .NET 9.0 SDK
- Docker and Docker Compose

### Setup

```bash
# Start PostgreSQL
docker compose up -d

# Run migrations
dotnet ef database update --project src/MarsVista.Api

# Start the API
dotnet run --project src/MarsVista.Api
```

The API runs at `http://localhost:5127`

## Tech Stack

- .NET 9.0
- PostgreSQL 15
- Entity Framework Core
- Redis (caching)

## License

MIT License - see [LICENSE](LICENSE) file.

## Acknowledgments

Raw image data provided by NASA/JPL-Caltech. This project is not affiliated with or endorsed by NASA.
