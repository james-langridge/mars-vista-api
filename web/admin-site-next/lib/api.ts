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
  PhotoSearchParams,
  PhotoSearchResponse,
  NasaComparisonResult,
  PhotoComparisonResult,
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

// Photo Search (v2 API)
export async function searchPhotos(params: PhotoSearchParams): Promise<PhotoSearchResponse> {
  const queryParams = new URLSearchParams()

  // Basic filters
  if (params.nasa_id) queryParams.set('nasa_id', params.nasa_id)
  if (params.rovers) queryParams.set('rovers', params.rovers)
  if (params.cameras) queryParams.set('cameras', params.cameras)

  // Sol range
  if (params.sol !== undefined) queryParams.set('sol', params.sol.toString())
  if (params.sol_min !== undefined) queryParams.set('sol_min', params.sol_min.toString())
  if (params.sol_max !== undefined) queryParams.set('sol_max', params.sol_max.toString())

  // Date range
  if (params.date_min) queryParams.set('date_min', params.date_min)
  if (params.date_max) queryParams.set('date_max', params.date_max)

  // Location filters
  if (params.site !== undefined) queryParams.set('site', params.site.toString())
  if (params.drive !== undefined) queryParams.set('drive', params.drive.toString())
  if (params.site_min !== undefined) queryParams.set('site_min', params.site_min.toString())
  if (params.site_max !== undefined) queryParams.set('site_max', params.site_max.toString())
  if (params.location_radius !== undefined) queryParams.set('location_radius', params.location_radius.toString())

  // Image quality filters
  if (params.min_width !== undefined) queryParams.set('min_width', params.min_width.toString())
  if (params.min_height !== undefined) queryParams.set('min_height', params.min_height.toString())
  if (params.sample_type) queryParams.set('sample_type', params.sample_type)

  // Mars time filters
  if (params.mars_time_min) queryParams.set('mars_time_min', params.mars_time_min)
  if (params.mars_time_max) queryParams.set('mars_time_max', params.mars_time_max)
  if (params.mars_time_golden_hour !== undefined) queryParams.set('mars_time_golden_hour', params.mars_time_golden_hour.toString())

  // Camera angle filters
  if (params.mast_elevation_min !== undefined) queryParams.set('mast_elevation_min', params.mast_elevation_min.toString())
  if (params.mast_elevation_max !== undefined) queryParams.set('mast_elevation_max', params.mast_elevation_max.toString())
  if (params.mast_azimuth_min !== undefined) queryParams.set('mast_azimuth_min', params.mast_azimuth_min.toString())
  if (params.mast_azimuth_max !== undefined) queryParams.set('mast_azimuth_max', params.mast_azimuth_max.toString())

  // Response control
  if (params.field_set) queryParams.set('field_set', params.field_set)
  if (params.fields) queryParams.set('fields', params.fields)
  if (params.include) queryParams.set('include', params.include)
  if (params.image_sizes) queryParams.set('image_sizes', params.image_sizes)
  if (params.sort) queryParams.set('sort', params.sort)

  // Pagination
  if (params.page !== undefined) queryParams.set('page', params.page.toString())
  if (params.per_page !== undefined) queryParams.set('per_page', params.per_page.toString())

  const query = queryParams.toString()
  const response = await fetch(`/api/photos${query ? `?${query}` : ''}`, {
    credentials: 'include',
  })

  if (!response.ok) {
    if (response.status === 401) {
      throw new Error('Authentication failed')
    }
    const errorData = await response.json().catch(() => ({}))
    throw new Error(errorData.error || `API request failed: ${response.statusText}`)
  }

  return response.json()
}

// NASA Compare endpoints
export async function compareNasaSol(rover: string, sol: number): Promise<NasaComparisonResult> {
  return fetchFromProxy<NasaComparisonResult>(`nasa/sol/${rover}/${sol}`)
}

export async function compareNasaPhoto(nasaId: string): Promise<PhotoComparisonResult> {
  return fetchFromProxy<PhotoComparisonResult>(`nasa/photo/${nasaId}`)
}

export async function compareNasaRange(
  rover: string,
  startSol: number,
  endSol: number
): Promise<{
  rover: string
  startSol: number
  endSol: number
  solsCompared: number
  summary: {
    totalNasaPhotos: number
    totalOurPhotos: number
    totalMissing: number
    totalExtra: number
    matchPercent: number
  }
  sols: Array<{
    sol: number
    nasaCount: number
    ourCount: number
    missing: number
    extra: number
    status: string
  }>
}> {
  return fetchFromProxy(`nasa/range/${rover}?startSol=${startSol}&endSol=${endSol}`)
}
