import { auth } from '@/server/auth-export';
import { NextResponse } from 'next/server';

/**
 * POST /api/keys/generate
 * Generate a new API key for the authenticated user.
 *
 * Flow:
 * 1. Validate Auth.js session
 * 2. Extract user email from session
 * 3. Call C# internal API with X-Internal-Secret header
 * 4. Return API key to client (only time it's shown in plaintext)
 */
export async function POST() {
  // Validate Auth.js session
  const session = await auth();

  if (!session || !session.user?.email) {
    return NextResponse.json(
      { error: 'Unauthorized', message: 'Please sign in to generate an API key' },
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
    const response = await fetch(`${apiUrl}/api/v1/internal/keys/generate`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Internal-Secret': internalSecret,
      },
      body: JSON.stringify({ userEmail }),
    });

    const data = await response.json();

    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    // Success - return plaintext API key (only time it's visible)
    return NextResponse.json({
      apiKey: data.apiKey,
      tier: data.tier,
      createdAt: data.createdAt,
    }, { status: 200 });
  } catch (error) {
    console.error('Failed to generate API key:', error);
    return NextResponse.json(
      { error: 'Internal Server Error', message: 'Failed to communicate with API server' },
      { status: 500 }
    );
  }
}
