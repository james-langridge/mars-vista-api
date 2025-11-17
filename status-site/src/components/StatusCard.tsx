import type { Monitor } from '../types'

interface StatusCardProps {
  monitor: Monitor
}

export default function StatusCard({ monitor }: StatusCardProps) {
  const getStatusInfo = () => {
    switch (monitor.status) {
      case 2:
        return {
          label: 'Operational',
          color: 'bg-emerald-500',
          textColor: 'text-emerald-700',
          bgColor: 'bg-emerald-50',
          borderColor: 'border-emerald-200',
        }
      case 8:
        return {
          label: 'Degraded',
          color: 'bg-yellow-500',
          textColor: 'text-yellow-700',
          bgColor: 'bg-yellow-50',
          borderColor: 'border-yellow-200',
        }
      case 9:
        return {
          label: 'Down',
          color: 'bg-red-500',
          textColor: 'text-red-700',
          bgColor: 'bg-red-50',
          borderColor: 'border-red-200',
        }
      case 0:
        return {
          label: 'Paused',
          color: 'bg-slate-400',
          textColor: 'text-slate-700',
          bgColor: 'bg-slate-50',
          borderColor: 'border-slate-200',
        }
      default:
        return {
          label: 'Unknown',
          color: 'bg-slate-500',
          textColor: 'text-slate-700',
          bgColor: 'bg-slate-50',
          borderColor: 'border-slate-200',
        }
    }
  }

  const status = getStatusInfo()
  const checkInterval = Math.floor(monitor.interval / 60) // Convert seconds to minutes

  return (
    <div className={`bg-white border ${status.borderColor} rounded-xl shadow-sm hover:shadow-md transition-shadow duration-200 p-6`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4 flex-1">
          <div className={`w-3 h-3 rounded-full ${status.color} ${monitor.status === 2 ? '' : 'animate-pulse'}`} />
          <div className="flex-1">
            <h4 className="text-lg font-semibold text-slate-800">{monitor.friendly_name}</h4>
            <a
              href={monitor.url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-sm text-slate-500 hover:text-[#d14524] transition-colors"
            >
              {monitor.url}
            </a>
          </div>
        </div>
        <div className="text-right">
          <div className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${status.bgColor} ${status.textColor}`}>
            {status.label}
          </div>
          <div className="text-xs text-slate-500 mt-2">
            Checked every {checkInterval} min
          </div>
        </div>
      </div>
    </div>
  )
}
