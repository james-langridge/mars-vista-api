'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState, useEffect } from 'react';

interface NavItem {
  title: string;
  href?: string;
  children?: NavItem[];
}

const navigation: NavItem[] = [
  {
    title: 'Getting Started',
    children: [
      { title: 'Introduction', href: '/docs' },
      { title: 'Quick Start', href: '/docs/quickstart' },
      { title: 'Authentication', href: '/docs/authentication' },
      { title: 'API Explorer', href: '/docs/explorer' },
    ],
  },
  {
    title: 'Guides',
    children: [
      { title: 'Understanding Mars Time', href: '/docs/guides/mars-time' },
      { title: 'Image Sizes', href: '/docs/guides/image-sizes' },
      { title: 'Rate Limits & Quotas', href: '/docs/guides/rate-limits' },
      { title: 'Filtering & Pagination', href: '/docs/guides/filtering' },
    ],
  },
  {
    title: 'API Reference',
    children: [
      { title: 'Photos', href: '/docs/reference/photos' },
      { title: 'Rovers', href: '/docs/reference/rovers' },
      { title: 'Cameras', href: '/docs/reference/cameras' },
      { title: 'Errors', href: '/docs/reference/errors' },
    ],
  },
  {
    title: 'Resources',
    children: [
      { title: 'Troubleshooting', href: '/docs/troubleshooting' },
      { title: 'API v1 (Legacy)', href: '/docs/v1' },
      { title: 'AI Agent Docs', href: '/docs/llm/reference.md' },
    ],
  },
];

export default function MobileDocsSidebar() {
  const [isOpen, setIsOpen] = useState(false);
  const pathname = usePathname();

  // Close sidebar on navigation
  useEffect(() => {
    setIsOpen(false);
  }, [pathname]);

  // Prevent body scroll when sidebar is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = '';
    }
    return () => {
      document.body.style.overflow = '';
    };
  }, [isOpen]);

  return (
    <>
      {/* Mobile menu button */}
      <button
        onClick={() => setIsOpen(true)}
        className="lg:hidden fixed bottom-4 right-4 z-40 bg-orange-600 hover:bg-orange-700 text-white p-3 rounded-full shadow-lg transition-colors"
        aria-label="Open navigation"
      >
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>

      {/* Backdrop */}
      {isOpen && (
        <div
          className="lg:hidden fixed inset-0 z-40 bg-black/50"
          onClick={() => setIsOpen(false)}
        />
      )}

      {/* Sidebar */}
      <div
        className={`lg:hidden fixed inset-y-0 left-0 z-50 w-72 bg-white dark:bg-slate-900 shadow-xl transform transition-transform duration-300 ease-in-out ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="flex items-center justify-between p-4 border-b border-slate-200 dark:border-slate-700">
          <span className="text-lg font-semibold text-slate-900 dark:text-white">Documentation</span>
          <button
            onClick={() => setIsOpen(false)}
            className="p-2 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-white"
            aria-label="Close navigation"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <nav className="p-4 overflow-y-auto max-h-[calc(100vh-5rem)]">
          {navigation.map((section) => (
            <div key={section.title} className="mb-6">
              <h3 className="text-sm font-semibold text-slate-900 dark:text-white mb-2">
                {section.title}
              </h3>
              {section.children && (
                <ul className="space-y-1">
                  {section.children.map((item) => (
                    <li key={item.href}>
                      <Link
                        href={item.href!}
                        className={`block py-2 px-3 text-sm rounded-md transition-colors ${
                          pathname === item.href
                            ? 'bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 font-medium'
                            : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'
                        }`}
                      >
                        {item.title}
                      </Link>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ))}
        </nav>
      </div>
    </>
  );
}
