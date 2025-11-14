# CLAUDE.md

This file provides guidance to Claude Code when working in this repository. Generic guidelines are in global skills.

**Global Skills Available:**
- `commit-guide` - Commit best practices (enforced by hooks)
- `functional-architecture` - Architecture patterns, pure functions, Grug principles

## Project Overview

Recreating the NASA Mars Rover API from scratch in C#/.NET (reference: /home/james/git/mars-photo-api).

### Current Implementation Status

**Completed Stories (1-8):**
- Story 001: Project scaffolding and PostgreSQL setup
- Story 002: Database schema with EF Core migrations
- Story 003: Rover and camera seed data
- Story 004: HTTP client with Polly resilience policies
- Story 005: Scraper service interface and strategy pattern
- Story 006: Perseverance NASA API scraper with manual trigger endpoints
- Story 007: Public query API endpoints (NASA-compatible)
- Story 008: Curiosity rover scraper with camera name mapping

**System Capabilities:**
- PostgreSQL 15 database with Docker Compose
- Complete rover/camera data for Perseverance, Curiosity, Opportunity, Spirit
- Hybrid storage: indexed columns + JSONB for 100% NASA data preservation
- HTTP resilience: exponential backoff retry + circuit breaker
- Idempotent photo ingestion with duplicate detection
- Multi-rover support: Curiosity and Perseverance scrapers
- Bulk scraper: POST /api/scraper/{rover}/bulk?startSol=X&endSol=Y
- Progress monitoring: GET /api/scraper/{rover}/progress
- CLI tool: ./scrape-monitor.sh {rover} for real-time visual progress
- Query API: GET /api/v1/rovers/{name}/photos with filtering
- Performance: 500+ photos in ~20 seconds, full rover scrape ~9-10 hours

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

## Essential reference documents

**User Documentation (docs/):**
- `docs/API_ENDPOINTS.md` - Complete API reference with examples for all endpoints
- `docs/DATABASE_ACCESS.md` - Database credentials, queries, and management
- `docs/CURIOSITY_SCRAPER_GUIDE.md` - Curiosity-specific scraper guide and implementation details

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

These are internal development artifacts that are gitignored and should not appear in the public commit history.

**Focus commit messages on:**
- What changed in the actual codebase
- Why the change was made (technical reasoning)
- How it works (implementation approach)
- Impact on the system (performance, behavior, etc.)

**When in doubt:**
- Architecture patterns → `functional-architecture` skill
- Commit guidelines → `commit-guide` skill
