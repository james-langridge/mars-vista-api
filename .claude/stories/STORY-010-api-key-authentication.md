# Story 010: API Key Authentication and User Management

## Status
Planning

## Overview
Implement API key-based authentication system for the C# API that integrates with the existing Auth.js dashboard (Story 009). Users sign in via the dashboard to generate/manage API keys for making requests to the Mars Vista API.

## Context
- **Story 009 (Completed)**: Next.js frontend with Auth.js authentication, `/signin`, `/dashboard`, and `/pricing` pages
- Mars Vista API is preparing for public launch
- Need to protect against abuse on Railway's pay-per-use infrastructure
- Want to track usage patterns and build user community
- Future plans for premium/paid tiers require user accounts
- Domain is `marsvista.dev` (not `.app`)
- Auth.js already handles user registration via magic links (Resend)

## Goals
1. Implement secure API key authentication for all C# API endpoints (`X-API-Key` header)
2. Integrate API key generation into existing Auth.js dashboard
3. Implement strategic rate limiting (60/hour, 500/day for free tier - see DECISION-019)
4. Add API key management UI to dashboard (generate, view, regenerate)
5. Update API documentation to show API key usage
6. Prepare foundation for future premium tiers ($9/month Pro, custom Enterprise)

## Technical Approach

### Two Authentication Systems (Why We Need Both)

**Auth.js (Story 009 - Already Implemented):**
- **Purpose**: Dashboard sessions for web app
- **Database**: Separate PostgreSQL instance (auth database)
- **Tables**: `User`, `Session`, `VerificationToken` (Prisma-managed)
- **Flow**: User signs in at `/signin` → accesses `/dashboard`
- **Used for**: Managing account, viewing API keys, settings

