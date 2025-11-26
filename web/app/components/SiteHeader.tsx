'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import DocsSearch from './docs/DocsSearch';
import ThemeToggle from './docs/ThemeToggle';

interface SiteHeaderProps {
  isAuthenticated: boolean;
}

export default function SiteHeader({ isAuthenticated }: SiteHeaderProps) {
  const pathname = usePathname();
  const isDocsPage = pathname?.startsWith('/docs');

  return (
    <header className="sticky top-0 z-40 w-full bg-white/80 dark:bg-slate-900/80 backdrop-blur-sm border-b border-slate-200 dark:border-slate-700">
      <div className="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex items-center gap-4">
            <Link
              href="/"
              className="text-xl font-bold text-slate-900 dark:text-white hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
            >
              Mars Vista
            </Link>
            {isDocsPage && (
              <span className="hidden sm:inline-block px-2 py-0.5 text-xs font-medium bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 rounded">
                Docs
              </span>
            )}
          </div>

          {/* Navigation */}
          <nav className="flex items-center gap-4">
            <Link
              href="/docs"
              className={`text-sm font-medium transition-colors ${
                isDocsPage
                  ? 'text-orange-600 dark:text-orange-400'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
              }`}
            >
              Docs
            </Link>
            <a
              href="https://api.marsvista.dev/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="hidden sm:inline-block text-sm font-medium text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors"
            >
              API
            </a>
          </nav>

          {/* Right side */}
          <div className="flex items-center gap-2">
            {isDocsPage && <DocsSearch />}
            <ThemeToggle />
            {isAuthenticated ? (
              <Link
                href="/api-keys"
                className="hidden sm:inline-flex px-3 py-1.5 text-sm font-medium bg-orange-600 hover:bg-orange-700 text-white rounded-lg transition-colors"
              >
                API Keys
              </Link>
            ) : (
              <Link
                href="/signin"
                className="hidden sm:inline-flex px-3 py-1.5 text-sm font-medium bg-orange-600 hover:bg-orange-700 text-white rounded-lg transition-colors"
              >
                Sign In
              </Link>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
