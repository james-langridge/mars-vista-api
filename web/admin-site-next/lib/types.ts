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
