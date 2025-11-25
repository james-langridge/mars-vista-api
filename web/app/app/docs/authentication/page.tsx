import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Authentication - Mars Vista API',
  description: 'Learn how to authenticate with the Mars Vista API',
};

export default function AuthenticationPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Authentication
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        All API requests require authentication via an API key.
      </p>

      {/* Getting API Key */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Getting Your API Key
        </h2>
        <ol className="list-decimal list-inside space-y-3 text-slate-700 dark:text-slate-300 mb-4">
          <li>
            <Link href="/signin" className="text-orange-600 dark:text-orange-400 hover:underline">
              Sign in
            </Link>
            {' '}with your email (we&apos;ll send a magic link)
          </li>
          <li>Visit your <Link href="/api-keys" className="text-orange-600 dark:text-orange-400 hover:underline">API Keys dashboard</Link></li>
          <li>Click &quot;Generate API Key&quot;</li>
          <li>Copy your key (you can only see it once!)</li>
        </ol>
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4">
          <p className="text-yellow-800 dark:text-yellow-200 font-medium">Important</p>
          <p className="text-yellow-700 dark:text-yellow-300 text-sm">
            Your API key is shown only once when generated. Store it securely.
            If you lose it, you can regenerate a new one (the old key will be invalidated).
          </p>
        </div>
      </section>

      {/* Using API Key */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Using Your API Key
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Include your API key in the <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded">X-API-Key</code> header with every request:
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: mv_live_your_key_here" \\
  "https://api.marsvista.dev/api/v2/photos?per_page=5"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          JavaScript Example
        </h3>
        <CodeBlock
          code={`const response = await fetch(
  'https://api.marsvista.dev/api/v2/photos?per_page=5',
  {
    headers: {
      'X-API-Key': process.env.MARS_VISTA_API_KEY
    }
  }
);
const data = await response.json();`}
          language="javascript"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Python Example
        </h3>
        <CodeBlock
          code={`import requests
import os

response = requests.get(
    'https://api.marsvista.dev/api/v2/photos',
    params={'per_page': 5},
    headers={'X-API-Key': os.environ['MARS_VISTA_API_KEY']}
)
data = response.json()`}
          language="python"
        />
      </section>

      {/* API Key Format */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          API Key Format
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Mars Vista API keys follow this format:
        </p>
        <CodeBlock
          code={`mv_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`}
          language="text"
        />
        <ul className="list-disc list-inside space-y-2 text-slate-700 dark:text-slate-300 mt-4">
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">mv_</code> - Mars Vista prefix</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">live_</code> - Live/production key</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">xxxxx...</code> - 40-character random string</li>
        </ul>
        <p className="text-sm text-slate-600 dark:text-slate-400 mt-4">
          Total length: 47 characters
        </p>
      </section>

      {/* Rate Limits */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Rate Limits
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          All accounts have the following rate limits:
        </p>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Limit Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Value</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Requests per hour</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">10,000</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Requests per day</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">100,000</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Concurrent requests</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">50</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Rate Limit Headers
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Every response includes rate limit headers:
        </p>
        <CodeBlock
          code={`X-RateLimit-Limit: 10000
X-RateLimit-Remaining: 9847
X-RateLimit-Reset: 1732580400`}
          language="text"
        />
        <ul className="list-disc list-inside space-y-2 text-slate-700 dark:text-slate-300 mt-4">
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">X-RateLimit-Limit</code> - Maximum requests in current window</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">X-RateLimit-Remaining</code> - Requests remaining</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">X-RateLimit-Reset</code> - Unix timestamp when limit resets</li>
        </ul>
      </section>

      {/* Error Responses */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Authentication Errors
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-4 mb-3">
          401 Unauthorized - Missing API Key
        </h3>
        <CodeBlock
          code={`{
  "type": "/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "API key required. Include your key in the X-API-Key header."
}`}
          language="json"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          401 Unauthorized - Invalid API Key
        </h3>
        <CodeBlock
          code={`{
  "type": "/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid API key. Check your key or generate a new one at marsvista.dev"
}`}
          language="json"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          429 Too Many Requests - Rate Limited
        </h3>
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
        <p className="text-sm text-slate-600 dark:text-slate-400 mt-2">
          The <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">retryAfter</code> field indicates seconds until you can make requests again.
        </p>
      </section>

      {/* Security Best Practices */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Security Best Practices
        </h2>
        <ul className="space-y-4 text-slate-700 dark:text-slate-300">
          <li className="flex gap-3">
            <span className="text-green-500 flex-shrink-0">✓</span>
            <span><strong>Store keys in environment variables</strong>, never in source code</span>
          </li>
          <li className="flex gap-3">
            <span className="text-green-500 flex-shrink-0">✓</span>
            <span><strong>Use server-side requests</strong> - never expose your API key in client-side JavaScript</span>
          </li>
          <li className="flex gap-3">
            <span className="text-green-500 flex-shrink-0">✓</span>
            <span><strong>Regenerate if compromised</strong> - if your key is exposed, regenerate immediately</span>
          </li>
          <li className="flex gap-3">
            <span className="text-red-500 flex-shrink-0">✗</span>
            <span><strong>Never commit API keys</strong> to git repositories</span>
          </li>
          <li className="flex gap-3">
            <span className="text-red-500 flex-shrink-0">✗</span>
            <span><strong>Never share your key</strong> - each user should have their own key</span>
          </li>
        </ul>
      </section>

      {/* Next steps */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Next Steps
        </h2>
        <ul className="space-y-2">
          <li>
            <Link
              href="/docs/guides/rate-limits"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Rate Limits Guide &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Learn how to optimize your API usage
            </span>
          </li>
          <li>
            <Link
              href="/docs/reference/photos"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Photos Reference &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Start querying Mars photos
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
