import Link from 'next/link';
import ExternalLinkIcon from './ExternalLinkIcon';

export default function Hero() {
  return (
    <div className="relative overflow-hidden bg-gradient-to-b from-orange-50 to-white dark:from-slate-900 dark:to-slate-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24 sm:py-32">
        <div className="text-center">
          <h1 className="text-5xl sm:text-7xl font-bold tracking-tight mb-6 text-slate-900 dark:text-white">
            Mars Vista API
          </h1>
          <p className="text-xl sm:text-2xl text-slate-600 dark:text-slate-300 mb-8 max-w-3xl mx-auto">
            The complete archive of Mars rover imagery. Over 1.5 million unique photos from Curiosity, Perseverance, Opportunity, and Spirit missions. Updated daily.
          </p>
          <div className="flex justify-center gap-4">
            <Link
              href="/signin"
              className="px-8 py-4 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-semibold text-lg transition-colors shadow-lg"
            >
              Get Started
            </Link>
            <a
              href="https://marsvista.space"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 px-8 py-4 bg-white dark:bg-slate-800 text-slate-900 dark:text-white border border-slate-300 dark:border-slate-600 hover:border-orange-400 dark:hover:border-orange-500 rounded-lg font-semibold text-lg transition-colors shadow-lg"
            >
              Explore Photos
              <ExternalLinkIcon className="h-4 w-4" />
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}
