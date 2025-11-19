# Story 012: Update Rate Limits and Simplify to Two-Tier Pricing

**Status:** In Progress
**Priority:** High
**Estimated Effort:** Medium

## Context

The current rate limits (60 req/hour free tier) are not competitive with NASA's API gateway (1,000 req/hour). Since Mars Vista uses a database-backed architecture rather than a proxy, we can offer significantly higher limits without hitting NASA's API.

Additionally, having three tiers (free, pro, enterprise) adds complexity for an early-stage product. Simplifying to two tiers (free and pro) provides clear differentiation while reducing maintenance overhead.

## Goals

1. **Increase free tier limits** to be competitive (10x NASA's limits)
2. **Simplify pricing** from 3 tiers to 2 tiers (remove enterprise)
3. **Update all code and UI** to reflect new limits
4. **Maintain concurrent request limits** to prevent abuse

## New Rate Limits

### Free Tier
- **Hourly:** 10,000 requests/hour (10x NASA's 1,000 req/hour)
- **Daily:** 100,000 requests/day
- **Concurrent:** 10 simultaneous requests
- **Cost:** $0/month

### Pro Tier
- **Hourly:** 50,000 requests/hour (50x NASA's limit)
- **Daily:** 1,000,000 requests/day (1 million)
- **Concurrent:** 50 simultaneous requests
- **Cost:** TBD (pricing page will show "Contact us" or specific price)

### Rationale

**Why these limits are sustainable:**
- Database-backed architecture (not a proxy to NASA)
- Most requests served from PostgreSQL (no external API calls)
- Railway Pro can handle 2,000-5,000 queries/sec
- 10,000 req/hour = 2.78 req/sec average per user
- Can support 5,000-10,000 active users with current infrastructure

**Competitive positioning:**
- Free tier: 10x better than NASA Gateway (10,000 vs 1,000)
- Pro tier: 50x better than NASA Gateway
- Attracts developers with generous free limits
- Pro tier for production apps and serious users

## Implementation Steps

### 1. Update Rate Limiting Service

**File:** `src/MarsVista.Api/Services/RateLimitService.cs`

- Update `TierLimits` dictionary:
  ```csharp
  { "free", (10000, 100000) },
  { "pro", (50000, 1000000) }
  ```
- Remove `enterprise` tier

### 2. Update Frontend Pricing Page

**File:** `web/app/app/pricing/page.tsx`

- Remove Enterprise tier card
- Update Free tier limits display
- Update Pro tier limits display
- Update messaging to emphasize competitive advantage

### 3. Update Dashboard Display

**File:** `web/app/app/dashboard/page.tsx`

- Update hardcoded "60 req/hour, 500 req/day" display
- Show actual tier limits dynamically (prepare for Story 011 usage stats)

### 4. Update Documentation

**Files to update:**
- `docs/API_ENDPOINTS.md` - Update rate limit examples
- `docs/AUTHENTICATION_GUIDE.md` - Update rate limit section
- `README.md` - Update feature list if rate limits mentioned

### 5. Update Tests (if any exist)

- Update test cases for rate limiting
- Verify behavior with new limits

## Acceptance Criteria

- [ ] Rate limiting service uses new limits (10k/100k free, 50k/1M pro)
- [ ] Enterprise tier removed from all code
- [ ] Pricing page shows only Free and Pro tiers
- [ ] Dashboard displays correct tier information
- [ ] Documentation updated with new limits
- [ ] No references to "enterprise" tier remain in codebase
- [ ] Rate limit headers return correct values

## Technical Decisions

**Decision:** Aggressively competitive free tier (10,000 req/hour)

**Reasoning:**
- Database architecture supports it (no external API costs)
- Differentiates from NASA Gateway (10x better)
- Attracts developers to adopt Mars Vista
- Demonstrates technical superiority
- Sustainable with current infrastructure (5,000-10,000 users)

**Trade-offs:**
- Higher free limits might reduce Pro conversions
- Counter: Generous free tier builds user base, Pro offers analytics and higher limits
- Abuse risk mitigated by concurrent request limits (10 for free tier)

## Out of Scope

- Implementing concurrent request limiting (already exists in design)
- Usage statistics dashboard (Story 011)
- Dynamic tier display based on actual API key (future enhancement)

## Dependencies

- None (standalone changes)

## Notes

- Consider adding abuse detection for users consistently hitting limits
- Future: Email notifications when users approach their limits (upgrade prompt)
- Future: Usage-based pricing for Pro tier (pay per million requests)
