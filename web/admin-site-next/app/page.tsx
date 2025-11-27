'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import {
  checkAuth,
  logout,
  fetchStats,
  fetchUsers,
  fetchActivity,
  fetchViolations,
  fetchPerformanceMetrics,
  fetchEndpointUsage,
  fetchErrors,
  fetchScraperStatus,
  fetchScraperMetrics,
} from '@/lib/api'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import {
  StatsOverview,
  RateLimitViolations,
  UserList,
  ActivityLog,
  PerformanceMetrics,
  EndpointUsage,
  ErrorTracking,
  ScraperMonitoring,
  PhotoSearch,
  NasaCompare,
} from '@/components/dashboard'
import type {
  Stats,
  User,
  Activity,
  Violation,
  PerformanceMetrics as PerformanceMetricsType,
  EndpointUsageData,
  ErrorData,
  ScraperStatus,
  ScraperMetrics,
} from '@/lib/types'

export default function DashboardPage() {
  const router = useRouter()
  const [authChecked, setAuthChecked] = useState(false)
  const [stats, setStats] = useState<Stats | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const [activities, setActivities] = useState<Activity[]>([])
  const [violations, setViolations] = useState<Violation[]>([])
  const [performanceMetrics, setPerformanceMetrics] = useState<PerformanceMetricsType | null>(null)
  const [endpointUsage, setEndpointUsage] = useState<EndpointUsageData | null>(null)
  const [errorData, setErrorData] = useState<ErrorData | null>(null)
  const [scraperStatus, setScraperStatus] = useState<ScraperStatus | null>(null)
  const [scraperMetrics, setScraperMetrics] = useState<ScraperMetrics | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function init() {
      const authenticated = await checkAuth()
      if (!authenticated) {
        router.push('/login')
        return
      }
      setAuthChecked(true)
      loadData()

      // Auto-refresh every 30 seconds
      const interval = setInterval(loadData, 30000)
      return () => clearInterval(interval)
    }
    init()
  }, [router])

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
        scraperStatusData,
        scraperMetricsData,
      ] = await Promise.all([
        fetchStats(),
        fetchUsers(),
        fetchActivity(50),
        fetchViolations(50),
        fetchPerformanceMetrics(),
        fetchEndpointUsage(),
        fetchErrors(50),
        fetchScraperStatus(),
        fetchScraperMetrics('7d'),
      ])

      setStats(statsData)
      setUsers(usersData.users)
      setActivities(activityData.events)
      setViolations(violationsData.violations)
      setPerformanceMetrics(perfMetrics)
      setEndpointUsage(endpointData)
      setErrorData(errorsData)
      setScraperStatus(scraperStatusData)
      setScraperMetrics(scraperMetricsData)
    } catch (error) {
      console.error('Failed to load data:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = async () => {
    await logout()
    router.push('/login')
  }

  if (!authChecked) {
    return (
      <div className="min-h-screen bg-slate-50 dark:bg-slate-900 flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-orange-500"></div>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
        <header className="bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 shadow-sm">
          <div className="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
            <div>
              <Skeleton className="h-8 w-48 mb-2" />
              <Skeleton className="h-4 w-64" />
            </div>
            <Skeleton className="h-10 w-24" />
          </div>
        </header>
        <main className="max-w-7xl mx-auto px-6 py-8">
          <div className="space-y-6">
            <Skeleton className="h-12 w-full" />
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
              {[...Array(8)].map((_, i) => (
                <Skeleton key={i} className="h-32" />
              ))}
            </div>
          </div>
        </main>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-900">
      {/* Header */}
      <header className="bg-white dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 shadow-sm">
        <div className="max-w-7xl mx-auto px-6 py-4 flex justify-between items-center">
          <div>
            <h1 className="text-2xl font-bold text-slate-800 dark:text-slate-200">Mars Vista Admin</h1>
            <p className="text-sm text-slate-600 dark:text-slate-400">System Monitoring Dashboard</p>
          </div>
          <Button onClick={handleLogout} variant="outline">
            Logout
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-6 py-8">
        <Tabs defaultValue="overview" className="space-y-6">
          <TabsList className="flex-wrap">
            <TabsTrigger value="overview">Overview</TabsTrigger>
            <TabsTrigger value="search">Search</TabsTrigger>
            <TabsTrigger value="scraper">Scraper</TabsTrigger>
            <TabsTrigger value="nasa-compare">NASA Compare</TabsTrigger>
            <TabsTrigger value="performance">Performance</TabsTrigger>
            <TabsTrigger value="endpoints">Endpoints</TabsTrigger>
            <TabsTrigger value="errors">Errors</TabsTrigger>
            <TabsTrigger value="users">Users</TabsTrigger>
            <TabsTrigger value="activity">Activity</TabsTrigger>
          </TabsList>

          <TabsContent value="overview" className="space-y-8">
            {stats && <StatsOverview stats={stats} />}
            {violations.length > 0 && <RateLimitViolations violations={violations} />}
          </TabsContent>

          <TabsContent value="search">
            <PhotoSearch />
          </TabsContent>

          <TabsContent value="nasa-compare">
            <NasaCompare />
          </TabsContent>

          <TabsContent value="scraper">
            {scraperStatus && scraperMetrics && (
              <ScraperMonitoring status={scraperStatus} metrics={scraperMetrics} />
            )}
          </TabsContent>

          <TabsContent value="performance">
            {performanceMetrics && <PerformanceMetrics metrics={performanceMetrics} />}
          </TabsContent>

          <TabsContent value="endpoints">{endpointUsage && <EndpointUsage data={endpointUsage} />}</TabsContent>

          <TabsContent value="errors">{errorData && <ErrorTracking data={errorData} />}</TabsContent>

          <TabsContent value="users">{users.length > 0 && <UserList users={users} />}</TabsContent>

          <TabsContent value="activity">{activities.length > 0 && <ActivityLog activities={activities} />}</TabsContent>
        </Tabs>
      </main>
    </div>
  )
}
