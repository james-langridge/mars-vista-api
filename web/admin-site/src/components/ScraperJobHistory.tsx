import { useEffect, useState } from 'react'
import { fetchScraperHistory } from '../utils/api'
import { Card, CardContent } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface PhotoDetail {
  sol: number
  nasaId: string
}

interface RoverDetail {
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

interface Job {
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

export default function ScraperJobHistory() {
  const [jobs, setJobs] = useState<Job[]>([])
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
        return { emoji: '✓', color: 'bg-green-100 text-green-800', label: 'Success' }
      case 'failed':
        return { emoji: '✗', color: 'bg-red-100 text-red-800', label: 'Failed' }
      case 'partial':
        return { emoji: '⚠', color: 'bg-yellow-100 text-yellow-800', label: 'Partial' }
      default:
        return { emoji: '?', color: 'bg-gray-100 text-gray-800', label: 'Unknown' }
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
        <CardContent className="p-12 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-orange-500 mx-auto"></div>
          <p className="mt-4 text-slate-600">Loading job history...</p>
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
            <label className="text-sm text-slate-600 mb-1 block">Rover</label>
            <select
              value={filters.rover}
              onChange={(e) => setFilters({ ...filters, rover: e.target.value })}
              className="px-3 py-2 border border-slate-300 rounded-md text-sm"
            >
              <option value="">All Rovers</option>
              <option value="curiosity">Curiosity</option>
              <option value="perseverance">Perseverance</option>
              <option value="opportunity">Opportunity</option>
              <option value="spirit">Spirit</option>
            </select>
          </div>
          <div>
            <label className="text-sm text-slate-600 mb-1 block">Status</label>
            <select
              value={filters.status}
              onChange={(e) => setFilters({ ...filters, status: e.target.value })}
              className="px-3 py-2 border border-slate-300 rounded-md text-sm"
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
                <TableCell colSpan={6} className="text-center text-slate-500 py-8">
                  No job history found
                </TableCell>
              </TableRow>
            ) : (
              jobs.map((job) => {
                const statusBadge = getStatusBadge(job.status)
                const isExpanded = expandedJobId === job.id

                return (
                  <>
                    <TableRow key={job.id} className="cursor-pointer hover:bg-slate-50">
                      <TableCell>{formatTimestamp(job.startedAt)}</TableCell>
                      <TableCell>
                        {job.totalRoversSucceeded}/{job.totalRoversAttempted}
                      </TableCell>
                      <TableCell>
                        <span
                          className={`px-2 py-1 text-xs font-medium rounded ${statusBadge.color}`}
                        >
                          {statusBadge.emoji} {statusBadge.label}
                        </span>
                      </TableCell>
                      <TableCell>{job.totalPhotosAdded.toLocaleString()}</TableCell>
                      <TableCell>{formatDuration(job.durationSeconds)}</TableCell>
                      <TableCell>
                        <button
                          onClick={() => toggleExpand(job.id)}
                          className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                        >
                          {isExpanded ? '▼ Hide' : '▶ View'}
                        </button>
                      </TableCell>
                    </TableRow>
                    {isExpanded && (
                      <TableRow>
                        <TableCell colSpan={6} className="bg-slate-50 p-4">
                          <div className="space-y-3">
                            <h4 className="font-semibold text-slate-800">Rover Details</h4>
                            {job.roverDetails.map((rover) => (
                              <div
                                key={rover.roverName}
                                className="bg-white border border-slate-200 rounded p-3"
                              >
                                <div className="flex items-center justify-between mb-2">
                                  <span className="font-medium capitalize">
                                    {rover.roverName}
                                  </span>
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
                                    <span className="text-slate-600">Sol Range:</span>
                                    <span className="font-medium ml-1">
                                      {rover.startSol}-{rover.endSol}
                                    </span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600">Sols:</span>
                                    <span className="font-medium ml-1">
                                      {rover.solsSucceeded}/{rover.solsAttempted}
                                    </span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600">Photos:</span>
                                    <span className="font-medium ml-1">
                                      {rover.photosAdded}
                                    </span>
                                  </div>
                                  <div>
                                    <span className="text-slate-600">Duration:</span>
                                    <span className="font-medium ml-1">
                                      {formatDuration(rover.durationSeconds)}
                                    </span>
                                  </div>
                                </div>
                                {rover.failedSols.length > 0 && (
                                  <div className="mt-3 p-3 bg-amber-50 border-l-4 border-amber-400 rounded">
                                    <div className="flex items-start">
                                      <span className="text-amber-600 font-semibold text-sm mr-2">⚠</span>
                                      <div className="flex-1">
                                        <p className="text-sm font-medium text-amber-800 mb-1">
                                          Failed to scrape {rover.failedSols.length} sol{rover.failedSols.length > 1 ? 's' : ''}: {rover.failedSols.join(', ')}
                                        </p>
                                        {rover.errorMessage && (
                                          <p className="text-xs text-amber-700 mt-2 leading-relaxed">
                                            <span className="font-semibold">Error:</span> {rover.errorMessage}
                                          </p>
                                        )}
                                      </div>
                                    </div>
                                  </div>
                                )}
                                {rover.errorMessage && rover.failedSols.length === 0 && (
                                  <div className="mt-3 p-3 bg-red-50 border-l-4 border-red-400 rounded">
                                    <div className="flex items-start">
                                      <span className="text-red-600 font-semibold text-sm mr-2">✗</span>
                                      <div className="flex-1">
                                        <p className="text-sm font-medium text-red-800 mb-1">Scraper Error</p>
                                        <p className="text-xs text-red-700 leading-relaxed">
                                          {rover.errorMessage}
                                        </p>
                                      </div>
                                    </div>
                                  </div>
                                )}
                                {rover.photosAddedDetails && rover.photosAddedDetails.length > 0 && (
                                  <div className="mt-3 p-3 bg-blue-50 border-l-4 border-blue-400 rounded">
                                    <details className="group">
                                      <summary className="cursor-pointer text-sm font-medium text-blue-800 mb-2 flex items-center">
                                        <span className="mr-2">📷</span>
                                        {rover.photosAdded} photo{rover.photosAdded > 1 ? 's' : ''} added
                                        <span className="ml-2 text-blue-600 group-open:rotate-90 transition-transform">▶</span>
                                      </summary>
                                      <div className="mt-2 max-h-60 overflow-y-auto">
                                        <table className="min-w-full text-xs">
                                          <thead className="bg-blue-100 sticky top-0">
                                            <tr>
                                              <th className="px-2 py-1 text-left font-semibold text-blue-900">Sol</th>
                                              <th className="px-2 py-1 text-left font-semibold text-blue-900">NASA ID</th>
                                            </tr>
                                          </thead>
                                          <tbody className="divide-y divide-blue-200">
                                            {rover.photosAddedDetails.map((photo, idx) => (
                                              <tr key={idx} className="hover:bg-blue-100">
                                                <td className="px-2 py-1 text-blue-800">{photo.sol}</td>
                                                <td className="px-2 py-1 text-blue-700 font-mono">{photo.nasaId}</td>
                                              </tr>
                                            ))}
                                          </tbody>
                                        </table>
                                      </div>
                                    </details>
                                  </div>
                                )}
                              </div>
                            ))}
                          </div>
                        </TableCell>
                      </TableRow>
                    )}
                  </>
                )
              })
            )}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
