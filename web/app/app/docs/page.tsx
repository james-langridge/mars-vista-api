import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Documentation - Mars Vista API',
  description: 'Learn how to use the Mars Vista API to access Mars rover imagery',
};

const learningPath = [
  {
    step: 1,
    title: 'Quick Start',
    description: 'Make your first API request in under 5 minutes',
    href: '/docs/quickstart',
    time: '5 min',
  },
  {
    step: 2,
    title: 'Authentication',
    description: 'Get your API key and understand rate limits',
    href: '/docs/authentication',
    time: '3 min',
  },
  {
    step: 3,
    title: 'Query Photos',
    description: 'Learn to filter, sort, and paginate results',
    href: '/docs/reference/photos',
    time: '10 min',
  },
  {
    step: 4,
    title: 'Explore Guides',
    description: 'Deep-dive into Mars time, image sizes, and more',
    href: '/docs/guides/mars-time',
    time: '15 min',
  },
];

const features = [
  {
    title: '4 Mars Rovers',
    description: 'Curiosity, Perseverance, Opportunity, and Spirit',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ),
  },
  {
    title: '600K+ Photos',
    description: 'Complete NASA Mars rover photo archives',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    ),
  },
  {
    title: 'Daily Updates',
    description: 'Fresh photos from active missions every day',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
      </svg>
    ),
  },
  {
    title: 'Rich Metadata',
    description: 'Location, camera angles, Mars time, and more',
    icon: (
      <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
    ),
  },
];

export default function DocsLanding() {
  return (
    <div>
      {/* Hero */}
      <div className="mb-12">
        <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
          Mars Vista API Documentation
        </h1>
        <p className="text-xl text-slate-600 dark:text-slate-400 max-w-3xl">
          Access comprehensive Mars rover imagery from NASA missions. Query photos by date,
          sol, camera, location, and more with our powerful REST API.
        </p>
      </div>

      {/* Quick links */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-12">
        {features.map((feature) => (
          <div
            key={feature.title}
            className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4"
          >
            <div className="text-orange-600 dark:text-orange-400 mb-2">
              {feature.icon}
            </div>
            <h3 className="font-semibold text-slate-900 dark:text-white">
              {feature.title}
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              {feature.description}
            </p>
          </div>
        ))}
      </div>

      {/* Learning path */}
      <div className="mb-12">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6">
          Learning Path
        </h2>
        <div className="space-y-4">
          {learningPath.map((item) => (
            <Link
              key={item.step}
              href={item.href}
              className="flex items-center gap-4 p-4 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg hover:border-orange-500 dark:hover:border-orange-500 transition-colors group"
            >
              <div className="flex-shrink-0 w-10 h-10 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full flex items-center justify-center font-bold">
                {item.step}
              </div>
              <div className="flex-grow">
                <h3 className="font-semibold text-slate-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400">
                  {item.title}
                </h3>
                <p className="text-sm text-slate-600 dark:text-slate-400">
                  {item.description}
                </p>
              </div>
              <div className="flex-shrink-0 text-sm text-slate-500 dark:text-slate-500">
                {item.time}
              </div>
              <svg
                className="w-5 h-5 text-slate-400 group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
            </Link>
          ))}
        </div>
      </div>

      {/* API Versions */}
      <div className="mb-12">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6">
          API Versions
        </h2>
        <div className="grid md:grid-cols-2 gap-6">
          <div className="border-2 border-orange-500 bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
            <div className="flex items-center gap-2 mb-3">
              <span className="px-2 py-1 bg-orange-500 text-white text-xs font-bold rounded">
                RECOMMENDED
              </span>
            </div>
            <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-2">
              API v2
            </h3>
            <p className="text-slate-600 dark:text-slate-400 mb-4">
              Modern REST API with powerful filtering, nested resources, Mars time queries,
              location-based search, and multiple image sizes.
            </p>
            <Link
              href="/docs/reference/photos"
              className="text-orange-600 dark:text-orange-400 font-medium hover:underline"
            >
              View v2 Reference &rarr;
            </Link>
          </div>
          <div className="border border-slate-200 dark:border-slate-700 rounded-lg p-6">
            <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-2">
              API v1 (Legacy)
            </h3>
            <p className="text-slate-600 dark:text-slate-400 mb-4">
              Drop-in replacement for the{' '}
              <a
                href="https://github.com/corincerami/mars-photo-api"
                target="_blank"
                rel="noopener noreferrer"
                className="text-orange-600 dark:text-orange-400 hover:underline"
              >
                archived NASA Mars Rover API
              </a>
              . Use for existing integrations.
            </p>
            <Link
              href="/docs/v1"
              className="text-orange-600 dark:text-orange-400 font-medium hover:underline"
            >
              View v1 Reference &rarr;
            </Link>
          </div>
        </div>
      </div>

      {/* Resources */}
      <div>
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-6">
          Additional Resources
        </h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <a
            href="https://api.marsvista.dev/swagger"
            target="_blank"
            rel="noopener noreferrer"
            className="p-4 bg-slate-50 dark:bg-slate-800 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
          >
            <h3 className="font-semibold text-slate-900 dark:text-white mb-1">
              Swagger UI
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Interactive API explorer
            </p>
          </a>
          <Link
            href="/docs/llm/reference.md"
            className="p-4 bg-slate-50 dark:bg-slate-800 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
          >
            <h3 className="font-semibold text-slate-900 dark:text-white mb-1">
              AI Agent Reference
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Plain markdown for LLMs
            </p>
          </Link>
          <a
            href="https://github.com/james-langridge/mars-vista-api"
            target="_blank"
            rel="noopener noreferrer"
            className="p-4 bg-slate-50 dark:bg-slate-800 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
          >
            <h3 className="font-semibold text-slate-900 dark:text-white mb-1">
              GitHub
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Source code and issues
            </p>
          </a>
        </div>
      </div>
    </div>
  );
}
