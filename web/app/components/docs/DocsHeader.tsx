'use client';

import Link from 'next/link';
import DocsSearch from './DocsSearch';
import ThemeToggle from './ThemeToggle';

export default function DocsHeader() {
  return (
    <header className="sticky top-0 z-40 w-full bg-white/80 dark:bg-slate-900/80 backdrop-blur-sm border-b border-slate-200 dark:border-slate-700">
      <div className="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <div className="flex items-center gap-4">
            <Link href="/" className="text-xl font-bold text-slate-900 dark:text-white hover:text-orange-600 dark:hover:text-orange-400 transition-colors">
              Mars Vista
            </Link>
            <span className="hidden sm:inline-block px-2 py-0.5 text-xs font-medium bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 rounded">
              Docs
            </span>
          </div>

          {/* Right side */}
          <div className="flex items-center gap-2">
            <DocsSearch />
            <ThemeToggle />
            <Link
              href="/api-keys"
              className="hidden sm:inline-flex px-3 py-1.5 text-sm font-medium bg-orange-600 hover:bg-orange-700 text-white rounded-lg transition-colors"
            >
              Get API Key
            </Link>
          </div>
        </div>
      </div>
    </header>
  );
}
