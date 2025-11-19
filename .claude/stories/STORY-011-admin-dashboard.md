# Story 011: Admin Dashboard for System Oversight

## Status
Planning

## Overview
Create a standalone admin dashboard (React + Vite + TypeScript + Tailwind) for system oversight and monitoring. This gives administrators visibility into users, API usage, rate limits, and system health without requiring complex user-facing analytics.

## Context
- **Story 010 Completed**: API key authentication and rate limiting deployed
- **Current State**: No visibility into overall system usage, user activity, or rate limit violations
- **Gap**: Administrators cannot monitor the system health or user behavior
- **Admin Need**: Simple oversight dashboard to monitor system activity and identify issues

## Goals
1. Standalone admin site at `web/admin-site/` (like `status-site`)
2. Hardcoded authentication (simple email/password)
3. System overview statistics (users, API calls, photos retrieved)
4. User list with usage stats and tier information
5. Recent API activity log
6. Rate limit violations tracking
7. **API performance metrics** - response times, slow queries, error rates
8. **Endpoint usage analytics** - most popular endpoints, success rates
9. **Performance trends** - response time over time, throughput graphs
10. System health indicators
11. **No user database complexity** - admin access only

## Technical Approach

### Architecture
- **React + Vite + TypeScript + Tailwind** (same stack as status-site)
- **Standalone SPA** at `web/admin-site/`
- **Hardcoded auth** in frontend (simple localStorage token after login)
- **Direct API calls** to C# backend using admin API key
- **No Auth.js** - simple credential check in frontend

### Authentication Strategy
```typescript
// Simple hardcoded credentials
const ADMIN_EMAIL = "admin@marsvista.com"
const ADMIN_PASSWORD = "mars-admin-2025"  // Change this in production!

// Store auth token in localStorage after successful login
// Use admin API key for backend requests
```

### Backend Requirements (New Endpoints)

**1. Admin Statistics Endpoint**
```
GET /api/v1/admin/stats
Authorization: X-API-Key: {admin_api_key}

Response:
{
  totalUsers: 42,
  activeApiKeys: 38,
  totalApiCalls: 125430,
  totalPhotosRetrieved: 3234567,
  apiCallsLast24h: 8452,
  apiCallsLast7d: 45231,
  averageResponseTime: 245,  // milliseconds
  systemUptime: "14 days"
}
```

**2. Admin Users List Endpoint**
```
GET /api/v1/admin/users
Authorization: X-API-Key: {admin_api_key}

Response:
{
  users: [
    {
      email: "user@example.com",
      tier: "free",
      apiKeyCreated: "2025-11-01T10:00:00Z",
      lastUsed: "2025-11-19T08:30:00Z",
      totalRequests: 1234,
      requestsToday: 45,
      requestsThisHour: 12,
      isActive: true
    },
    ...
  ]
}
```

**3. Admin Recent Activity Endpoint**
```
GET /api/v1/admin/activity?limit=50
Authorization: X-API-Key: {admin_api_key}

Response:
{
  events: [
    {
      timestamp: "2025-11-19T12:34:56Z",
      userEmail: "user@example.com",
      endpoint: "/api/v1/rovers/curiosity/photos",
      statusCode: 200,
      responseTime: 156,
      photosReturned: 25
    },
    ...
  ]
}
```

**4. Admin Rate Limit Violations Endpoint**
```
GET /api/v1/admin/rate-limit-violations?limit=50
Authorization: X-API-Key: {admin_api_key}

Response:
{
  violations: [
    {
      timestamp: "2025-11-19T12:34:56Z",
      userEmail: "user@example.com",
      tier: "free",
      violationType: "hourly",  // "hourly", "daily", "concurrent"
      requestCount: 61,
      limit: 60
    },
    ...
  ]
}
```

**5. Admin API Performance Metrics Endpoint**
```
GET /api/v1/admin/metrics/performance
Authorization: X-API-Key: {admin_api_key}

Response:
{
  currentMetrics: {
    averageResponseTime: 245,      // ms (last hour)
    p50ResponseTime: 180,           // median
    p95ResponseTime: 450,           // 95th percentile
    p99ResponseTime: 890,           // 99th percentile
    requestsPerMinute: 42.5,
    errorRate: 2.3,                 // percentage
    successRate: 97.7               // percentage
  },
  last24Hours: [
    {
      hour: "2025-11-19T12:00:00Z",
      avgResponseTime: 250,
      requests: 2450,
      errors: 45,
      successRate: 98.2
    },
    ...
  ],
  slowQueries: [
    {
      endpoint: "/api/v1/rovers/curiosity/photos",
      avgResponseTime: 1250,
      maxResponseTime: 3400,
      count: 15,
      lastOccurrence: "2025-11-19T12:34:56Z"
    },
    ...
  ]
}
```

**6. Admin Endpoint Usage Statistics**
```
GET /api/v1/admin/metrics/endpoints
Authorization: X-API-Key: {admin_api_key}

Response:
{
  topEndpoints: [
    {
      endpoint: "/api/v1/rovers/curiosity/photos",
      calls: 45230,
      avgResponseTime: 245,
      errorRate: 1.2,
      successRate: 98.8,
      last24h: 3450
    },
    {
      endpoint: "/api/v1/rovers/perseverance/photos",
      calls: 38920,
      avgResponseTime: 210,
      errorRate: 0.8,
      successRate: 99.2,
      last24h: 2890
    },
    ...
  ],
  roverUsage: {
    curiosity: 45230,
    perseverance: 38920,
    opportunity: 12340,
    spirit: 8920
  },
  cameraUsage: {
    "NAVCAM_LEFT": 12450,
    "NAVCAM_RIGHT": 11230,
    "FHAZ": 8920,
    ...
  }
}
```

