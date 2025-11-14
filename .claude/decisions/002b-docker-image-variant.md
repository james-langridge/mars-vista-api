# Decision 002B: Docker Image Variant (Alpine vs Full)

**Date:** 2025-10-13
**Story:** 002 - Set Up PostgreSQL with Docker
**Status:** Active

## Context

PostgreSQL Docker images come in two variants:
- **Full** (`postgres:15`) - Debian-based, ~130MB
- **Alpine** (`postgres:15-alpine`) - Alpine Linux-based, ~80MB

We need to choose which variant to use for development and potentially production.

## Alternatives Considered

### Option 1: Full Debian Image (`postgres:15`)
**Pros:**
- More compatible - all PostgreSQL extensions work
- More debugging tools pre-installed (ps, top, bash, etc.)
- Fewer edge cases and compatibility issues
- Industry standard for production

**Cons:**
- Larger image size (~130MB vs ~80MB)
- Slower downloads and container startup
- More attack surface (more packages installed)

### Option 2: Alpine Image (`postgres:15-alpine`) (RECOMMENDED)
**Pros:**
- **Smaller size** - ~80MB vs ~130MB (38% smaller)
- **Faster downloads** - Matters for CI/CD and team onboarding
- **Faster container startup** - Less to load into memory
- **Smaller attack surface** - Minimal base image
- **Good for development** - Less resource usage on dev machines
- PostgreSQL core functionality identical to full image
- All standard features work (JSONB, extensions we need)

**Cons:**
- Missing some debugging tools (can add if needed)
- Some exotic PostgreSQL extensions may not work (we don't need these)
- Uses musl libc instead of glibc (rarely an issue)
- Slightly less common in production (but widely used)

## Decision

**Use Alpine image (`postgres:15-alpine`)**

## Reasoning

1. **Development Efficiency** - Smaller image = faster downloads, faster CI/CD, less disk space
2. **Resource Efficiency** - Team members will appreciate faster startup and less memory usage
3. **Sufficient for Our Needs** - We're using standard PostgreSQL features (JSONB, indexes, basic queries). No exotic extensions needed.
4. **Production Viability** - Alpine is production-ready and widely used. If we need to switch later, it's just changing the image tag.
5. **Simplicity First** - Start with the lighter option. Can upgrade to full if we hit actual issues (we won't).
6. **Grug-Approved** - Less is more. Smaller image = less stuff to break.

## Trade-offs Accepted

- Fewer debugging tools by default (can install if needed: `apk add bash`)
- Potential (unlikely) compatibility issues with exotic extensions (not relevant for our use case)

## Implementation

```yaml
services:
  postgres:
    image: postgres:15-alpine  # Not postgres:15
```

## Migration Path

If we ever need the full image:
```yaml
image: postgres:15-alpine  # Change to postgres:15
```
All data persists in volumes, so switching is seamless.

## References

- [PostgreSQL Alpine Docker Images](https://hub.docker.com/_/postgres)
- [Alpine Linux Security](https://alpinelinux.org/about/)
- [Docker Best Practices - Use minimal base images](https://docs.docker.com/develop/dev-best-practices/)
