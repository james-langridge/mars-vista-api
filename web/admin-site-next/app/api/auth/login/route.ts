import { NextRequest, NextResponse } from 'next/server'
import { cookies } from 'next/headers'

// Server-side only credentials - must be set via environment variables
const ADMIN_EMAIL = process.env.ADMIN_EMAIL
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD

export async function POST(request: NextRequest) {
  // Ensure credentials are configured
  if (!ADMIN_EMAIL || !ADMIN_PASSWORD) {
    console.error('Admin credentials not configured. Set ADMIN_EMAIL and ADMIN_PASSWORD environment variables.')
    return NextResponse.json({ success: false, error: 'Server configuration error' }, { status: 500 })
  }

  try {
    const body = await request.json()
    const { email, password } = body

    if (email === ADMIN_EMAIL && password === ADMIN_PASSWORD) {
      // Set a secure HTTP-only cookie for authentication
      const cookieStore = await cookies()
      cookieStore.set('admin_session', 'authenticated', {
        httpOnly: true,
        secure: process.env.NODE_ENV === 'production',
        sameSite: 'lax',
        maxAge: 60 * 60 * 24, // 24 hours
        path: '/',
      })

      return NextResponse.json({ success: true })
    }

    return NextResponse.json({ success: false, error: 'Invalid credentials' }, { status: 401 })
  } catch {
    return NextResponse.json({ success: false, error: 'Invalid request' }, { status: 400 })
  }
}
