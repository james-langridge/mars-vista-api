# Story 002: Set Up PostgreSQL with Docker

## Story
As a developer, I need a PostgreSQL database with JSONB support running in Docker so that I can develop and test locally without installing PostgreSQL directly on my machine.

## Acceptance Criteria
- [ ] Docker Compose file created for PostgreSQL
- [ ] PostgreSQL 15+ running in Docker container
- [ ] Database configured with proper credentials
- [ ] JSONB support verified (PostgreSQL has this by default)
- [ ] Can connect to database from host machine
- [ ] Database persists data between container restarts (volume mounted)
- [ ] Documentation on how to start/stop the database

## Context
The Mars Vista API will store Mars rover photo data with a hybrid approach:
- Structured columns for queryable fields (rover name, sol, camera, earth date)
- JSONB column for complete NASA API response (preserving 100% of data)

PostgreSQL is ideal because:
- Native JSONB support with indexing and querying
- Robust, production-ready
- Widely used in .NET applications
- Free and open source

Using Docker ensures:
- Consistent development environment across machines
- Easy setup/teardown
- No conflicts with system PostgreSQL installations
- Matches production deployment patterns

## Implementation Steps

### 1. Create docker-compose.yml in Project Root

Create a `docker-compose.yml` file at the root of your project (same level as `MarsVista.sln`).

**What is Docker Compose?**
Docker Compose is a tool for defining and running multi-container Docker applications. You define services in a YAML file, then start them all with one command.

**Basic structure:**
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    container_name: marsvista-postgres
    environment:
      POSTGRES_USER: marsvista
      POSTGRES_PASSWORD: marsvista_dev_password
      POSTGRES_DB: marsvista_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

volumes:
  postgres_data:
```

**Key decisions to document in DECISIONS.md:**
- PostgreSQL version (15+ for latest features vs 14 for stability)
- alpine vs full image (size vs features)
- Credentials strategy (hardcoded for dev vs environment variables)
- Port mapping (5432:5432 vs custom port to avoid conflicts)

**Documentation:**
- [Docker Compose file reference](https://docs.docker.com/compose/compose-file/)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)

### 2. Understand the Configuration

**image: postgres:15-alpine**
- Uses PostgreSQL 15 (latest stable)
- Alpine Linux base (smaller image size)
- Alternative: `postgres:15` (full Debian-based image)

**container_name:**
- Gives the container a friendly name
- Makes it easy to reference: `docker logs marsvista-postgres`

**environment:**
- Sets up initial database and credentials
- These are development credentials (never use in production!)
- `POSTGRES_DB`: Creates this database on first startup
- `POSTGRES_USER`: Database username
- `POSTGRES_PASSWORD`: Database password

**ports:**
- Maps container port 5432 to host port 5432
- Format: `"host:container"`
- Allows connecting from your machine: `localhost:5432`

**volumes:**
- Named volume `postgres_data` persists data
- Without this, data is lost when container stops
- Data stored in Docker-managed location

**restart: unless-stopped**
- Container auto-restarts if it crashes
- Doesn't restart if manually stopped
- Good for development reliability

### 3. Create .env File (Optional but Recommended)

Create `.env` in project root:

```env
# PostgreSQL Configuration
POSTGRES_USER=marsvista
POSTGRES_PASSWORD=marsvista_dev_password
POSTGRES_DB=marsvista_dev
POSTGRES_PORT=5432
```

Then update docker-compose.yml to use these:

```yaml
environment:
  POSTGRES_USER: ${POSTGRES_USER}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
  POSTGRES_DB: ${POSTGRES_DB}
ports:
  - "${POSTGRES_PORT}:5432"
```

**Important:** Add `.env` to `.gitignore` if it contains sensitive data. For this project, dev credentials are fine to commit.

### 4. Add Docker Compose Commands to Documentation

Create `docs/DATABASE.md` or add to `README.md`:

```markdown
## Database Setup

### Start PostgreSQL
docker-compose up -d

### Stop PostgreSQL
docker-compose down

### View logs
docker logs marsvista-postgres

### Access PostgreSQL CLI
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev

### Reset database (deletes all data!)
docker-compose down -v
docker-compose up -d
```

### 5. Start the Database

```bash
docker-compose up -d
```

The `-d` flag runs in detached mode (background).

**First run:**
- Downloads PostgreSQL image (~80MB for alpine)
- Creates container and named volume
- Initializes database

**Subsequent runs:**
- Starts instantly (image already downloaded)
- Data persists from previous sessions

### 6. Verify PostgreSQL is Running

**Check container status:**
```bash
docker ps
```

Should show `marsvista-postgres` container running.

**Check logs:**
```bash
docker logs marsvista-postgres
```

Should show:
```
PostgreSQL init process complete; ready for start up.
database system is ready to accept connections
```

### 7. Test Database Connection

**Using Docker exec:**
```bash
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev
```

You should get a PostgreSQL prompt:
```
marsvista_dev=#
```

**Test commands:**
```sql
-- Check PostgreSQL version
SELECT version();

