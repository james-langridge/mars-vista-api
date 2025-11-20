import { getAdminApiKey } from './auth'

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5127'

async function fetchWithAuth(endpoint: string) {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    headers: {
      'X-API-Key': getAdminApiKey() || '',
    },
  })
  if (!response.ok) {
    throw new Error(`API request failed: ${response.statusText}`)
  }
  return response.json()
}

export async function fetchStats() {
  return fetchWithAuth('/api/v1/admin/stats')
}

export async function fetchUsers() {
  return fetchWithAuth('/api/v1/admin/users')
}

export async function fetchActivity(limit = 50) {
  return fetchWithAuth(`/api/v1/admin/activity?limit=${limit}`)
}

export async function fetchViolations(limit = 50) {
  return fetchWithAuth(`/api/v1/admin/rate-limit-violations?limit=${limit}`)
}

export async function fetchPerformanceMetrics() {
  return fetchWithAuth('/api/v1/admin/metrics/performance')
}

export async function fetchEndpointUsage() {
  return fetchWithAuth('/api/v1/admin/metrics/endpoints')
}

export async function fetchErrors(limit = 50) {
  return fetchWithAuth(`/api/v1/admin/metrics/errors?limit=${limit}`)
}

export async function fetchPerformanceTrends(period: '1h' | '24h' | '7d' | '30d' = '24h') {
  return fetchWithAuth(`/api/v1/admin/metrics/trends?period=${period}`)
}

// Scraper monitoring endpoints
export async function fetchScraperStatus() {
  return fetchWithAuth('/api/v1/admin/scraper/status')
}

export async function fetchScraperHistory(params: {
  limit?: number
  offset?: number
  rover?: string
  status?: string
  startDate?: string
  endDate?: string
} = {}) {
  const queryParams = new URLSearchParams()
  if (params.limit) queryParams.set('limit', params.limit.toString())
  if (params.offset) queryParams.set('offset', params.offset.toString())
  if (params.rover) queryParams.set('rover', params.rover)
  if (params.status) queryParams.set('status', params.status)
  if (params.startDate) queryParams.set('startDate', params.startDate)
  if (params.endDate) queryParams.set('endDate', params.endDate)

  const query = queryParams.toString()
  return fetchWithAuth(`/api/v1/admin/scraper/history${query ? `?${query}` : ''}`)
}

export async function fetchScraperMetrics(period: '24h' | '7d' | '30d' = '7d') {
  return fetchWithAuth(`/api/v1/admin/scraper/metrics?period=${period}`)
}
