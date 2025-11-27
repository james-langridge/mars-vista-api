'use client'

import { Card, CardContent } from '@/components/ui/card'
import { ScraperJobHistory } from './scraper-job-history'
import type { ScraperStatus, ScraperMetrics } from '@/lib/types'

interface ScraperMonitoringProps {
  status: ScraperStatus
  metrics: ScraperMetrics
}

export function ScraperMonitoring({ status, metrics }: ScraperMonitoringProps) {
  const getHealthBadge = (health: string) => {
    switch (health) {
      case 'healthy':
        return {
          color: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
          label: 'Healthy',
        }
      case 'warning':
        return {
          color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
          label: 'Warning',
        }
      case 'error':
        return { color: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400', label: 'Error' }
      default:
        return { color: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300', label: 'Unknown' }
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
        <h2 className="text-xl font-semibold text-slate-800 dark:text-slate-200 mb-4">Rover Status</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {status.scrapers.map((scraper) => {
            const healthBadge = getHealthBadge(scraper.healthStatus)
            return (
              <Card key={scraper.roverName}>
                <CardContent className="p-6">
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-lg font-semibold text-slate-800 dark:text-slate-200 capitalize">
                      {scraper.roverName}
                    </h3>
                    <span className={`px-2 py-1 rounded text-xs font-medium ${healthBadge.color}`}>
                      {healthBadge.label}
                    </span>
                  </div>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-slate-600 dark:text-slate-400">Last Sol:</span>
                      <span className="font-medium">{scraper.lastScrapedSol}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600 dark:text-slate-400">Last Run:</span>
                      <span className="font-medium">{formatTimestamp(scraper.lastRunTimestamp)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600 dark:text-slate-400">Photos Added:</span>
                      <span className="font-medium">{scraper.photosAddedLastRun.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-600 dark:text-slate-400">Total Photos:</span>
                      <span className="font-medium">{scraper.totalPhotos.toLocaleString()}</span>
                    </div>
                  </div>
                  {scraper.errorMessage && (
                    <div className="mt-3 p-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded text-xs text-red-700 dark:text-red-400">
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
        <h2 className="text-xl font-semibold text-slate-800 dark:text-slate-200 mb-4">
          Performance Metrics (Last{' '}
          {metrics.period === '24h' ? '24 Hours' : metrics.period === '7d' ? '7 Days' : '30 Days'})
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 dark:text-slate-200 mb-1">
                {metrics.metrics.totalJobs}
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Total Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-green-600 dark:text-green-400 mb-1">
                {metrics.metrics.successfulJobs}
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Successful Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-red-600 dark:text-red-400 mb-1">
                {metrics.metrics.failedJobs}
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Failed Jobs</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-blue-600 dark:text-blue-400 mb-1">
                {metrics.metrics.successRate.toFixed(1)}%
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Success Rate</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 dark:text-slate-200 mb-1">
                {metrics.metrics.totalPhotosAdded.toLocaleString()}
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Photos Added</div>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="p-6">
              <div className="text-3xl font-bold text-slate-800 dark:text-slate-200 mb-1">
                {metrics.metrics.averageDurationSeconds}s
              </div>
              <div className="text-sm text-slate-600 dark:text-slate-400">Avg Duration</div>
            </CardContent>
          </Card>
        </div>
      </div>

      {/* Job History Table */}
      <div>
        <h2 className="text-xl font-semibold text-slate-800 dark:text-slate-200 mb-4">Recent Job History</h2>
        <ScraperJobHistory />
      </div>
    </div>
  )
}
