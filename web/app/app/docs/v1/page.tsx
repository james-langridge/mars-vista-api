import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'API v1 (Legacy) - Mars Vista API',
  description: 'Legacy API v1 for compatibility with existing Mars Rover API integrations',
};

export default function V1Page() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        API v1 (Legacy)
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Drop-in replacement for the archived NASA Mars Rover API.
      </p>

      {/* Notice */}
      <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4 mb-8">
        <h2 className="font-semibold text-yellow-800 dark:text-yellow-400 mb-2">
          Recommendation: Use API v2
        </h2>
        <p className="text-yellow-700 dark:text-yellow-300 mb-3">
          API v1 exists for compatibility with existing integrations. For new projects, use{' '}
          <Link href="/docs/reference/photos" className="text-orange-600 dark:text-orange-400 hover:underline font-medium">
            API v2
          </Link>
          {' '}for more features:
        </p>
        <ul className="list-disc list-inside text-yellow-700 dark:text-yellow-300 text-sm space-y-1">
          <li>Multiple image sizes (small, medium, large, full)</li>
          <li>Mars time and location queries</li>
          <li>Camera angle filtering</li>
          <li>Rich metadata and telemetry</li>
        </ul>
      </div>

      {/* Overview */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Overview
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          API v1 mirrors the original{' '}
          <a
            href="https://github.com/corincerami/mars-photo-api"
            target="_blank"
            rel="noopener noreferrer"
            className="text-orange-600 dark:text-orange-400 hover:underline"
          >
            mars-photo-api
          </a>
          {' '}(now archived). If you have existing code using that API, you can switch to Mars Vista with minimal changes.
        </p>
        <p className="text-slate-700 dark:text-slate-300">
          The main difference: Mars Vista requires authentication via the <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">X-API-Key</code> header.
        </p>
      </section>

      {/* Endpoints */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Endpoints
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Get Photos by Sol
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v1/rovers/{'{rover}'}/photos?sol={'{sol}'}</code>
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Get Photos by Earth Date
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v1/rovers/{'{rover}'}/photos?earth_date={'{date}'}</code>
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?earth_date=2015-05-30"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Filter by Camera
        </h3>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&camera=fhaz"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Pagination
        </h3>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000&page=2"`}
          language="bash"
        />
      </section>

      {/* Response Format */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Response Format
        </h2>
        <CodeBlock
          code={`{
  "photos": [
    {
      "id": 102693,
      "sol": 1000,
      "camera": {
        "id": 20,
        "name": "FHAZ",
        "rover_id": 5,
        "full_name": "Front Hazard Avoidance Camera"
      },
      "img_src": "https://mars.nasa.gov/msl-raw-images/...",
      "earth_date": "2015-05-30",
      "rover": {
        "id": 5,
        "name": "Curiosity",
        "landing_date": "2012-08-06",
        "launch_date": "2011-11-26",
        "status": "active"
      }
    }
  ]
}`}
          language="json"
        />
      </section>

      {/* Rovers Endpoint */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Rover Information
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          List All Rovers
        </h3>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/rovers"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Get Rover Manifest
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          Get mission information including available sols:
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v1/manifests/curiosity"`}
          language="bash"
        />
      </section>

      {/* Migration Guide */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Migration from Original API
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          If you&apos;re migrating from <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">api.nasa.gov/mars-photos</code>:
        </p>

        <div className="space-y-4">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              1. Update Base URL
            </h3>
            <CodeBlock
              code={`// Old
https://api.nasa.gov/mars-photos/api/v1/rovers/curiosity/photos

// New
https://api.marsvista.dev/api/v1/rovers/curiosity/photos`}
              language="text"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              2. Switch from api_key param to X-API-Key header
            </h3>
            <CodeBlock
              code={`// Old
curl "...?api_key=DEMO_KEY"

// New
curl -H "X-API-Key: YOUR_KEY" "..."`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              3. Get Your API Key
            </h3>
            <p className="text-slate-700 dark:text-slate-300">
              <Link href="/signin" className="text-orange-600 dark:text-orange-400 hover:underline">
                Sign in
              </Link>
              {' '}to get a free API key. The original DEMO_KEY won&apos;t work.
            </p>
          </div>
        </div>
      </section>

      {/* Upgrade to v2 */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Ready to Upgrade to v2?
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          API v2 offers powerful features not available in v1:
        </p>
        <ul className="list-disc list-inside text-slate-700 dark:text-slate-300 space-y-1 mb-4">
          <li>Multiple image sizes for progressive loading</li>
          <li>Mars time and golden hour filtering</li>
          <li>Location-based queries and journey tracking</li>
          <li>Camera angle and panorama detection</li>
          <li>Full NASA metadata (100% vs v1&apos;s 5%)</li>
        </ul>
        <Link
          href="/docs/quickstart"
          className="inline-block px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium transition-colors"
        >
          Get Started with v2 &rarr;
        </Link>
      </section>
    </div>
  );
}