**7. Admin Error Tracking Endpoint**
```
GET /api/v1/admin/metrics/errors?limit=50
Authorization: X-API-Key: {admin_api_key}

Response:
{
  errorSummary: {
    total: 245,
    last24h: 48,
    byStatusCode: {
      "400": 12,
      "404": 156,
      "429": 45,
      "500": 32
    }
  },
  recentErrors: [
    {
      timestamp: "2025-11-19T12:34:56Z",
      userEmail: "user@example.com",
      endpoint: "/api/v1/rovers/invalid/photos",
      statusCode: 404,
      errorMessage: "Rover not found",
      responseTime: 45
    },
    ...
  ],
  errorsByEndpoint: [
    {
      endpoint: "/api/v1/rovers/curiosity/photos",
      errorCount: 32,
      errorRate: 2.1,
      mostCommonError: "404"
    },
    ...
  ]
}
```

**8. Admin Performance Trends Endpoint**
```
GET /api/v1/admin/metrics/trends?period=24h
Authorization: X-API-Key: {admin_api_key}
Query params: period = "1h" | "24h" | "7d" | "30d"

Response:
{
  responseTimeTrend: [
    { timestamp: "2025-11-19T12:00:00Z", avgMs: 245, p95Ms: 450 },
    { timestamp: "2025-11-19T13:00:00Z", avgMs: 250, p95Ms: 460 },
    ...
  ],
  throughputTrend: [
    { timestamp: "2025-11-19T12:00:00Z", requestsPerMinute: 42.5 },
    { timestamp: "2025-11-19T13:00:00Z", requestsPerMinute: 45.2 },
    ...
  ],
  errorRateTrend: [
    { timestamp: "2025-11-19T12:00:00Z", errorRate: 2.3 },
    { timestamp: "2025-11-19T13:00:00Z", errorRate: 1.8 },
    ...
  ]
}
```

### Frontend Structure

```
web/admin-site/
├── src/
│   ├── App.tsx                 # Main app with routing
│   ├── main.tsx                # Entry point
│   ├── components/
│   │   ├── Login.tsx           # Login form with hardcoded credentials
│   │   ├── Dashboard.tsx       # Main dashboard layout with tabs
│   │   ├── StatsOverview.tsx   # System statistics cards
│   │   ├── UserList.tsx        # User table with stats
│   │   ├── ActivityLog.tsx     # Recent API activity
│   │   ├── RateLimitViolations.tsx  # Rate limit violations table
│   │   ├── PerformanceMetrics.tsx   # API performance metrics (NEW)
│   │   ├── EndpointUsage.tsx   # Endpoint usage statistics (NEW)
│   │   ├── ErrorTracking.tsx   # Error tracking and analysis (NEW)
│   │   ├── PerformanceTrends.tsx    # Performance trend charts (NEW)
│   │   └── Header.tsx          # App header with logout
│   ├── types.ts                # TypeScript interfaces
│   └── utils/
│       ├── auth.ts             # Auth helpers (login, logout, isAuthenticated)
│       └── api.ts              # API client for backend requests
├── package.json
├── vite.config.ts
├── tsconfig.json
└── tailwind.config.js
```

## Implementation Steps

### Phase 1: Backend Admin Endpoints (Day 1)

#### 1. Create Admin API Key

Add a special admin API key to the database with a `role` field:

```sql
-- Add role column to api_keys table (migration)
ALTER TABLE api_keys ADD COLUMN role VARCHAR(20) DEFAULT 'user';

-- Create admin API key
INSERT INTO api_keys (
  key_hash,
  user_email,
  tier,
  role,
  is_active,
  created_at,
  last_used_at
) VALUES (
  '...hash of admin key...',
  'admin@marsvista.com',
  'enterprise',
  'admin',
  true,
  NOW(),
  NOW()
);
```

**Admin API Key Format**: `mv_admin_{40-char-random}`

#### 2. Create Admin Controller

**File**: `src/MarsVista.Api/Controllers/AdminController.cs`

