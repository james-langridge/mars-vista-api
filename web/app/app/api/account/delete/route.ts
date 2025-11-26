import { auth } from '@/server/auth-export';
import { db } from '@/server/db';
import { NextResponse } from 'next/server';

/**
 * DELETE /api/account/delete
 * Delete the authenticated user's account and all associated data.
 *
 * Flow:
 * 1. Validate Auth.js session
 * 2. Call C# internal API to delete API key from photos database
 * 3. Delete user from Auth.js database (cascades to sessions)
 * 4. Return success (client should redirect to home)
 */
export async function DELETE() {
  // Validate Auth.js session
  const session = await auth();

  if (!session || !session.user?.email || !session.user?.id) {
    return NextResponse.json(
      { error: 'Unauthorized', message: 'Please sign in to delete your account' },
      { status: 401 }
    );
  }

  const userEmail = session.user.email;
  const userId = session.user.id;
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
    // Step 1: Delete API key from photos database via C# internal API
    const apiResponse = await fetch(`${apiUrl}/api/v1/internal/keys/delete`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'X-Internal-Secret': internalSecret,
      },
      body: JSON.stringify({ userEmail }),
    });

    if (!apiResponse.ok) {
      const errorData = await apiResponse.json();
      console.error('Failed to delete API key:', errorData);
      // Continue with user deletion even if API key deletion fails
      // (user might not have an API key)
    }

    // Step 2: Delete user from Auth.js database
    // This cascades to delete sessions due to the Prisma schema relation
    await db.user.delete({
      where: { id: userId },
    });

    return NextResponse.json({
      success: true,
      message: 'Account deleted successfully',
    });
  } catch (error) {
    console.error('Failed to delete account:', error);
    return NextResponse.json(
      { error: 'Internal Server Error', message: 'Failed to delete account' },
      { status: 500 }
    );
  }
}
