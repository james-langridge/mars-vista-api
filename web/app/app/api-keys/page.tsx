import { auth } from '@/server/auth-export';
import { redirect } from 'next/navigation';
import type { Metadata } from 'next';
import ApiKeyManager from '@/components/ApiKeyManager';
import AccountSettings from '@/components/AccountSettings';

export const metadata: Metadata = {
  title: 'API Keys - Mars Vista API',
  description: 'Manage your Mars Vista API keys',
};

export default async function ApiKeys() {
  const session = await auth();

  // Protect this page - redirect to signin if not authenticated
  if (!session || !session.user?.email) {
    redirect('/signin');
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-bold mb-2 text-slate-900 dark:text-white">API Keys</h1>
        <p className="text-slate-600 dark:text-slate-300">Manage your Mars Vista API keys</p>
      </div>

      <ApiKeyManager />

      <AccountSettings userEmail={session.user.email} />
    </div>
  );
}
