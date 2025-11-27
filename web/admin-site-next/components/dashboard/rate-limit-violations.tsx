'use client'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { Violation } from '@/lib/types'

interface RateLimitViolationsProps {
  violations: Violation[]
}

export function RateLimitViolations({ violations }: RateLimitViolationsProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Rate Limit Violations</CardTitle>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Time</TableHead>
              <TableHead>User</TableHead>
              <TableHead>Tier</TableHead>
              <TableHead>Violation Type</TableHead>
              <TableHead>Details</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {violations.map((violation, index) => (
              <TableRow key={index}>
                <TableCell className="text-slate-600 dark:text-slate-400">
                  {new Date(violation.timestamp).toLocaleString()}
                </TableCell>
                <TableCell>{violation.userEmail}</TableCell>
                <TableCell>
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300">
                    {violation.tier}
                  </span>
                </TableCell>
                <TableCell>
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400">
                    {violation.violationType}
                  </span>
                </TableCell>
                <TableCell className="text-sm text-slate-600 dark:text-slate-400">
                  {violation.requestCount && violation.limit
                    ? `${violation.requestCount} / ${violation.limit}`
                    : 'Rate limit exceeded'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
