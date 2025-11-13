# Database Setup

This project uses PostgreSQL 15 running in Docker for local development.

## Prerequisites

- Docker installed and running
- Docker Compose installed (usually included with Docker Desktop)

## Quick Start

### Start PostgreSQL

```bash
docker-compose up -d
```

The `-d` flag runs the container in detached mode (background).

### Stop PostgreSQL

```bash
docker-compose down
```

### Stop PostgreSQL and Delete All Data

```bash
docker-compose down -v
```

The `-v` flag removes the volume, deleting all database data.

## Database Connection Details

- **Host**: localhost
- **Port**: 5432
- **Database**: marsvista_dev
- **Username**: marsvista
- **Password**: marsvista_dev_password

## Useful Commands

### View Container Status

```bash
docker ps
```

Should show `marsvista-postgres` in the list.

### View Database Logs

```bash
docker logs marsvista-postgres
```

To follow logs in real-time:

```bash
docker logs -f marsvista-postgres
```

### Access PostgreSQL CLI

Connect to the database using psql inside the container:

```bash
docker exec -it marsvista-postgres psql -U marsvista -d marsvista_dev
```

Once connected, you can run SQL commands:

```sql
-- List all tables
\dt

-- List all databases
\l

-- Describe a table
\d table_name

-- Execute SQL
SELECT * FROM your_table;

-- Exit
\q
```

### Access PostgreSQL from Host (if psql installed)

```bash
psql -h localhost -U marsvista -d marsvista_dev
```

When prompted, enter password: `marsvista_dev_password`

### Check Container Health

```bash
docker ps --filter name=marsvista-postgres
```

The STATUS column should show "healthy" after the container has started.

## Configuration

Configuration is stored in `.env` file at the project root:

```env
POSTGRES_USER=marsvista
POSTGRES_PASSWORD=marsvista_dev_password
POSTGRES_DB=marsvista_dev
POSTGRES_PORT=5432
```

You can modify these values before starting the container. If you change them after the container is created, you'll need to recreate it:

```bash
docker-compose down -v
docker-compose up -d
```

## Data Persistence

Database data is stored in a Docker named volume called `postgres_data`. This means:

- Data persists when you stop/start the container
- Data persists when you restart your computer
- Data is only deleted when you run `docker-compose down -v`

To inspect the volume:

```bash
docker volume inspect marsvista_postgres_data
```

## Troubleshooting

### Port 5432 Already in Use

If you have PostgreSQL running locally on port 5432, you can either:

1. Stop your local PostgreSQL:
   ```bash
   sudo systemctl stop postgresql
   ```

2. Or change the port in `.env`:
   ```env
   POSTGRES_PORT=5433
   ```
   Then connect using `localhost:5433`

### Container Won't Start

Check the logs:

```bash
docker logs marsvista-postgres
```

Common issues:
- Port conflict (see above)
- Insufficient disk space
- Docker daemon not running

### Reset Everything

If something goes wrong, you can completely reset:

```bash
# Stop and remove container and volume
docker-compose down -v

# Remove the Docker image (optional, will be re-downloaded)
docker rmi postgres:15-alpine

# Start fresh
docker-compose up -d
```

## JSONB Support

PostgreSQL 15 includes native JSONB support (binary JSON with indexing). This project uses JSONB to store complete NASA API responses alongside structured columns.

Example JSONB operations:

```sql
-- Create a test table
CREATE TABLE test_json (
    id SERIAL PRIMARY KEY,
    data JSONB
);

-- Insert JSON data
INSERT INTO test_json (data) VALUES
    ('{"name": "Curiosity", "status": "active", "cameras": ["FHAZ", "RHAZ"]}');

-- Query JSON field
SELECT data->>'name' AS rover_name FROM test_json;

-- Query nested JSON
SELECT data->'cameras' AS cameras FROM test_json;

-- Query with JSON condition
SELECT * FROM test_json WHERE data->>'status' = 'active';

-- Clean up
DROP TABLE test_json;
```

## Next Steps

After setting up PostgreSQL:

1. Configure Entity Framework Core
2. Design database schema
3. Create initial migration
4. Start building the API

See the main README.md for the full development roadmap.
