# Technical Decisions Index

This document indexes all technical decisions made during the development of the Mars Vista API.

## Decision Log

### 019. API v2 Design Architecture
**Date:** 2024-11-20
**Status:** Approved
**Summary:** Complete redesign of API as v2 while maintaining v1 for NASA compatibility. Implements unified photos endpoint, proper error handling, always-on pagination, field selection, and modern REST patterns.
**Key Points:**
- Dual API versions (v1 NASA-compatible, v2 modern design)
- Unified `/api/v2/photos` endpoint with powerful filtering
- RFC 7807 error format
- Always paginate with metadata
- HTTP caching with ETags
**File:** [019-api-v2-design-decisions.md](.claude/decisions/019-api-v2-design-decisions.md)

---

## Decision Categories

### API Design
- 019: API v2 complete redesign with dual version strategy

### Architecture Patterns
- *Decisions following functional architecture principles*

### Database Design
- *Decisions about schema, storage strategy, JSONB usage*

### Performance Optimization
- *Decisions about caching, query optimization, pagination*

### Integration Patterns
- *Decisions about external API integration, resilience policies*

## Decision Status Types

- **Proposed**: Under consideration
- **Approved**: Accepted and will be/has been implemented
- **Deprecated**: No longer applicable
- **Superseded**: Replaced by a newer decision

## Template for New Decisions

When adding a new technical decision:

1. Create file in `.claude/decisions/` with number and descriptive name
2. Follow the decision record template
3. Add entry to this index with summary
4. Update relevant documentation if needed
5. Reference decision number in commit messages when implementing