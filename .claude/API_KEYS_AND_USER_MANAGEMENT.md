# API Keys and User Management Strategy

## Context

The Mars Vista API is preparing to go public. Key considerations:

1. **Original Heroku API**: No API keys required (fully open)
2. **NASA API**: Requires API key even for free tier (DEMO_KEY has strict limits)
3. **Industry standard**: Most public APIs require registration even for free tiers
4. **Future monetization**: Paid tiers would require user accounts and API keys

## Analysis: To API Key or Not?

### Option 1: Fully Open (No API Keys)

**Pros:**
- Zero friction - developers can start immediately
- Maximum accessibility and adoption
- Matches original Heroku API experience
- Simple to implement (current state)

**Cons:**
- No usage tracking or analytics
- No way to enforce rate limits per user
- No protection against abuse/DOS
- Cannot implement premium tiers later without breaking changes
- No way to contact users about outages/changes
- No community building (can't track who's using it)

### Option 2: API Keys Required (Even for Free Tier)

**Pros:**
- Track usage patterns and popular endpoints
- Enforce reasonable rate limits per user
- Protect against abuse and runaway scripts
- Can identify and contact heavy users
- Easy path to premium tiers (just check key tier)
- Can gather metrics for infrastructure planning
- Build user community and mailing list
- Professional appearance

**Cons:**
- Registration friction (but minimal with modern flows)
- Need to implement user management system
- Need to store and manage API keys securely

### Option 3: Hybrid Approach

