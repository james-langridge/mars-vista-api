'use client';

import { useEffect, useState } from 'react';

interface Heading {
  id: string;
  text: string;
  level: number;
}

export default function OnThisPage() {
  const [headings, setHeadings] = useState<Heading[]>([]);
  const [activeId, setActiveId] = useState<string>('');

  useEffect(() => {
    // Find all h2 and h3 elements in the main content
    const elements = document.querySelectorAll('main h2, main h3');
    const items: Heading[] = [];

    elements.forEach((element) => {
      // Generate ID if not present
      if (!element.id) {
        element.id = element.textContent
          ?.toLowerCase()
          .replace(/[^a-z0-9]+/g, '-')
          .replace(/(^-|-$)/g, '') || '';
      }

      items.push({
        id: element.id,
        text: element.textContent || '',
        level: element.tagName === 'H2' ? 2 : 3,
      });
    });

    setHeadings(items);
  }, []);

  useEffect(() => {
    if (headings.length === 0) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setActiveId(entry.target.id);
          }
        });
      },
      { rootMargin: '-80px 0px -80% 0px' }
    );

    headings.forEach((heading) => {
      const element = document.getElementById(heading.id);
      if (element) observer.observe(element);
    });

    return () => observer.disconnect();
  }, [headings]);

  if (headings.length === 0) return null;

  return (
    <nav className="w-56 flex-shrink-0 hidden xl:block">
      <div className="sticky top-20 overflow-y-auto max-h-[calc(100vh-5rem)] pb-10">
        <h4 className="text-sm font-semibold text-slate-900 dark:text-white mb-3">
          On this page
        </h4>
        <ul className="space-y-2 text-sm border-l border-slate-200 dark:border-slate-700">
          {headings.map((heading) => (
            <li key={heading.id}>
              <a
                href={`#${heading.id}`}
                className={`block py-1 transition-colors ${
                  heading.level === 3 ? 'pl-6' : 'pl-4'
                } ${
                  activeId === heading.id
                    ? 'text-orange-600 dark:text-orange-400 border-l-2 border-orange-600 dark:border-orange-400 -ml-[1px] font-medium'
                    : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                }`}
              >
                {heading.text}
              </a>
            </li>
          ))}
        </ul>
      </div>
    </nav>
  );
}
