import { useEffect, useState } from 'react'
import {
  fetchStats,
  fetchUsers,
  fetchActivity,
  fetchViolations,
  fetchPerformanceMetrics,
  fetchEndpointUsage,
  fetchErrors,
} from '../utils/api'
import { logout } from '../utils/auth'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import StatsOverview from './StatsOverview'
import UserList from './UserList'
import ActivityLog from './ActivityLog'
import RateLimitViolations from './RateLimitViolations'
import PerformanceMetrics from './PerformanceMetrics'
import EndpointUsage from './EndpointUsage'
import ErrorTracking from './ErrorTracking'

export default function Dashboard() {
  const [stats, setStats] = useState<any>(null)
  const [users, setUsers] = useState<any[]>([])
  const [activities, setActivities] = useState<any[]>([])
  const [violations, setViolations] = useState<any[]>([])
  const [performanceMetrics, setPerformanceMetrics] = useState<any>(null)
  const [endpointUsage, setEndpointUsage] = useState<any>(null)
  const [errorData, setErrorData] = useState<any>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 30000) // Refresh every 30 seconds
    return () => clearInterval(interval)
  }, [])

  async function loadData() {
    try {
      const [
        statsData,
        usersData,
        activityData,
        violationsData,
        perfMetrics,
        endpointData,
        errorsData,
      ] = await Promise.all([
        fetchStats(),
        fetchUsers(),
        fetchActivity(50),
        fetchViolations(50),
        fetchPerformanceMetrics(),
        fetchEndpointUsage(),
        fetchErrors(50),
      ])

      setStats(statsData)
      setUsers(usersData.users)
      setActivities(activityData.events)
      setViolations(violationsData.violations)
      setPerformanceMetrics(perfMetrics)
      setEndpointUsage(endpointData)
      setErrorData(errorsData)
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    logout()
    window.location.reload()
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50">
      {/* Header */}
      <header className="bg-white border-b border-slate-200 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-slate-800">Mars Vista Admin</h1>
            <p className="text-sm text-slate-600">System Monitoring Dashboard</p>
          </div>
          <Button
            onClick={handleLogout}
            variant="outline"
          >
            Logout
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList>
            <TabsTrigger value="overview">📊 Overview</TabsTrigger>
            <TabsTrigger value="performance">⚡ Performance</TabsTrigger>
            <TabsTrigger value="endpoints">🎯 Endpoints</TabsTrigger>
            <TabsTrigger value="errors">🚨 Errors</TabsTrigger>
            <TabsTrigger value="users">👥 Users</TabsTrigger>
            <TabsTrigger value="activity">📝 Activity</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="space-y-8">
            {stats && <StatsOverview stats={stats} />}
            {violations.length > 0 && <RateLimitViolations violations={violations} />}
          </TabsContent>

          <TabsContent value="performance">
            {performanceMetrics && <PerformanceMetrics metrics={performanceMetrics} />}
          </TabsContent>

          <TabsContent value="endpoints">
            {endpointUsage && <EndpointUsage data={endpointUsage} />}
          </TabsContent>

          <TabsContent value="errors">
            {errorData && <ErrorTracking data={errorData} />}
          </TabsContent>

          <TabsContent value="users">
            {users.length > 0 && <UserList users={users} />}
          </TabsContent>

          <TabsContent value="activity">
            {activities.length > 0 && <ActivityLog activities={activities} />}
          </TabsContent>
        </Tabs>
      </main>
    </div>
  )
}