-- Test JSONB support
CREATE TABLE test_json (id SERIAL PRIMARY KEY, data JSONB);
INSERT INTO test_json (data) VALUES ('{"name": "Curiosity", "status": "active"}');
SELECT data->>'name' as rover_name FROM test_json;
DROP TABLE test_json;

-- Exit
\q
```

**Using psql from host (if installed):**
```bash
psql -h localhost -U marsvista -d marsvista_dev
```

Password: `marsvista_dev_password`

### 8. Update .gitignore

Ensure `.gitignore` includes:
```
# Docker
.env

# PostgreSQL
*.sql
*.dump
```

(Only if `.env` contains production secrets. Development `.env` can be committed.)

## Testing Checklist

- [ ] `docker-compose up -d` starts successfully
- [ ] `docker ps` shows container running
- [ ] `docker logs marsvista-postgres` shows "ready to accept connections"
- [ ] Can connect via `docker exec -it marsvista-postgres psql`
- [ ] JSONB operations work (create table, insert, query)
- [ ] `docker-compose down` stops container
- [ ] `docker-compose up -d` restarts with data intact
- [ ] `docker-compose down -v` removes data (volume deleted)

## Technical Decisions

The following decisions have been documented in `DECISIONS.md` with full trade-off analysis and recommendations. Please review and confirm:

### Decision 002: Database System Selection
**Recommendation:** PostgreSQL
- Native JSONB support (binary JSON with indexing)
- Hybrid relational + JSON storage in same table
- Excellent .NET/EF Core support (Npgsql)
- Free, production-ready, cloud-deployable
- Superior JSON querying compared to SQL Server/MySQL

**Alternatives considered:** SQL Server, MySQL/MariaDB, MongoDB, SQLite
**Why PostgreSQL wins:** JSONB is the best relational database JSON storage, perfect for our hybrid model (queryable columns + complete NASA JSON)

### Decision 002A: PostgreSQL Version Selection
**Recommendation:** PostgreSQL 15
- Stable (2+ years in production)
- JSONB performance improvements over PG14
- LTS until November 2027
- Default in most Docker images and cloud providers

### Decision 002B: Docker Image Variant
**Recommendation:** Alpine image (`postgres:15-alpine`)
- 38% smaller than full image (80MB vs 130MB)
- Faster downloads and startup
- Sufficient for our standard PostgreSQL needs
- Can switch to full image if needed (seamless)

### Decision 002C: Development Credentials Strategy
**Recommendation:** Hardcode in docker-compose.yml
- Simple, zero-configuration setup
- Development database is not sensitive
- Everyone uses same credentials (consistency)
- Production will use different credential management

### Decision 002D: PostgreSQL Port Configuration
**Recommendation:** Standard port 5432:5432
- Convention over configuration
- Works with all default PostgreSQL tooling
- Conflicts unlikely (and easily resolved with override file)

**Action Required:** Review the decisions in `.claude/DECISIONS.md` and confirm or suggest changes before proceeding with implementation.

## Key Documentation Links

**Essential Reading:**
1. [Docker Compose Overview](https://docs.docker.com/compose/)
2. [PostgreSQL Docker Official Image](https://hub.docker.com/_/postgres)
3. [Docker Compose CLI Reference](https://docs.docker.com/compose/reference/)
4. [PostgreSQL JSONB Documentation](https://www.postgresql.org/docs/current/datatype-json.html)

**Helpful for Understanding:**
5. [Docker Volumes](https://docs.docker.com/storage/volumes/)
6. [Docker Networking](https://docs.docker.com/network/)
7. [PostgreSQL Connection Strings](https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING)

## Success Criteria

✅ Technical decisions reviewed and confirmed
✅ PostgreSQL running in Docker
✅ Can connect and run queries
✅ JSONB support verified
✅ Data persists between restarts
✅ Clear documentation on database commands

## Next Steps

After completing this story, you'll be ready for:
- Story 003: Configure Entity Framework Core
- Story 004: Design initial database schema
- Story 005: Create first migration

## Notes

**Why PostgreSQL over MySQL/SQL Server?**
- Native JSONB support (MySQL's JSON is less mature)
- Excellent for complex queries
- Free for all uses (vs SQL Server licensing)
- Industry standard for Rails/Node.js apps (matches original API ecosystem)

**Why Docker over Local Install?**
- Clean separation from system
- Easy reset/rebuild
- Team consistency
- Mirrors production containers

**Database will be empty** - this is correct! Next stories will set up Entity Framework Core and create tables.
