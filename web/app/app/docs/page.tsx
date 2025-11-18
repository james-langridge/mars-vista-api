import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'API Documentation - Mars Vista API',
  description: 'Complete API documentation for Mars Vista rover imagery API',
};

export default function Docs() {
  return (
    <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <h1 className="text-4xl font-bold mb-8">API Documentation</h1>

      <div className="bg-blue-900/20 border border-blue-700 rounded-lg p-6 mb-8">
        <h2 className="text-xl font-semibold mb-2">Documentation Coming Soon</h2>
        <p className="text-gray-300">
          Interactive API documentation will be available once the OpenAPI specification is published.
          In the meantime, here&apos;s how to get started with the Mars Vista API.
        </p>
      </div>

      <div className="space-y-8">
        <section>
          <h2 className="text-2xl font-bold mb-4">Quick Start</h2>
          <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
            <h3 className="text-lg font-semibold mb-3">Get Photos from Curiosity</h3>
            <pre className="bg-gray-900 rounded p-4 overflow-x-auto">
              <code className="text-green-400">
                {`curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"`}
              </code>
            </pre>
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-bold mb-4">Available Endpoints</h2>
          <div className="space-y-4">
            <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
              <h3 className="text-lg font-semibold mb-2">List All Rovers</h3>
              <code className="text-sm text-gray-300">GET /api/v1/rovers</code>
              <p className="text-gray-400 mt-2">Get information about all available Mars rovers.</p>
            </div>

            <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
              <h3 className="text-lg font-semibold mb-2">Get Rover Photos</h3>
              <code className="text-sm text-gray-300">GET /api/v1/rovers/{'{name}'}/photos</code>
              <p className="text-gray-400 mt-2 mb-3">
                Retrieve photos from a specific rover. Supports multiple query parameters:
              </p>
              <ul className="list-disc list-inside text-gray-400 space-y-1">
                <li><code>sol</code> - Martian sol (day) number</li>
                <li><code>earth_date</code> - Earth date (YYYY-MM-DD)</li>
                <li><code>camera</code> - Camera name (e.g., navcam, fhaz, rhaz)</li>
                <li><code>page</code> - Page number for pagination</li>
              </ul>
            </div>

            <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
              <h3 className="text-lg font-semibold mb-2">Get Rover Details</h3>
              <code className="text-sm text-gray-300">GET /api/v1/rovers/{'{name}'}</code>
              <p className="text-gray-400 mt-2">
                Get detailed information about a specific rover including available cameras.
              </p>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-bold mb-4">Available Rovers</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
              <h3 className="font-semibold">Curiosity</h3>
              <p className="text-sm text-gray-400">Active since 2012</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
              <h3 className="font-semibold">Perseverance</h3>
              <p className="text-sm text-gray-400">Active since 2021</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
              <h3 className="font-semibold">Opportunity</h3>
              <p className="text-sm text-gray-400">2004-2018</p>
            </div>
            <div className="bg-gray-800 rounded-lg p-4 border border-gray-700">
              <h3 className="font-semibold">Spirit</h3>
              <p className="text-sm text-gray-400">2004-2010</p>
            </div>
          </div>
        </section>

        <section>
          <h2 className="text-2xl font-bold mb-4">Authentication</h2>
          <p className="text-gray-300 mb-4">
            API authentication is coming soon. For now, all endpoints are publicly accessible with rate limiting.
          </p>
          <Link
            href="/pricing"
            className="inline-block px-6 py-3 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors"
          >
            View Rate Limits
          </Link>
        </section>
      </div>
    </main>
  );
}