```csharp
[ApiController]
[Route("api/v1/admin")]
[AdminKeyAuthorization]  // Custom attribute to check for admin role
public class AdminController : ControllerBase
{
    private readonly MarsVistaDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(MarsVistaDbContext db, ILogger<AdminController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetSystemStats()
    {
        var totalUsers = await _db.ApiKeys
            .Select(k => k.UserEmail)
            .Distinct()
            .CountAsync();

        var activeApiKeys = await _db.ApiKeys
            .Where(k => k.IsActive)
            .CountAsync();

        // If usage tracking exists (Story 011 alternative)
        var totalApiCalls = await _db.UsageEvents.CountAsync();
        var totalPhotos = await _db.UsageEvents.SumAsync(e => e.PhotosReturned);

        var last24h = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        var last7d = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        var avgResponseTime = await _db.UsageEvents
            .Where(e => e.CreatedAt >= DateTime.UtcNow.AddHours(-1))
            .AverageAsync(e => (int?)e.ResponseTimeMs) ?? 0;

        return Ok(new
        {
            totalUsers,
            activeApiKeys,
            totalApiCalls,
            totalPhotosRetrieved = totalPhotos,
            apiCallsLast24h = last24h,
            apiCallsLast7d = last7d,
            averageResponseTime = avgResponseTime,
            systemUptime = GetSystemUptime()
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.ApiKeys
            .Where(k => k.IsActive)
            .GroupBy(k => k.UserEmail)
            .Select(g => new
            {
                email = g.Key,
                tier = g.First().Tier,
                apiKeyCreated = g.Min(k => k.CreatedAt),
                lastUsed = g.Max(k => k.LastUsedAt),
                isActive = true
            })
            .ToListAsync();

        // Add usage stats if available
        var usersWithStats = new List<object>();
        foreach (var user in users)
        {
            var today = DateTime.UtcNow.Date;
            var hourStart = DateTime.UtcNow.AddHours(-1);

            var totalRequests = await _db.UsageEvents
                .Where(e => e.UserEmail == user.email)
                .CountAsync();

            var requestsToday = await _db.UsageEvents
                .Where(e => e.UserEmail == user.email && e.CreatedAt >= today)
                .CountAsync();

            var requestsThisHour = await _db.UsageEvents
                .Where(e => e.UserEmail == user.email && e.CreatedAt >= hourStart)
                .CountAsync();

            usersWithStats.Add(new
            {
                user.email,
                user.tier,
                user.apiKeyCreated,
                user.lastUsed,
                totalRequests,
                requestsToday,
                requestsThisHour,
                user.isActive
            });
        }

        return Ok(new { users = usersWithStats });
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 50)
    {
        var events = await _db.UsageEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new
            {
                timestamp = e.CreatedAt,
                userEmail = e.UserEmail,
                endpoint = e.Endpoint,
                statusCode = e.StatusCode,
                responseTime = e.ResponseTimeMs,
                photosReturned = e.PhotosReturned
            })
            .ToListAsync();

        return Ok(new { events });
    }

    [HttpGet("rate-limit-violations")]
    public async Task<IActionResult> GetRateLimitViolations([FromQuery] int limit = 50)
    {
        // Query for 429 status codes
        var violations = await _db.UsageEvents
            .Where(e => e.StatusCode == 429)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .Select(e => new
            {
                timestamp = e.CreatedAt,
                userEmail = e.UserEmail,
                tier = e.Tier,
                violationType = "rate_limit",  // Could be enhanced to track type
                endpoint = e.Endpoint
            })
            .ToListAsync();

        return Ok(new { violations });
    }

    private string GetSystemUptime()
    {
        // Simple uptime calculation
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Days} days, {uptime.Hours} hours";
    }
}
```

#### 3. Create Admin Authorization Attribute

**File**: `src/MarsVista.Api/Attributes/AdminKeyAuthorizationAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminKeyAuthorizationAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var role = context.HttpContext.User.FindFirst("role")?.Value;

        if (role != "admin")
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
```

#### 4. Update API Key Authentication to Support Role

Modify `ApiKeyAuthenticationHandler` to include role claim:

```csharp
// In ApiKeyAuthenticationHandler.cs
var claims = new[]
{
    new Claim("email", apiKey.UserEmail),
    new Claim("tier", apiKey.Tier),
    new Claim("api_key_hash", hashedKey),
    new Claim("role", apiKey.Role ?? "user")  // Add role claim
};
```

### Phase 2: Frontend Setup (Day 1-2)

#### 5. Initialize Admin Site

```bash
cd web
npm create vite@latest admin-site -- --template react-ts
cd admin-site
npm install
npm install -D tailwindcss @tailwindcss/vite postcss autoprefixer
npx tailwindcss init
```

#### 6. Configure Vite and Tailwind

**File**: `web/admin-site/vite.config.ts`

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5175  // Different port from app (5173) and status-site (5174)
  }
})
```

**File**: `web/admin-site/tailwind.config.js`

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {},
  },
  plugins: [],
}
```

#### 7. Create Auth Utilities

**File**: `web/admin-site/src/utils/auth.ts`

```typescript
const ADMIN_EMAIL = "admin@marsvista.com"
const ADMIN_PASSWORD = "mars-admin-2025"  // TODO: Change in production
const ADMIN_API_KEY = "mv_admin_..."  // From database

export function login(email: string, password: string): boolean {
  if (email === ADMIN_EMAIL && password === ADMIN_PASSWORD) {
    localStorage.setItem('admin_token', 'authenticated')
    localStorage.setItem('admin_api_key', ADMIN_API_KEY)
    return true
  }
  return false
}

export function logout(): void {
  localStorage.removeItem('admin_token')
  localStorage.removeItem('admin_api_key')
}

export function isAuthenticated(): boolean {
  return localStorage.getItem('admin_token') === 'authenticated'
}

export function getAdminApiKey(): string | null {
  return localStorage.getItem('admin_api_key')
}
```

**File**: `web/admin-site/src/utils/api.ts`

```typescript
import { getAdminApiKey } from './auth'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5127'

export async function fetchStats() {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/stats`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch stats')
  return response.json()
}

export async function fetchUsers() {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/users`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch users')
  return response.json()
}

export async function fetchActivity(limit = 50) {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/activity?limit=${limit}`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch activity')
  return response.json()
}

export async function fetchViolations(limit = 50) {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/rate-limit-violations?limit=${limit}`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch violations')
  return response.json()
}

export async function fetchPerformanceMetrics() {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/metrics/performance`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch performance metrics')
  return response.json()
}

export async function fetchEndpointUsage() {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/metrics/endpoints`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch endpoint usage')
  return response.json()
}

export async function fetchErrors(limit = 50) {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/metrics/errors?limit=${limit}`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch errors')
  return response.json()
}

export async function fetchPerformanceTrends(period: '1h' | '24h' | '7d' | '30d' = '24h') {
  const response = await fetch(`${API_BASE_URL}/api/v1/admin/metrics/trends?period=${period}`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) throw new Error('Failed to fetch performance trends')
  return response.json()
}
```

### Phase 3: Frontend Components (Day 2-3)

#### 8. Create Login Component

**File**: `web/admin-site/src/components/Login.tsx`

```typescript
import { useState } from 'react'
import { login } from '../utils/auth'

