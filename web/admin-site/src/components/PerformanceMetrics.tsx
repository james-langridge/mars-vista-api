import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface PerformanceMetricsProps {
  metrics: {
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
}

export default function PerformanceMetrics({ metrics }: PerformanceMetricsProps) {
  const MetricCard = ({ label, value, icon, color }: { label: string; value: string; icon: string; color: string }) => {
    const colorClasses = {
      blue: 'bg-blue-50 border-blue-200',
      indigo: 'bg-indigo-50 border-indigo-200',
      green: 'bg-green-50 border-green-200',
      yellow: 'bg-yellow-50 border-yellow-200',
    }

    return (
      <Card className={colorClasses[color as keyof typeof colorClasses]}>
        <CardContent className="p-6">
          <div className="text-2xl mb-2">{icon}</div>
          <div className="text-2xl font-bold text-slate-800">{value}</div>
          <div className="text-sm text-slate-600">{label}</div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-6">
      {/* Current Performance Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <MetricCard
          label="Avg Response Time"
          value={`${metrics.currentMetrics.averageResponseTime}ms`}
          icon="⚡"
          color="blue"
        />
        <MetricCard
          label="P95 Response Time"
          value={`${metrics.currentMetrics.p95ResponseTime}ms`}
          icon="📊"
          color="indigo"
        />
        <MetricCard
          label="Requests/Min"
          value={metrics.currentMetrics.requestsPerMinute.toFixed(1)}
          icon="🚀"
          color="green"
        />
        <MetricCard
          label="Success Rate"
          value={`${metrics.currentMetrics.successRate.toFixed(1)}%`}
          icon="✅"
          color={metrics.currentMetrics.successRate > 95 ? 'green' : 'yellow'}
        />
      </div>

      {/* Percentile Breakdown */}
      <Card>
        <CardHeader>
          <CardTitle>Response Time Percentiles</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <div className="text-sm text-slate-600">P50 (Median)</div>
              <div className="text-2xl font-bold text-slate-800">
                {metrics.currentMetrics.p50ResponseTime}ms
              </div>
            </div>
            <div>
              <div className="text-sm text-slate-600">P95</div>
              <div className="text-2xl font-bold text-slate-800">
                {metrics.currentMetrics.p95ResponseTime}ms
              </div>
            </div>
            <div>
              <div className="text-sm text-slate-600">P99</div>
              <div className="text-2xl font-bold text-slate-800">
                {metrics.currentMetrics.p99ResponseTime}ms
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Slow Queries Table */}
      {metrics.slowQueries.length > 0 && (
        <Card className="border-yellow-200 bg-yellow-50">
          <CardHeader>
            <CardTitle>⚠️ Slow Queries (&gt;1s)</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Endpoint</TableHead>
                  <TableHead>Avg Time</TableHead>
                  <TableHead>Max Time</TableHead>
                  <TableHead>Count</TableHead>
                  <TableHead>Last Seen</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {metrics.slowQueries.map((query, index) => (
                  <TableRow key={index}>
                    <TableCell className="font-mono text-sm">{query.endpoint}</TableCell>
                    <TableCell className="text-orange-600 font-semibold">
                      {query.avgResponseTime}ms
                    </TableCell>
                    <TableCell className="text-red-600 font-semibold">
                      {query.maxResponseTime}ms
                    </TableCell>
                    <TableCell>{query.count}</TableCell>
                    <TableCell className="text-slate-600">
                      {new Date(query.lastOccurrence).toLocaleString()}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
