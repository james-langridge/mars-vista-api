import { NextRequest, NextResponse } from 'next/server'
import { cookies } from 'next/headers'

const API_BASE_URL = process.env.API_URL
const ADMIN_API_KEY = process.env.ADMIN_API_KEY

async function checkAuth(): Promise<boolean> {
  const cookieStore = await cookies()
  const session = cookieStore.get('admin_session')
  return session?.value === 'authenticated'
}

export async function GET(request: NextRequest) {
  if (!API_BASE_URL || !ADMIN_API_KEY) {
    console.error('API configuration missing. Set API_URL and ADMIN_API_KEY environment variables.')
    return NextResponse.json({ error: 'Server configuration error' }, { status: 500 })
  }

  if (!(await checkAuth())) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 })
  }

  const searchParams = request.nextUrl.searchParams.toString()
  const url = `${API_BASE_URL}/api/v2/photos${searchParams ? `?${searchParams}` : ''}`

  try {
    const response = await fetch(url, {
      headers: {
        'X-API-Key': ADMIN_API_KEY,
        'Content-Type': 'application/json',
      },
    })

    const data = await response.json()
    return NextResponse.json(data, { status: response.status })
  } catch (error) {
    console.error('Photos API proxy error:', error)
    return NextResponse.json({ error: 'Failed to fetch from API' }, { status: 500 })
  }
}
