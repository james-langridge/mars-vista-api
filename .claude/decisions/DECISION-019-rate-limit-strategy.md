# Decision 019: Rate Limit Strategy and Free Tier Boundaries

## Context

Mars Vista API is preparing for public launch with API key authentication. We need to determine appropriate rate limits for the free tier that:

1. **Protect infrastructure costs** (Railway pay-per-use model)
2. **Prevent abuse** (runaway scripts, DOS attacks)
3. **Encourage upgrades** (free tier should be useful but limited)
4. **Competitive positioning** (industry standards for free APIs)

## Current Database Stats

- **Total photos**: 1,977,520 (nearly 2 million!)
- **Photos per rover**:
  - Perseverance: 451,602
  - Curiosity: 675,765
  - Opportunity: 548,817
  - Spirit: 301,336

## Railway Cost Structure (2025)

**Hobby Plan**: $5/month + usage
- Includes $5 of resource credits
- If usage < $5, no additional charges
- If usage > $5, pay the delta

**Usage-based billing**:
- RAM hours
- CPU hours
- Database storage
- Network egress

**Real-world example**: Small Rails app + PostgreSQL = ~$12/month

## Problem: Original Proposal Too Generous

**Proposed free tier**: 1,000 requests/hour, 10,000 requests/day

**Why this is concerning**:

### Cost Analysis

1. **Database queries**: Each photo request queries 2M+ row table with joins
2. **Network egress**: Railway charges for data transfer
3. **CPU usage**: JSON serialization, filtering, pagination
4. **Concurrent users**: If 100 users max out free tier = 1M requests/day

**Scenario**: 10 power users maxing out free tier
- 10 users × 10,000 req/day = 100,000 requests/day
- 100,000 × 30 days = 3,000,000 requests/month
- At ~0.5KB per response = 1.5 GB egress/month
- Plus database CPU load for 2M row queries

**Estimated Railway cost**: $20-50/month from free tier users alone!

### User Behavior Risk

**Original limits enable**:
- Full database scraping (2M photos ÷ 10k/day = 200 days to scrape everything)
- Production-scale apps running on free tier forever
- No incentive to upgrade

**Result**: Zero paid conversions, high infrastructure costs

## Industry Comparison

### Data APIs (Similar to Mars Vista)

**GitHub API**:
- **Unauthenticated**: 60 requests/hour
- **Authenticated (free)**: 5,000 requests/hour
- **Paid tiers**: Higher limits + GraphQL

**NASA Mars Photo API**:
- **DEMO_KEY**: ~30 requests/hour (actual, undocumented)
- **Registered (free)**: 1,000 requests/hour
- **No paid tier** (government service)

**Stripe API**:
- **Test mode**: 25 requests/second (90,000/hour!)
- **Live mode**: Rate limited by account tier

**Twilio API**:
- **Free trial**: $15.50 credit, then pay-per-use
- **No unlimited free tier**

### AI APIs (More Restrictive)

**OpenAI API**:
- **Free tier**: 3 requests/minute (180/hour)
- **Paid tier**: 3,500+ requests/minute

**Google Gemini API**:
- **Free tier**: 5 requests/minute (300/hour)
- **Paid tier**: Higher limits

## Recommended Rate Limits

### Free Tier (Revised)

**Limits**:
- **60 requests/hour** (1 per minute sustained)
- **500 requests/day** (max burst usage)
- **3 concurrent requests**

**Why this makes sense**:

1. **Protects costs**:
   - 100 users × 500/day = 50,000 requests/day (manageable)
   - Prevents database scraping (2M photos ÷ 500/day = 10+ years)

2. **Useful for developers**:
   - 60/hour is GitHub's unauthenticated standard
   - Enough for testing, prototypes, hobby projects
   - Supports small personal websites (2-3 requests/page load)

3. **Encourages upgrades**:
   - Production apps will quickly need more
   - Serious projects will pay for reliability
   - Clear value proposition for Pro tier

4. **Aligns with NASA**:
   - Similar to NASA's DEMO_KEY limits
   - Higher than their free tier (30/hour) actually

