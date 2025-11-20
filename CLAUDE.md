# CLAUDE.md

This file provides guidance to Claude Code when working in this repository. Generic guidelines are in global skills.

**Global Skills Available:**
- `commit-guide` - Commit best practices (enforced by hooks)
- `functional-architecture` - Architecture patterns, pure functions, Grug principles

## Project Overview

Recreating the NASA Mars Rover API from scratch in C#/.NET (reference: /home/james/git/mars-photo-api).

### Current Implementation Status

**Completed Stories (1-11):**
- Story 001: Project scaffolding and PostgreSQL setup
- Story 002: Database schema with EF Core migrations
- Story 003: Rover and camera seed data
- Story 004: HTTP client with Polly resilience policies
- Story 005: Scraper service interface and strategy pattern
- Story 006: Perseverance NASA API scraper with manual trigger endpoints
- Story 007: Public query API endpoints (NASA-compatible)
- Story 008: Curiosity rover scraper with camera name mapping
- Story 009: Unified Next.js frontend application with Auth.js authentication
- Story 010: API key authentication and user management with rate limiting
- Story 011: Incremental photo scraper and daily update automation

**System Capabilities:**

**Backend (C#/.NET API):**
- PostgreSQL 15 photos database with Docker Compose (separate from auth database)
- Complete rover/camera data for Perseverance, Curiosity, Opportunity, Spirit
- Hybrid storage: indexed columns + JSONB for 100% NASA data preservation
- HTTP resilience: exponential backoff retry + circuit breaker
- Idempotent photo ingestion with duplicate detection
- Multi-rover support: Curiosity and Perseverance scrapers
- **API Key Authentication:**
  - Per-user API keys stored as SHA-256 hashes in photos database
  - API key format: `mv_live_{40-char-random}` (47 chars total)
  - Links to Auth.js users via email (logical link across databases)
  - Internal API endpoints (`/api/v1/internal/*`) for trusted Next.js proxy
  - Scraper endpoints use separate admin API key
- **Rate Limiting:**
  - Free tier: 60 req/hour, 500 req/day, 3 concurrent
  - Pro tier: 5,000 req/hour, 100,000 req/day, 50 concurrent
  - Enterprise tier: 100,000+ req/hour, unlimited daily, 100 concurrent
  - In-memory tracking (sufficient for single-instance deployment)
  - Rate limit headers on all responses (X-RateLimit-*)
- **Incremental Scraper (MarsVista.Scraper):**
  - Standalone .NET console app for automated daily updates
  - Queries NASA API for current mission sol (not database max)
  - 7-sol lookback window handles delayed photo transmissions
  - Scraper state tracking per rover in database
  - Idempotent: one run captures all new photos, subsequent runs find zero
  - Deployed as Railway cron job (daily at 2 AM UTC)
  - Exit codes for monitoring (0=success, 1=failure)
  - Structured JSON logging with Serilog
- Bulk scraper: POST /api/scraper/{rover}/bulk?startSol=X&endSol=Y
- Progress monitoring: GET /api/scraper/{rover}/progress
- CLI tools:
  - ./scrape-monitor.sh {rover} - Real-time visual progress monitoring
  - ./scrape-retry-failed.sh - Retry failed sols from bulk scrape
  - ./scrape-resume.sh - Resume scraping from specific sol
  - ./db-backup.sh - Create local database backups
  - ./db-restore-to-railway.sh - Restore backup to remote database
  - ./db-sync-to-railway.sh - Sync/upsert local data to remote database
- Query API: GET /api/v1/rovers/{name}/photos with filtering (requires API key)
- Performance: 500+ photos in ~20 seconds, full rover scrape ~9-10 hours, incremental scrape ~5-60 seconds

**Frontend (Next.js Web App):**
- Location: `web/app/`
- Next.js 16 with App Router and TypeScript
- Auth.js (NextAuth v5) with magic link authentication via Resend
- Prisma ORM for Auth.js database tables (User, Session, VerificationToken)
- Tailwind CSS for styling
- Pages implemented:
  - `/` - Landing page with hero, features, and quick start
  - `/docs` - API documentation with Redoc integration
  - `/pricing` - Pricing tiers (Free, Pro, Enterprise)
  - `/signin` - Magic link email authentication
  - `/dashboard` - User dashboard with API key management (protected route)
- **API Key Management:**
  - Generate, view (masked), and regenerate API keys from dashboard
  - Next.js API routes (`/api/keys/*`) act as trusted proxy to C# API
  - Validates Auth.js session, calls C# internal endpoints with shared secret
  - Copy-to-clipboard functionality with usage examples
- Shared components: Header (with auth state), Footer, Hero, Features, QuickStart, ApiKeyManager, CopyButton
- Auth middleware protects /dashboard routes
- Loading states and error boundaries
- Responsive design (mobile/tablet/desktop)
- **Two Authentication Systems:**
  - **Auth.js (Dashboard):** Magic link authentication for web dashboard access
  - **API Keys (C# API):** Per-user keys for programmatic API access
  - Linked by email: `User.email` (auth DB) ↔ `api_keys.user_email` (photos DB)
- **Database Architecture:** Separate PostgreSQL database for auth (clean separation from photos DB)
  - Local: `marsvista_auth_dev` (Auth.js) vs `marsvista_dev` (Photos)
  - Railway: Separate PostgreSQL instances
  - Migration systems: Prisma Migrate (Auth.js) vs EF Core (C# API)
  - No migration conflicts, professional microservices pattern

**Scraper Implementation Pattern:**
- **ALWAYS use direct JSON parsing** (Perseverance approach): `JsonDocument.Parse(element.GetRawText())`
- **NEVER use DTO mapping** for raw_data storage - it risks missing fields
- Use helper methods: `TryGetString()`, `TryGetInt()`, `TryGetDateTime()` for field extraction
- Store complete NASA response in raw_data JSONB column for 100% data preservation

**Technical Decisions Documented:** 18 decisions covering API architecture, database design, storage strategy, scraper patterns, resilience policies, etc.

### Development Workflow

When implementing new features:
1. Create user story/ticket in `.claude/` directory with requirements and implementation steps
2. Document all non-trivial technical decisions in `.claude/decisions/` with trade-off analysis
3. Update main `DECISIONS.md` index file with decision summaries
4. Implement the story following functional architecture principles
5. **Update documentation** as you go (see Documentation Updates section below)
6. Make atomic commits with clear technical reasoning

Every non-trivial technical decision should be documented. Don't assume decisions based on implementation guides - sanity check with documented analysis. Question and decide differently if appropriate.

### Commit and Push Workflow

**When to commit:**
1. After completing each atomic logical change (one bug fix, one feature, one refactoring)
2. After finishing a story (may involve multiple atomic commits)
3. Before switching context to a different task
4. After tests pass and build succeeds
5. After adding/updating documentation

**When to push:**
1. **ALWAYS after completing a story** - ensures work is backed up and visible
2. After creating a significant set of related commits (3-5 commits)
3. At the end of a work session
4. Before asking the user to review your work
5. After any commit that represents important progress

**Important notes:**
- Use the `commit-guide` skill for proper commit message formatting
- Create atomic commits following the principles in the commit guide
- Never commit broken code - build must pass before committing
- Push completed stories immediately to ensure work is not lost

### Documentation Updates

**When completing stories, update relevant documentation:**

1. **CLAUDE.md** (this file):
   - Add completed story to "Completed Stories" list
   - Update "System Capabilities" with new features
   - Add new reference documents to "Essential reference documents"
   - Update expected file paths if structure changes

2. **README.md**:
   - Update feature list if adding major capabilities
   - Update API endpoints section if adding new routes
   - Update "Development Status" to reflect completed work
   - Add links to new documentation in `docs/`

3. **docs/** directory:
   - Create/update guides for new features (e.g., `CURIOSITY_SCRAPER_GUIDE.md`)
   - Update `API_ENDPOINTS.md` with new endpoints
   - Update `DATABASE_ACCESS.md` with new queries/tables
   - Keep guides focused and cross-referenced

**Golden rule**: If you had to figure something out (credentials, endpoints, gotchas), document it so you don't have to figure it out again.

### Problem Documentation

**IMPORTANT:** When encountering problems during implementation, document the solution so it's not forgotten in future sessions:

1. **During implementation:** If you struggle with an issue (database credentials, API routes, null coalescing with empty strings, etc.), note the problem and solution
2. **Where to document:**
   - Add to relevant guide in `docs/` (e.g., troubleshooting section)
   - Update TROUBLESHOOTING.md with common issues
   - Include fix details in story completion notes
3. **What to document:**
   - The problem encountered
   - Why it was confusing or non-obvious
   - The solution that worked
   - How to avoid it in the future

**Examples of issues to document:**
- Database credentials that differ from expected
- API endpoint paths that required trial-and-error
- C# gotchas (null coalescing with empty strings)
- Build/runtime issues (zombie processes on ports)

This prevents re-encountering the same issues in future stories or conversation resumptions.

### Database Backup and Deployment

**Backing up the local database:**

After completing major scrapes or milestones, create backups:

```bash
./db-backup.sh [optional_name]
```

Backups are stored in `./backups/` (gitignored) as PostgreSQL custom format dumps (.dump files).

**Deploying to Railway (production):**

When local database has new data to deploy:

1. **Simple restore (recommended):** Replaces Railway database with local backup
   ```bash
   ./db-restore-to-railway.sh
   ```
   Automatically uses latest backup from `./backups/`

2. **Upsert sync (advanced):** Merges local and Railway data
   ```bash
   ./db-sync-to-railway.sh --dry-run  # Preview changes
   ./db-sync-to-railway.sh            # Perform sync
   ```

**Railway connection details** are hardcoded in the scripts (credentials in Railway dashboard under "Connect" tab).

**Workflow example:**
```bash
# 1. Complete a large scrape locally
curl -X POST "http://localhost:5127/api/scraper/curiosity/bulk?startSol=1&endSol=4683"

# 2. Backup the results
./db-backup.sh curiosity_complete

# 3. Deploy to Railway
./db-restore-to-railway.sh
```

## Essential reference documents

**User Documentation (docs/):**
- `docs/API_ENDPOINTS.md` - Complete API reference with examples for all endpoints (includes authentication section)
- `docs/AUTHENTICATION_GUIDE.md` - Comprehensive guide to API keys, rate limits, code examples, and troubleshooting
- `docs/DATABASE_ACCESS.md` - Database credentials, queries, and management
- `docs/CURIOSITY_SCRAPER_GUIDE.md` - Curiosity-specific scraper guide and implementation details
- `docs/SCRAPER_DEPLOYMENT_GUIDE.md` - Railway cron deployment guide for incremental scraper with NASA API integration

**Implementation Guides (.claude/):**
- `.claude/ADVANCED_FEATURES_POSSIBILITIES.md` - Advanced features (panoramas, stereo pairs, location search, analytics) possible with enhanced NASA data storage
- `.claude/ARCHITECTURE_ANALYSIS.md` - Comprehensive analysis of the Rails Mars Photo API architecture (473 lines, scrapers, caching, patterns)
- `.claude/CSHARP_IMPLEMENTATION_GUIDE_V2.md` - Complete C#/.NET implementation guide with hybrid JSONB storage for 100% NASA data preservation
- `.claude/EXISTING_MARS_TOOLS_COMPARISON.md` - Comparison of existing Mars photo tools and the unique features this API can offer
- `.claude/JSONB_VS_COLUMNS_ANALYSIS.md` - Analysis of database storage strategies (pure columns vs JSONB vs hybrid approach)
- `.claude/KEY_INSIGHTS.md` - Critical implementation insights and gotchas for recreating the NASA Mars Photo API
- `.claude/NASA_API_DOCUMENTATION.md` - Documentation of NASA's unofficial Mars rover image APIs and data sources
- `.claude/NASA_DATA_ANALYSIS.md` - Analysis of NASA's available data fields vs what the Rails API stores (5-10% storage ratio)
- `.claude/NASA_JPL_API_GUIDE.md` - Guide to JPL's direct Mars rover APIs with C# scraper architecture

## Commit Message Rules (Project-Specific)

**NEVER reference in commit messages:**
- `.claude/` directory or any files within it
- `CLAUDE.md` file
- Decision documents (e.g., "Technical decisions documented:")
- Story files or tickets
- Internal AI workflow artifacts

**Why not reference .claude/ in commits?**

While `.claude/` files ARE committed to the repository (they contain valuable project context), they should not be mentioned in commit messages because:
- They are internal planning/analysis documents, not user-facing features
- Commit messages should focus on what changed in the actual codebase
- The public commit history should describe technical changes, not documentation updates

**Exception:** When `.claude/` files contain significant technical analysis that influenced implementation decisions, you may commit them, but the commit message should describe the technical insights gained, not the document itself.

**What to commit in .claude/:**
- ✅ Technical decision documents
- ✅ Implementation guides and analysis
- ✅ User stories and tickets
- ✅ Architecture analysis
- ❌ `settings.local.json` (gitignored)

**Focus commit messages on:**
- What changed in the actual codebase
- Why the change was made (technical reasoning)
- How it works (implementation approach)
- Impact on the system (performance, behavior, etc.)

**When in doubt:**
- Architecture patterns → `functional-architecture` skill
- Commit guidelines → `commit-guide` skill
