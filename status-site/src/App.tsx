import { useEffect, useState } from 'react'
import StatusCard from './components/StatusCard'
import Header from './components/Header'
import IncidentLog from './components/IncidentLog'
import type { Monitor } from './types'

const UPTIME_ROBOT_API_KEY = import.meta.env.VITE_UPTIME_ROBOT_API_KEY || 'm801804144-145630aec910310422fdd04a'
const API_URL = 'https://api.uptimerobot.com/v2/getMonitors'

function App() {
  const [monitors, setMonitors] = useState<Monitor[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date())

  const fetchStatus = async () => {
    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          api_key: UPTIME_ROBOT_API_KEY,
          format: 'json',
          logs: 1,
          logs_limit: 10,
        }),
      })

      const data = await response.json()

      if (data.stat === 'ok') {
        setMonitors(data.monitors)
        setLastUpdate(new Date())
        setError(null)
      } else {
        setError('Failed to fetch status data')
      }
    } catch (err) {
      setError('Network error: Unable to fetch status')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchStatus()
    const interval = setInterval(fetchStatus, 60000) // Refresh every 60 seconds
    return () => clearInterval(interval)
  }, [])

  const getOverallStatus = (): { status: 'operational' | 'degraded' | 'down', message: string } => {
    if (monitors.length === 0) return { status: 'operational', message: 'All systems operational' }

    const hasDown = monitors.some(m => m.status === 9)
    const hasDegraded = monitors.some(m => m.status === 8)

    if (hasDown) return { status: 'down', message: 'Service disruption' }
    if (hasDegraded) return { status: 'degraded', message: 'Degraded performance' }
    return { status: 'operational', message: 'All systems operational' }
  }

  const overall = getOverallStatus()

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100">
      <Header />

      <main className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-[#d14524]"></div>
          </div>
        ) : error ? (
          <div className="bg-red-50 border border-red-200 text-red-700 px-6 py-4 rounded-lg">
            {error}
          </div>
        ) : (
          <div className="space-y-8">
            {/* Overall Status Banner */}
            <div className={`rounded-2xl shadow-lg overflow-hidden ${
              overall.status === 'operational' ? 'bg-gradient-to-r from-emerald-500 to-teal-500' :
              overall.status === 'degraded' ? 'bg-gradient-to-r from-yellow-500 to-orange-500' :
              'bg-gradient-to-r from-red-500 to-pink-500'
            }`}>
              <div className="px-8 py-10 text-white">
                <div className="flex items-center gap-3 mb-2">
                  <div className={`w-4 h-4 rounded-full ${
                    overall.status === 'operational' ? 'bg-white' :
                    overall.status === 'degraded' ? 'bg-white animate-pulse' :
                    'bg-white animate-pulse'
                  }`} />
                  <h2 className="text-3xl font-bold">{overall.message}</h2>
                </div>
                <p className="text-white/90 text-sm">
                  Last updated: {lastUpdate.toLocaleString()}
                </p>
              </div>
            </div>

            {/* Service Status Cards */}
            <div className="space-y-4">
              <h3 className="text-xl font-semibold text-slate-800">Services</h3>
              {monitors.map((monitor) => (
                <StatusCard key={monitor.id} monitor={monitor} />
              ))}
            </div>

            {/* Incident Log */}
            {monitors.length > 0 && monitors[0].logs && (
              <IncidentLog logs={monitors[0].logs} />
            )}

            {/* Footer Info */}
            <div className="text-center text-sm text-slate-500 pt-8 border-t border-slate-200">
              <p>
                Status page powered by{' '}
                <a href="https://uptimerobot.com" target="_blank" rel="noopener noreferrer" className="text-[#d14524] hover:underline">
                  UptimeRobot
                </a>
                {' '}&middot;{' '}
                <a href="https://api.marsvista.dev" target="_blank" rel="noopener noreferrer" className="text-[#d14524] hover:underline">
                  API Documentation
                </a>
              </p>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}

export default App
