import type { MonitorLog } from '../types'

interface IncidentLogProps {
  logs: MonitorLog[]
}

export default function IncidentLog({ logs }: IncidentLogProps) {
  const getLogTypeInfo = (type: number) => {
    switch (type) {
      case 1:
        return {
          label: 'Down',
          icon: '❌',
          color: 'text-red-600 dark:text-red-400',
          bgColor: 'bg-red-50 dark:bg-red-900/20',
          borderColor: 'border-red-200 dark:border-red-800',
        }
      case 2:
        return {
          label: 'Up',
          icon: '✅',
          color: 'text-emerald-600 dark:text-emerald-400',
          bgColor: 'bg-emerald-50 dark:bg-emerald-900/20',
          borderColor: 'border-emerald-200 dark:border-emerald-800',
        }
      case 99:
        return {
          label: 'Paused',
          icon: '⏸️',
          color: 'text-slate-600 dark:text-slate-400',
          bgColor: 'bg-slate-50 dark:bg-slate-800',
          borderColor: 'border-slate-200 dark:border-slate-700',
        }
      case 98:
        return {
          label: 'Started',
          icon: '▶️',
          color: 'text-blue-600 dark:text-blue-400',
          bgColor: 'bg-blue-50 dark:bg-blue-900/20',
          borderColor: 'border-blue-200 dark:border-blue-800',
        }
      default:
        return {
          label: 'Unknown',
          icon: '❓',
          color: 'text-slate-600 dark:text-slate-400',
          bgColor: 'bg-slate-50 dark:bg-slate-800',
          borderColor: 'border-slate-200 dark:border-slate-700',
        }
    }
  }

  const formatDuration = (seconds: number) => {
    if (seconds < 60) return `${seconds}s`
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${seconds % 60}s`
    return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`
  }

  const formatDate = (timestamp: number) => {
    const date = new Date(timestamp * 1000)
    return date.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    })
  }

  if (!logs || logs.length === 0) {
    return null
  }

  return (
    <div className="space-y-4">
      <h3 className="text-xl font-semibold text-slate-800 dark:text-white">Recent Activity</h3>
      <div className="bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-200 dark:border-slate-700 overflow-hidden">
        <div className="divide-y divide-slate-100 dark:divide-slate-700">
          {logs.map((log, index) => {
            const logInfo = getLogTypeInfo(log.type)
            return (
              <div
                key={index}
                className={`flex items-center gap-4 px-6 py-4 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors`}
              >
                <div className={`text-2xl`}>{logInfo.icon}</div>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className={`font-semibold ${logInfo.color}`}>
                      {logInfo.label}
                    </span>
                    {log.reason?.detail && (
                      <span className="text-slate-600 dark:text-slate-400">
                        - {log.reason.code} {log.reason.detail}
                      </span>
                    )}
                  </div>
                  <div className="text-sm text-slate-500 dark:text-slate-400 mt-1">
                    {formatDate(log.datetime)} · Duration: {formatDuration(log.duration)}
                  </div>
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
