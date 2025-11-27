'use client'

import { useEffect, useState, Fragment } from 'react'
import { fetchScraperHistory } from '@/lib/api'
import { Card, CardContent } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Skeleton } from '@/components/ui/skeleton'
import type { ScraperJob } from '@/lib/types'

export function ScraperJobHistory() {
  const [jobs, setJobs] = useState<ScraperJob[]>([])
  const [loading, setLoading] = useState(true)
  const [expandedJobId, setExpandedJobId] = useState<number | null>(null)
  const [filters, setFilters] = useState({
    rover: '',
    status: '',
    limit: 50,
  })

  useEffect(() => {
    loadHistory()
  }, [filters])

  async function loadHistory() {
    try {
      setLoading(true)
      const data = await fetchScraperHistory({
        limit: filters.limit,
        rover: filters.rover || undefined,
        status: filters.status || undefined,
      })
      setJobs(data.jobs)
    } catch (error) {
      console.error('Failed to load scraper history:', error)
    } finally {
      setLoading(false)
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'success':
        return {
          color: 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400',
          label: 'Success',
        }
      case 'failed':
        return { color: 'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400', label: 'Failed' }
      case 'partial':
        return {
          color: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400',
          label: 'Partial',
        }
      default:
        return { color: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300', label: 'Unknown' }
    }
  }

  const formatDuration = (seconds: number | null) => {
    if (!seconds) return 'N/A'
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins}m ${secs}s`
  }

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp)
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  const toggleExpand = (jobId: number) => {
    setExpandedJobId(expandedJobId === jobId ? null : jobId)
  }

  if (loading) {
    return (
      <Card>
        <CardContent className="p-6">
          <div className="space-y-4">
            {[...Array(5)].map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardContent className="p-6">
        {/* Filters */}
        <div className="flex gap-4 mb-4">
          <div>
            <label className="text-sm text-slate-600 dark:text-slate-400 mb-1 block">Rover</label>
            <select
              value={filters.rover}
              onChange={(e) => setFilters({ ...filters, rover: e.target.value })}
              className="px-3 py-2 border border-slate-300 dark:border-slate-700 rounded-md text-sm bg-white dark:bg-slate-900"
            >
              <option value="">All Rovers</option>
              <option value="curiosity">Curiosity</option>
              <option value="perseverance">Perseverance</option>
              <option value="opportunity">Opportunity</option>
              <option value="spirit">Spirit</option>
            </select>
          </div>
          <div>
            <label className="text-sm text-slate-600 dark:text-slate-400 mb-1 block">Status</label>
            <select
              value={filters.status}
              onChange={(e) => setFilters({ ...filters, status: e.target.value })}
              className="px-3 py-2 border border-slate-300 dark:border-slate-700 rounded-md text-sm bg-white dark:bg-slate-900"
            >
              <option value="">All Status</option>
              <option value="success">Success</option>
              <option value="failed">Failed</option>
              <option value="partial">Partial</option>
            </select>
          </div>
        </div>

        {/* Table */}
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Time</TableHead>
              <TableHead>Rovers</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Photos</TableHead>
              <TableHead>Duration</TableHead>
              <TableHead>Details</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {jobs.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center text-slate-500 dark:text-slate-400 py-8">
                  No job history found
                </TableCell>
              </TableRow>
            ) : (
              jobs.map((job) => {
                const statusBadge = getStatusBadge(job.status)
                const isExpanded = expandedJobId === job.id

                return (
                  <Fragment key={job.id}>
                    <TableRow className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-800/50">
                      <TableCell>{formatTimestamp(job.startedAt)}</TableCell>
                      <TableCell>
                        {job.totalRoversSucceeded}/{job.totalRoversAttempted}
                      </TableCell>
                      <TableCell>
                        <span className={`px-2 py-1 text-xs font-medium rounded ${statusBadge.color}`}>
                          {statusBadge.label}
                        </span>
                      </TableCell>
                      <TableCell>{job.totalPhotosAdded.toLocaleString()}</TableCell>
                      <TableCell>{formatDuration(job.durationSeconds)}</TableCell>
                      <TableCell>
                        <button
                          onClick={() => toggleExpand(job.id)}
                          className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium"
                        >
                          {isExpanded ? 'Hide' : 'View'}
                        </button>
                      </TableCell>
                    </TableRow>
                    {isExpanded && (
                      <TableRow>
                        <TableCell colSpan={6} className="bg-slate-50 dark:bg-slate-800/50 p-4">
                          <div className="space-y-3">
                            <h4 className="font-semibold text-slate-800 dark:text-slate-200">Rover Details</h4>
                            {job.roverDetails.map((rover) => (
                              <div
                                key={rover.roverName}
                                className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded p-3"
                              >
                                <div className="flex items-center justify-between mb-2">
                                  <span className="font-medium capitalize">{rover.roverName}</span>
                                  <span
                                    className={`px-2 py-1 text-xs font-medium rounded ${
                                      getStatusBadge(rover.status).color
                                    }`}
                                  >
                                    {getStatusBadge(rover.status).label}
                                  </span>
                                </div>
                                <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
                                  <div>
                                    <span className="text-slate-600 dark:text-slate-400">Sol Range:</span>
                                    <span className="font-medium ml-1">
                                      {rover.startSol}-{rover.endSol}
                                    </span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600 dark:text-slate-400">Sols:</span>
                                    <span className="font-medium ml-1">
                                      {rover.solsSucceeded}/{rover.solsAttempted}
                                    </span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600 dark:text-slate-400">Photos:</span>
                                    <span className="font-medium ml-1">{rover.photosAdded}</span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600 dark:text-slate-400">Duration:</span>
                                    <span className="font-medium ml-1">{formatDuration(rover.durationSeconds)}</span>
                                  </div>
                                </div>
                                {rover.failedSols.length > 0 && (
                                  <div className="mt-3 p-3 bg-amber-50 dark:bg-amber-900/20 border-l-4 border-amber-400 dark:border-amber-600 rounded">
                                    <p className="text-sm font-medium text-amber-800 dark:text-amber-400">
                                      Failed to scrape {rover.failedSols.length} sol
                                      {rover.failedSols.length > 1 ? 's' : ''}: {rover.failedSols.join(', ')}
                                    </p>
                                    {rover.errorMessage && (
                                      <p className="text-xs text-amber-700 dark:text-amber-500 mt-2">
                                        Error: {rover.errorMessage}
                                      </p>
                                    )}
                                  </div>
                                )}
                                {rover.errorMessage && rover.failedSols.length === 0 && (
                                  <div className="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border-l-4 border-red-400 dark:border-red-600 rounded">
                                    <p className="text-sm font-medium text-red-800 dark:text-red-400">Scraper Error</p>
                                    <p className="text-xs text-red-700 dark:text-red-500">{rover.errorMessage}</p>
                                  </div>
                                )}
                                {rover.photosAddedDetails && rover.photosAddedDetails.length > 0 && (
                                  <details className="mt-3 p-3 bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-400 dark:border-blue-600 rounded">
                                    <summary className="cursor-pointer text-sm font-medium text-blue-800 dark:text-blue-400">
                                      {rover.photosAdded} photo{rover.photosAdded > 1 ? 's' : ''} added
                                    </summary>
                                    <div className="mt-2 max-h-60 overflow-y-auto">
                                      <table className="min-w-full text-xs">
                                        <thead className="bg-blue-100 dark:bg-blue-900/40 sticky top-0">
                                          <tr>
                                            <th className="px-2 py-1 text-left font-semibold text-blue-900 dark:text-blue-300">
                                              Sol
                                            </th>
                                            <th className="px-2 py-1 text-left font-semibold text-blue-900 dark:text-blue-300">
                                              NASA ID
                                            </th>
                                          </tr>
                                        </thead>
                                        <tbody className="divide-y divide-blue-200 dark:divide-blue-800">
                                          {rover.photosAddedDetails.map((photo, idx) => (
                                            <tr key={idx} className="hover:bg-blue-100 dark:hover:bg-blue-900/30">
                                              <td className="px-2 py-1 text-blue-800 dark:text-blue-400">{photo.sol}</td>
                                              <td className="px-2 py-1 text-blue-700 dark:text-blue-400 font-mono">
                                                {photo.nasaId}
                                              </td>
                                            </tr>
                                          ))}
                                        </tbody>
                                      </table>
                                    </div>
                                  </details>
                                )}
                              </div>
                            ))}
                          </div>
                        </TableCell>
                      </TableRow>
                    )}
                  </Fragment>
                )
              })
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
