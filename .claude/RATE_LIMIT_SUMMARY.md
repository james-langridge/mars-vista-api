# Rate Limit Strategy - Quick Reference

## Why We Changed From 1k/hour to 60/hour

**Original proposal**: 1,000 requests/hour, 10,000 requests/day (FREE)

**Problem**:
- Too generous → users never upgrade (1-2% conversion)
- High infrastructure costs on Railway pay-per-use
- Enables full database scraping (2M photos)
- No incentive for paid tiers

**Revised limits**: 60 requests/hour, 500 requests/day (FREE)

**Benefits**:
- Protects Railway costs (100 users × 500/day = manageable)
- Encourages upgrades (10%+ conversion rate expected)
- Still exceeds NASA DEMO_KEY limits (30/hour)
- Matches industry standard (GitHub: 60/hour unauthenticated)

## Tier Comparison

| Tier | Requests/Hour | Requests/Day | Price | Use Case |
|------|---------------|--------------|-------|----------|
| **Free** | 60 | 500 | $0 | Testing, prototypes, hobby projects |
| **Pro** | 5,000 | 100,000 | $9/mo | Production apps, moderate traffic |
| **Enterprise** | Unlimited* | Unlimited* | Custom | Research, high-traffic sites, SLA needs |

*Soft limit: 100k/hour

## Why Users Will Upgrade

**Free → Pro jump**:
- 83x more hourly capacity (60 → 5,000)
- 200x more daily capacity (500 → 100,000)
- $9/month < cost of self-hosting ($22-27/month)
- $9/month < time spent managing infrastructure

**Conversion triggers**:
- Hit rate limit during development
- Launch production app
- User growth
- Need reliability/SLA

## Revenue Projections

**Conservative (1,000 users, 10% conversion)**:
- Free users: 900
- Pro users: 100 × $9 = $900/month
- Infrastructure cost: ~$40/month
- **Net: $860/month profit**

**Moderate growth (5,000 users, 5% conversion)**:
- Free users: 4,750
- Pro users: 250 × $9 = $2,250/month
- Infrastructure cost: ~$150/month
- **Net: $2,100/month profit**

## Competitive Positioning

**vs. NASA Mars Photo API**:
- NASA DEMO_KEY: ~30 requests/hour
- NASA Registered: ~1,000 requests/hour
- NASA Reliability: Often down, no SLA
- **Mars Vista Free**: 60/hour (2x NASA DEMO_KEY)
- **Mars Vista Pro**: 5,000/hour (5x NASA registered, better uptime)

**vs. Self-Hosting**:
- Self-host cost: $22-27/month + 5-10 hours maintenance
- **Mars Vista Pro**: $9/month, zero maintenance
- **Savings**: $13-18/month + 5-10 hours

## Implementation Notes

**Error response when limit hit**:
```json
{
  "error": "Rate limit exceeded",
  "message": "Free tier limit: 60 requests/hour. Upgrade to Pro for 5,000 requests/hour.",
  "upgrade": {
    "url": "https://marsvista.dev/pricing",
    "proTier": {
      "price": "$9/month",
      "hourlyLimit": 5000,
      "dailyLimit": 100000
    }
  }
}
```

**Rate limit headers**:
```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 43
X-RateLimit-Reset: 1731859200
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
```

## Future Adjustments

Monitor and adjust based on:
- Free tier conversion rate (target: 5-10%)
- User complaints about limits
- Infrastructure costs (should be < 20% of revenue)
- Heavy user churn

**Possible middle tier** if needed:
- **Hobbyist** ($4/month): 500/hour, 10k/day

## Related Documents

- `.claude/decisions/DECISION-019-rate-limit-strategy.md` - Full analysis
- `.claude/stories/STORY-010-api-key-authentication.md` - Implementation plan
- `.claude/API_KEYS_AND_USER_MANAGEMENT.md` - Overall auth strategy
