import type {
  Stats,
  User,
  Activity,
  Violation,
  PerformanceMetrics,
  EndpointUsageData,
  ErrorData,
  ScraperStatus,
  ScraperMetrics,
  ScraperJob,
} from './types'

// All API calls go through our Next.js proxy which adds the API key server-side
async function fetchFromProxy<T>(endpoint: string): Promise<T> {
  const response = await fetch(`/api/admin/${endpoint}`, {
    credentials: 'include', // Include cookies for auth
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication failed')
    }
    throw new Error(`API request failed: ${response.statusText}`)
  }

  return response.json()
}

// Auth functions
export async function login(email: string, password: string): Promise<boolean> {
  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
    credentials: 'include',
  })

  const data = await response.json()
  return data.success === true
}

export async function logout(): Promise<void> {
  await fetch('/api/auth/logout', {
    method: 'POST',
    credentials: 'include',
  })
}

export async function checkAuth(): Promise<boolean> {
  try {
    const response = await fetch('/api/auth/check', {
      credentials: 'include',
    })
    const data = await response.json()
    return data.authenticated === true
  } catch {
    return false
  }
}

// Stats endpoints
export async function fetchStats(): Promise<Stats> {
  return fetchFromProxy<Stats>('stats')
}

export async function fetchUsers(): Promise<{ users: User[] }> {
  return fetchFromProxy<{ users: User[] }>('users')
}

export async function fetchActivity(limit = 50): Promise<{ events: Activity[] }> {
  return fetchFromProxy<{ events: Activity[] }>(`activity?limit=${limit}`)
}

export async function fetchViolations(limit = 50): Promise<{ violations: Violation[] }> {
  return fetchFromProxy<{ violations: Violation[] }>(`rate-limit-violations?limit=${limit}`)
}

export async function fetchPerformanceMetrics(): Promise<PerformanceMetrics> {
  return fetchFromProxy<PerformanceMetrics>('metrics/performance')
}

export async function fetchEndpointUsage(): Promise<EndpointUsageData> {
  return fetchFromProxy<EndpointUsageData>('metrics/endpoints')
}

export async function fetchErrors(limit = 50): Promise<ErrorData> {
  return fetchFromProxy<ErrorData>(`metrics/errors?limit=${limit}`)
}

// Scraper endpoints
export async function fetchScraperStatus(): Promise<ScraperStatus> {
  return fetchFromProxy<ScraperStatus>('scraper/status')
}

export async function fetchScraperHistory(params: {
  limit?: number
  offset?: number
  rover?: string
  status?: string
  startDate?: string
  endDate?: string
} = {}): Promise<{ jobs: ScraperJob[] }> {
  const queryParams = new URLSearchParams()
  if (params.limit) queryParams.set('limit', params.limit.toString())
  if (params.offset) queryParams.set('offset', params.offset.toString())
  if (params.rover) queryParams.set('rover', params.rover)
  if (params.status) queryParams.set('status', params.status)
  if (params.startDate) queryParams.set('startDate', params.startDate)
  if (params.endDate) queryParams.set('endDate', params.endDate)

  const query = queryParams.toString()
  return fetchFromProxy<{ jobs: ScraperJob[] }>(`scraper/history${query ? `?${query}` : ''}`)
}

export async function fetchScraperMetrics(period: '24h' | '7d' | '30d' = '7d'): Promise<ScraperMetrics> {
  return fetchFromProxy<ScraperMetrics>(`scraper/metrics?period=${period}`)
}
