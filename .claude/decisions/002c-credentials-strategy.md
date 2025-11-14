# Decision 002C: Development Credentials Strategy

**Date:** 2025-10-13
**Story:** 002 - Set Up PostgreSQL with Docker
**Status:** Active

## Context

Docker Compose needs database credentials to initialize PostgreSQL. We have several options for how to provide these credentials in development.

## Alternatives Considered

### Option 1: Hardcoded in docker-compose.yml (RECOMMENDED)
**Pros:**
- **Simple** - No extra files, no environment variable setup
- **Team Consistency** - Everyone uses the same credentials automatically
- **Zero Configuration** - `git clone` → `docker-compose up` just works
- **Good for Development** - Dev database is not sensitive
- **Easy to Debug** - Credentials visible in config, no mystery
- **Standard Practice** - Common pattern for development databases

**Cons:**
- Credentials in repository (fine for dev, bad for production)
- Can't easily customize per developer
- If accidentally used for production, credentials would be exposed (mitigated by deployment process)

**Example:**
```yaml
environment:
  POSTGRES_USER: marsvista
  POSTGRES_PASSWORD: marsvista_dev_password
  POSTGRES_DB: marsvista_dev
```

### Option 2: Environment Variables via .env File
**Pros:**
- Can be gitignored for security
- Customizable per developer
- More "production-like" setup

**Cons:**
- **More Complex** - Extra file to manage
- **Inconsistent Team Setup** - Each developer might have different credentials
- **Harder Onboarding** - New developers need to create .env file
- **Overkill for Development** - Dev database has no sensitive data
- Can forget to create .env and get cryptic errors

### Option 3: Docker Secrets
**Pros:**
- Production-ready security
- Encrypted at rest

**Cons:**
- **Way Too Complex for Development** - Designed for production orchestration
- Requires Docker Swarm or Kubernetes
- Complete overkill for local development

## Decision

**Hardcode credentials in docker-compose.yml for development**

## Reasoning

1. **Simplicity** - Development should be frictionless. `git clone` → `docker-compose up` should just work.
2. **Not Sensitive** - Development database is local-only, contains no production data, no PII, no secrets. These credentials cannot be used to access anything sensitive.
3. **Team Efficiency** - No setup required, no ".env file not found" errors, no credential mismatch issues
4. **Clear Separation** - Production deployment will use different credential management (secrets manager, environment variables from hosting provider)
5. **Standard Practice** - Most open-source projects hardcode dev credentials for ease of use
6. **Grug-Approved** - Simple solution for simple problem. No complexity demons.

## Security Notes

- Development credentials are clearly labeled (e.g., `marsvista_dev_password`)
- Docker binds to localhost only by default (not exposed to network)
- Production will use different credential strategy (documented when deploying)
- .gitignore will ignore any `.env.production` files if created

## Trade-offs Accepted

- Credentials visible in repository (acceptable for development-only credentials)
- All developers use same credentials (actually a feature for consistency)

## Implementation

```yaml
# docker-compose.yml
environment:
  POSTGRES_USER: marsvista
  POSTGRES_PASSWORD: marsvista_dev_password  # Clearly labeled as dev
  POSTGRES_DB: marsvista_dev
```

## Production Strategy (Future)

When deploying to production:
- Use cloud provider secrets management (AWS Secrets Manager, Azure Key Vault, etc.)
- Or environment variables set by hosting platform
- Or Kubernetes secrets
- Never use these dev credentials in production

## References

- [Docker Compose Environment Variables](https://docs.docker.com/compose/environment-variables/)
- [12-Factor App Config](https://12factor.net/config)
