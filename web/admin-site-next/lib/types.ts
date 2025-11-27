// API Response Types

export interface Stats {
  totalUsers: number
  activeApiKeys: number
  totalApiCalls: number
  totalPhotosRetrieved: number
  apiCallsLast24h: number
  apiCallsLast7d: number
  averageResponseTime: number
  systemUptime: string
}

export interface User {
  email: string
  tier: string
  apiKeyCreated: string
  lastUsed: string
  totalRequests: number
  requestsToday: number
  requestsThisHour: number
  isActive: boolean
}

export interface Activity {
  timestamp: string
  userEmail: string
  endpoint: string
  statusCode: number
  responseTime: number
  photosReturned: number
}

export interface Violation {
  timestamp: string
  userEmail: string
  tier: string
  violationType: string
  requestCount?: number
  limit?: number
}

export interface PerformanceMetrics {
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

export interface EndpointUsageData {
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

export interface ErrorData {
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

// Scraper Types
export interface ScraperState {
  roverName: string
  lastScrapedSol: number
  currentMissionSol: number
  lastRunTimestamp: string
  lastRunStatus: string
  photosAddedLastRun: number
  errorMessage: string | null
  healthStatus: 'healthy' | 'warning' | 'error'
  totalPhotos: number
}

export interface ScraperStatus {
  scrapers: ScraperState[]
  nextScheduledRun: string | null
}

export interface ScraperMetrics {
  period: string
  metrics: {
    totalJobs: number
    successfulJobs: number
    failedJobs: number
    partialJobs: number
    successRate: number
    totalPhotosAdded: number
    averageDurationSeconds: number
    roverBreakdown: Array<{
      roverName: string
      totalPhotos: number
      photosAddedPeriod: number
      successfulRuns: number
      failedRuns: number
      averageDurationSeconds: number
    }>
  }
}

export interface PhotoDetail {
  sol: number
  nasaId: string
}

export interface RoverDetail {
  roverName: string
  startSol: number
  endSol: number
  solsAttempted: number
  solsSucceeded: number
  solsFailed: number
  photosAdded: number
  durationSeconds: number
  status: string
  errorMessage: string | null
  failedSols: number[]
  photosAddedDetails: PhotoDetail[]
}

export interface ScraperJob {
  id: number
  startedAt: string
  completedAt: string | null
  durationSeconds: number | null
  totalRoversAttempted: number
  totalRoversSucceeded: number
  totalPhotosAdded: number
  status: string
  errorSummary: string | null
  roverDetails: RoverDetail[]
}

// Photo Search Types (v2 API)
export interface PhotoSearchParams {
  nasa_id?: string
  rovers?: string
  cameras?: string
  sol?: number
  sol_min?: number
  sol_max?: number
  date_min?: string
  date_max?: string
  sample_type?: string
  min_width?: number
  min_height?: number
  site?: number
  drive?: number
  field_set?: string
  page?: number
  per_page?: number
  sort?: string
}

export interface PhotoResource {
  id: string
  type: string
  attributes: {
    nasa_id: string
    sol: number
    earth_date: string
    camera: {
      name: string
      full_name: string
    }
    rover: {
      name: string
      status: string
    }
    images?: {
      small?: string
      medium?: string
      large?: string
      full?: string
    }
    dimensions?: {
      width: number | null
      height: number | null
      aspect_ratio: number | null
    }
    location?: {
      site: number | null
      drive: number | null
    }
    mars_time?: {
      date_taken_mars: string | null
      local_time: string | null
      is_golden_hour: boolean
    }
    telemetry?: {
      mast_azimuth: number | null
      mast_elevation: number | null
    }
    meta?: {
      sample_type: string | null
      credit: string | null
      caption: string | null
    }
    raw_data?: Record<string, unknown>
  }
  links?: {
    self: string
    nasa: string
  }
}

export interface PhotoSearchResponse {
  data: PhotoResource[]
  meta: {
    total_count: number
    returned_count: number
    query?: Record<string, unknown>
    timestamp: string
  }
  pagination: {
    page: number
    per_page: number
    total_pages: number
  }
  links?: {
    self: string
    first?: string
    previous?: string | null
    next?: string | null
    last?: string
  }
}

// NASA Compare Types
export interface NasaComparisonResult {
  rover: string
  sol: number
  comparison: {
    nasaPhotoCount: number
    ourPhotoCount: number
    matchCount: number
    matchPercent: number
    missingFromOurs: number
    extraInOurs: number
    status: 'match' | 'missing' | 'extra'
  }
  details: {
    missingNasaIds: string[]
    extraNasaIds: string[]
    truncatedMissing: boolean
    truncatedExtra: boolean
  }
  nasaPhotos: Array<{
    nasaId: string
    sol: number
    earthDate: string | null
    camera: string | null
    imgSrc: string | null
    width: number | null
    height: number | null
    sampleType: string | null
  }>
  ourPhotos: Array<{
    id: number
    nasaId: string
    sol: number
    earthDate: string | null
    cameraName: string
    width: number | null
    height: number | null
    sampleType: string | null
    imgSrcFull: string | null
  }>
}

export interface PhotoComparisonResult {
  nasaId: string
  foundInNasa: boolean
  foundInOurs: boolean
  ourData: {
    id: number
    nasaId: string
    sol: number
    earthDate: string | null
    camera: string
    rover: string
    width: number | null
    height: number | null
    sampleType: string | null
    imgSrc: string | null
    site: number | null
    drive: number | null
    mastAz: number | null
    mastEl: number | null
    dateTakenUtc: string | null
    dateTakenMars: string | null
    rawDataPreview: string | null
  }
  nasaData: {
    nasaId: string
    sol: number
    earthDate: string | null
    camera: string | null
    width: number | null
    height: number | null
    sampleType: string | null
    imgSrc: string | null
  } | null
  differences: Array<{
    field: string
    ourValue: string
    nasaValue: string
  }>
}
