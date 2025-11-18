# Story 010: API Key Authentication and User Management

## Status
Planning

## Overview
Implement API key-based authentication system with magic link (passwordless) registration to protect the Mars Vista API from abuse, enable usage tracking, and prepare for future premium tiers.

## Context
- Mars Vista API is preparing for public launch
- Need to protect against abuse on Railway's pay-per-use infrastructure
- Want to track usage patterns and build user community
- Future plans for premium/paid tiers require user accounts
- Domain is `marsvista.dev` (not `.app`)
- Email service: Resend (has official .NET SDK)

## Goals
1. Implement secure API key authentication for all public API endpoints
2. Create passwordless (magic link) user registration flow
3. Implement strategic rate limiting (60/hour, 500/day for free tier - see DECISION-019)
4. Build simple signup landing page at `https://marsvista.dev/signup`
5. Update API documentation to show API key usage
6. Prepare foundation for future premium tiers ($9/month Pro, custom Enterprise)

## Technical Approach

### Authentication Library Choice

After analysis, recommended approach is **custom implementation** using:

**Why not third-party auth services?**
- **Clerk**: $25/month minimum, designed for frontend apps (React/Next.js), overkill for API-only service
- **Auth0**: Extremely expensive at scale ($150+/month), complex setup for simple API key use case
- **Supabase Auth**: Requires using Supabase as database (we have PostgreSQL)
- **ASP.NET Core Identity**: Heavy framework designed for password-based auth with sessions

**Our use case is simpler:**
- Email-only magic link authentication (no passwords to manage)
- API keys for request authentication (not session cookies)
- Rate limiting per API key
- User tier management (free, pro, enterprise)

**Custom implementation advantages:**
- Full control over user data and API key format
- Simple to understand and maintain (< 500 lines of code)
- No monthly fees or vendor lock-in
- Easy to extend for premium features
- Fits our functional architecture principles

**Libraries we WILL use:**
- **Resend .NET SDK**: Official SDK for sending magic link emails (`dotnet add package Resend`)
- **ASP.NET Core Data Protection**: Built-in token generation/validation (secure, time-limited)
- **Built-in Middleware**: Custom `AuthenticationHandler<T>` for API key validation
- **EF Core**: Store users and API keys in PostgreSQL

### Architecture

**Three-layer approach (functional architecture):**

1. **Data Layer** (`Models/`):
   - `User` entity (id, email, api_key_hash, tier, created_at, etc.)
   - `MagicLinkToken` entity (email, token_hash, expires_at, used_at)
   - EF Core migration for new tables

2. **Calculation Layer** (`Services/`):
   - `IApiKeyService`: Generate API keys, hash keys, validate format
   - `IMagicLinkService`: Generate magic link tokens, validate tokens
   - `IRateLimitService`: Check rate limits, track usage (in-memory initially)
   - Pure functions, no side effects

3. **Action Layer** (`Controllers/`, `Middleware/`):
   - `AuthController`: Registration, magic link endpoints
   - `ApiKeyAuthenticationHandler`: Middleware for validating `X-API-Key` header
   - `RateLimitMiddleware`: Enforce rate limits before processing requests
   - Resend email sending (side effect)

### Database Schema

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    api_key_hash VARCHAR(64) NOT NULL,  -- SHA-256 hash
    tier VARCHAR(20) DEFAULT 'free',    -- 'free', 'pro', 'enterprise'
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    email_verified_at TIMESTAMP,
    last_request_at TIMESTAMP
);

CREATE TABLE magic_link_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    token_hash VARCHAR(64) NOT NULL,    -- SHA-256 hash
    expires_at TIMESTAMP NOT NULL,
    used_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE rate_limits (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    window_start TIMESTAMP NOT NULL,
    window_type VARCHAR(10) NOT NULL,   -- 'hour', 'day'
    request_count INT DEFAULT 0,
    UNIQUE(user_id, window_start, window_type)
);

CREATE INDEX idx_users_api_key ON users(api_key_hash);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_magic_tokens_email ON magic_link_tokens(email, token_hash);
CREATE INDEX idx_rate_limits_user_window ON rate_limits(user_id, window_start, window_type);
```

### API Key Format

```
mv_live_a1b2c3d4e5f6789012345678901234567890abcd  (47 chars total)
```

- `mv_` = Mars Vista prefix (3 chars)
- `live_` = environment (5 chars)
- `{random}` = 40 hex chars (cryptographically random)
- Store SHA-256 hash in database (never plaintext)
- Use ASP.NET Core `RandomNumberGenerator` for generation

### Magic Link Flow

**Registration:**
```
1. POST /api/v1/auth/register { "email": "user@example.com" }
   → Generate magic link token (15-minute expiry)
   → Send email via Resend with link: https://marsvista.dev/verify?token={token}
   → Return 200 { "message": "Check your email for the magic link" }

2. User clicks link → GET /verify?token={token} (HTML page)
   → Frontend auto-submits to POST /api/v1/auth/verify { "token": "{token}" }
   → Validate token (check expiry, not used)
   → Create user account
   → Generate API key
   → Mark token as used
   → Return API key to display on success page

