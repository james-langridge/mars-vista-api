'use client';

import { useEffect, useState } from 'react';
import { usePathname } from 'next/navigation';

interface Heading {
  id: string;
  text: string;
}

// Extract direct text content, skipping child elements (like step number spans)
function getDirectTextContent(element: Element): string {
  let text = '';
  element.childNodes.forEach((node) => {
    if (node.nodeType === Node.TEXT_NODE) {
      text += node.textContent;
    }
  });
  return text.trim();
}

export default function OnThisPage() {
  const pathname = usePathname();
  const [headings, setHeadings] = useState<Heading[]>([]);
  const [activeId, setActiveId] = useState<string>('');

  useEffect(() => {
    // Small delay to ensure DOM is updated after navigation
    const timer = setTimeout(() => {
      // Only show h2 headings (main sections, not subsections or card titles)
      const elements = document.querySelectorAll('main h2');
      const items: Heading[] = [];

      elements.forEach((element) => {
        // Get text, preferring direct text nodes over full textContent
        // This skips step numbers in spans, etc.
        const text = getDirectTextContent(element) || element.textContent || '';

        if (!text) return;

        // Generate ID if not present
        if (!element.id) {
          element.id = text
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/(^-|-$)/g, '');
        }

        items.push({
          id: element.id,
          text,
        });
      });

      setHeadings(items);
      setActiveId('');
    }, 100);

    return () => clearTimeout(timer);
  }, [pathname]);

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
                className={`block py-1 pl-4 transition-colors ${
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
