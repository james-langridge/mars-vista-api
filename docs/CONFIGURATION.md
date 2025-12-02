# Configuration Reference

All configuration is done via environment variables. This document lists all available options.

## Required Variables

### DATABASE_URL

PostgreSQL connection string for the photo database.

```bash
DATABASE_URL=postgresql://user:password@host:5432/marsvista
```

**Format:** `postgresql://[user]:[password]@[host]:[port]/[database]`

## Optional Variables

### REDIS_URL

Redis connection string for caching and rate limiting. If not set, the API falls back to in-memory caching.

```bash
REDIS_URL=redis://host:6379
```

**Note:** Without Redis:
- Cache is not shared across API instances
- Rate limits reset on restart
- Recommended for development only

### INTERNAL_API_SECRET

Shared secret for authenticating internal API calls (e.g., from a dashboard frontend).

```bash
INTERNAL_API_SECRET=generate-a-secure-random-string
```

**Generate with:**
```bash
openssl rand -base64 32
```

Internal endpoints (`/api/v1/internal/*`) require this secret in the `X-Internal-Secret` header.

### ADMIN_API_KEY

API key for admin/scraper endpoints. Required to trigger manual scrapes.

```bash
ADMIN_API_KEY=your-admin-key
```

### ASPNETCORE_ENVIRONMENT

Controls runtime behavior and logging.

```bash
ASPNETCORE_ENVIRONMENT=Production  # Production, Development, or Test
```

**Effects:**
- `Development`: Detailed errors, Swagger UI enabled
- `Production`: Minimal errors, optimized performance
- `Test`: Used by CI/CD pipelines

### SENTRY_DSN

Sentry error tracking DSN. If not set, Sentry is disabled.

```bash
SENTRY_DSN=https://key@sentry.io/project
```

## Docker Compose Variables

When using `docker-compose.yml` for local development:

```bash
# PostgreSQL container settings
POSTGRES_USER=marsvista
POSTGRES_PASSWORD=marsvista_dev_password
POSTGRES_DB=marsvista_dev
POSTGRES_PORT=5432
```

## Rate Limiting Configuration

Rate limits are configured in code but can be overridden:

| Limit | Default | Description |
|-------|---------|-------------|
| Hourly | 10,000 | Requests per hour per API key |
| Daily | 100,000 | Requests per day per API key |

Rate limit headers are included in all responses:
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Requests remaining
- `X-RateLimit-Reset`: Unix timestamp when limit resets

## Caching TTLs

Cache durations are configured in code:

| Resource | L1 (Memory) | L2 (Redis) |
|----------|-------------|------------|
| Rovers | 15 minutes | 24 hours |
| Cameras | 15 minutes | 24 hours |
| Active manifests | 15 minutes | 1 hour |
| Inactive manifests | 15 minutes | 1 year |

## Example Configurations

### Local Development

```bash
# .env
DATABASE_URL=postgresql://marsvista:marsvista_dev_password@localhost:5432/marsvista_dev
REDIS_URL=redis://localhost:6379
ASPNETCORE_ENVIRONMENT=Development
```

### Production (Railway)

```bash
# Set in Railway dashboard
DATABASE_URL=${{Postgres.DATABASE_URL}}
REDIS_URL=${{Redis.REDIS_URL}}
ASPNETCORE_ENVIRONMENT=Production
INTERNAL_API_SECRET=<generated-secret>
ADMIN_API_KEY=<generated-key>
SENTRY_DSN=https://key@sentry.io/project
```

### Production (Docker)

```bash
# docker-compose.production.yml environment
DATABASE_URL=postgresql://marsvista:secure-password@postgres:5432/marsvista
REDIS_URL=redis://redis:6379
ASPNETCORE_ENVIRONMENT=Production
```

## Connection String Formats

### PostgreSQL

```
postgresql://[user]:[password]@[host]:[port]/[database]?[options]
```

Common options:
- `sslmode=require` - Require SSL (recommended for production)
- `pooling=true` - Enable connection pooling (default)

### Redis

```
redis://[password]@[host]:[port]/[database]
```

Or with TLS:
```
rediss://[password]@[host]:[port]/[database]
```

## Security Notes

1. **Never commit secrets** - Use environment variables or secret management
2. **Use strong passwords** - Generate with `openssl rand -base64 32`
3. **Enable SSL** - Use `sslmode=require` for PostgreSQL in production
4. **Rotate secrets** - Periodically rotate API keys and passwords
5. **Least privilege** - Database user should only have necessary permissions

## Troubleshooting

### "Connection refused" errors

- Verify the hostname/IP is correct
- Check firewall rules allow the connection
- Ensure the service is running

### "Authentication failed" errors

- Double-check username and password
- Verify the user has access to the specified database
- Check for special characters in password (may need URL encoding)

### Rate limiting not working

- Ensure REDIS_URL is set
- Check Redis is accessible
- Verify Redis connection in health check