**API Key Auth (This Story - C# Implementation):**
- **Purpose**: Authenticate API requests to `api.marsvista.dev`
- **Database**: Separate PostgreSQL instance (photos database)
- **Tables**: `api_keys`, `rate_limits` (EF Core-managed)
- **Flow**: User generates API key in dashboard → uses key in API requests
- **Used for**: Making requests to `/api/v1/*` endpoints

**Link Between Systems:**
- Two separate PostgreSQL databases (no foreign keys between them)
- Linked by email: `User.email` (auth DB) ↔ `api_keys.user_email` (photos DB)
- Next.js dashboard queries C# API endpoints to show user's API keys
- Email serves as the logical identifier across both systems

**Libraries we WILL use:**
- **Built-in Middleware**: Custom `AuthenticationHandler<T>` for API key validation
- **EF Core**: Store API keys and rate limits in PostgreSQL
- **ASP.NET Core Cryptography**: Generate and hash API keys

### Architecture

**Three-layer approach (functional architecture):**

1. **Data Layer** (`Models/`):
   - `ApiKey` entity (id, user_email, api_key_hash, tier, created_at, last_used_at)
   - `RateLimit` entity (user_email, window_start, window_type, request_count)
   - EF Core migration for new tables

2. **Calculation Layer** (`Services/`):
   - `IApiKeyService`: Generate API keys, hash keys, validate format
   - `IRateLimitService`: Check rate limits, track usage (in-memory initially)
   - Pure functions, no side effects

3. **Action Layer** (`Controllers/`, `Middleware/`):
   - `ApiKeyController`: Generate, view, regenerate API keys (called from dashboard)
   - `ApiKeyAuthenticationHandler`: Middleware for validating `X-API-Key` header
   - `RateLimitMiddleware`: Enforce rate limits before processing requests
   - Database writes (side effects)

### Database Schema

**Important**: This story adds tables to the **photos database** (C# API), NOT the auth database.

**Database Architecture:**
- **Auth Database** (Prisma): `User`, `Session`, `VerificationToken`
- **Photos Database** (EF Core): `rovers`, `cameras`, `photos`, `api_keys`, `rate_limits`

```sql
-- API keys table (stored in photos database)
-- Links to User.email in auth database by email address (logical link, not FK)
CREATE TABLE api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_email VARCHAR(255) NOT NULL UNIQUE,    -- Email from Auth.js User table
    api_key_hash VARCHAR(64) UNIQUE NOT NULL,   -- SHA-256 hash of the API key
    tier VARCHAR(20) DEFAULT 'free',            -- 'free', 'pro', 'enterprise'
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP
);

-- Rate limiting tracking (in-memory initially, table for future persistence)
CREATE TABLE rate_limits (
    id BIGSERIAL PRIMARY KEY,
    user_email VARCHAR(255) NOT NULL,
    window_start TIMESTAMP NOT NULL,
    window_type VARCHAR(10) NOT NULL,   -- 'hour', 'day'
    request_count INT DEFAULT 0,
    UNIQUE(user_email, window_start, window_type)
);

-- Indexes for performance
CREATE INDEX idx_api_keys_hash ON api_keys(api_key_hash);
CREATE INDEX idx_api_keys_user_email ON api_keys(user_email);
CREATE INDEX idx_rate_limits_user_window ON rate_limits(user_email, window_start, window_type);
```

**Key Points:**
- `api_keys` and `rate_limits` are stored in the C# photos database
- Email is the logical link between auth DB and photos DB
- No foreign key constraint (can't have FK across databases)
- C# API validates email exists before creating API keys
- One user (email) can have one API key (regenerate to get new one)

### API Key Format

```
mv_live_a1b2c3d4e5f6789012345678901234567890abcd  (47 chars total)
```

- `mv_` = Mars Vista prefix (3 chars)
- `live_` = environment (5 chars)
- `{random}` = 40 hex chars (cryptographically random)
- Store SHA-256 hash in database (never plaintext)
- Use ASP.NET Core `RandomNumberGenerator` for generation

### User Flow (Integrated with Dashboard)

**Initial Registration:**
```
1. User visits marsvista.dev → clicks "Sign In"
2. User enters email → Auth.js sends magic link (Story 009 - already working)
3. User clicks magic link → authenticated, redirected to /dashboard
4. Dashboard shows "Generate API Key" button (no key exists yet)
5. User clicks "Generate API Key"
   → Next.js calls: POST /api/v1/keys/generate
     (includes Auth.js session cookie for authentication)
   → C# API verifies session, creates API key for user.email
   → Returns API key (only time it's shown in plaintext)
6. Dashboard displays API key with copy button and usage instructions
7. User copies API key → starts making API requests with X-API-Key header
```

**Key Regeneration:**
```
1. User signs in to dashboard
2. Dashboard shows existing API key (masked: mv_live_****...**cd)
3. User clicks "Regenerate API Key"
   → Confirms regeneration (warns that old key will stop working)
   → Next.js calls: POST /api/v1/keys/regenerate
   → C# API invalidates old key, generates new one
   → Returns new API key
4. Dashboard displays new key with copy button
```

**Using API Key:**
```bash
curl -H "X-API-Key: mv_live_a1b2c3..." \
  https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000
```

### Rate Limiting

**Free Tier Limits (Revised):**
- 60 requests/hour (1 per minute sustained)
- 500 requests/day
- 3 concurrent requests
- Track in-memory initially (migrate to Redis later if needed)

**Rationale** (see DECISION-019):
- Matches GitHub's unauthenticated rate (60/hour)
- Exceeds NASA DEMO_KEY limits (~30/hour)
- Protects Railway infrastructure costs
- Encourages Pro tier upgrades for production use

**Implementation:**
- Middleware checks `X-API-Key` header
- Look up user tier
- Check request count in current hour/day windows
- Increment count
- Return `429 Too Many Requests` if exceeded

**⚠️ Multi-Instance Limitation:**
In-memory rate limiting only works for single-instance deployments. If Railway auto-scales the API to multiple instances, rate limits will be inconsistent across instances (each instance tracks its own counts independently). This is acceptable for initial launch. Migrate to Redis-based distributed rate limiting when scaling beyond one instance.

**Rate Limit Headers (standard):**
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 43
X-RateLimit-Reset: 1731859200  (Unix timestamp)
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

### Dashboard Integration (Next.js)

**Dashboard Page** (`/dashboard` - Story 009 already created structure):
- **Add API Key Section**:
  - If no API key: "Generate API Key" button
  - If API key exists: Masked key display (`mv_live_****...**cd`)
  - "Regenerate" button (with confirmation dialog)
  - Copy-to-clipboard button
  - Usage instructions with code example

- **Add Usage Stats Section** (future enhancement):
  - Requests this hour / limit
  - Requests today / limit
  - Rate limit tier display
  - Link to upgrade (if free tier)

**Pricing Page** (`/pricing` - Story 009 already created):
- Update to show API features per tier
- Add CTA: "Sign In to Get Started"

## Implementation Steps

### Phase 1: Database and Models (Day 1)

1. Create `ApiKey` entity model:
   - `Id` (Guid)
   - `UserEmail` (string, indexed, unique - links to Auth.js User by email)
   - `ApiKeyHash` (string, SHA-256 hash)
   - `Tier` (string: 'free', 'pro', 'enterprise')
   - `IsActive` (bool)
   - `CreatedAt`, `LastUsedAt` (DateTime)

2. Create `RateLimit` entity model (optional, for persistence):
   - `Id` (long)
   - `UserEmail` (string)
   - `WindowStart` (DateTime)
   - `WindowType` (string: 'hour', 'day')
   - `RequestCount` (int)

3. Add entities to `MarsVistaDbContext` (photos database)
4. Create and apply EF Core migration to photos database
5. Test migration locally (verify tables created in photos DB, not auth DB)

### Phase 2: Core Services (Day 1-2)

6. Create `IApiKeyService` and implementation:
   - `string GenerateApiKey()` - Generate `mv_live_{random}` format
   - `string HashApiKey(string key)` - SHA-256 hash
   - `bool ValidateApiKeyFormat(string key)` - Check format
   - `string MaskApiKey(string key)` - Mask for display (`mv_live_****...**cd`)

7. Create `IRateLimitService` and implementation:
   - `Task<(bool allowed, int remaining)> CheckRateLimitAsync(string userEmail, string tier)`
   - Track hourly, daily, and concurrent request windows
   - In-memory tracking with `MemoryCache`
   - Return upgrade URL in rate limit response
   - Store rate limit headers (X-RateLimit-*)

### Phase 3: Authentication Middleware (Day 2)

8. Create `ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>`
   - Extract `X-API-Key` header
   - Hash and look up in `api_keys` table
   - Verify `IsActive = true`
   - Set `HttpContext.User` claims (email, tier)
   - Update `LastUsedAt` timestamp
   - Return `401 Unauthorized` if invalid/missing

9. Create `RateLimitMiddleware`
   - Check rate limits after authentication
   - Use `IRateLimitService` to check limits
   - Return `429 Too Many Requests` if exceeded
   - Add rate limit headers to all responses:
     - `X-RateLimit-Limit`
     - `X-RateLimit-Remaining`
     - `X-RateLimit-Reset`
     - `X-RateLimit-Tier`
     - `X-RateLimit-Upgrade-Url`

10. Register middleware in `Program.cs`:
    - Add authentication scheme
    - Add middleware pipeline (auth before rate limit)
    - Exempt `/health` endpoint from auth

11. Replace existing `ApiKeyMiddleware` with new user-based authentication:
    - Keep simple API_KEY check for scraper endpoints (`/api/scraper/*`)
    - Apply new user API key authentication to public query endpoints:
      - `RoverController` - `/api/v1/rovers/*`
      - `PhotoController` - `/api/v1/rovers/{name}/photos`
      - `ManifestController` - `/api/v1/manifests/*`
    - Exempt `/health` endpoint from all authentication
    - Exempt `/api/v1/internal/*` endpoints from user API key check (use INTERNAL_API_SECRET instead)

**Note on Scraper Endpoints:**
Scraper endpoints (`/api/scraper/*`) remain protected by the existing simple API_KEY middleware for admin access. These are internal tools, not public API endpoints. User-facing query endpoints require per-user API keys from this story.

### Phase 4: API Key Management Endpoints (Day 2-3)

**Architecture Decision:** Next.js acts as a trusted proxy between the dashboard and C# API.
- Next.js validates Auth.js sessions (already knows how to do this)
- Next.js calls C# API with authenticated user's email
- C# API trusts requests from Next.js via shared secret

**Why this approach?**
- Avoids tight coupling between Auth.js (Prisma) and C# API (EF Core)
- No cross-domain session cookie complexity
- Follows standard microservices pattern (trusted internal API)
- Next.js already has session validation logic built-in

12. Create Next.js API routes (`web/app/api/keys/*`):
    - `POST /api/keys/generate` - Generate new API key
      - Validates Auth.js session (server-side)
      - Extracts user email from session
      - Calls C# API: `POST /api/v1/internal/keys/generate` with email + internal secret
      - Returns API key to client

    - `POST /api/keys/regenerate` - Regenerate existing API key
      - Validates Auth.js session
      - Extracts user email from session
      - Calls C# API: `POST /api/v1/internal/keys/regenerate` with email + internal secret
      - Returns new API key to client

    - `GET /api/keys/current` - Get current API key info (masked)
      - Validates Auth.js session
      - Extracts user email from session
      - Calls C# API: `GET /api/v1/internal/keys/current` with email + internal secret
      - Returns masked key info to client

13. Create C# internal API controller (`ApiKeyInternalController`):
    - `POST /api/v1/internal/keys/generate`
      - Validates `X-Internal-Secret` header (shared secret with Next.js)
      - Accepts `user_email` in request body (trusted from Next.js)
      - Check if user already has API key (one per user)
      - Generate API key using `IApiKeyService`
      - Store hash in database
      - Return plaintext key (only time it's visible)

    - `POST /api/v1/internal/keys/regenerate`
      - Validates `X-Internal-Secret` header
      - Accepts `user_email` in request body
      - Invalidate old key (set `IsActive = false`)
      - Generate new key
      - Return new plaintext key

    - `GET /api/v1/internal/keys/current`
      - Validates `X-Internal-Secret` header
      - Accepts `user_email` query parameter
      - Query `api_keys` table for user's key
      - Return masked key, tier, created date, last used date

14. Add internal API authentication middleware:
    - Create `InternalApiMiddleware` to validate `X-Internal-Secret` header
    - Only apply to `/api/v1/internal/*` endpoints
    - Shared secret stored in environment variable: `INTERNAL_API_SECRET`
    - Return 403 if secret missing or invalid

15. Add validation and error handling:
    - Handle "user already has API key" error (return 409 Conflict)
    - Handle "no API key found" error (return 404 Not Found)
    - Rate limit key generation in Next.js route (max 5 per hour per user)
    - Validate email format

16. Test endpoints locally:
    - Test Next.js routes with authenticated session
    - Test C# internal endpoints with curl + internal secret
    - Verify error cases (no secret, wrong secret, missing email)

### Phase 5: Dashboard Integration (Day 3-4)

17. Update Dashboard page (`web/app/app/dashboard/page.tsx`):
    - Add API Key section
    - Create client component for API key management
    - Fetch current API key on mount: `GET /api/keys/current` (Next.js API route)
    - Show masked key if exists
    - Show "Generate API Key" button if no key
    - Show "Regenerate" button if key exists

18. Create `components/ApiKeyManager.tsx` (client component):
    - State: `apiKey`, `isLoading`, `error`, `showKey`
    - Function: `generateKey()` - calls `POST /api/keys/generate` (Next.js route)
    - Function: `regenerateKey()` - calls `POST /api/keys/regenerate` (Next.js route)
    - Show confirmation dialog for regeneration
    - Display API key with copy-to-clipboard button
    - Show usage example with curl command

19. Create `components/CopyButton.tsx`:
    - Copy API key to clipboard
    - Show "Copied!" feedback

20. Update pricing page (`web/app/app/pricing/page.tsx`):
    - Update CTA button: "Sign In to Get Started"
    - Show API features per tier (rate limits, etc.)

21. Test dashboard flow:
    - Sign in → generate key → copy key → use in curl
    - Sign in → regenerate key → verify old key invalid

### Phase 6: Documentation Updates (Day 4)

22. Update `docs/API_ENDPOINTS.md`:
    - Add authentication section at the top
    - Show `X-API-Key` header in all curl examples
    - Document error responses (401, 429)
    - Add rate limit headers documentation

23. Update `README.md`:
    - Add "Getting Started" section:
      - Sign up at marsvista.dev
      - Generate API key from dashboard
      - Make your first request
    - Update feature list (add "API key authentication")
    - Add rate limits info by tier

24. Create `docs/AUTHENTICATION_GUIDE.md`:
    - How to get an API key (sign in to dashboard)
    - How to use the API key (X-API-Key header)
    - Rate limits by tier (free, pro, enterprise)
    - How to regenerate your key
    - Troubleshooting (401, 429 errors)

25. Update OpenAPI spec (`public/openapi.json`):
    - Add security scheme for API key
    - Add X-API-Key to all endpoints
    - Document rate limit responses

26. Update `CLAUDE.md`:
    - Add Story 010 to completed stories
    - Update system capabilities
    - Document two-auth-system architecture

### Phase 7: Testing and Deployment (Day 5)

27. Test complete flow locally:
    - Sign in to dashboard (Auth.js magic link)
    - Generate API key from dashboard
    - Copy API key
    - Make API request with `X-API-Key` header
    - Verify 200 response
    - Verify rate limit headers present

28. Test API key authentication on all endpoints:
    - `/api/v1/rovers` - requires API key
    - `/api/v1/rovers/{name}/photos` - requires API key
    - `/api/v1/manifests/{name}` - requires API key
    - `/health` - no API key required

29. Test rate limiting:
    - Make 61 requests in an hour
    - Verify 61st returns 429
    - Verify rate limit headers correct
    - Verify upgrade URL in response

30. Test key regeneration:
    - Regenerate API key from dashboard
    - Verify old key returns 401
    - Verify new key returns 200

31. Test error cases:
    - Missing API key → 401
    - Invalid API key → 401
    - Inactive API key → 401
    - Rate limit exceeded → 429

32. Deploy C# API to Railway:
    - Run database migration
    - Verify api_keys table created in photos database
    - Verify email-based linking works (generate key with email, query succeeds)
    - No foreign key exists (databases are separate - this is expected)

33. Deploy Next.js frontend to Railway:
    - Verify dashboard shows API key section
    - Test generate/regenerate flow in production

34. Test production flow end-to-end:
    - Sign up at marsvista.dev
    - Generate API key
    - Make request to api.marsvista.dev
    - Verify authentication works

35. Create backup before launch:
    - Backup database with new tables
    - Document rollback plan

## Configuration

### Environment Variables (Railway)

**IMPORTANT:** Two separate PostgreSQL databases are used:
- **Photos Database** (`marsvista_dev`): Used by C# API for rovers, cameras, photos, api_keys
- **Auth Database** (`marsvista_auth_dev`): Used by Next.js/Auth.js for User, Session, VerificationToken

**C# API** (`api.marsvista.dev`):
```bash
# Photos Database (separate from auth database)
DATABASE_URL=postgresql://user:pass@host:port/marsvista_dev

# Internal API Secret (shared with Next.js for trusted internal calls)
INTERNAL_API_SECRET=<generate-secure-random-secret>

# Application URLs
FRONTEND_URL=https://marsvista.dev

# Rate Limits (optional, defaults in appsettings.json)
RATE_LIMIT_HOURLY_FREE=60
RATE_LIMIT_DAILY_FREE=500
RATE_LIMIT_HOURLY_PRO=5000
RATE_LIMIT_DAILY_PRO=100000
```

**Next.js** (`marsvista.dev`):
```bash
# Auth Database (separate from photos database)
DATABASE_URL=postgresql://user:pass@host:port/marsvista_auth_dev

# Internal API Secret (same value as C# API)
INTERNAL_API_SECRET=<same-secret-as-csharp-api>

# Already configured in Story 009
RESEND_API_KEY=re_xxxxx
AUTH_SECRET=<secret>
NEXTAUTH_URL=https://marsvista.dev
NEXT_PUBLIC_API_URL=https://api.marsvista.dev
```

### appsettings.json

```json
{
  "RateLimits": {
    "Free": {
      "RequestsPerHour": 60,
      "RequestsPerDay": 500,
      "ConcurrentRequests": 3
    },
    "Pro": {
      "RequestsPerHour": 5000,
      "RequestsPerDay": 100000,
      "ConcurrentRequests": 50
    },
    "Enterprise": {
      "RequestsPerHour": 100000,
      "RequestsPerDay": -1,
      "ConcurrentRequests": 100
    }
  },
  "ApiKey": {
    "Prefix": "mv",
    "Environment": "live",
    "Length": 40
  }
}
```

## Success Criteria

- ✅ User can sign in to dashboard via Auth.js (Story 009 - already working)
- ✅ User can generate API key from dashboard
- ✅ User can regenerate API key (old key becomes invalid)
- ✅ User can make authenticated API requests with `X-API-Key` header
- ✅ Rate limits are enforced (60/hour, 500/day for free tier)
- ✅ Invalid/missing API keys return 401 with clear error message
- ✅ Rate limit exceeded returns 429 with proper headers
- ✅ Rate limit headers included in all API responses
- ✅ API keys are stored as SHA-256 hashes (never plaintext)
- ✅ Documentation shows authentication examples
- ✅ Dashboard at marsvista.dev/dashboard shows API key management

## Testing Checklist

**Unit Tests:**
- [ ] `ApiKeyService.GenerateApiKey()` creates valid format (`mv_live_...`)
- [ ] `ApiKeyService.HashApiKey()` creates SHA-256 hash
- [ ] `ApiKeyService.MaskApiKey()` masks key correctly
- [ ] `RateLimitService.CheckRateLimit()` enforces limits
- [ ] `RateLimitService` returns correct remaining count

**Integration Tests:**
- [ ] POST /api/v1/keys/generate creates API key for authenticated user
- [ ] POST /api/v1/keys/generate returns error if key already exists
- [ ] POST /api/v1/keys/regenerate invalidates old key
- [ ] GET /api/v1/keys/current returns masked key
- [ ] GET /api/v1/rovers/curiosity/photos requires X-API-Key header
- [ ] Missing API key returns 401
- [ ] Invalid API key returns 401
- [ ] 61st request in an hour returns 429 with upgrade info
- [ ] Rate limit headers present on all responses

**Manual Tests:**
- [ ] Sign in to dashboard at marsvista.dev
- [ ] Generate API key from dashboard
- [ ] Copy API key and make request with curl
- [ ] Verify 200 response with data
- [ ] Trigger rate limit (61 requests) and verify 429 response
- [ ] Regenerate API key from dashboard
- [ ] Verify old key returns 401
- [ ] Verify new key returns 200

## Future Enhancements (Not in this story)

- [ ] Usage statistics dashboard (track popular endpoints, daily/hourly usage graphs)
- [ ] Multiple API keys per user (production, development, testing)
- [ ] API key scopes/permissions (read-only, specific rovers, etc.)
- [ ] Premium tier payment processing (Stripe integration)
- [ ] Redis-based distributed rate limiting (when scaling to multiple instances)
- [ ] Webhook notifications for new photos
- [ ] Team/organization accounts (share API keys, team billing)
- [ ] Usage API endpoint (`GET /api/v1/usage` - show current usage stats)

## Technical Decisions to Document

1. **Why two separate auth systems (Auth.js + API Key)?**
   - **Auth.js**: Dashboard sessions (user logs in to manage account)
   - **API Key**: API request authentication (stateless, no session cookies)
   - Different concerns, different mechanisms
   - API keys needed for programmatic access (curl, scripts, apps)
   - Session cookies don't work for API clients

2. **Why integrate with existing Auth.js instead of separate signup?**
   - **Simpler**: One signup flow, not two
   - **Better UX**: User signs in once, manages everything in dashboard
   - **Less code**: Reuse Auth.js magic links, no duplicate email system
   - **Follows Grug principles**: Simple > clever

3. **Why Next.js API routes as proxy (not direct C# session validation)?**
   - **Decoupling**: C# API doesn't need to understand Auth.js/Prisma
   - **No cross-domain cookie complexity**: Session cookies stay on marsvista.dev
   - **Standard microservices pattern**: Trusted internal API calls
   - **Next.js already validates sessions**: No duplicate logic needed
   - **Simpler**: Shared secret authentication vs. complex session validation

4. **Why link api_keys to Auth.js User by email (not UUID)?**
   - Auth.js User table already has verified emails
   - Email is natural logical identifier (unique, human-readable)
   - Avoids UUID synchronization between Prisma and EF Core
   - Easy to query in both systems
   - No foreign key possible (separate databases)

5. **Why two separate PostgreSQL databases?**
   - **Clean separation of concerns**: Auth data vs. application data
   - **Independent scaling**: Can scale databases separately if needed
   - **Different migration systems**: Prisma Migrate vs. EF Core Migrations
   - **No migration conflicts**: Auth.js and C# API don't interfere
   - **Professional microservices pattern**: Each service owns its data

6. **Why in-memory rate limiting initially?**
   - Sufficient for single-instance deployment
   - No Redis cost until needed
   - Easy to migrate to Redis later when scaling
   - Railway currently runs single instance

7. **API key format choice (`mv_live_{random}`)?**
   - Prefix makes keys identifiable in logs
   - Environment marker prevents prod/dev key mixup
   - Standard pattern (like Stripe: `sk_live_...`)
   - 40 hex chars = 160 bits of entropy (very secure)

8. **Why hash API keys in database?**
   - Security: leaked database doesn't expose keys
   - Same principle as password hashing
   - SHA-256 is fast and sufficient (not bcrypt - keys are random)
   - Can't recover lost keys (user must regenerate)

## Notes

- This story integrates with Story 009 (Next.js dashboard already exists)
- Focus on C# API backend + dashboard UI integration
- Keep implementation simple and functional
- Usage analytics dashboard can be Story 011
- Premium tier payment processing can be Story 012
- Two auth systems (Auth.js + API Key) serve different purposes - not duplication

## Estimated Effort
3-4 days (simplified from original 5 days due to Story 009 foundation)

## Dependencies
- **Story 009 completed**: Next.js dashboard with Auth.js
- Resend account (already configured in Story 009)
- marsvista.dev domain configured (already done in Story 009)
- Railway deployment ready (already done)

## Story Completion Checklist

When marking this story as complete:
- [ ] All implementation steps completed (Phases 1-7)
- [ ] Tests passing (unit + integration + manual)
- [ ] C# API deployed to Railway with migrations
- [ ] Next.js dashboard updated and deployed to marsvista.dev
- [ ] API key generation/regeneration working in production
- [ ] Rate limiting enforced on all endpoints
- [ ] Documentation updated:
  - [ ] `docs/API_ENDPOINTS.md` (authentication section)
  - [ ] `docs/AUTHENTICATION_GUIDE.md` (new file)
  - [ ] `README.md` (getting started section)
  - [ ] `CLAUDE.md` (Story 010 completed, two-auth architecture)
  - [ ] `public/openapi.json` (security scheme added)
- [ ] Technical decisions documented in `.claude/decisions/`
- [ ] Database backup created before and after launch
- [ ] Commit messages follow commit-guide skill
- [ ] Changes pushed to GitHub
