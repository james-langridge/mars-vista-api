'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useState } from 'react';

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

function NavSection({ item, pathname }: { item: NavItem; pathname: string }) {
  const [isOpen, setIsOpen] = useState(true);
  const hasChildren = item.children && item.children.length > 0;

  return (
    <div className="mb-4">
      {hasChildren ? (
        <>
          <button
            onClick={() => setIsOpen(!isOpen)}
            className="flex items-center justify-between w-full text-left text-sm font-semibold text-slate-900 dark:text-white mb-2 hover:text-orange-600 dark:hover:text-orange-400"
          >
            {item.title}
            <svg
              className={`w-4 h-4 transition-transform ${isOpen ? 'rotate-90' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
            </svg>
          </button>
          {isOpen && item.children && (
            <ul className="space-y-1 ml-2 border-l border-slate-200 dark:border-slate-700">
              {item.children.map((child) => (
                <li key={child.href}>
                  <Link
                    href={child.href!}
                    className={`block pl-4 py-1.5 text-sm transition-colors ${
                      pathname === child.href
                        ? 'text-orange-600 dark:text-orange-400 font-medium border-l-2 border-orange-600 dark:border-orange-400 -ml-[1px]'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    {child.title}
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </>
      ) : (
        <Link
          href={item.href!}
          className={`block text-sm font-medium ${
            pathname === item.href
              ? 'text-orange-600 dark:text-orange-400'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          }`}
        >
          {item.title}
        </Link>
      )}
    </div>
  );
}

export default function DocsSidebar() {
  const pathname = usePathname();

  return (
    <nav className="w-64 flex-shrink-0 hidden lg:block">
      <div className="sticky top-20 overflow-y-auto max-h-[calc(100vh-5rem)] pb-10 pr-4">
        {navigation.map((item) => (
          <NavSection key={item.title} item={item} pathname={pathname} />
        ))}
      </div>
    </nav>
  );
}
