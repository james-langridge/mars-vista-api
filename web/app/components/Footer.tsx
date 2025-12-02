import Link from 'next/link';
import ExternalLinkIcon from './ExternalLinkIcon';

export default function Footer() {
  return (
    <footer className="border-t border-slate-200 dark:border-slate-700 mt-16 bg-slate-50 dark:bg-slate-900">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-8">
          {/* Brand */}
          <div className="md:col-span-2 lg:col-span-1">
            <Link
              href="/"
              className="text-xl font-bold text-slate-900 dark:text-white hover:text-orange-600 dark:hover:text-orange-400 transition-colors"
            >
              Mars Vista
            </Link>
            <p className="mt-2 text-slate-600 dark:text-slate-400 text-sm">
              Mars rover imagery API with photos from Curiosity, Perseverance, Opportunity, and
              Spirit.
            </p>
            <Link
              href="/signin"
              className="inline-block mt-4 px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white text-sm font-medium rounded-lg transition-colors"
            >
              Get API Key
            </Link>
          </div>

          {/* Documentation */}
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-4">Documentation</h3>
            <ul className="space-y-2">
              <li>
                <Link
                  href="/docs/quickstart"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Quick Start
                </Link>
              </li>
              <li>
                <Link
                  href="/docs/guides/mars-time"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Guides
                </Link>
              </li>
              <li>
                <Link
                  href="/docs/reference/photos"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  API Reference
                </Link>
              </li>
              <li>
                <Link
                  href="/docs/troubleshooting"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Troubleshooting
                </Link>
              </li>
            </ul>
          </div>

          {/* API */}
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-4">API</h3>
            <ul className="space-y-2">
              <li>
                <a
                  href="https://api.marsvista.dev/swagger"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Swagger UI
                  <ExternalLinkIcon />
                </a>
              </li>
              <li>
                <a
                  href="/docs/llm/openapi.json"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  OpenAPI Spec
                  <ExternalLinkIcon />
                </a>
              </li>
              <li>
                <a
                  href="https://status.marsvista.dev"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Status
                  <ExternalLinkIcon />
                </a>
              </li>
            </ul>
          </div>

          {/* Connect */}
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-4">Connect</h3>
            <ul className="space-y-2">
              <li>
                <a
                  href="https://marsvista.space"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Photo Gallery
                  <ExternalLinkIcon />
                </a>
              </li>
              <li>
                <a
                  href="https://github.com/james-langridge/mars-vista-api"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  GitHub
                  <ExternalLinkIcon />
                </a>
              </li>
              <li>
                <a
                  href="https://github.com/james-langridge/mars-vista-api/issues"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Report an Issue
                  <ExternalLinkIcon />
                </a>
              </li>
            </ul>
          </div>

          {/* Legal */}
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-4">Legal</h3>
            <ul className="space-y-2">
              <li>
                <Link
                  href="/privacy"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Privacy Policy
                </Link>
              </li>
              <li>
                <Link
                  href="/terms"
                  className="text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white transition-colors text-sm"
                >
                  Terms of Service
                </Link>
              </li>
            </ul>
          </div>
        </div>

        {/* Bottom bar */}
        <div className="mt-10 pt-8 border-t border-slate-200 dark:border-slate-700 flex flex-col sm:flex-row justify-between items-center gap-4 text-sm text-slate-500 dark:text-slate-400">
          <p className="inline-flex items-center gap-1">
            Built by{' '}
            <a
              href="https://langridge.dev"
              className="inline-flex items-center gap-1 text-slate-600 dark:text-slate-300 hover:text-slate-900 dark:hover:text-white transition-colors"
              target="_blank"
              rel="noopener noreferrer"
            >
              James Langridge
              <ExternalLinkIcon />
            </a>
          </p>
          <p>NASA imagery courtesy of JPL-Caltech</p>
        </div>
      </div>
    </footer>
  );
}
