import { auth } from '@/server/auth-export';
import { NextResponse } from 'next/server';

export async function POST() {
  const session = await auth();

  if (!session?.user?.email) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  try {
    // Call C# API internal endpoint to generate API key
    const response = await fetch(
      `${process.env.NEXT_PUBLIC_API_URL}/api/v1/internal/keys/generate`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Internal-Secret': process.env.INTERNAL_API_SECRET!,
        },
        body: JSON.stringify({
          userEmail: session.user.email,
        }),
      }
    );

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: 'Unknown error' }));
      console.error('Failed to generate API key:', errorData);

      if (response.status === 409) {
        return NextResponse.json(
          { error: 'API key already exists. Use regenerate instead.' },
          { status: 409 }
        );
      }

      return NextResponse.json(
        { error: errorData.error || 'Failed to generate API key' },
        { status: response.status }
      );
    }

    const data = await response.json();

    // Return the new API key (only time it's visible in plaintext)
    return NextResponse.json({
      apiKey: data.apiKey,
      tier: data.tier,
      createdAt: data.createdAt,
    });
  } catch (error) {
    console.error('Error generating API key:', error);
    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    );
  }
}
