export interface MonitorLog {
  type: number // 1 = down, 2 = up, 99 = paused, 98 = started
  datetime: number // Unix timestamp
  duration: number // In seconds
  reason?: {
    code?: string
    detail?: string
  }
}

export interface Monitor {
  id: number
  friendly_name: string
  url: string
  type: number
  status: number // 0 = paused, 1 = not checked yet, 2 = up, 8 = seems down, 9 = down
  interval: number
  logs?: MonitorLog[]
  create_datetime: number
}

export interface UptimeRobotResponse {
  stat: string
  pagination: {
    offset: number
    limit: number
    total: number
  }
  monitors: Monitor[]
}