### Pro Tier ($9/month)

**Limits**:
- **5,000 requests/hour** (matches GitHub authenticated)
- **100,000 requests/day**
- **50 concurrent requests**

**Why users will upgrade**:
- **20x more hourly capacity** (60 → 5,000)
- **200x more daily capacity** (500 → 100,000)
- Supports production websites with moderate traffic
- Still costs less than running their own scraper

**Revenue potential**:
- If 10% of users upgrade: 100 users × $9 = $900/month
- Covers infrastructure + profit

### Enterprise Tier (Custom pricing)

**Limits**:
- **Unlimited requests** (or 100k/hour soft limit)
- **Dedicated infrastructure** (optional)
- **SLA guarantees** (99.9% uptime)
- **Custom data delivery** (bulk exports, webhooks)

**Target customers**:
- Research institutions
- Space education platforms
- Media/journalism organizations
- High-traffic public websites

**Pricing**: $99-499/month depending on needs

## Comparison Table

| Feature | Free | Pro ($9/mo) | Enterprise (Custom) |
|---------|------|-------------|---------------------|
| **Requests/Hour** | 60 | 5,000 | Unlimited |
| **Requests/Day** | 500 | 100,000 | Unlimited |
| **Concurrent Requests** | 3 | 50 | 100+ |
| **Rate Limit Headers** | ✅ | ✅ | ✅ |
| **Support** | Community | Email | Priority |
| **SLA** | None | 99% | 99.9% |
| **Custom Features** | ❌ | ❌ | ✅ |
| **Cost/Month** | $0 | $9 | $99-499 |

## Cost-Benefit Analysis

### Free Tier Revenue Impact

**Scenario 1: Conservative (1,000 users, 10% upgrade rate)**
- Free users: 900 × 500 req/day = 450,000 req/day
- Pro users: 100 × $9 = $900/month revenue
- Infrastructure cost: ~$20-40/month
- **Net profit: $860-880/month**

**Scenario 2: Moderate Growth (5,000 users, 5% upgrade rate)**
- Free users: 4,750 × 500 req/day = 2.375M req/day
- Pro users: 250 × $9 = $2,250/month revenue
- Infrastructure cost: ~$100-150/month (scaled)
- **Net profit: $2,100-2,150/month**

**Scenario 3: If we kept 10k/day free tier (1,000 users, 1% upgrade rate)**
- Free users: 990 × 10k/day = 9.9M req/day
- Pro users: 10 × $9 = $90/month revenue
- Infrastructure cost: ~$150-300/month (heavy load)
- **Net loss: -$60 to -$210/month**

### Strategic Implications

**Tight free tier (60/hour, 500/day)**:
- ✅ Forces serious users to upgrade
- ✅ Keeps infrastructure costs low
- ✅ Higher conversion rate (10%+ possible)
- ✅ Sustainable business model
- ⚠️ May limit viral growth

**Generous free tier (1k/hour, 10k/day)**:
- ✅ Easy viral growth
- ❌ Most users never upgrade (1-2% conversion)
- ❌ High infrastructure costs
- ❌ Unsustainable without venture funding

## Competitive Positioning

### vs. NASA Mars Photo API

**NASA**:
- Free tier: ~30 requests/hour (DEMO_KEY)
- Registered: ~1,000 requests/hour
- Reliability: Often down, no SLA
- Data: Only NASA API sources

**Mars Vista (Proposed)**:
- Free tier: 60 requests/hour (2x NASA DEMO_KEY)
- Pro tier: 5,000 requests/hour (5x NASA registered)
- Reliability: 99%+ uptime (Railway)
- Data: NASA + potentially other sources (future)

**Value proposition**: "More reliable than NASA, with support and SLAs"

### vs. Self-Hosting

**Self-hosting costs**:
- Server: $5-10/month (DigitalOcean)
- Database: $15/month (managed PostgreSQL)
- Domain/SSL: $2/month
- Maintenance time: 5-10 hours/month
- **Total: $22-27/month + 5-10 hours**

