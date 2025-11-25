import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Troubleshooting - Mars Vista API',
  description: 'Common issues and solutions for the Mars Vista API',
};

export default function TroubleshootingPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Troubleshooting
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Solutions to common issues when using the Mars Vista API.
      </p>

      {/* Empty Responses */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Empty or Missing Data
        </h2>

        <div className="space-y-6">
          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: relationships object is empty
            </h3>
            <CodeBlock
              code={`"relationships": {}`}
              language="json"
            />
            <p className="text-red-700 dark:text-red-300 mt-3 mb-2">
              <strong>Cause:</strong> Missing <code className="bg-red-100 dark:bg-red-900/30 px-1 rounded">include</code> parameter.
            </p>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solution:</strong> Add <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">include=rover,camera</code> to your request:
            </p>
            <CodeBlock
              code={`curl "...?include=rover,camera"`}
              language="bash"
            />
          </div>

          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: img_src is empty
            </h3>
            <CodeBlock
              code={`"img_src": ""`}
              language="json"
            />
            <p className="text-red-700 dark:text-red-300 mt-3 mb-2">
              <strong>Cause:</strong> The <code className="bg-red-100 dark:bg-red-900/30 px-1 rounded">img_src</code> field is deprecated.
            </p>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solution:</strong> Use the <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">images</code> object instead:
            </p>
            <CodeBlock
              code={`// Old (deprecated)
photo.img_src

// New (correct)
photo.attributes.images.medium  // or small, large, full`}
              language="javascript"
            />
          </div>

          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: Query returns no photos
            </h3>
            <p className="text-red-700 dark:text-red-300 mb-2">
              <strong>Possible causes:</strong>
            </p>
            <ul className="list-disc list-inside text-red-700 dark:text-red-300 space-y-1 mb-3">
              <li>Filters are too restrictive</li>
              <li>Invalid rover or camera name</li>
              <li>Date range has no photos</li>
              <li>Sol doesn&apos;t exist for that rover</li>
            </ul>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solution:</strong> Start with a broad query and add filters one at a time:
            </p>
            <CodeBlock
              code={`# Start broad
curl "...?rovers=curiosity&per_page=5"

# Add filters one by one
curl "...?rovers=curiosity&sol_min=1000&sol_max=1000&per_page=5"
curl "...?rovers=curiosity&sol_min=1000&sol_max=1000&cameras=NAVCAM&per_page=5"`}
              language="bash"
            />
          </div>
        </div>
      </section>

      {/* Authentication Issues */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Authentication Issues
        </h2>

        <div className="space-y-6">
          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: 401 Unauthorized
            </h3>
            <p className="text-red-700 dark:text-red-300 mb-3">
              <strong>Possible causes:</strong>
            </p>
            <ul className="list-disc list-inside text-red-700 dark:text-red-300 space-y-1 mb-3">
              <li>API key is missing from request</li>
              <li>API key is in the wrong header</li>
              <li>API key has been regenerated (old key is invalid)</li>
              <li>API key is malformed or truncated</li>
            </ul>
            <p className="text-green-700 dark:text-green-400 mb-2">
              <strong>Checklist:</strong>
            </p>
            <ul className="list-disc list-inside text-green-700 dark:text-green-400 space-y-1">
              <li>Header name is exactly <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">X-API-Key</code> (case-sensitive)</li>
              <li>Key starts with <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">mv_live_</code></li>
              <li>Key is 47 characters total</li>
              <li>No extra spaces or quotes around the key</li>
            </ul>
          </div>

          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: 429 Too Many Requests
            </h3>
            <p className="text-green-700 dark:text-green-400 mb-2">
              <strong>Solutions:</strong>
            </p>
            <ul className="list-disc list-inside text-green-700 dark:text-green-400 space-y-1 mb-3">
              <li>Check the <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">retryAfter</code> field and wait that many seconds</li>
              <li>Implement exponential backoff</li>
              <li>Use larger page sizes (per_page=100) to reduce total requests</li>
              <li>Cache responses locally</li>
              <li>Use ETags to avoid counting 304 responses</li>
            </ul>
            <p className="text-slate-700 dark:text-slate-300">
              See <Link href="/docs/guides/rate-limits" className="text-orange-600 dark:text-orange-400 hover:underline">Rate Limits Guide</Link> for optimization tips.
            </p>
          </div>
        </div>
      </section>

      {/* Data Issues */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Data Issues
        </h2>

        <div className="space-y-6">
          <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4">
            <h3 className="font-semibold text-yellow-800 dark:text-yellow-400 mb-2">
              Issue: Photos seem out of order
            </h3>
            <p className="text-yellow-700 dark:text-yellow-300 mb-2">
              <strong>Cause:</strong> Default sort is by earth_date descending. Photos from the same day may appear in any order.
            </p>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solution:</strong> Add explicit sorting:
            </p>
            <CodeBlock
              code={`# Sort by sol and capture time
curl "...?sort=sol,-date_taken_utc"`}
              language="bash"
            />
          </div>

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4">
            <h3 className="font-semibold text-yellow-800 dark:text-yellow-400 mb-2">
              Issue: Can&apos;t find recent photos
            </h3>
            <p className="text-yellow-700 dark:text-yellow-300 mb-2">
              <strong>Cause:</strong> There&apos;s typically a 1-2 day delay between when NASA receives photos and when they appear in the API.
            </p>
            <p className="text-green-700 dark:text-green-400">
              <strong>Note:</strong> Photos are scraped daily at 2 AM UTC. The most recent photos will be from 1-2 days ago.
            </p>
          </div>

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4">
            <h3 className="font-semibold text-yellow-800 dark:text-yellow-400 mb-2">
              Issue: Different rovers have different cameras
            </h3>
            <p className="text-yellow-700 dark:text-yellow-300 mb-2">
              <strong>Cause:</strong> Each rover has unique instruments. Camera names differ between rovers.
            </p>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solution:</strong> Check the <Link href="/docs/reference/cameras" className="text-orange-600 dark:text-orange-400 hover:underline">Cameras Reference</Link> for each rover&apos;s available cameras.
            </p>
          </div>
        </div>
      </section>

      {/* Integration Issues */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Integration Issues
        </h2>

        <div className="space-y-6">
          <div className="bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 p-4">
            <h3 className="font-semibold text-red-800 dark:text-red-400 mb-2">
              Problem: CORS errors in browser
            </h3>
            <p className="text-red-700 dark:text-red-300 mb-2">
              <strong>Cause:</strong> API keys should never be exposed in client-side JavaScript.
            </p>
            <p className="text-green-700 dark:text-green-400 mb-2">
              <strong>Solution:</strong> Make API calls from your backend server, not the browser:
            </p>
            <CodeBlock
              code={`// DON'T: Client-side JavaScript
// This exposes your API key!
fetch('https://api.marsvista.dev/...', {
  headers: { 'X-API-Key': 'mv_live_xxx' }
});

// DO: Server-side (Next.js API route, Express, etc.)
// app/api/photos/route.ts
export async function GET(request) {
  const response = await fetch('https://api.marsvista.dev/...', {
    headers: { 'X-API-Key': process.env.MARS_VISTA_API_KEY }
  });
  return Response.json(await response.json());
}`}
              language="javascript"
            />
          </div>

          <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4">
            <h3 className="font-semibold text-yellow-800 dark:text-yellow-400 mb-2">
              Issue: Slow response times
            </h3>
            <p className="text-yellow-700 dark:text-yellow-300 mb-2">
              <strong>Possible causes:</strong>
            </p>
            <ul className="list-disc list-inside text-yellow-700 dark:text-yellow-300 space-y-1 mb-3">
              <li>Query is too broad (scanning many records)</li>
              <li>Requesting all fields when you only need a few</li>
              <li>Not using caching</li>
            </ul>
            <p className="text-green-700 dark:text-green-400">
              <strong>Solutions:</strong>
            </p>
            <ul className="list-disc list-inside text-green-700 dark:text-green-400 space-y-1">
              <li>Add specific filters (rover, date range, camera)</li>
              <li>Use <code className="bg-green-100 dark:bg-green-900/30 px-1 rounded">field_set=minimal</code> for faster responses</li>
              <li>Cache rover/camera data (they rarely change)</li>
              <li>Use ETags for conditional requests</li>
            </ul>
          </div>
        </div>
      </section>

      {/* Still Need Help */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Still Need Help?
        </h2>
        <ul className="space-y-2 text-slate-700 dark:text-slate-300">
          <li>
            Check the{' '}
            <Link href="/docs/reference/errors" className="text-orange-600 dark:text-orange-400 hover:underline">
              Error Reference
            </Link>
            {' '}for detailed error explanations
          </li>
          <li>
            Browse the{' '}
            <a
              href="https://api.marsvista.dev/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Swagger UI
            </a>
            {' '}to test requests interactively
          </li>
          <li>
            Report issues on{' '}
            <a
              href="https://github.com/james-langridge/mars-vista-api/issues"
              target="_blank"
              rel="noopener noreferrer"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              GitHub Issues
            </a>
          </li>
        </ul>
      </section>
    </div>
  );
}
