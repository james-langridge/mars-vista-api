import Link from 'next/link';
import { auth } from '@/server/auth-export';

export default async function Header() {
  const session = await auth();

  return (
    <header className="border-b border-gray-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <Link href="/" className="text-xl font-bold hover:text-gray-300 transition-colors">
            Mars Vista
          </Link>

          <nav className="flex items-center gap-6">
            <Link href="/docs" className="hover:text-gray-300 transition-colors">
              Docs
            </Link>
            {session ? (
              <Link
                href="/dashboard"
                className="px-4 py-2 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors"
              >
                Dashboard
              </Link>
            ) : (
              <Link
                href="/signin"
                className="px-4 py-2 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors"
              >
                Sign In
              </Link>
            )}
          </nav>
        </div>
      </div>
    </header>
  );
}