**Pros:**
- Offer both anonymous and registered access
- Anonymous has strict limits (like NASA's DEMO_KEY)
- Registered gets higher limits
- Gradual onboarding path

**Cons:**
- More complex to implement
- Still need rate limiting infrastructure
- Anonymous users can't be contacted

## Recommendation: **API Keys Required (Option 2)**

### Reasoning

1. **Future-proofing**: Adding API keys later is a breaking change. Starting with them is easier.

2. **Abuse prevention**: Without keys, a single bad actor can overwhelm your infrastructure. With Railway's pay-per-use model, this directly costs you money.

3. **Usage insights**: You'll want to know:
   - Which rovers are most popular?
   - Which endpoints get the most traffic?
   - Are people using advanced features (date ranges, camera filters)?
   - When do you need to scale infrastructure?

4. **Community building**:
   - Email list for announcements
   - Can showcase user projects
   - Feedback channel for improvements

5. **Monetization readiness**: When you're ready to add premium tiers:
   - No breaking changes needed
   - Just add tier metadata to user accounts
   - Check tier in middleware, enforce limits

6. **Industry standard**: Users expect API keys for public APIs. It signals professionalism and reliability.

7. **Low friction**: Modern registration can be very simple:
   ```
   Email → Verify → Get API Key (30 seconds)
   ```

## Implementation Strategy

### Phase 1: Basic API Key System (MVP for Launch)

**User Model:**
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string ApiKey { get; set; }  // SHA-256 hash stored
    public DateTime CreatedAt { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string Tier { get; set; } = "free";  // free, pro, enterprise
}
```

**Rate Limit Model:**
```csharp
public class RateLimitConfig
{
    public string Tier { get; set; }
    public int RequestsPerHour { get; set; }
    public int RequestsPerDay { get; set; }
    public int ConcurrentRequests { get; set; }
}
```

**Free Tier Limits (Suggested):**
- 1,000 requests/hour
- 10,000 requests/day
- 5 concurrent requests
- No SLA guarantees

**Registration Endpoints:**
```
POST /api/v1/auth/register
  { "email": "user@example.com" }
  → Sends verification email

POST /api/v1/auth/verify
  { "email": "user@example.com", "code": "123456" }
  → Returns API key

POST /api/v1/auth/reset-key
  { "email": "user@example.com" }
  → Sends new key via email
```

**Authentication Middleware:**
- Check for `X-API-Key` header on all `/api/v1/rovers/*` endpoints
- Return `401 Unauthorized` if missing/invalid
- Return `429 Too Many Requests` if rate limit exceeded
- Use in-memory cache (Redis later) for rate limit tracking

**Database:**
- New `users` table
- New `api_requests` table for usage tracking (optional, can add later)

### Phase 2: Enhanced Features (Post-Launch)

**Usage Dashboard:**
- Web UI at `https://marsvista.app/dashboard`
- Show API key, usage stats, rate limits
- Regenerate API key button
- View request history

**Premium Tiers:**
- **Pro Tier** ($9/month):
  - 100,000 requests/day
  - 10,000 requests/hour
  - 20 concurrent requests
  - Priority support

- **Enterprise Tier** (Custom pricing):
  - Unlimited requests
  - Dedicated infrastructure
  - SLA guarantees
  - Custom data delivery

**Analytics:**
- Track popular queries for caching optimization
- Identify power users for case studies
- Monitor for abuse patterns

### Phase 3: Advanced Features (Future)

- OAuth2 support for third-party integrations
- Webhook notifications for new photos
- Custom rate limits per user
- Team/organization accounts
- API usage analytics API

## Migration Path for Existing Users

**If you've already shared the API publicly without keys:**

1. **Grace period**: Allow both authenticated and unauthenticated access for 60 days
2. **Announcements**:
   - Add banner to API docs
   - Email known users (if any)
   - Post to social media
3. **Deprecation**: After 60 days, require API keys
4. **Legacy support**: Offer "legacy" tier with same limits as old unauthenticated access

**Since you're pre-launch:** Perfect time to implement API keys!

## Technical Implementation Notes

### Security Best Practices

1. **API Key Format**:
   ```
   mv_live_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6  (36 chars after prefix)
   ```
   - `mv_` prefix (brand)
   - `live_` or `test_` environment
   - Random 32-byte hex string
   - Store SHA-256 hash in database (never plaintext)

2. **Rate Limiting**:
   - Use middleware to check limits before hitting database
   - Cache user tier in memory (Redis/MemoryCache)
   - Return standard rate limit headers:
     ```
     X-RateLimit-Limit: 1000
     X-RateLimit-Remaining: 543
     X-RateLimit-Reset: 1637251200
     ```

3. **Email Verification**:
   - Use time-limited codes (15 minutes)
   - Don't activate API key until verified
   - Prevents spam/abuse

4. **Secrets Management**:
   - Store email API credentials in Railway environment variables
   - Use JWT tokens for password reset flows
   - Never log API keys

### Database Schema

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    api_key_hash VARCHAR(64) NOT NULL,
    tier VARCHAR(20) DEFAULT 'free',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    email_verified_at TIMESTAMP,
    last_request_at TIMESTAMP
);

CREATE INDEX idx_users_api_key ON users(api_key_hash);
CREATE INDEX idx_users_email ON users(email);

-- Optional: Detailed usage tracking
CREATE TABLE api_requests (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    endpoint VARCHAR(255),
    method VARCHAR(10),
    status_code INT,
    response_time_ms INT,
    requested_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_api_requests_user_time ON api_requests(user_id, requested_at);
```

### Email Service Options

1. **Resend** (Recommended for MVP):
   - Free tier: 3,000 emails/month
   - Simple API, great for transactional emails
   - Good deliverability
   - Cost: Free → $20/month for 50k emails

2. **SendGrid**:
   - Free tier: 100 emails/day
   - More complex but powerful
   - Cost: Free → $15/month for 40k emails

3. **AWS SES**:
   - $0.10 per 1,000 emails
   - Requires setup/verification
   - Best for high volume

**Recommendation**: Start with Resend for simplicity.

## User Experience Flow

### Registration (30 seconds)

1. Visit `https://marsvista.app/signup`
2. Enter email address
3. Receive verification code via email
4. Enter code → Get API key immediately
5. Copy API key, view quick start guide

### Using the API

```bash
curl "https://api.marsvista.app/api/v1/rovers/curiosity/photos?sol=1000" \
  -H "X-API-Key: mv_live_abc123..."
```

### Error Responses

```json
// Missing API key
{
  "error": "API key required",
  "message": "Get your free API key at https://marsvista.app/signup",
  "status": 401
}

// Invalid API key
{
  "error": "Invalid API key",
  "message": "Check your API key or register at https://marsvista.app/signup",
  "status": 401
}

// Rate limit exceeded
{
  "error": "Rate limit exceeded",
  "message": "Free tier limit: 1000 requests/hour. Upgrade at https://marsvista.app/pricing",
  "status": 429,
  "rateLimit": {
    "limit": 1000,
    "remaining": 0,
    "resetAt": "2025-11-17T15:00:00Z"
  }
}
```

## What to Build Now vs Later

### Build Now (Pre-Launch MVP):

1. ✅ User registration endpoint
2. ✅ Email verification flow
3. ✅ API key generation and storage
4. ✅ Authentication middleware
5. ✅ Basic rate limiting (in-memory)
6. ✅ Simple landing page with signup form
7. ✅ Update API docs to show API key usage

### Build Later (Post-Launch):

1. ⏰ User dashboard web UI
2. ⏰ Detailed usage analytics
3. ⏰ Premium tier payment processing
4. ⏰ Redis-based distributed rate limiting
5. ⏰ Webhook notifications
6. ⏰ Team/organization accounts

## Marketing Benefits

**With API keys, you can:**

1. **Announce milestones**: "1,000 developers using Mars Vista API!"
2. **Showcase users**: "Featured projects built with Mars Vista"
3. **Gather testimonials**: Email power users for quotes
4. **Newsletter**: Updates about new rovers, features, data
5. **Analytics for blog posts**: "Curiosity is 3x more popular than Perseverance"

**Without API keys:**
- You're flying blind
- Can't prove adoption
- Can't build community

## Cost Considerations

**Additional infrastructure needed:**

1. **Email service**: Resend free tier (3k emails/month) is plenty for launch
2. **Database storage**: Users table is tiny (KB per user)
3. **Rate limiting cache**: In-memory is fine initially, Redis later (~$5/month)

**Estimated costs for 1,000 users:**
- Email: $0 (free tier)
- Database: +0.01 MB
- Rate limiting: $0 (in-memory)

**Total additional cost: ~$0** for MVP!

## Recommended Launch Strategy

### Week 1: Build API Key System

1. Create user registration system
2. Implement API key authentication
3. Add basic rate limiting
4. Update docs to show API key usage

### Week 2: Build Landing Page

1. Simple signup form at `https://marsvista.app`
2. Quick start guide
3. Pricing page (Free tier + "Pro coming soon")
4. Link to API docs

### Week 3: Soft Launch

1. Share with small community (Reddit, HN)
2. Monitor for issues
3. Gather feedback

### Week 4+: Public Launch

1. Product Hunt launch
2. Blog post announcement
3. Social media promotion
4. Monitor growth and scale as needed

## Decision Summary

**Implement API keys from day one:**

✅ Protects against abuse
✅ Enables usage analytics
✅ Builds user community
✅ Future-proofs for monetization
✅ Industry standard practice
✅ Low implementation cost
✅ Professional appearance

**The friction is minimal, the benefits are massive.**

## Next Steps

1. Review this analysis and decide on approach
2. If API keys approved, create Story 010 for implementation
3. Choose email service provider (Resend recommended)
4. Design user registration flow
5. Implement authentication middleware
6. Build simple landing page
7. Update API documentation
8. Launch! 🚀