3. User copies API key → starts using API
```

**Key Reset:**
```
POST /api/v1/auth/reset-key { "email": "user@example.com" }
→ Generate new magic link
→ Send email with link
→ User clicks → verify → get new API key
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

**Rate Limit Headers (standard):**
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 43
X-RateLimit-Reset: 1731859200  (Unix timestamp)
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

### Frontend Pages

**Landing Page** (`/signup`):
- Simple form: email input + submit button
- "Get your free API key" headline
- Link to pricing page
- Hosted on marsvista.dev (separate Vite/React app or simple HTML)

**Verification Page** (`/verify?token={token}`):
- Auto-submits token to API
- Shows API key on success
- Copy-to-clipboard button
- Link to API docs with quick start example

**Pricing Page** (`/pricing`):
- Free tier features
- "Pro tier coming soon" placeholder
- Link to signup

## Implementation Steps

### Phase 1: Database and Models (Day 1)

1. ✅ Create `User` entity model
2. ✅ Create `MagicLinkToken` entity model
3. ✅ Add entities to `MarsVistaDbContext`
4. ✅ Create and apply EF Core migration
5. ✅ Test migration locally

### Phase 2: Core Services (Day 1-2)

6. ✅ Install Resend SDK: `dotnet add package Resend`
7. ✅ Create `IApiKeyService` and implementation:
   - `string GenerateApiKey()` - Generate `mv_live_{random}` format
   - `string HashApiKey(string key)` - SHA-256 hash
   - `bool ValidateApiKeyFormat(string key)` - Check format
8. ✅ Create `IMagicLinkService` and implementation:
   - `string GenerateMagicLinkToken(string email)` - Create token, store hash
   - `(bool valid, string? email) ValidateToken(string token)` - Check validity
   - Use ASP.NET Core Data Protection for token generation
9. ✅ Create `IEmailService` and `ResendEmailService`:
   - `Task SendMagicLinkAsync(string email, string token)`
   - Configure Resend API key from environment
10. ✅ Create `IRateLimitService` and implementation:
    - `Task<(bool allowed, int remaining)> CheckRateLimitAsync(Guid userId, string tier)`
    - Track hourly, daily, and concurrent request windows
    - In-memory tracking with `MemoryCache`
    - Return upgrade URL in rate limit response

### Phase 3: Authentication Middleware (Day 2)

11. ✅ Create `ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>`
    - Extract `X-API-Key` header
    - Hash and look up user in database
    - Set `HttpContext.User` claims (user ID, email, tier)
    - Return `401 Unauthorized` if invalid
12. ✅ Create `RateLimitMiddleware`
    - Check rate limits after authentication
    - Return `429 Too Many Requests` if exceeded
    - Add rate limit headers to all responses
13. ✅ Register middleware in `Program.cs`
14. ✅ Add `[Authorize]` attribute to rover endpoints

### Phase 4: Auth Endpoints (Day 2-3)

15. ✅ Create `AuthController`:
    - `POST /api/v1/auth/register` - Send magic link
    - `POST /api/v1/auth/verify` - Verify token, return API key
    - `POST /api/v1/auth/reset-key` - Send key reset magic link
16. ✅ Add validation and error handling
17. ✅ Test endpoints with curl/Postman
18. ✅ Add rate limiting to auth endpoints (prevent abuse)

### Phase 5: Frontend Pages (Day 3-4)

19. ✅ Create simple HTML signup page at `/signup`
    - Email form
    - Submit to `/api/v1/auth/register`
    - Show success message
20. ✅ Create verification page at `/verify`
    - Parse `?token=` from URL
    - Auto-submit to `/api/v1/auth/verify`
    - Display API key with copy button
    - Link to API docs
21. ✅ Create pricing page at `/pricing`
    - Free tier details
    - Pro tier "coming soon"
    - Link to signup
22. ✅ Update main landing page to link to signup
23. ✅ Host frontend on marsvista.dev

**Frontend Tech Stack Options:**
- **Option A (Simplest)**: Plain HTML/CSS/JS (no build step, easy to deploy)
- **Option B (Modern)**: Vite + React (better UX, but requires build/deploy)
- **Recommendation**: Start with Option A, upgrade to B later if needed

### Phase 6: Documentation Updates (Day 4)

24. ✅ Update `docs/API_ENDPOINTS.md`:
    - Add authentication section
    - Show `X-API-Key` header in all examples
    - Document auth endpoints
    - Document error responses (401, 429)
25. ✅ Update `README.md`:
    - Add "Getting Started" section with signup link
    - Update feature list
    - Add rate limits info
26. ✅ Create `docs/AUTHENTICATION_GUIDE.md`:
    - How to get an API key
    - How to use the API key
    - Rate limits by tier
    - How to reset your key
27. ✅ Update `CLAUDE.md`:
    - Add Story 010 to completed stories
    - Update system capabilities

### Phase 7: Testing and Deployment (Day 5)

