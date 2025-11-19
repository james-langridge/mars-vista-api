import { auth } from '@/server/auth-export';
import { NextResponse } from 'next/server';

export async function GET() {
  const session = await auth();

  if (!session?.user?.email) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  try {
    // Call C# API internal endpoint to get current API key
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/api/v1/internal/keys/current?user_email=${encodeURIComponent(session.user.email)}`,
      {
        headers: {
          'X-Internal-Secret': process.env.INTERNAL_API_SECRET!,
        },
      }
    );

    if (response.status === 404) {
      // User doesn't have an API key yet
      return NextResponse.json({ error: 'No API key found' }, { status: 404 });
    }

    if (!response.ok) {
      console.error('Failed to fetch API key:', await response.text());
      return NextResponse.json(
        { error: 'Failed to fetch API key' },
        { status: response.status }
      );
    }

    const data = await response.json();

    // Return the masked key info
    return NextResponse.json({
      maskedKey: data.masked_key,
      tier: data.tier,
      createdAt: data.created_at,
      lastUsedAt: data.last_used_at,
    });
  } catch (error) {
    console.error('Error fetching current API key:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}