**Mars Vista Pro tier**: $9/month, zero maintenance

**Value proposition**: "Cheaper and easier than running your own"

## Implementation Notes

### Grace Period for Early Adopters

If we already have users before implementing API keys:

1. **Legacy tier** (grandfathered):
   - Same limits as current free tier
   - Email users: "Thanks for early adoption, you're grandfathered!"
   - Creates goodwill and testimonials

2. **New users** get standard free tier (60/hour, 500/day)

### Rate Limit Header Standards

```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 42
X-RateLimit-Reset: 1731859200
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

**Note the upgrade URL**: Gentle nudge to convert when limits are hit!

### Error Messages That Convert

**When rate limit exceeded**:

```json
{
  "error": "Rate limit exceeded",
  "message": "Free tier limit: 60 requests/hour. Upgrade to Pro for 5,000 requests/hour.",
  "status": 429,
  "rateLimit": {
    "limit": 60,
    "remaining": 0,
    "resetAt": "2025-11-17T15:00:00Z",
    "tier": "free"
  },
  "upgrade": {
    "url": "https://marsvista.dev/pricing",
    "proTier": {
      "price": "$9/month",
      "limit": "5,000 requests/hour",
      "dailyLimit": "100,000 requests/day"
    }
  }
}
```

**Psychology**: When users hit the limit, they're most motivated to upgrade!

## Alternative: Usage-Based Pricing

Instead of tiers, charge per 1,000 requests:

**Pricing**:
- First 500 requests/day: Free
- Next 10,000 requests: $0.10/1,000 ($1 for 10k)
- Next 100,000 requests: $0.05/1,000 ($5 for 100k)
- 1M+ requests: $0.02/1,000 ($20 for 1M)

**Pros**:
- Fair: pay for what you use
- No "cliff" when hitting tier limits
- Scales smoothly with growth

**Cons**:
- Unpredictable bills (users hate this)
- Harder to market ("$9/month" vs "starting at $0.10/1k requests")
- Requires payment processing for every user (vs just paid tiers)

**Recommendation**: Stick with tier-based pricing for simplicity

## Decision

**Implement the following rate limits**:

### Free Tier
- ✅ 60 requests/hour (1/minute sustained)
- ✅ 500 requests/day
- ✅ 3 concurrent requests
- ✅ No SLA
- ✅ Community support

### Pro Tier ($9/month)
- ✅ 5,000 requests/hour
- ✅ 100,000 requests/day
- ✅ 50 concurrent requests
- ✅ 99% uptime SLA
- ✅ Email support

### Enterprise Tier (Custom)
- ✅ Unlimited (or 100k/hour soft cap)
- ✅ Dedicated resources (optional)
- ✅ 99.9% uptime SLA
- ✅ Priority support
- ✅ Custom features (webhooks, bulk exports, etc.)

## Rationale Summary

1. **Cost protection**: Free tier won't bankrupt the project
2. **Conversion optimization**: Clear upgrade incentive at 60→5,000 jump
3. **Industry alignment**: Matches GitHub (60/hour) and exceeds NASA DEMO_KEY (30/hour)
4. **Sustainable growth**: 5-10% conversion at scale = profitable
5. **Competitive value**: Pro tier cheaper than self-hosting

## Future Adjustments

Monitor these metrics and adjust if needed:

**Watch for**:
- Free tier conversion rate < 3% → limits may be too generous
- Complaints about limits being too low → collect feedback, maybe add middle tier
- Infrastructure costs > 20% of revenue → tighten free tier
- Heavy users churning → add custom enterprise pricing

**Possible middle tier** (if demand exists):
- **Hobbyist** ($4/month): 500/hour, 10k/day (fills gap between free and pro)

## Related Documents

- `.claude/API_KEYS_AND_USER_MANAGEMENT.md` - Overall authentication strategy
- `.claude/stories/STORY-010-api-key-authentication.md` - Implementation story

## Implementation

Update Story 010 with these revised limits before implementation.