interface LoginProps {
  onLogin: () => void
}

export default function Login({ onLogin }: LoginProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (login(email, password)) {
      onLogin()
    } else {
      setError('Invalid credentials')
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-800 to-slate-900 flex items-center justify-center">
      <div className="bg-white rounded-2xl shadow-2xl p-8 w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-slate-800">Mars Vista</h1>
          <p className="text-slate-600 mt-2">Admin Dashboard</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">
              Email
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-transparent"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">
              Password
            </label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full px-4 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-orange-500 focus:border-transparent"
              required
            />
          </div>

          {error && (
            <div className="bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm">
              {error}
            </div>
          )}

          <button
            type="submit"
            className="w-full bg-gradient-to-r from-orange-500 to-red-500 text-white py-3 rounded-lg font-semibold hover:from-orange-600 hover:to-red-600 transition-all"
          >
            Sign In
          </button>
        </form>
      </div>
    </div>
  )
}
```

#### 9. Create Stats Overview Component

**File**: `web/admin-site/src/components/StatsOverview.tsx`

```typescript
interface Stats {
  totalUsers: number
  activeApiKeys: number
  totalApiCalls: number
  totalPhotosRetrieved: number
  apiCallsLast24h: number
  apiCallsLast7d: number
  averageResponseTime: number
  systemUptime: string
}

interface StatsOverviewProps {
  stats: Stats
}

export default function StatsOverview({ stats }: StatsOverviewProps) {
  const statCards = [
    { label: 'Total Users', value: stats.totalUsers, icon: '👥' },
    { label: 'Active API Keys', value: stats.activeApiKeys, icon: '🔑' },
    { label: 'Total API Calls', value: stats.totalApiCalls.toLocaleString(), icon: '📊' },
    { label: 'Photos Retrieved', value: stats.totalPhotosRetrieved.toLocaleString(), icon: '🖼️' },
    { label: 'Calls (24h)', value: stats.apiCallsLast24h.toLocaleString(), icon: '📈' },
    { label: 'Calls (7d)', value: stats.apiCallsLast7d.toLocaleString(), icon: '📉' },
    { label: 'Avg Response', value: `${stats.averageResponseTime}ms`, icon: '⚡' },
    { label: 'System Uptime', value: stats.systemUptime, icon: '🚀' },
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {statCards.map((stat) => (
        <div
          key={stat.label}
          className="bg-white rounded-xl shadow-md p-6 border border-slate-200"
        >
          <div className="flex items-center justify-between mb-2">
            <span className="text-2xl">{stat.icon}</span>
          </div>
          <div className="text-3xl font-bold text-slate-800 mb-1">
            {stat.value}
          </div>
          <div className="text-sm text-slate-600">{stat.label}</div>
        </div>
      ))}
    </div>
  )
}
```

#### 10. Create User List Component

**File**: `web/admin-site/src/components/UserList.tsx`

```typescript
interface User {
  email: string
  tier: string
  apiKeyCreated: string
  lastUsed: string
  totalRequests: number
  requestsToday: number
  requestsThisHour: number
  isActive: boolean
}

interface UserListProps {
  users: User[]
}