28. ✅ Test complete registration flow locally
29. ✅ Test API key authentication on all endpoints
30. ✅ Test rate limiting (trigger 429 response)
31. ✅ Test magic link expiration
32. ✅ Test key reset flow
33. ✅ Deploy API to Railway
34. ✅ Deploy frontend to marsvista.dev
35. ✅ Test production flow end-to-end
36. ✅ Create backup before launch

## Configuration

### Environment Variables (Railway)

```bash
# Resend
RESEND_API_KEY=re_xxxxx

# Application
BASE_URL=https://api.marsvista.app
FRONTEND_URL=https://marsvista.dev

# Rate Limits (optional, defaults shown)
RATE_LIMIT_HOURLY_FREE=60
RATE_LIMIT_DAILY_FREE=500
RATE_LIMIT_HOURLY_PRO=5000
RATE_LIMIT_DAILY_PRO=100000
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
  "MagicLink": {
    "ExpirationMinutes": 15,
    "BaseUrl": "https://marsvista.dev"
  }
}
```

## Success Criteria

- ✅ User can register with email and receive magic link
- ✅ User can verify email and receive API key
- ✅ User can make authenticated API requests with `X-API-Key` header
- ✅ Rate limits are enforced (60/hour, 500/day for free tier)
- ✅ Invalid/missing API keys return 401
- ✅ Rate limit exceeded returns 429 with proper headers
- ✅ Magic links expire after 15 minutes
- ✅ API keys are stored as SHA-256 hashes (never plaintext)
- ✅ Documentation shows authentication examples
- ✅ Signup page is live at marsvista.dev/signup

## Testing Checklist

**Unit Tests:**
- [ ] `ApiKeyService.GenerateApiKey()` creates valid format
- [ ] `ApiKeyService.HashApiKey()` creates SHA-256 hash
- [ ] `MagicLinkService.GenerateMagicLinkToken()` creates token
- [ ] `MagicLinkService.ValidateToken()` checks expiry and usage
- [ ] `RateLimitService.CheckRateLimit()` enforces limits

**Integration Tests:**
- [ ] POST /api/v1/auth/register sends email
- [ ] POST /api/v1/auth/verify creates user and returns API key
- [ ] GET /api/v1/rovers/curiosity/photos requires API key
- [ ] 61st request in an hour returns 429 with upgrade info
- [ ] Expired magic link returns error
- [ ] Used magic link returns error

**Manual Tests:**
- [ ] Complete registration flow in browser
- [ ] Copy API key and make request with curl
- [ ] Trigger rate limit and verify 429 response
- [ ] Request key reset and verify new key works

## Future Enhancements (Not in this story)

- [ ] User dashboard web UI (view usage stats, regenerate key)
- [ ] Detailed usage analytics (track popular endpoints)
- [ ] Premium tier payment processing (Stripe integration)
- [ ] Redis-based distributed rate limiting
- [ ] Social auth (GitHub, Google OAuth)
- [ ] Webhook notifications for new photos
- [ ] Team/organization accounts
- [ ] Usage API (`GET /api/v1/auth/usage`)

## Technical Decisions to Document

1. **Why custom auth instead of Auth0/Clerk?**
   - Simpler use case (API keys, not sessions)
   - Cost (Auth0/Clerk expensive at scale)
   - Control (own user data, no vendor lock-in)
   - Fits functional architecture better

2. **Why magic links instead of passwords?**
   - No password management/hashing/reset complexity
   - More secure (no weak passwords)
   - Better UX (one-click registration)
   - Simpler implementation

3. **Why in-memory rate limiting initially?**
   - Sufficient for single-instance deployment
   - No Redis cost until needed
   - Easy to migrate to Redis later

4. **API key format choice (`mv_live_{random}`)?**
   - Prefix makes keys identifiable in logs
   - Environment marker prevents prod/dev key mixup
   - Standard pattern (like Stripe: `sk_live_...`)

5. **Why hash API keys in database?**
   - Security: leaked database doesn't expose keys
   - Same principle as password hashing
   - SHA-256 is fast and sufficient (not bcrypt - keys are random)

## Notes

- This story focuses on MVP features for launch
- Keep implementation simple and functional
- User dashboard and analytics can be Story 011
- Premium tier payment processing can be Story 012
- Social auth (GitHub/Google) can be added later if users request it

## Estimated Effort
5 days (can be done faster if focused)

## Dependencies
- Resend account (free tier: 3,000 emails/month, 100/day)
- marsvista.dev domain configured
- Railway deployment ready

## Story Completion Checklist

When marking this story as complete:
- [ ] All implementation steps completed
- [ ] Tests passing (unit + integration)
- [ ] Deployed to Railway production
- [ ] Frontend deployed to marsvista.dev
- [ ] Documentation updated (API docs, README, CLAUDE.md)
- [ ] Technical decisions documented in `.claude/decisions/`
- [ ] Backup created
- [ ] Commit message follows commit-guide skill
- [ ] Changes pushed to GitHub
