# Decision 002D: PostgreSQL Port Configuration

**Date:** 2025-10-13
**Story:** 002 - Set Up PostgreSQL with Docker
**Status:** Active

## Context

PostgreSQL's default port is 5432. We need to decide whether to use the standard port or a custom port to avoid potential conflicts with existing PostgreSQL installations on developers' machines.

## Alternatives Considered

### Option 1: Standard Port 5432:5432 (RECOMMENDED)
**Pros:**
- **Standard Convention** - Everyone knows PostgreSQL is on 5432
- **Simple Connection Strings** - `localhost:5432` (can omit port in many tools)
- **No Surprises** - Works with default PostgreSQL tool settings
- **Documentation Matches Reality** - All PostgreSQL docs assume 5432
- **Fewer Gotchas** - Team doesn't need to remember custom port

**Cons:**
- May conflict if PostgreSQL already installed on system
- May conflict with other Docker PostgreSQL containers

### Option 2: Custom Port (e.g., 5433:5432)
**Pros:**
- Avoids conflicts with system PostgreSQL
- Can run multiple PostgreSQL containers simultaneously

**Cons:**
- **Non-Standard** - Must remember custom port
- **More Configuration** - Connection strings need explicit port
- **Confusing** - Why not use standard port?
- **Harder Troubleshooting** - Docs and Stack Overflow assume 5432

## Decision

**Use standard port 5432:5432**

## Reasoning

1. **Convention Over Configuration** - Standard port is standard for a reason
2. **Conflict Unlikely** - Most developers won't have system PostgreSQL running, and if they do, they can stop it or adjust
3. **Simplicity** - Don't deviate from standards without good reason
4. **Docker Isolation** - Docker's namespace isolation prevents most conflicts
5. **Easy to Change** - If someone has a conflict, they can override locally (documented below)
6. **Grug-Approved** - Use standard. Standard good. Custom port is premature optimization.

## Conflict Resolution

If a developer has port 5432 in use, they can create `docker-compose.override.yml` (gitignored):

```yaml
# docker-compose.override.yml (per-developer, gitignored)
services:
  postgres:
    ports:
      - "5433:5432"  # Use custom port locally
```

This overrides the default without changing the shared config.

## Trade-offs Accepted

- Potential port conflict (rare, easily solved with override file)
- Cannot run multiple PostgreSQL containers on same port (not a requirement)

## Implementation

```yaml
# docker-compose.yml
services:
  postgres:
    ports:
      - "5432:5432"  # Standard port
```

## Connection String

```
Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password
```

Or simplified (port defaults to 5432):
```
Host=localhost;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password
```

## References

- [PostgreSQL Default Port](https://www.postgresql.org/docs/current/runtime-config-connection.html)
- [Docker Compose Override](https://docs.docker.com/compose/extends/)