export default function UserList({ users }: UserListProps) {
  const getTierBadge = (tier: string) => {
    const colors = {
      free: 'bg-slate-100 text-slate-700',
      pro: 'bg-blue-100 text-blue-700',
      enterprise: 'bg-purple-100 text-purple-700',
    }
    return colors[tier as keyof typeof colors] || colors.free
  }

  return (
    <div className="bg-white rounded-xl shadow-md border border-slate-200 overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-200">
        <h2 className="text-xl font-bold text-slate-800">Users</h2>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Email</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Tier</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Total Requests</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Today</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">This Hour</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Last Used</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {users.map((user) => (
              <tr key={user.email} className="hover:bg-slate-50">
                <td className="px-6 py-4 text-sm text-slate-800">{user.email}</td>
                <td className="px-6 py-4">
                  <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getTierBadge(user.tier)}`}>
                    {user.tier}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-slate-800">{user.totalRequests.toLocaleString()}</td>
                <td className="px-6 py-4 text-sm text-slate-800">{user.requestsToday}</td>
                <td className="px-6 py-4 text-sm text-slate-800">{user.requestsThisHour}</td>
                <td className="px-6 py-4 text-sm text-slate-600">
                  {user.lastUsed ? new Date(user.lastUsed).toLocaleString() : 'Never'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

#### 11. Create Activity Log Component

**File**: `web/admin-site/src/components/ActivityLog.tsx`

```typescript
interface Activity {
  timestamp: string
  userEmail: string
  endpoint: string
  statusCode: number
  responseTime: number
  photosReturned: number
}

interface ActivityLogProps {
  activities: Activity[]
}

export default function ActivityLog({ activities }: ActivityLogProps) {
  const getStatusColor = (statusCode: number) => {
    if (statusCode >= 200 && statusCode < 300) return 'text-green-600'
    if (statusCode >= 400 && statusCode < 500) return 'text-yellow-600'
    return 'text-red-600'
  }

  return (
    <div className="bg-white rounded-xl shadow-md border border-slate-200 overflow-hidden">
      <div className="px-6 py-4 border-b border-slate-200">
        <h2 className="text-xl font-bold text-slate-800">Recent Activity</h2>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-slate-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Time</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">User</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Endpoint</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Response Time</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Photos</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {activities.map((activity, index) => (
              <tr key={index} className="hover:bg-slate-50">
                <td className="px-6 py-4 text-sm text-slate-600">
                  {new Date(activity.timestamp).toLocaleTimeString()}
                </td>
                <td className="px-6 py-4 text-sm text-slate-800">{activity.userEmail}</td>
                <td className="px-6 py-4 text-xs font-mono text-slate-600">{activity.endpoint}</td>
                <td className={`px-6 py-4 text-sm font-semibold ${getStatusColor(activity.statusCode)}`}>
                  {activity.statusCode}
                </td>
                <td className="px-6 py-4 text-sm text-slate-800">{activity.responseTime}ms</td>
                <td className="px-6 py-4 text-sm text-slate-800">{activity.photosReturned}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

#### 12. Create Main Dashboard Component

**File**: `web/admin-site/src/components/Dashboard.tsx`

```typescript
import { useEffect, useState } from 'react'
import { fetchStats, fetchUsers, fetchActivity, fetchViolations } from '../utils/api'
import { logout } from '../utils/auth'
import StatsOverview from './StatsOverview'
import UserList from './UserList'
import ActivityLog from './ActivityLog'
import RateLimitViolations from './RateLimitViolations'

export default function Dashboard() {
  const [stats, setStats] = useState<any>(null)
  const [users, setUsers] = useState<any[]>([])
  const [activities, setActivities] = useState<any[]>([])
  const [violations, setViolations] = useState<any[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 30000) // Refresh every 30 seconds
    return () => clearInterval(interval)
  }, [])

  async function loadData() {
    try {
      const [statsData, usersData, activityData, violationsData] = await Promise.all([
        fetchStats(),
        fetchUsers(),
        fetchActivity(50),
        fetchViolations(50),
      ])

      setStats(statsData)
      setUsers(usersData.users)
      setActivities(activityData.events)
      setViolations(violationsData.violations)
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    logout()
    window.location.reload()
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Mars Vista Admin</h1>
            <p className="text-sm text-slate-600">System Monitoring Dashboard</p>
          </div>
          <button
            onClick={handleLogout}
            className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors"
          >
            Logout
          </button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8 space-y-8">
        {/* Stats Overview */}
        {stats && <StatsOverview stats={stats} />}

        {/* User List */}
        {users.length > 0 && <UserList users={users} />}

        {/* Activity Log */}
        {activities.length > 0 && <ActivityLog activities={activities} />}

        {/* Rate Limit Violations */}
        {violations.length > 0 && <RateLimitViolations violations={violations} />}
      </main>
    </div>
  )
}
```

#### 13. Create Performance Metrics Component

**File**: `web/admin-site/src/components/PerformanceMetrics.tsx`

```typescript
interface PerformanceMetricsProps {
  metrics: {
    currentMetrics: {
      averageResponseTime: number
      p50ResponseTime: number
      p95ResponseTime: number
      p99ResponseTime: number
      requestsPerMinute: number
      errorRate: number
      successRate: number
    }
    slowQueries: Array<{
      endpoint: string
      avgResponseTime: number
      maxResponseTime: number
      count: number
      lastOccurrence: string
    }>
  }
}

export default function PerformanceMetrics({ metrics }: PerformanceMetricsProps) {
  return (
    <div className="space-y-6">
      {/* Current Performance Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <MetricCard
          label="Avg Response Time"
          value={`${metrics.currentMetrics.averageResponseTime}ms`}
          icon="⚡"
          color="blue"
        />
        <MetricCard
          label="P95 Response Time"
          value={`${metrics.currentMetrics.p95ResponseTime}ms`}
          icon="📊"
          color="indigo"
        />
        <MetricCard
          label="Requests/Min"
          value={metrics.currentMetrics.requestsPerMinute.toFixed(1)}
          icon="🚀"
          color="green"
        />
        <MetricCard
          label="Success Rate"
          value={`${metrics.currentMetrics.successRate.toFixed(1)}%`}
          icon="✅"
          color={metrics.currentMetrics.successRate > 95 ? 'green' : 'yellow'}
        />
      </div>

      {/* Percentile Breakdown */}
      <div className="bg-white rounded-xl shadow-md border border-slate-200 p-6">
        <h3 className="text-lg font-bold text-slate-800 mb-4">Response Time Percentiles</h3>
        <div className="grid grid-cols-3 gap-4">
          <div>
            <div className="text-sm text-slate-600">P50 (Median)</div>
            <div className="text-2xl font-bold text-slate-800">
              {metrics.currentMetrics.p50ResponseTime}ms
            </div>
          </div>
          <div>
            <div className="text-sm text-slate-600">P95</div>
            <div className="text-2xl font-bold text-slate-800">
              {metrics.currentMetrics.p95ResponseTime}ms
            </div>
          </div>
          <div>
            <div className="text-sm text-slate-600">P99</div>
            <div className="text-2xl font-bold text-slate-800">
              {metrics.currentMetrics.p99ResponseTime}ms
            </div>
          </div>
        </div>
      </div>

      {/* Slow Queries Table */}
      {metrics.slowQueries.length > 0 && (
        <div className="bg-white rounded-xl shadow-md border border-slate-200 overflow-hidden">
          <div className="px-6 py-4 border-b border-slate-200 bg-yellow-50">
            <h3 className="text-lg font-bold text-slate-800">⚠️ Slow Queries (>1s)</h3>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Endpoint</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Avg Time</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Max Time</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Count</th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Last Seen</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {metrics.slowQueries.map((query, index) => (
                  <tr key={index} className="hover:bg-slate-50">
                    <td className="px-6 py-4 text-sm font-mono text-slate-800">{query.endpoint}</td>
                    <td className="px-6 py-4 text-sm text-orange-600 font-semibold">
                      {query.avgResponseTime}ms
                    </td>
                    <td className="px-6 py-4 text-sm text-red-600 font-semibold">
                      {query.maxResponseTime}ms
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-800">{query.count}</td>
                    <td className="px-6 py-4 text-sm text-slate-600">
                      {new Date(query.lastOccurrence).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  )
}

function MetricCard({ label, value, icon, color }: any) {
  const colorClasses = {
    blue: 'bg-blue-50 border-blue-200',
    indigo: 'bg-indigo-50 border-indigo-200',
    green: 'bg-green-50 border-green-200',
    yellow: 'bg-yellow-50 border-yellow-200',
  }

  return (
    <div className={`rounded-xl shadow-md border p-6 ${colorClasses[color]}`}>
      <div className="text-2xl mb-2">{icon}</div>
      <div className="text-2xl font-bold text-slate-800">{value}</div>
      <div className="text-sm text-slate-600">{label}</div>
    </div>
  )
}
```

#### 14. Create Endpoint Usage Component

**File**: `web/admin-site/src/components/EndpointUsage.tsx`

```typescript
interface EndpointUsageProps {
  data: {
    topEndpoints: Array<{
      endpoint: string
      calls: number
      avgResponseTime: number
      errorRate: number
      successRate: number
      last24h: number
    }>
    roverUsage: Record<string, number>
    cameraUsage: Record<string, number>
  }
}

export default function EndpointUsage({ data }: EndpointUsageProps) {
  return (
    <div className="space-y-6">
      {/* Top Endpoints Table */}
      <div className="bg-white rounded-xl shadow-md border border-slate-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-200">
          <h3 className="text-lg font-bold text-slate-800">Top Endpoints</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Endpoint</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Total Calls</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Last 24h</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Avg Time</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Success Rate</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {data.topEndpoints.map((endpoint, index) => (
                <tr key={index} className="hover:bg-slate-50">
                  <td className="px-6 py-4 text-sm font-mono text-slate-800">{endpoint.endpoint}</td>
                  <td className="px-6 py-4 text-sm text-slate-800">{endpoint.calls.toLocaleString()}</td>
                  <td className="px-6 py-4 text-sm text-slate-800">{endpoint.last24h.toLocaleString()}</td>
                  <td className="px-6 py-4 text-sm text-slate-800">{endpoint.avgResponseTime}ms</td>
                  <td className="px-6 py-4">
                    <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                      endpoint.successRate > 99 ? 'bg-green-100 text-green-700' :
                      endpoint.successRate > 95 ? 'bg-yellow-100 text-yellow-700' :
                      'bg-red-100 text-red-700'
                    }`}>
                      {endpoint.successRate.toFixed(1)}%
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Rover and Camera Usage */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Rover Usage */}
        <div className="bg-white rounded-xl shadow-md border border-slate-200 p-6">
          <h3 className="text-lg font-bold text-slate-800 mb-4">Rover Usage</h3>
          <div className="space-y-3">
            {Object.entries(data.roverUsage)
              .sort(([, a], [, b]) => b - a)
              .map(([rover, count]) => (
                <div key={rover} className="flex items-center justify-between">
                  <span className="text-sm font-medium text-slate-700 capitalize">{rover}</span>
                  <span className="text-sm font-bold text-slate-800">{count.toLocaleString()}</span>
                </div>
              ))}
          </div>
        </div>

        {/* Camera Usage */}
        <div className="bg-white rounded-xl shadow-md border border-slate-200 p-6">
          <h3 className="text-lg font-bold text-slate-800 mb-4">Top Cameras</h3>
          <div className="space-y-3">
            {Object.entries(data.cameraUsage)
              .sort(([, a], [, b]) => b - a)
              .slice(0, 10)
              .map(([camera, count]) => (
                <div key={camera} className="flex items-center justify-between">
                  <span className="text-sm font-medium text-slate-700">{camera}</span>
                  <span className="text-sm font-bold text-slate-800">{count.toLocaleString()}</span>
                </div>
              ))}
          </div>
        </div>
      </div>
    </div>
  )
}
```

#### 15. Create Error Tracking Component

**File**: `web/admin-site/src/components/ErrorTracking.tsx`

```typescript
interface ErrorTrackingProps {
  data: {
    errorSummary: {
      total: number
      last24h: number
      byStatusCode: Record<string, number>
    }
    recentErrors: Array<{
      timestamp: string
      userEmail: string
      endpoint: string
      statusCode: number
      errorMessage: string
      responseTime: number
    }>
    errorsByEndpoint: Array<{
      endpoint: string
      errorCount: number
      errorRate: number
      mostCommonError: string
    }>
  }
}

