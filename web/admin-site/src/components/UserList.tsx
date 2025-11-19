import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface User {
  email: string
  tier: string
  apiKeyCreated: string
  lastUsed: string
  totalRequests: number
  requestsToday: number
  requestsThisHour: number
  isActive: boolean
}

interface UserListProps {
  users: User[]
}

export default function UserList({ users }: UserListProps) {
  const getTierBadge = (tier: string) => {
    const colors = {
      free: 'bg-slate-100 text-slate-700',
      pro: 'bg-blue-100 text-blue-700',
      enterprise: 'bg-purple-100 text-purple-700',
    }
    return colors[tier as keyof typeof colors] || colors.free
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Users</CardTitle>
      </CardHeader>
      <CardContent>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Email</TableHead>
              <TableHead>Tier</TableHead>
              <TableHead>Total Requests</TableHead>
              <TableHead>Today</TableHead>
              <TableHead>This Hour</TableHead>
              <TableHead>Last Used</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((user) => (
              <TableRow key={user.email}>
                <TableCell className="font-medium">{user.email}</TableCell>
                <TableCell>
                  <span className={`px-2 py-1 text-xs font-semibold rounded-full ${getTierBadge(user.tier)}`}>
                    {user.tier}
                  </span>
                </TableCell>
                <TableCell>{user.totalRequests.toLocaleString()}</TableCell>
                <TableCell>{user.requestsToday}</TableCell>
                <TableCell>{user.requestsThisHour}</TableCell>
                <TableCell className="text-slate-600">
                  {user.lastUsed ? new Date(user.lastUsed).toLocaleString() : 'Never'}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
