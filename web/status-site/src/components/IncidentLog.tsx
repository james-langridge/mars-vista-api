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
          color: 'text-red-600',
          bgColor: 'bg-red-50',
          borderColor: 'border-red-200',
        }
      case 2:
        return {
          label: 'Up',
          icon: '✅',
          color: 'text-emerald-600',
          bgColor: 'bg-emerald-50',
          borderColor: 'border-emerald-200',
        }
      case 99:
        return {
          label: 'Paused',
          icon: '⏸️',
          color: 'text-slate-600',
          bgColor: 'bg-slate-50',
          borderColor: 'border-slate-200',
        }
      case 98:
        return {
          label: 'Started',
          icon: '▶️',
          color: 'text-blue-600',
          bgColor: 'bg-blue-50',
          borderColor: 'border-blue-200',
        }
      default:
        return {
          label: 'Unknown',
          icon: '❓',
          color: 'text-slate-600',
          bgColor: 'bg-slate-50',
          borderColor: 'border-slate-200',
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
      <h3 className="text-xl font-semibold text-slate-800">Recent Activity</h3>
      <div className="bg-white rounded-xl shadow-sm border border-slate-200 overflow-hidden">
        <div className="divide-y divide-slate-100">
          {logs.map((log, index) => {
            const logInfo = getLogTypeInfo(log.type)
            return (
              <div
                key={index}
                className={`flex items-center gap-4 px-6 py-4 hover:bg-slate-50 transition-colors`}
              >
                <div className={`text-2xl`}>{logInfo.icon}</div>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <span className={`font-semibold ${logInfo.color}`}>
                      {logInfo.label}
                    </span>
                    {log.reason?.detail && (
                      <span className="text-slate-600">
                        - {log.reason.code} {log.reason.detail}
                      </span>
                    )}
                  </div>
                  <div className="text-sm text-slate-500 mt-1">
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
