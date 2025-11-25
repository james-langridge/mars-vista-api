'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { useRouter } from 'next/navigation';

interface SearchResult {
  title: string;
  href: string;
  section: string;
  content: string;
}

// Static search index - in production, this would be generated at build time
const searchIndex: SearchResult[] = [
  { title: 'Introduction', href: '/docs', section: 'Getting Started', content: 'Mars Vista API documentation access rover imagery' },
  { title: 'Quick Start', href: '/docs/quickstart', section: 'Getting Started', content: 'Make your first API request in 5 minutes get started quickly' },
  { title: 'Authentication', href: '/docs/authentication', section: 'Getting Started', content: 'API key authentication rate limits security' },
  { title: 'Understanding Mars Time', href: '/docs/guides/mars-time', section: 'Guides', content: 'sols Earth dates Martian day golden hour sunrise sunset' },
  { title: 'Image Sizes', href: '/docs/guides/image-sizes', section: 'Guides', content: 'small medium large full thumbnail progressive loading dimensions' },
  { title: 'Rate Limits & Quotas', href: '/docs/guides/rate-limits', section: 'Guides', content: 'rate limiting quotas 10000 requests per hour optimization' },
  { title: 'Filtering & Pagination', href: '/docs/guides/filtering', section: 'Guides', content: 'query parameters filters sorting pagination page per_page' },
  { title: 'Photos Reference', href: '/docs/reference/photos', section: 'API Reference', content: 'GET /api/v2/photos query parameters response format' },
  { title: 'Rovers Reference', href: '/docs/reference/rovers', section: 'API Reference', content: 'GET /api/v2/rovers curiosity perseverance opportunity spirit' },
  { title: 'Cameras Reference', href: '/docs/reference/cameras', section: 'API Reference', content: 'NAVCAM FHAZ RHAZ MAST CHEMCAM cameras list' },
  { title: 'Error Handling', href: '/docs/reference/errors', section: 'API Reference', content: '400 401 404 429 500 error codes troubleshooting' },
  { title: 'Troubleshooting', href: '/docs/troubleshooting', section: 'Resources', content: 'common issues problems empty response CORS' },
  { title: 'API v1 (Legacy)', href: '/docs/v1', section: 'Resources', content: 'legacy compatibility migration mars-photo-api' },
];

export default function DocsSearch() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResult[]>([]);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);
  const router = useRouter();

  // Search function
  const search = useCallback((q: string) => {
    if (!q.trim()) {
      setResults([]);
      return;
    }

    const terms = q.toLowerCase().split(' ').filter(Boolean);
    const scored = searchIndex
      .map((item) => {
        const searchText = `${item.title} ${item.section} ${item.content}`.toLowerCase();
        let score = 0;
        for (const term of terms) {
          if (item.title.toLowerCase().includes(term)) score += 10;
          if (item.section.toLowerCase().includes(term)) score += 5;
          if (item.content.toLowerCase().includes(term)) score += 1;
        }
        return { ...item, score };
      })
      .filter((item) => item.score > 0)
      .sort((a, b) => b.score - a.score)
      .slice(0, 8);

    setResults(scored);
    setSelectedIndex(0);
  }, []);

  useEffect(() => {
    search(query);
  }, [query, search]);

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Open search with Cmd/Ctrl + K
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        setIsOpen(true);
      }
      // Close with Escape
      if (e.key === 'Escape') {
        setIsOpen(false);
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, []);

  // Focus input when opening
  useEffect(() => {
    if (isOpen && inputRef.current) {
      inputRef.current.focus();
    }
  }, [isOpen]);

  // Handle navigation
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.min(prev + 1, results.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setSelectedIndex((prev) => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter' && results[selectedIndex]) {
      router.push(results[selectedIndex].href);
      setIsOpen(false);
      setQuery('');
    }
  };

  const handleSelect = (result: SearchResult) => {
    router.push(result.href);
    setIsOpen(false);
    setQuery('');
  };

  return (
    <>
      {/* Search trigger button */}
      <button
        onClick={() => setIsOpen(true)}
        className="flex items-center gap-2 px-3 py-1.5 text-sm text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-800 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
      >
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <span className="hidden sm:inline">Search docs...</span>
        <kbd className="hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 text-xs bg-slate-200 dark:bg-slate-700 rounded">
          <span className="text-xs">⌘</span>K
        </kbd>
      </button>

      {/* Modal backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 z-50 bg-black/50"
          onClick={() => setIsOpen(false)}
        />
      )}

      {/* Search modal */}
      {isOpen && (
        <div className="fixed inset-x-4 top-20 z-50 mx-auto max-w-xl">
          <div className="bg-white dark:bg-slate-900 rounded-xl shadow-2xl border border-slate-200 dark:border-slate-700 overflow-hidden">
            {/* Search input */}
            <div className="flex items-center px-4 border-b border-slate-200 dark:border-slate-700">
              <svg className="w-5 h-5 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                ref={inputRef}
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Search documentation..."
                className="flex-1 px-4 py-4 bg-transparent text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none"
              />
              <kbd className="px-2 py-1 text-xs bg-slate-100 dark:bg-slate-800 text-slate-500 rounded">
                ESC
              </kbd>
            </div>

            {/* Results */}
            <div className="max-h-96 overflow-y-auto">
              {results.length > 0 ? (
                <ul className="py-2">
                  {results.map((result, index) => (
                    <li key={result.href}>
                      <button
                        onClick={() => handleSelect(result)}
                        className={`w-full px-4 py-3 text-left flex items-center gap-3 ${
                          index === selectedIndex
                            ? 'bg-orange-50 dark:bg-orange-900/20'
                            : 'hover:bg-slate-50 dark:hover:bg-slate-800'
                        }`}
                      >
                        <div className="flex-1">
                          <div className="font-medium text-slate-900 dark:text-white">
                            {result.title}
                          </div>
                          <div className="text-sm text-slate-500 dark:text-slate-400">
                            {result.section}
                          </div>
                        </div>
                        <svg
                          className={`w-4 h-4 text-slate-400 ${
                            index === selectedIndex ? 'text-orange-500' : ''
                          }`}
                          fill="none"
                          stroke="currentColor"
                          viewBox="0 0 24 24"
                        >
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                        </svg>
                      </button>
                    </li>
                  ))}
                </ul>
              ) : query ? (
                <div className="px-4 py-8 text-center text-slate-500 dark:text-slate-400">
                  No results for &quot;{query}&quot;
                </div>
              ) : (
                <div className="px-4 py-8 text-center text-slate-500 dark:text-slate-400">
                  Type to search documentation...
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="px-4 py-2 border-t border-slate-200 dark:border-slate-700 text-xs text-slate-500 dark:text-slate-400 flex items-center gap-4">
              <span className="flex items-center gap-1">
                <kbd className="px-1.5 py-0.5 bg-slate-100 dark:bg-slate-800 rounded">↑</kbd>
                <kbd className="px-1.5 py-0.5 bg-slate-100 dark:bg-slate-800 rounded">↓</kbd>
                to navigate
              </span>
              <span className="flex items-center gap-1">
                <kbd className="px-1.5 py-0.5 bg-slate-100 dark:bg-slate-800 rounded">Enter</kbd>
                to select
              </span>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
