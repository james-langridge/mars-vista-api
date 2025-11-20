import { Card, CardContent } from '@/components/ui/card'
import ScraperJobHistory from './ScraperJobHistory'

interface ScraperState {
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

interface ScraperStatus {
  scrapers: ScraperState[]
  nextScheduledRun: string | null
}

interface ScraperMetrics {
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

interface ScraperMonitoringProps {
  status: ScraperStatus
  metrics: ScraperMetrics
}

export default function ScraperMonitoring({ status, metrics }: ScraperMonitoringProps) {
  const getHealthBadge = (health: string) => {
    switch (health) {
      case 'healthy':
        return { emoji: '✓', color: 'bg-green-100 text-green-800', label: 'Healthy' }
      case 'warning':
        return { emoji: '⚠', color: 'bg-yellow-100 text-yellow-800', label: 'Warning' }
      case 'error':
        return { emoji: '✗', color: 'bg-red-100 text-red-800', label: 'Error' }
      default:
        return { emoji: '?', color: 'bg-gray-100 text-gray-800', label: 'Unknown' }
    }
  }

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp)
    const now = new Date()
    const diffMs = now.getTime() - date.getTime()
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)

    if (diffMins < 60) return `${diffMins}m ago`
    if (diffHours < 24) return `${diffHours}h ago`
    return `${diffDays}d ago`
  }

  return (
    <div className="space-y-6">
      {/* Rover Status Cards */}
      <div>
        <h2 className="text-xl font-semibold text-slate-800 mb-4">Rover Status</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {status.scrapers.map((scraper) => {
            const healthBadge = getHealthBadge(scraper.healthStatus)
            return (
              <Card key={scraper.roverName}>
                <CardContent className="p-6">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-lg font-semibold text-slate-800 capitalize">
                      {scraper.roverName}
                    </h3>
                    <span
                      className={`px-2 py-1 rounded text-xs font-medium ${healthBadge.color}`}
                    >
                      {healthBadge.emoji} {healthBadge.label}
                    </span>
                  </div>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-slate-600">Last Sol:</span>
                      <span className="font-medium">{scraper.lastScrapedSol}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600">Last Run:</span>
                      <span className="font-medium">
                        {formatTimestamp(scraper.lastRunTimestamp)}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600">Photos Added:</span>
                      <span className="font-medium">
                        {scraper.photosAddedLastRun.toLocaleString()}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600">Total Photos:</span>
                      <span className="font-medium">
                        {scraper.totalPhotos.toLocaleString()}
                      </span>
                    </div>
                  </div>
                  {scraper.errorMessage && (
                    <div className="mt-3 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-700">
                      {scraper.errorMessage}
                    </div>
                  )}
                </CardContent>
              </Card>
            )
          })}
        </div>
      </div>

      {/* Performance Metrics */}
      <div>
        <h2 className="text-xl font-semibold text-slate-800 mb-4">
          Performance Metrics (Last {metrics.period === '24h' ? '24 Hours' : metrics.period === '7d' ? '7 Days' : '30 Days'})
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 mb-1">
                {metrics.metrics.totalJobs}
              </div>
              <div className="text-sm text-slate-600">Total Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-green-600 mb-1">
                {metrics.metrics.successfulJobs}
              </div>
              <div className="text-sm text-slate-600">Successful Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-red-600 mb-1">
                {metrics.metrics.failedJobs}
              </div>
              <div className="text-sm text-slate-600">Failed Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-blue-600 mb-1">
                {metrics.metrics.successRate.toFixed(1)}%
              </div>
              <div className="text-sm text-slate-600">Success Rate</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 mb-1">
                {metrics.metrics.totalPhotosAdded.toLocaleString()}
              </div>
              <div className="text-sm text-slate-600">Photos Added</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 mb-1">
                {metrics.metrics.averageDurationSeconds}s
              </div>
              <div className="text-sm text-slate-600">Avg Duration</div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Job History Table */}
      <div>
        <h2 className="text-xl font-semibold text-slate-800 mb-4">Recent Job History</h2>
        <ScraperJobHistory />
      </div>
    </div>
  )
}
