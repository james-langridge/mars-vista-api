import { auth } from '@/server/auth-export';
import { redirect } from 'next/navigation';
import type { Metadata } from 'next';
import ApiKeyManager from '@/components/ApiKeyManager';

export const metadata: Metadata = {
  title: 'Dashboard - Mars Vista API',
  description: 'Manage your Mars Vista API keys and usage',
};

export default async function Dashboard() {
  const session = await auth();

  // Protect this page - redirect to signin if not authenticated
  if (!session) {
    redirect('/signin');
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
        <p className="text-gray-300">
          Welcome to your Mars Vista API dashboard, {session?.user?.name || session?.user?.email}
        </p>
      </div>

      <ApiKeyManager />
    </div>
  );
}
