import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Rate Limits & Quotas - Mars Vista API',
  description: 'Understand rate limits and how to optimize your API usage',
};

export default function RateLimitsPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Rate Limits & Quotas
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Understand your usage limits and how to optimize your API requests.
      </p>

      {/* Current Limits */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Current Limits
        </h2>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Limit Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Value</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Reset</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Hourly requests</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-semibold">10,000</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Top of each hour</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Daily requests</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-semibold">100,000</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Midnight UTC</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Concurrent requests</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-semibold">50</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Immediate</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p className="text-sm text-slate-600 dark:text-slate-400 mt-4">
          These limits are generous for most use cases. If you need higher limits, please <a href="mailto:support@marsvista.dev" className="text-orange-600 dark:text-orange-400 hover:underline">contact us</a>.
        </p>
      </section>

      {/* What Counts */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          What Counts as a Request?
        </h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="bg-green-50 dark:bg-green-900/20 rounded-lg p-4">
            <h3 className="font-semibold text-green-800 dark:text-green-400 mb-2">
              Counts as 1 Request
            </h3>
            <ul className="space-y-2 text-slate-700 dark:text-slate-300 text-sm">
              <li className="flex gap-2">
                <span className="text-green-500">•</span>
                Any API call (photos, rovers, cameras)
              </li>
              <li className="flex gap-2">
                <span className="text-green-500">•</span>
                Regardless of page size (1-100 items)
              </li>
              <li className="flex gap-2">
                <span className="text-green-500">•</span>
                Both successful and error responses
              </li>
            </ul>
          </div>
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-800 dark:text-slate-200 mb-2">
              Does NOT Count
            </h3>
            <ul className="space-y-2 text-slate-700 dark:text-slate-300 text-sm">
              <li className="flex gap-2">
                <span className="text-slate-500">•</span>
                Requests that return 304 Not Modified
              </li>
              <li className="flex gap-2">
                <span className="text-slate-500">•</span>
                Fetching actual image files (NASA servers)
              </li>
              <li className="flex gap-2">
                <span className="text-slate-500">•</span>
                Health check endpoints
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* Rate Limit Headers */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Rate Limit Headers
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Every API response includes rate limit information in the headers:
        </p>
        <CodeBlock
          code={`HTTP/1.1 200 OK
X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9847
X-RateLimit-Reset: 1732580400`}
          language="text"
        />
        <div className="overflow-x-auto mt-4">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Header</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">X-RateLimit-Limit</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Maximum requests allowed in the current window</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">X-RateLimit-Remaining</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Requests remaining in the current window</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">X-RateLimit-Reset</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Unix timestamp when the limit resets</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Handling Rate Limits */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Handling Rate Limit Errors
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          When you exceed a rate limit, you&apos;ll receive a 429 response:
        </p>
        <CodeBlock
          code={`{
  "type": "/errors/rate-limit-exceeded",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "You have exceeded the hourly rate limit of 10000 requests.",
  "retryAfter": 1523
}`}
          language="json"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-4 mb-4">
          Implement exponential backoff to handle rate limits gracefully:
        </p>
        <CodeBlock
          code={`async function fetchWithRetry(url, options, maxRetries = 3) {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    const response = await fetch(url, options);

    if (response.status === 429) {
      const data = await response.json();
      const waitTime = data.retryAfter || Math.pow(2, attempt) * 1000;
      console.log(\`Rate limited. Waiting \${waitTime}ms...\`);
      await new Promise(resolve => setTimeout(resolve, waitTime * 1000));
      continue;
    }

    return response;
  }
  throw new Error('Max retries exceeded');
}`}
          language="javascript"
        />
      </section>

      {/* Optimization Tips */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Optimization Tips
        </h2>
        <div className="space-y-6">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              1. Use Maximum Page Size
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Request 100 items per page instead of the default 25 to reduce total API calls:
            </p>
            <CodeBlock
              code={`# Instead of this (4 requests for 100 items):
curl "...?per_page=25"  # 4 requests needed

# Do this (1 request for 100 items):
curl "...?per_page=100"  # 1 request`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              2. Use HTTP Caching (ETags)
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Cache responses and use conditional requests. 304 responses don&apos;t count against your limit:
            </p>
            <CodeBlock
              code={`// First request - store the ETag
const response = await fetch(url, { headers });
const etag = response.headers.get('ETag');
localStorage.setItem('etag', etag);

// Subsequent requests - use If-None-Match
const cachedEtag = localStorage.getItem('etag');
const response = await fetch(url, {
  headers: { ...headers, 'If-None-Match': cachedEtag }
});

if (response.status === 304) {
  // Use cached data - doesn't count as a request!
  return cachedData;
}`}
              language="javascript"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              3. Request Only Needed Fields
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Use field sets or custom field selection to reduce response size and processing time:
            </p>
            <CodeBlock
              code={`# Minimal response (faster)
curl "...?field_set=minimal"

# Or specific fields only
curl "...?fields=id,sol,images"`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              4. Cache Static Data Locally
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Rovers and cameras don&apos;t change often. Cache them locally:
            </p>
            <CodeBlock
              code={`// Fetch rovers once per day
async function getRovers() {
  const cached = localStorage.getItem('rovers');
  const cachedAt = localStorage.getItem('rovers_cached_at');

  // Cache for 24 hours
  if (cached && Date.now() - cachedAt < 86400000) {
    return JSON.parse(cached);
  }

  const response = await fetch('/api/v2/rovers', { headers });
  const data = await response.json();

  localStorage.setItem('rovers', JSON.stringify(data));
  localStorage.setItem('rovers_cached_at', Date.now());

  return data;
}`}
              language="javascript"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              5. Use Batch Endpoints
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Fetch multiple photos by ID in a single request:
            </p>
            <CodeBlock
              code={`# Instead of 10 separate requests:
curl ".../photos/1"
curl ".../photos/2"
# ...

# Use batch endpoint (1 request):
curl -X POST ".../photos/batch" \\
  -d '{"ids": [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]}'`}
              language="bash"
            />
          </div>
        </div>
      </section>

      {/* Monitoring Usage */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Monitoring Your Usage
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Track your usage by monitoring rate limit headers in your application:
        </p>
        <CodeBlock
          code={`async function trackUsage(response) {
  const limit = response.headers.get('X-RateLimit-Limit');
  const remaining = response.headers.get('X-RateLimit-Remaining');
  const reset = response.headers.get('X-RateLimit-Reset');

  const usedPercent = ((limit - remaining) / limit * 100).toFixed(1);
  const resetTime = new Date(reset * 1000).toLocaleTimeString();

  console.log(\`API Usage: \${usedPercent}% (\${remaining}/\${limit} remaining)\`);
  console.log(\`Resets at: \${resetTime}\`);

  // Alert when approaching limit
  if (remaining < limit * 0.1) {
    console.warn('WARNING: Approaching rate limit!');
  }
}`}
          language="javascript"
        />
      </section>

      {/* Next steps */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Next Steps
        </h2>
        <ul className="space-y-2">
          <li>
            <Link
              href="/docs/authentication"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Authentication &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              API key management and security
            </span>
          </li>
          <li>
            <Link
              href="/docs/reference/errors"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Error Reference &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Handle all error types
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
