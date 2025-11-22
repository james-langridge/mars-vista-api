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

      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700 max-w-sm">
        <h3 className="text-sm font-medium text-gray-400 mb-2">Rate Limits</h3>
        <p className="text-2xl font-bold">Free for All Users</p>
        <p className="text-sm text-gray-400 mt-2">10,000 req/hour, 100,000 req/day</p>
      </div>

      <ApiKeyManager />

      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-bold mb-4">Get Started</h2>
        <div className="space-y-4">
          <div className="flex items-start gap-4">
            <div className="bg-red-600 rounded-full w-8 h-8 flex items-center justify-center flex-shrink-0">
              1
            </div>
            <div>
              <h3 className="font-semibold mb-1">Generate an API Key</h3>
              <p className="text-gray-400 text-sm">
                Create your first API key above to start making requests.
              </p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="bg-gray-600 rounded-full w-8 h-8 flex items-center justify-center flex-shrink-0">
              2
            </div>
            <div>
              <h3 className="font-semibold mb-1">Make Your First Request</h3>
              <p className="text-gray-400 text-sm">
                Use your API key to fetch Mars rover photos from our comprehensive database.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
