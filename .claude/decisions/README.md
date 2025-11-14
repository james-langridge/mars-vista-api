# Decisions

This directory contains all technical decisions made during the development of Mars Vista API. Each decision is documented in a separate file for easy reference and maintenance.

## File Naming Convention

Decisions are numbered and named according to the story they relate to:

- **001-xxx.md** - Decisions from Story 001
- **002-xxx.md** - Decisions from Story 002
- **002a-xxx.md** - Sub-decisions within Story 002

## Decision Format

Each decision file follows this structure:
- **Date:** When the decision was made
- **Story:** Which story/ticket this decision relates to
- **Status:** Active | Superseded | Deprecated
- **Context:** Why this decision was needed
- **Alternatives Considered:** What other options were evaluated
- **Decision:** What was chosen
- **Reasoning:** Why this choice was made
- **Trade-offs Accepted:** Pros and cons
- **Implementation:** Code examples
- **References:** Links to documentation

## Quick Index

### Story 001: Initialize .NET Project Structure
- [001-controller-based-api.md](001-controller-based-api.md) - Use controller-based API instead of minimal API

### Story 002: Set Up PostgreSQL with Docker
- [002-database-system-selection.md](002-database-system-selection.md) - Use PostgreSQL over SQL Server/MySQL/MongoDB/SQLite
- [002a-postgresql-version.md](002a-postgresql-version.md) - Use PostgreSQL 15 (stable + modern features)
- [002b-docker-image-variant.md](002b-docker-image-variant.md) - Use Alpine image (smaller, faster)
- [002c-credentials-strategy.md](002c-credentials-strategy.md) - Hardcode dev credentials in docker-compose.yml
- [002d-port-configuration.md](002d-port-configuration.md) - Use standard port 5432

## Why Separate Files?

- **Easier to find** - One decision per file, clear naming
- **Better git history** - Changes to one decision don't affect others
- **Simpler to reference** - Link to specific decision files in code comments or PRs
- **Parallel work** - Multiple people can document decisions simultaneously
- **Cleaner diffs** - Small, focused changes

## How to Add a New Decision

1. Create a new file: `XXX-descriptive-name.md` (where XXX is the story number)
2. Use the format from existing decisions
3. Update this README's Quick Index
4. Update the main `.claude/DECISIONS.md` index file
5. Reference the decision file in the relevant story

## Status Values

- **Active** - Currently in use, the latest decision on this topic
- **Superseded** - Replaced by a newer decision (document which one)
- **Deprecated** - No longer recommended but may still be in use
