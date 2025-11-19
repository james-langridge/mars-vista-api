import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface Violation {
  timestamp: string
  userEmail: string
  tier: string
  violationType: string
  requestCount?: number
  limit?: number
}

interface RateLimitViolationsProps {
  violations: Violation[]
}

export default function RateLimitViolations({ violations }: RateLimitViolationsProps) {
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
                <TableCell className="text-slate-600">
                  {new Date(violation.timestamp).toLocaleString()}
                </TableCell>
                <TableCell>{violation.userEmail}</TableCell>
                <TableCell>
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-slate-100 text-slate-700">
                    {violation.tier}
                  </span>
                </TableCell>
                <TableCell>
                  <span className="px-2 py-1 text-xs font-semibold rounded-full bg-orange-100 text-orange-700">
                    {violation.violationType}
                  </span>
                </TableCell>
                <TableCell className="text-sm text-slate-600">
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
