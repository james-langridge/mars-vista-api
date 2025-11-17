# API Key Setup Guide

The Mars Vista API supports optional API key authentication to protect endpoints during development or private deployment.

## Overview

The API has two layers of protection:

### Layer 1: Rate Limiting (Always Active)
- ✅ Limits any IP to **100 requests per minute**
- ✅ Protects against spam/DDoS even without valid API key
- ✅ Returns 429 Too Many Requests when exceeded
- ✅ Resets every minute

### Layer 2: API Key Authentication (Optional)
- ✅ Protects all endpoints (except `/health`)
- ✅ Accepts API key via header OR query parameter
- ✅ Returns clear error messages for unauthorized requests
- ✅ Logs failed authentication attempts
- ✅ Can be disabled by not setting the key

## Configuration

### Railway (Production)

Add the `API_KEY` environment variable in Railway dashboard:

1. Go to your Railway project
2. Select the `mars-vista-api` service
3. Click "Variables" tab
4. Add new variable:
   - **Name**: `API_KEY`
   - **Value**: Your secret key (e.g., `sk_prod_abc123xyz789`)

Railway will automatically restart the service with the new variable.

### Local Development

Set the API_KEY environment variable:

**Option 1: Environment variable**
```bash
export API_KEY=your_local_test_key
dotnet run --project src/MarsVista.Api
```

**Option 2: appsettings.Development.json** (not recommended - keep keys out of git)
```json
{
  "API_KEY": "your_local_test_key"
}
```

**Option 3: .env file** (recommended for local dev)
```bash
# Create .env file (gitignored)
echo "API_KEY=your_local_test_key" > .env

# Use with dotnet-env or similar tool
dotnet run --project src/MarsVista.Api
```

## Usage

### With Header (Recommended)

```bash
curl -H "X-API-Key: your_secret_key" \
  "https://api.marsvista.dev/api/v1/rovers"
```

### With Query Parameter

```bash
curl "https://api.marsvista.dev/api/v1/rovers?api_key=your_secret_key"
```

**Note**: Query parameter is convenient for testing but less secure (appears in logs/URLs). Use header in production.

### JavaScript/TypeScript Example

```typescript
// Using fetch with header (recommended)
const response = await fetch('https://api.marsvista.dev/api/v1/rovers', {
  headers: {
    'X-API-Key': 'your_secret_key'
  }
});

// Or with query parameter
const response = await fetch(
  'https://api.marsvista.dev/api/v1/rovers?api_key=your_secret_key'
);
```

### Python Example

```python
import requests

# Using header
headers = {'X-API-Key': 'your_secret_key'}
response = requests.get('https://api.marsvista.dev/api/v1/rovers', headers=headers)

# Or with query parameter
response = requests.get(
    'https://api.marsvista.dev/api/v1/rovers',
    params={'api_key': 'your_secret_key'}
)
```

## Error Responses

### Rate Limit Exceeded (429)

If you exceed 100 requests per minute:

**Request**:
```bash
# 101st request within same minute
curl "https://api.marsvista.dev/api/v1/rovers"
```

**Response** (429 Too Many Requests):
```json
{
  "error": "Too Many Requests",
  "message": "Rate limit exceeded. Maximum 100 requests per minute per IP address.",
  "retryAfter": 42
}
```

The `retryAfter` field tells you how many seconds until the limit resets.

### No API Key Provided

**Request**:
```bash
curl "https://api.marsvista.dev/api/v1/rovers"
```

**Response** (401 Unauthorized):
```json
{
  "error": "Unauthorized",
  "message": "API key required. Provide via X-API-Key header or api_key query parameter."
}
```

### Invalid API Key

**Request**:
```bash
curl -H "X-API-Key: wrong_key" \
  "https://api.marsvista.dev/api/v1/rovers"
```

**Response** (403 Forbidden):
```json
{
  "error": "Forbidden",
  "message": "Invalid API key."
}
```

## Health Check Exemption

The `/health` endpoint is **always accessible** without an API key:

```bash
# Works without API key
curl "https://api.marsvista.dev/health"
```

This allows:
- Railway health checks to work
- Uptime monitoring services
- Quick status checks without authentication

## Disabling API Key Protection

To make the API publicly accessible without authentication:

1. **Remove** or **leave empty** the `API_KEY` environment variable
2. Restart the service

The middleware will log a warning and allow all requests:
```
API_KEY not configured - API is unprotected!
```

## Security Best Practices

### Generating Secure Keys

Use a cryptographically secure random string:

```bash
# Generate random API key (32 characters)
openssl rand -base64 32

# Or use UUIDs
uuidgen
```

Example keys:
- `sk_prod_a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6`
- `7f8e9d8c-7b6a-5949-3827-1605040302010`

### Key Management

**DO**:
- ✅ Use different keys for development and production
- ✅ Rotate keys periodically
- ✅ Store keys in environment variables (not in code)
- ✅ Use Railway's built-in secrets management
- ✅ Monitor logs for failed auth attempts

**DON'T**:
- ❌ Commit keys to git
- ❌ Share keys in public documentation
- ❌ Use simple/guessable keys like "test123"
- ❌ Include keys in error messages
- ❌ Log API keys

## Monitoring

Failed authentication attempts are logged with the client's IP:

```
[Warning] API request without API key from 192.168.1.100
[Warning] Invalid API key attempt from 192.168.1.100
```

Check Railway logs:
```bash
railway logs --filter "Invalid API key"
railway logs --filter "without API key"
```

## Future Enhancements

This is a simple hardcoded key for initial protection. Future improvements (Story 015):

1. **Multiple API Keys**: Database-backed keys for different users
2. **Rate Limiting**: Per-key request limits
3. **Key Scopes**: Read-only vs admin keys
4. **Usage Analytics**: Track usage per key
5. **Automatic Expiration**: Time-limited keys
6. **Key Management API**: Create/revoke keys via endpoints

## Troubleshooting

### API returns 401 but I provided the key

**Check**:
1. Header name is exactly `X-API-Key` (case-sensitive)
2. No extra spaces in header value
3. Key matches exactly (no newlines/whitespace)

**Test with curl verbose**:
```bash
curl -v -H "X-API-Key: your_key" "https://api.marsvista.dev/api/v1/rovers"
```

Look for the request header:
```
> X-API-Key: your_key
```

### Health check works but other endpoints don't

This is expected! `/health` is exempt from authentication. All other endpoints require the API key.

### API key works locally but not in production

**Check**:
1. `API_KEY` environment variable is set in Railway
2. Railway service restarted after adding variable
3. Using the correct production key (not local dev key)

**Verify in Railway**:
```bash
railway variables | grep API_KEY
```

## Related Documentation

- [Deployment Guide](./DEPLOYMENT.md) - Railway deployment
- [API Endpoints](./API_ENDPOINTS.md) - Complete API reference
- [Scaling Guide](./SCALING.md) - Performance and capacity planning
