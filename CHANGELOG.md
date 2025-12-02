# Changelog

All notable changes to Mars Vista API will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Open source release preparation
- Comprehensive deployment documentation
- Production Docker Compose configuration

## [1.0.0] - 2025-12-01

### Added

#### API Features
- **v1 API** - NASA-compatible endpoints for easy migration
  - `GET /api/v1/rovers` - List all rovers
  - `GET /api/v1/rovers/{name}/photos` - Query photos by rover
  - `GET /api/v1/manifests/{rover}` - Rover mission manifests

- **v2 API** - Enhanced endpoints with advanced features
  - `GET /api/v2/rovers` - Rovers with extended metadata
  - `GET /api/v2/photos` - Multi-rover queries with filtering
  - `GET /api/v2/cameras` - Camera information
  - Mars time filtering (local solar time, golden hour)
  - Location-based queries (site, drive, proximity)
  - Image dimension filtering
  - Field selection and relationship includes

- **Admin API** - Scraper control and monitoring
  - Manual scrape triggers for all rovers
  - Bulk scraping endpoints
  - Scraper status and history

#### Data Features
- **1.5M+ photos** from all four Mars rovers
- **100% NASA metadata** preservation via JSONB storage
- **Multiple image sizes** - thumbnails to full resolution
- **Hybrid storage** - indexed columns + raw NASA data

#### Infrastructure
- PostgreSQL 15 with optimized indexes
- Redis two-level caching (L1 memory + L2 Redis)
- Rate limiting (10K/hour, 100K/day)
- API key authentication
- Response compression (Brotli/Gzip)
- Health check endpoints

#### Scrapers
- Perseverance scraper (NASA raw images API)
- Curiosity scraper (NASA raw images API)
- Opportunity scraper (PDS archive)
- Spirit scraper (PDS archive)
- Incremental daily updates
- 7-sol lookback for delayed transmissions

### Performance
- Sub-second response times with caching
- Connection pooling
- Partial indexes for common queries
- Computed columns for aspect ratio

## [0.1.0] - 2025-01-15

### Added
- Initial project scaffolding
- Basic PostgreSQL schema
- Perseverance scraper prototype
- Simple query API

[Unreleased]: https://github.com/james-langridge/mars-vista-api/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/james-langridge/mars-vista-api/releases/tag/v1.0.0
[0.1.0]: https://github.com/james-langridge/mars-vista-api/releases/tag/v0.1.0