export default function ErrorTracking({ data }: ErrorTrackingProps) {
  return (
    <div className="space-y-6">
      {/* Error Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl shadow-md border border-slate-200 p-6">
          <h3 className="text-lg font-bold text-slate-800 mb-4">Error Summary</h3>
          <div className="space-y-3">
            <div className="flex justify-between items-center">
              <span className="text-sm text-slate-600">Total Errors</span>
              <span className="text-2xl font-bold text-red-600">{data.errorSummary.total}</span>
            </div>
            <div className="flex justify-between items-center">
              <span className="text-sm text-slate-600">Last 24 Hours</span>
              <span className="text-2xl font-bold text-orange-600">{data.errorSummary.last24h}</span>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-xl shadow-md border border-slate-200 p-6">
          <h3 className="text-lg font-bold text-slate-800 mb-4">Errors by Status Code</h3>
          <div className="space-y-2">
            {Object.entries(data.errorSummary.byStatusCode)
              .sort(([, a], [, b]) => b - a)
              .map(([code, count]) => (
                <div key={code} className="flex items-center justify-between">
                  <span className={`px-2 py-1 text-xs font-semibold rounded ${
                    code === '404' ? 'bg-yellow-100 text-yellow-700' :
                    code === '429' ? 'bg-orange-100 text-orange-700' :
                    code === '500' ? 'bg-red-100 text-red-700' :
                    'bg-slate-100 text-slate-700'
                  }`}>
                    {code}
                  </span>
                  <span className="text-sm font-bold text-slate-800">{count}</span>
                </div>
              ))}
          </div>
        </div>
      </div>

      {/* Recent Errors Table */}
      <div className="bg-white rounded-xl shadow-md border border-slate-200 overflow-hidden">
        <div className="px-6 py-4 border-b border-slate-200">
          <h3 className="text-lg font-bold text-slate-800">Recent Errors</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-slate-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Time</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">User</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Endpoint</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Status</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-slate-600 uppercase">Error</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {data.recentErrors.map((error, index) => (
                <tr key={index} className="hover:bg-slate-50">
                  <td className="px-6 py-4 text-sm text-slate-600">
                    {new Date(error.timestamp).toLocaleTimeString()}
                  </td>
                  <td className="px-6 py-4 text-sm text-slate-800">{error.userEmail}</td>
                  <td className="px-6 py-4 text-xs font-mono text-slate-600">{error.endpoint}</td>
                  <td className="px-6 py-4">
                    <span className={`px-2 py-1 text-xs font-semibold rounded ${
                      error.statusCode === 404 ? 'bg-yellow-100 text-yellow-700' :
                      error.statusCode === 429 ? 'bg-orange-100 text-orange-700' :
                      'bg-red-100 text-red-700'
                    }`}>
                      {error.statusCode}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm text-slate-600">{error.errorMessage}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
```

#### 16. Update Dashboard Component with Tabs

Update the Dashboard component to include tabs for different views:

**File**: `web/admin-site/src/components/Dashboard.tsx` (Updated)

```typescript
import { useEffect, useState } from 'react'
import {
  fetchStats,
  fetchUsers,
  fetchActivity,
  fetchViolations,
  fetchPerformanceMetrics,
  fetchEndpointUsage,
  fetchErrors,
} from '../utils/api'
import { logout } from '../utils/auth'
import StatsOverview from './StatsOverview'
import UserList from './UserList'
import ActivityLog from './ActivityLog'
import RateLimitViolations from './RateLimitViolations'
import PerformanceMetrics from './PerformanceMetrics'
import EndpointUsage from './EndpointUsage'
import ErrorTracking from './ErrorTracking'

type TabName = 'overview' | 'performance' | 'endpoints' | 'errors' | 'users' | 'activity'

export default function Dashboard() {
  const [activeTab, setActiveTab] = useState<TabName>('overview')
  const [stats, setStats] = useState<any>(null)
  const [users, setUsers] = useState<any[]>([])
  const [activities, setActivities] = useState<any[]>([])
  const [violations, setViolations] = useState<any[]>([])
  const [performanceMetrics, setPerformanceMetrics] = useState<any>(null)
  const [endpointUsage, setEndpointUsage] = useState<any>(null)
  const [errorData, setErrorData] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 30000) // Refresh every 30 seconds
    return () => clearInterval(interval)
  }, [])

  async function loadData() {
    try {
      const [
        statsData,
        usersData,
        activityData,
        violationsData,
        perfMetrics,
        endpointData,
        errorsData,
      ] = await Promise.all([
        fetchStats(),
        fetchUsers(),
        fetchActivity(50),
        fetchViolations(50),
        fetchPerformanceMetrics(),
        fetchEndpointUsage(),
        fetchErrors(50),
      ])

      setStats(statsData)
      setUsers(usersData.users)
      setActivities(activityData.events)
      setViolations(violationsData.violations)
      setPerformanceMetrics(perfMetrics)
      setEndpointUsage(endpointData)
      setErrorData(errorsData)
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    logout()
    window.location.reload()
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
      </div>
    )
  }

  const tabs = [
    { id: 'overview', label: 'Overview', icon: '📊' },
    { id: 'performance', label: 'Performance', icon: '⚡' },
    { id: 'endpoints', label: 'Endpoints', icon: '🎯' },
    { id: 'errors', label: 'Errors', icon: '🚨' },
    { id: 'users', label: 'Users', icon: '👥' },
    { id: 'activity', label: 'Activity', icon: '📝' },
  ]

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Mars Vista Admin</h1>
            <p className="text-sm text-slate-600">System Monitoring Dashboard</p>
          </div>
          <button
            onClick={handleLogout}
            className="px-4 py-2 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 transition-colors"
          >
            Logout
          </button>
        </div>

        {/* Tabs */}
        <div className="max-w-7xl mx-auto px-6">
          <div className="flex space-x-1 border-b border-slate-200">
            {tabs.map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as TabName)}
                className={`px-4 py-3 text-sm font-medium transition-colors ${
                  activeTab === tab.id
                    ? 'text-orange-600 border-b-2 border-orange-600'
                    : 'text-slate-600 hover:text-slate-800'
                }`}
              >
                <span className="mr-2">{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        {activeTab === 'overview' && (
          <div className="space-y-8">
            {stats && <StatsOverview stats={stats} />}
            {violations.length > 0 && <RateLimitViolations violations={violations} />}
          </div>
        )}

        {activeTab === 'performance' && performanceMetrics && (
          <PerformanceMetrics metrics={performanceMetrics} />
        )}

        {activeTab === 'endpoints' && endpointUsage && (
          <EndpointUsage data={endpointUsage} />
        )}

        {activeTab === 'errors' && errorData && (
          <ErrorTracking data={errorData} />
        )}

        {activeTab === 'users' && users.length > 0 && (
          <UserList users={users} />
        )}

        {activeTab === 'activity' && activities.length > 0 && (
          <ActivityLog activities={activities} />
        )}
      </main>
    </div>
  )
}
```

#### 17. Create Main App Component

**File**: `web/admin-site/src/App.tsx`

```typescript
import { useState, useEffect } from 'react'
import { isAuthenticated } from './utils/auth'
import Login from './components/Login'
import Dashboard from './components/Dashboard'

function App() {
  const [authenticated, setAuthenticated] = useState(false)

  useEffect(() => {
    setAuthenticated(isAuthenticated())
  }, [])

  if (!authenticated) {
    return <Login onLogin={() => setAuthenticated(true)} />
  }

  return <Dashboard />
}

export default App
```

### Phase 4: Testing and Deployment (Day 3-4)

#### 14. Local Testing

```bash
# Terminal 1: Start C# API
cd src/MarsVista.Api
dotnet run

# Terminal 2: Start Admin Dashboard
cd web/admin-site
npm run dev
```

Visit `http://localhost:5175` and test:
- [ ] Login with hardcoded credentials
- [ ] **Overview tab**: View system stats and rate limit violations
- [ ] **Performance tab**: Check response times, P95/P99 metrics, slow queries
- [ ] **Endpoints tab**: View top endpoints, rover/camera usage statistics
- [ ] **Errors tab**: Review error summary, recent errors by status code
- [ ] **Users tab**: Browse user list with usage stats
- [ ] **Activity tab**: Check recent API activity log
- [ ] Verify auto-refresh (30 seconds) on all tabs
- [ ] Test tab switching and data persistence
- [ ] Test logout functionality

#### 15. Deploy Admin Dashboard

**Option 1: Deploy to Vercel (separate from main app)**

```bash
cd web/admin-site
vercel --prod
```

**Option 2: Host alongside status-site on Railway**

Add admin-site to Railway deployment config.

**Option 3: Static hosting (Netlify, Cloudflare Pages)**

```bash
cd web/admin-site
npm run build
# Deploy dist/ folder
```

## Success Criteria

### Backend
- ✅ Admin API key created with `admin` role
- ✅ Admin endpoints return correct data
- ✅ Authorization prevents non-admin access
- ✅ All statistics calculated accurately
- ✅ **Performance metrics** compute P50/P95/P99 correctly
- ✅ **Slow query detection** identifies requests >1s
- ✅ **Endpoint usage** aggregates by rover and camera
- ✅ **Error tracking** groups by status code and endpoint
- ✅ **Trends API** supports multiple time periods (1h/24h/7d/30d)

### Frontend
- ✅ Hardcoded login works correctly
- ✅ Dashboard shows all system statistics
- ✅ **Performance metrics** display response times and percentiles
- ✅ **Slow queries** highlighted with >1s threshold
- ✅ **Endpoint usage** shows top endpoints with success rates
- ✅ **Rover/camera statistics** display popular resources
- ✅ **Error tracking** shows errors by status code and endpoint
- ✅ **Tab navigation** works smoothly between views
- ✅ User list displays with usage stats
- ✅ Activity log shows recent API calls
- ✅ Rate limit violations tracked
- ✅ Auto-refresh every 30 seconds
- ✅ Responsive design (mobile/desktop)
- ✅ Logout clears authentication

### Security
- ✅ Admin API key never exposed in frontend code
- ✅ Login credentials stored securely (change defaults!)
- ✅ Backend validates admin role on all endpoints
- ✅ No user data exposed without authentication

## Dependencies

**Required:**
- Story 010 (API key authentication) must be deployed

**Optional but Recommended:**
- Usage tracking tables (if implementing activity log)
- If no usage tracking: simplify to only show user list and basic stats

## Future Enhancements (Not in This Story)

**Dashboard Features:**
- [ ] Filter/search users by email or tier
- [ ] Export data to CSV/JSON
- [ ] Ban/suspend user accounts
- [ ] Modify user tiers from dashboard
- [ ] Real-time WebSocket updates for live metrics
- [ ] Scraper status and control panel
- [ ] Database size and health metrics
- [ ] Log retention configuration

**Performance Monitoring:**
- [ ] Add charts for performance trends (Chart.js, Recharts, or Apache ECharts)
- [ ] Historical performance comparison (week-over-week, month-over-month)
- [ ] Alerting when performance degrades (email/Slack notifications)
- [ ] Database query performance metrics
- [ ] Cache hit/miss rates
- [ ] Request/response size tracking
- [ ] Geographic distribution of requests (if tracking IP data)

**Advanced Analytics:**
- [ ] Anomaly detection for unusual traffic patterns
- [ ] Predictive analytics for capacity planning
- [ ] Cost analysis per user/tier
- [ ] A/B testing results tracking
- [ ] Custom dashboard widgets
- [ ] Saved filters and views
- [ ] Scheduled reports via email

## Notes

**Usage Tracking Dependency:**
- This story assumes basic usage tracking exists (for activity log and violations)
- If not implemented, remove those components and focus on:
  - User list with API key info
  - Basic system stats (user count, active keys)
  - Simple health checks

**Security Warning:**
- Change hardcoded credentials before production!
- Consider environment variables for credentials
- Implement proper session management if needed
- Use HTTPS in production

**Alternative Approach (if no usage tracking):**
- Focus on user management only
- Show API key creation dates and last used times
- Basic system info (uptime, active keys)
- Simplified single-page dashboard
