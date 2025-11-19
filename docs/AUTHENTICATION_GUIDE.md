# Mars Vista API Authentication Guide

Complete guide to authentication, API keys, and rate limits for the Mars Vista API.

## Overview

The Mars Vista API uses API key-based authentication for all query endpoints. This ensures fair usage, prevents abuse, and enables features like usage tracking and premium tiers.

## Getting Your API Key

### Step 1: Sign In

Visit [marsvista.dev/signin](https://marsvista.dev/signin) and enter your email address. We use passwordless authentication via magic links:

1. Enter your email address
2. Click "Send Magic Link"
3. Check your email for a sign-in link from Mars Vista
4. Click the link to authenticate (link expires in 24 hours)
5. You'll be redirected to your dashboard

**No password needed** - we use Auth.js with Resend for secure, passwordless authentication.

### Step 2: Generate Your API Key

Once signed in, navigate to [marsvista.dev/dashboard](https://marsvista.dev/dashboard):

1. Click "Generate API Key" button
2. Your API key will be displayed **once** - copy it immediately
3. Store it securely (treat it like a password)

**API Key Format:**
```
mv_live_a1b2c3d4e5f6789012345678901234567890abcd
```

- `mv_` - Mars Vista prefix
- `live_` - Environment indicator (production)
- `{random}` - 40 cryptographically random characters

### Step 3: Use Your API Key

Include your API key in the `X-API-Key` header for all requests:

```bash
curl -H "X-API-Key: mv_live_a1b2c3d4e5f6789012345678901234567890abcd" \
  "https://api.marsvista.dev/api/v1/rovers"
```

## Using Your API Key

### Basic Example

```bash
curl -H "X-API-Key: YOUR_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"
```

### Python Example

```python
import requests

API_KEY = "mv_live_a1b2c3d4e5f6789012345678901234567890abcd"
BASE_URL = "https://api.marsvista.dev"

headers = {
    "X-API-Key": API_KEY
}

response = requests.get(
    f"{BASE_URL}/api/v1/rovers/curiosity/photos",
    headers=headers,
    params={"sol": 1000, "per_page": 25}
)

photos = response.json()
print(f"Found {len(photos['photos'])} photos")
```

### JavaScript/Node.js Example

```javascript
const API_KEY = "mv_live_a1b2c3d4e5f6789012345678901234567890abcd";
const BASE_URL = "https://api.marsvista.dev";

async function getPhotos(rover, sol) {
  const response = await fetch(
    `${BASE_URL}/api/v1/rovers/${rover}/photos?sol=${sol}`,
    {
      headers: {
        "X-API-Key": API_KEY
      }
    }
  );

  const data = await response.json();
  return data.photos;
}

const photos = await getPhotos("curiosity", 1000);
console.log(`Found ${photos.length} photos`);
```

### Environment Variables

**Best practice:** Store your API key in an environment variable:

```bash
export MARS_VISTA_API_KEY="mv_live_a1b2c3d4e5f6789012345678901234567890abcd"

curl -H "X-API-Key: $MARS_VISTA_API_KEY" \
  "https://api.marsvista.dev/api/v1/rovers"
```

**Never commit API keys to version control** - add them to `.gitignore`:

```bash
# .env file
MARS_VISTA_API_KEY=mv_live_a1b2c3d4e5f6789012345678901234567890abcd
```

## Rate Limits

### Free Tier (Default)

When you create an account, you start on the free tier:

- **1,000 requests per hour** (16 per minute sustained)
- **10,000 requests per day**
- **5 concurrent requests**

**Matches NASA's API Gateway** (which offers 1,000 req/hour shared across all 16 APIs)

**Advantage over NASA:** Our 1,000 req/hour is dedicated to Mars photos only, not shared with other APIs!

Perfect for:
- Learning and experimentation
- Personal projects
- Small applications
- Educational use

### Pro Tier ($20/month)

Upgrade at [marsvista.dev/pricing](https://marsvista.dev/pricing):

- **10,000 requests per hour** (166 per minute sustained)
- **100,000 requests per day**
- **25 concurrent requests**
- **Usage analytics dashboard**
- **Priority support**

**10x better than NASA's API Gateway**

Perfect for:
- Production applications
- Growing startups
- Research projects
- Commercial use
- High-traffic websites

## Rate Limit Headers

Every API response includes rate limit information in the headers:

```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 987
X-RateLimit-Reset: 1731859200
X-RateLimit-Tier: free
X-RateLimit-Upgrade-Url: https://marsvista.dev/pricing
Content-Type: application/json
```

**Header Descriptions:**

- `X-RateLimit-Limit` - Maximum requests allowed in current window (hour)
- `X-RateLimit-Remaining` - Requests remaining in current window
- `X-RateLimit-Reset` - Unix timestamp when the limit resets
- `X-RateLimit-Tier` - Your current tier (`free` or `pro`)
- `X-RateLimit-Upgrade-Url` - URL to upgrade your tier

### Checking Rate Limits in Code

**Python:**
```python
response = requests.get(url, headers=headers)

limit = response.headers.get("X-RateLimit-Limit")
remaining = response.headers.get("X-RateLimit-Remaining")
reset = response.headers.get("X-RateLimit-Reset")

print(f"Rate Limit: {remaining}/{limit} remaining")
print(f"Resets at: {datetime.fromtimestamp(int(reset))}")
```

**JavaScript:**
```javascript
const response = await fetch(url, { headers });

const limit = response.headers.get("X-RateLimit-Limit");
const remaining = response.headers.get("X-RateLimit-Remaining");
const reset = response.headers.get("X-RateLimit-Reset");

console.log(`Rate Limit: ${remaining}/${limit} remaining`);
console.log(`Resets at: ${new Date(reset * 1000)}`);
```

## Regenerating Your API Key

If your API key is compromised or you want to rotate it:

1. Sign in to [marsvista.dev/dashboard](https://marsvista.dev/dashboard)
2. Click "Regenerate API Key"
3. Confirm the action (old key will stop working immediately)
4. Copy and save your new API key

**Important:** Your old API key becomes invalid immediately after regeneration. Update all applications using the old key.

## Error Responses

### 401 Unauthorized - Missing or Invalid API Key

**Cause:** No `X-API-Key` header provided, or the key is invalid.

**Response:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid or missing API key. Sign in at https://marsvista.dev to get your API key."
}
```

**Solution:**
- Verify you're including the `X-API-Key` header
- Check that your API key is correct (no typos)
- Ensure you haven't regenerated your key and are using an old one
- Sign in to [marsvista.dev/dashboard](https://marsvista.dev/dashboard) to view/regenerate your key

### 429 Too Many Requests - Rate Limit Exceeded

**Cause:** You've exceeded your tier's rate limits.

**Response:**
```json
{
  "error": "Rate limit exceeded",
  "message": "You have exceeded the 60 requests per hour limit for the free tier.",
  "tier": "free",
  "limit": 60,
  "resetAt": "2025-11-18T15:00:00Z",
  "upgradeUrl": "https://marsvista.dev/pricing"
}
```

**Solutions:**
1. **Wait:** Rate limits reset every hour
2. **Optimize:** Reduce unnecessary requests, implement caching
3. **Upgrade:** Switch to Pro or Enterprise tier

### Best Practices for Avoiding Rate Limits

**1. Implement Caching**

Cache API responses to reduce repeated requests:

```python
import requests
from datetime import datetime, timedelta

cache = {}
CACHE_DURATION = timedelta(hours=1)

def get_photos_cached(rover, sol):
    cache_key = f"{rover}:{sol}"

    # Check cache
    if cache_key in cache:
        data, timestamp = cache[cache_key]
        if datetime.now() - timestamp < CACHE_DURATION:
            return data

    # Fetch from API
    response = requests.get(
        f"{BASE_URL}/api/v1/rovers/{rover}/photos",
        headers={"X-API-Key": API_KEY},
        params={"sol": sol}
    )

    data = response.json()
    cache[cache_key] = (data, datetime.now())
    return data
```

**2. Use Pagination Wisely**

Request only what you need:

```bash
# Bad: requesting 100 photos per page when you only need 10
curl -H "X-API-Key: $KEY" \
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&per_page=100"

# Good: request only what you need
curl -H "X-API-Key: $KEY" \
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&per_page=10"
```

**3. Monitor Your Usage**

Track your rate limits in your application:

```python
def make_request(url):
    response = requests.get(url, headers={"X-API-Key": API_KEY})

    remaining = int(response.headers.get("X-RateLimit-Remaining", 0))
    limit = int(response.headers.get("X-RateLimit-Limit", 60))

    if remaining < limit * 0.1:  # Less than 10% remaining
        print(f"Warning: Only {remaining} requests remaining!")

    return response.json()
```

**4. Implement Backoff**

If you hit rate limits, wait before retrying:

```python
import time

def fetch_with_backoff(url, max_retries=3):
    for attempt in range(max_retries):
        response = requests.get(url, headers={"X-API-Key": API_KEY})

        if response.status_code == 429:
            reset_time = int(response.headers.get("X-RateLimit-Reset"))
            wait_seconds = reset_time - int(time.time())
            print(f"Rate limited. Waiting {wait_seconds} seconds...")
            time.sleep(wait_seconds + 1)
            continue

        return response.json()

    raise Exception("Max retries exceeded")
```

## Endpoints That Don't Require Authentication

The following endpoints are publicly accessible without an API key:

**Health Check:**
```bash
curl "https://api.marsvista.dev/health"
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-11-18T12:00:00Z"
}
```

## Security Best Practices

### DO:
- ✅ Store API keys in environment variables
- ✅ Add `.env` files to `.gitignore`
- ✅ Rotate API keys periodically
- ✅ Use HTTPS for all requests (enforced by API)
- ✅ Monitor your API usage regularly

### DON'T:
- ❌ Commit API keys to version control
- ❌ Share API keys publicly (GitHub, Stack Overflow, etc.)
- ❌ Use API keys in client-side JavaScript (use a backend proxy)
- ❌ Use the same API key across multiple applications
- ❌ Ignore rate limit headers

### Client-Side Applications

**Never expose API keys in frontend code** - use a backend proxy:

**Bad:**
```javascript
// ❌ DON'T DO THIS - API key exposed in browser
const API_KEY = "mv_live_a1b2c3d4e5f6789012345678901234567890abcd";
fetch(`https://api.marsvista.dev/api/v1/rovers`, {
  headers: { "X-API-Key": API_KEY }
});
```

**Good:**
```javascript
// ✅ Use a backend proxy
// Frontend code:
fetch("/api/mars-photos?rover=curiosity&sol=1000")
  .then(res => res.json());

// Backend proxy (Node.js/Express):
app.get("/api/mars-photos", async (req, res) => {
  const response = await fetch(
    `https://api.marsvista.dev/api/v1/rovers/${req.query.rover}/photos?sol=${req.query.sol}`,
    {
      headers: {
        "X-API-Key": process.env.MARS_VISTA_API_KEY
      }
    }
  );
  const data = await response.json();
  res.json(data);
});
```

## Troubleshooting

### "Invalid or missing API key" Error

**Checklist:**
1. Verify the header name is exactly `X-API-Key` (case-sensitive)
2. Check for typos in your API key
3. Ensure you're not using an old/regenerated key
4. Confirm the key starts with `mv_live_`
5. Try regenerating your key from the dashboard

### Rate Limits Not Resetting

Rate limits operate on **rolling windows**:
- **Hourly limit:** Resets exactly 60 minutes after your first request in the window
- **Daily limit:** Resets at midnight UTC

Check the `X-RateLimit-Reset` header for the exact reset time.

### API Key Not Working After Regeneration

After regenerating your API key:
1. Old key becomes invalid immediately
2. Update all applications using the old key
3. Clear any cached credentials
4. Restart applications that may have cached the old key

## Support

Need help with authentication?

- **Documentation:** [docs/API_ENDPOINTS.md](API_ENDPOINTS.md)
- **Dashboard:** [marsvista.dev/dashboard](https://marsvista.dev/dashboard)
- **Pricing:** [marsvista.dev/pricing](https://marsvista.dev/pricing)

## Summary

1. **Sign in** at marsvista.dev/signin with magic link
2. **Generate API key** from dashboard
3. **Include key** in `X-API-Key` header for all requests
4. **Monitor usage** via rate limit headers
5. **Upgrade tier** if needed at marsvista.dev/pricing

Happy exploring Mars! 🚀
