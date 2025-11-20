import { auth } from '@/server/auth-export';
import { NextResponse } from 'next/server';

/**
 * GET /api/keys/current
 * Get current API key information (masked) for the authenticated user.
 *
 * Flow:
 * 1. Validate Auth.js session
 * 2. Extract user email from session
 * 3. Call C# internal API with X-Internal-Secret header
 * 4. Return masked API key info
 */
export async function GET() {
  // Validate Auth.js session
  const session = await auth();

  if (!session || !session.user?.email) {
    return NextResponse.json(
      { error: 'Unauthorized', message: 'Please sign in to view your API key' },
      { status: 401 }
    );
  }

  const userEmail = session.user.email;
  const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5127';
  const internalSecret = process.env.INTERNAL_API_SECRET;

  if (!internalSecret) {
    console.error('INTERNAL_API_SECRET not configured');
    return NextResponse.json(
      { error: 'Internal Server Error', message: 'Server configuration error' },
      { status: 500 }
    );
  }

  try {
    // Call C# internal API
    const response = await fetch(
      `${apiUrl}/api/v1/internal/keys/current?userEmail=${encodeURIComponent(userEmail)}`,
      {
        method: 'GET',
        headers: {
          'X-Internal-Secret': internalSecret,
        },
      }
    );

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    // Success - map C# API fields to frontend format
    return NextResponse.json({
      maskedKey: data.keyPreview,
      tier: data.tier,
      createdAt: data.createdAt,
      lastUsedAt: data.lastUsedAt,
      isActive: data.isActive,
    }, { status: 200 });
  } catch (error) {
    console.error('Failed to fetch API key info:', error);
    return NextResponse.json(
      { error: 'Internal Server Error', message: 'Failed to communicate with API server' },
      { status: 500 }
    );
  }
}
