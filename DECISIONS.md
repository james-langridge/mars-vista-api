# Technical Decisions Index

This document indexes all technical decisions made during the development of the Mars Vista API.

## Decision Log

### 019. API v2 Design Architecture (Enhanced)
**Date:** 2024-11-20
**Status:** Approved - Enhanced Version Available
**Summary:** Complete redesign of API as v2 leveraging 100% of NASA data stored. Offers revolutionary features beyond original NASA API including Mars time queries, panorama detection, location tracking, and multiple image sizes.
**Key Points:**
- Exposes rich data: 4 image sizes, Mars time, location, telemetry
- Advanced queries: Mars sunrise/sunset, location proximity, journey tracking
- Specialized endpoints: panoramas, stereo pairs, time machine
- Progressive disclosure: field sets from minimal to complete
- Materialized views for complex features
**Files:**
- Enhanced: [019-api-v2-design-decisions-enhanced.md](.claude/decisions/019-api-v2-design-decisions-enhanced.md)
- Original: [019-api-v2-design-decisions.md](.claude/decisions/019-api-v2-design-decisions.md)

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