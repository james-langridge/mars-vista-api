'use client'

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import type { Activity } from '@/lib/types'

interface ActivityLogProps {
  activities: Activity[]
}

export function ActivityLog({ activities }: ActivityLogProps) {
  const getStatusColor = (statusCode: number) => {
    if (statusCode >= 200 && statusCode < 300) return 'text-green-600 dark:text-green-400'
    if (statusCode >= 400 && statusCode < 500) return 'text-yellow-600 dark:text-yellow-400'
    return 'text-red-600 dark:text-red-400'
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Recent Activity</CardTitle>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Time</TableHead>
              <TableHead>User</TableHead>
              <TableHead>Endpoint</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Response Time</TableHead>
              <TableHead>Photos</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {activities.map((activity, index) => (
              <TableRow key={index}>
                <TableCell className="text-slate-600 dark:text-slate-400">
                  {new Date(activity.timestamp).toLocaleTimeString()}
                </TableCell>
                <TableCell>{activity.userEmail}</TableCell>
                <TableCell className="font-mono text-xs text-slate-600 dark:text-slate-400">
                  {activity.endpoint}
                </TableCell>
                <TableCell className={`font-semibold ${getStatusColor(activity.statusCode)}`}>
                  {activity.statusCode}
                </TableCell>
                <TableCell>{activity.responseTime}ms</TableCell>
                <TableCell>{activity.photosReturned}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
