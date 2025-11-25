import type { Metadata } from 'next';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Error Handling - Mars Vista API',
  description: 'API error codes and how to handle them',
};

export default function ErrorsReferencePage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Error Handling
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        The API uses standard HTTP status codes and returns detailed error information.
      </p>

      {/* Error Format */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Error Response Format
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          All errors follow the RFC 7807 Problem Details format:
        </p>
        <CodeBlock
          code={`{
  "type": "/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request contains invalid parameters",
  "instance": "/api/v2/photos?date_min=invalid",
  "errors": [
    {
      "field": "date_min",
      "value": "invalid",
      "message": "Must be in YYYY-MM-DD format",
      "example": "2024-01-15"
    }
  ]
}`}
          language="json"
        />
      </section>

      {/* Status Codes */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          HTTP Status Codes
        </h2>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Code</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Name</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Meaning</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-green-600 dark:text-green-400 font-mono">200</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">OK</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Request successful</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-600 dark:text-slate-400 font-mono">304</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Not Modified</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Cached response is still valid</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-orange-600 dark:text-orange-400 font-mono">400</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Bad Request</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Invalid request parameters</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-orange-600 dark:text-orange-400 font-mono">401</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Unauthorized</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Missing or invalid API key</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-orange-600 dark:text-orange-400 font-mono">404</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Not Found</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Resource does not exist</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-orange-600 dark:text-orange-400 font-mono">429</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Too Many Requests</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rate limit exceeded</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-red-600 dark:text-red-400 font-mono">500</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Internal Server Error</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Server-side error</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-red-600 dark:text-red-400 font-mono">503</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Service Unavailable</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">API temporarily offline</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Common Errors */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Common Errors
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          401 - Missing API Key
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
        <p className="text-slate-700 dark:text-slate-300 mt-2 mb-6">
          <strong>Solution:</strong> Add the <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">X-API-Key</code> header to your request.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          401 - Invalid API Key
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
        <p className="text-slate-700 dark:text-slate-300 mt-2 mb-6">
          <strong>Solution:</strong> Verify your API key is correct. If lost, regenerate at your dashboard.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          400 - Validation Error
        </h3>
        <CodeBlock
          code={`{
  "type": "/errors/validation-error",
  "title": "Validation Error",
  "status": 400,
  "detail": "The request contains invalid parameters",
  "errors": [
    {
      "field": "rovers",
      "value": "invalid_rover",
      "message": "Unknown rover. Valid options: curiosity, perseverance, opportunity, spirit"
    }
  ]
}`}
          language="json"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-2 mb-6">
          <strong>Solution:</strong> Check the <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">errors</code> array for specific field issues and examples.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          429 - Rate Limited
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
        <p className="text-slate-700 dark:text-slate-300 mt-2 mb-6">
          <strong>Solution:</strong> Wait <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">retryAfter</code> seconds, then retry. See the rate limits guide for optimization tips.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          404 - Resource Not Found
        </h3>
        <CodeBlock
          code={`{
  "type": "/errors/not-found",
  "title": "Not Found",
  "status": 404,
  "detail": "Rover 'viking' not found"
}`}
          language="json"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-2">
          <strong>Solution:</strong> Check the resource identifier. Use the list endpoints to find valid IDs.
        </p>
      </section>

      {/* Error Handling Code */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Error Handling Example
        </h2>
        <CodeBlock
          code={`async function fetchMarsPhotos(params) {
  const response = await fetch(
    \`https://api.marsvista.dev/api/v2/photos?\${new URLSearchParams(params)}\`,
    { headers: { 'X-API-Key': API_KEY } }
  );

  if (!response.ok) {
    const error = await response.json();

    switch (response.status) {
      case 401:
        throw new Error('Invalid API key. Please check your credentials.');

      case 429:
        // Wait and retry
        const waitTime = error.retryAfter || 60;
        console.log(\`Rate limited. Retrying in \${waitTime}s...\`);
        await new Promise(r => setTimeout(r, waitTime * 1000));
        return fetchMarsPhotos(params);

      case 400:
        // Show validation errors to user
        const messages = error.errors?.map(e =>
          \`\${e.field}: \${e.message}\`
        ).join('\\n');
        throw new Error(\`Invalid request:\\n\${messages}\`);

      default:
        throw new Error(error.detail || 'An error occurred');
    }
  }

  return response.json();
}`}
          language="javascript"
        />
      </section>
    </div>
  );
}
