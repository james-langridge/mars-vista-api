import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface EndpointUsageProps {
  data: {
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
}

export default function EndpointUsage({ data }: EndpointUsageProps) {
  return (
    <div className="space-y-6">
      {/* Top Endpoints Table */}
      <Card>
        <CardHeader>
          <CardTitle>Top Endpoints</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Endpoint</TableHead>
                <TableHead>Total Calls</TableHead>
                <TableHead>Last 24h</TableHead>
                <TableHead>Avg Time</TableHead>
                <TableHead>Success Rate</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {data.topEndpoints.map((endpoint, index) => (
                <TableRow key={index}>
                  <TableCell className="font-mono text-sm">{endpoint.endpoint}</TableCell>
                  <TableCell>{endpoint.calls.toLocaleString()}</TableCell>
                  <TableCell>{endpoint.last24h.toLocaleString()}</TableCell>
                  <TableCell>{endpoint.avgResponseTime}ms</TableCell>
                  <TableCell>
                    <span className={`px-2 py-1 text-xs font-semibold rounded-full ${
                      endpoint.successRate > 99 ? 'bg-green-100 text-green-700' :
                      endpoint.successRate > 95 ? 'bg-yellow-100 text-yellow-700' :
                      'bg-red-100 text-red-700'
                    }`}>
                      {endpoint.successRate.toFixed(1)}%
                    </span>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Rover and Camera Usage */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Rover Usage */}
        <Card>
          <CardHeader>
            <CardTitle>Rover Usage</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Object.entries(data.roverUsage)
                .sort(([, a], [, b]) => b - a)
                .map(([rover, count]) => (
                  <div key={rover} className="flex items-center justify-between">
                    <span className="text-sm font-medium text-slate-700 capitalize">{rover}</span>
                    <span className="text-sm font-bold text-slate-800">{count.toLocaleString()}</span>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>

        {/* Camera Usage */}
        <Card>
          <CardHeader>
            <CardTitle>Top Cameras</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Object.entries(data.cameraUsage)
                .sort(([, a], [, b]) => b - a)
                .slice(0, 10)
                .map(([camera, count]) => (
                  <div key={camera} className="flex items-center justify-between">
                    <span className="text-sm font-medium text-slate-700">{camera}</span>
                    <span className="text-sm font-bold text-slate-800">{count.toLocaleString()}</span>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
