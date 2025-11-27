'use client'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { PerformanceMetrics as PerformanceMetricsType } from '@/lib/types'

interface PerformanceMetricsProps {
  metrics: PerformanceMetricsType
}

function MetricCard({
  label,
  value,
  icon,
  colorClass,
}: {
  label: string
  value: string
  icon: string
  colorClass: string
}) {
  return (
    <Card className={colorClass}>
      <CardContent className="p-6">
        <div className="text-2xl mb-2">{icon}</div>
        <div className="text-2xl font-bold text-slate-800 dark:text-slate-200">{value}</div>
        <div className="text-sm text-slate-600 dark:text-slate-400">{label}</div>
      </CardContent>
    </Card>
  )
}

export function PerformanceMetrics({ metrics }: PerformanceMetricsProps) {
  const colorClasses: Record<string, string> = {
    blue: 'bg-blue-50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800',
    indigo: 'bg-indigo-50 border-indigo-200 dark:bg-indigo-900/20 dark:border-indigo-800',
    green: 'bg-green-50 border-green-200 dark:bg-green-900/20 dark:border-green-800',
    yellow: 'bg-yellow-50 border-yellow-200 dark:bg-yellow-900/20 dark:border-yellow-800',
  }

  return (
    <div className="space-y-6">
      {/* Current Performance Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <MetricCard
          label="Avg Response Time"
          value={`${metrics.currentMetrics.averageResponseTime}ms`}
          icon="⚡"
          colorClass={colorClasses.blue}
        />
        <MetricCard
          label="P95 Response Time"
          value={`${metrics.currentMetrics.p95ResponseTime}ms`}
          icon="📊"
          colorClass={colorClasses.indigo}
        />
        <MetricCard
          label="Requests/Min"
          value={metrics.currentMetrics.requestsPerMinute.toFixed(1)}
          icon="🚀"
          colorClass={colorClasses.green}
        />
        <MetricCard
          label="Success Rate"
          value={`${metrics.currentMetrics.successRate.toFixed(1)}%`}
          icon="✅"
          colorClass={metrics.currentMetrics.successRate > 95 ? colorClasses.green : colorClasses.yellow}
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
              <div className="text-sm text-slate-600 dark:text-slate-400">P50 (Median)</div>
              <div className="text-2xl font-bold text-slate-800 dark:text-slate-200">
                {metrics.currentMetrics.p50ResponseTime}ms
              </div>
            </div>
            <div>
              <div className="text-sm text-slate-600 dark:text-slate-400">P95</div>
              <div className="text-2xl font-bold text-slate-800 dark:text-slate-200">
                {metrics.currentMetrics.p95ResponseTime}ms
              </div>
            </div>
            <div>
              <div className="text-sm text-slate-600 dark:text-slate-400">P99</div>
              <div className="text-2xl font-bold text-slate-800 dark:text-slate-200">
                {metrics.currentMetrics.p99ResponseTime}ms
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Slow Queries Table */}
      {metrics.slowQueries.length > 0 && (
        <Card className="border-yellow-200 bg-yellow-50 dark:border-yellow-800 dark:bg-yellow-900/20">
          <CardHeader>
            <CardTitle>Slow Queries (&gt;1s)</CardTitle>
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
                    <TableCell className="text-orange-600 dark:text-orange-400 font-semibold">
                      {query.avgResponseTime}ms
                    </TableCell>
                    <TableCell className="text-red-600 dark:text-red-400 font-semibold">
                      {query.maxResponseTime}ms
                    </TableCell>
                    <TableCell>{query.count}</TableCell>
                    <TableCell className="text-slate-600 dark:text-slate-400">
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
