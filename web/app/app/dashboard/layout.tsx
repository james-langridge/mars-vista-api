import { auth } from '@/server/auth-export';
import { redirect } from 'next/navigation';
import Link from 'next/link';
import SignOutButton from '@/components/SignOutButton';

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect('/signin');
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex flex-col md:flex-row gap-8">
        <aside className="md:w-64 flex-shrink-0">
          <div className="bg-gray-800 rounded-lg p-6 border border-gray-700 sticky top-8">
            <div className="mb-6">
              <p className="text-sm text-gray-400">Signed in as</p>
              <p className="font-medium truncate">{session.user.email}</p>
            </div>

            <nav className="space-y-2">
              <Link
                href="/dashboard"
                className="block px-4 py-2 rounded-lg hover:bg-gray-700 transition-colors"
              >
                Overview
              </Link>
              <Link
                href="/dashboard/api-keys"
                className="block px-4 py-2 rounded-lg hover:bg-gray-700 transition-colors text-gray-400"
              >
                API Keys
                <span className="ml-2 text-xs bg-gray-700 px-2 py-0.5 rounded">Soon</span>
              </Link>
              <Link
                href="/dashboard/usage"
                className="block px-4 py-2 rounded-lg hover:bg-gray-700 transition-colors text-gray-400"
              >
                Usage
                <span className="ml-2 text-xs bg-gray-700 px-2 py-0.5 rounded">Soon</span>
              </Link>
            </nav>

            <div className="mt-6 pt-6 border-t border-gray-700">
              <SignOutButton />
            </div>
          </div>
        </aside>

        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}
