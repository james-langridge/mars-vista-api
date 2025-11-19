import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

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
        <Card>
          <CardHeader>
            <CardTitle>Error Summary</CardTitle>
          </CardHeader>
          <CardContent>
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
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Errors by Status Code</CardTitle>
          </CardHeader>
          <CardContent>
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
          </CardContent>
        </Card>
      </div>

      {/* Recent Errors Table */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Errors</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Time</TableHead>
                <TableHead>User</TableHead>
                <TableHead>Endpoint</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Error</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.recentErrors.map((error, index) => (
                <TableRow key={index}>
                  <TableCell className="text-slate-600">
                    {new Date(error.timestamp).toLocaleTimeString()}
                  </TableCell>
                  <TableCell>{error.userEmail}</TableCell>
                  <TableCell className="font-mono text-xs text-slate-600">{error.endpoint}</TableCell>
                  <TableCell>
                    <span className={`px-2 py-1 text-xs font-semibold rounded ${
                      error.statusCode === 404 ? 'bg-yellow-100 text-yellow-700' :
                      error.statusCode === 429 ? 'bg-orange-100 text-orange-700' :
                      'bg-red-100 text-red-700'
                    }`}>
                      {error.statusCode}
                    </span>
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">{error.errorMessage}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  )
}
