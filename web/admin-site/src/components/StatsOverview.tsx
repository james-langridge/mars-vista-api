import { Card, CardContent } from '@/components/ui/card'

interface Stats {
  totalUsers: number
  activeApiKeys: number
  totalApiCalls: number
  totalPhotosRetrieved: number
  apiCallsLast24h: number
  apiCallsLast7d: number
  averageResponseTime: number
  systemUptime: string
}

interface StatsOverviewProps {
  stats: Stats
}

export default function StatsOverview({ stats }: StatsOverviewProps) {
  const statCards = [
    { label: 'Total Users', value: stats.totalUsers, icon: '👥' },
    { label: 'Active API Keys', value: stats.activeApiKeys, icon: '🔑' },
    { label: 'Total API Calls', value: stats.totalApiCalls.toLocaleString(), icon: '📊' },
    { label: 'Photos Retrieved', value: stats.totalPhotosRetrieved.toLocaleString(), icon: '🖼️' },
    { label: 'Calls (24h)', value: stats.apiCallsLast24h.toLocaleString(), icon: '📈' },
    { label: 'Calls (7d)', value: stats.apiCallsLast7d.toLocaleString(), icon: '📉' },
    { label: 'Avg Response', value: `${stats.averageResponseTime}ms`, icon: '⚡' },
    { label: 'System Uptime', value: stats.systemUptime, icon: '🚀' },
  ]

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      {statCards.map((stat) => (
        <Card key={stat.label}>
          <CardContent className="p-6">
            <div className="flex items-center justify-between mb-2">
              <span className="text-2xl">{stat.icon}</span>
            </div>
            <div className="text-3xl font-bold text-slate-800 mb-1">
              {stat.value}
            </div>
            <div className="text-sm text-slate-600">{stat.label}</div>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
