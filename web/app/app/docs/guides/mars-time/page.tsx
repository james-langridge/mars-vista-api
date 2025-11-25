import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Understanding Mars Time - Mars Vista API',
  description: 'Learn about sols, Mars local time, and how to query photos by Martian time',
};

export default function MarsTimePage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Understanding Mars Time
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Mars has its own time system. Understanding it unlocks powerful query capabilities.
      </p>

      {/* What is a Sol */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          What is a Sol?
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          A <strong>sol</strong> is one Martian day. It&apos;s slightly longer than an Earth day:
        </p>
        <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4 mb-4">
          <p className="text-lg font-mono text-slate-900 dark:text-white">
            1 sol = 24 hours, 39 minutes, 35 seconds (Earth time)
          </p>
        </div>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Each rover mission counts sols from the day of landing, starting at Sol 0 or Sol 1.
          This means <strong>Sol 1000 for Curiosity is a completely different Earth date than Sol 1000 for Perseverance</strong>.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Rover Landing Dates
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Rover</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Landing Date</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Sol 1</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-medium">Perseverance</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">February 18, 2021</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Feb 19, 2021</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-sm">Active</span></td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-medium">Curiosity</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">August 6, 2012</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Aug 7, 2012</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-sm">Active</span></td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-medium">Opportunity</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">January 25, 2004</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Jan 26, 2004</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400 rounded text-sm">Complete</span></td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-medium">Spirit</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">January 4, 2004</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Jan 5, 2004</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400 rounded text-sm">Complete</span></td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Querying by Sol */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Querying by Sol
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Query photos from a specific sol or range of sols:
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          Single Sol
        </h3>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Sol Range
        </h3>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=perseverance&sol_min=100&sol_max=200"`}
          language="bash"
        />

        <div className="bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 p-4 mt-4">
          <p className="text-blue-800 dark:text-blue-200 font-medium">Tip</p>
          <p className="text-blue-700 dark:text-blue-300 text-sm">
            Use sol queries to follow a rover&apos;s journey chronologically. Each sol typically has 10-200 photos
            depending on the rover&apos;s activities that day.
          </p>
        </div>
      </section>

      {/* Mars Local Time */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Mars Local Time
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Each photo includes the local Mars time when it was taken. The format is:
        </p>
        <CodeBlock
          code={`"date_taken_mars": "Sol-04728M14:23:45.528"`}
          language="json"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-4 mb-4">
          This breaks down as:
        </p>
        <ul className="list-disc list-inside space-y-2 text-slate-700 dark:text-slate-300 mb-6">
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">Sol-04728</code> - Mission sol number</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">M14:23:45</code> - Mars local time (24-hour format)</li>
          <li><code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">.528</code> - Fractional seconds</li>
        </ul>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          Query by Mars Time
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Find photos taken at specific Mars local times:
        </p>
        <CodeBlock
          code={`# Photos taken during Mars morning (6 AM - 10 AM)
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?mars_time_min=M06:00:00&mars_time_max=M10:00:00"

# Photos taken during Mars noon (12 PM - 2 PM)
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?mars_time_min=M12:00:00&mars_time_max=M14:00:00"`}
          language="bash"
        />
      </section>

      {/* Golden Hour */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Golden Hour Photography
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Just like on Earth, Mars has beautiful &quot;golden hour&quot; lighting during sunrise and sunset.
          Use the special <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">mars_time_golden_hour</code> filter:
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?mars_time_golden_hour=true&rovers=curiosity"`}
          language="bash"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-4">
          This returns photos taken approximately 1 hour after sunrise or 1 hour before sunset on Mars.
        </p>

        <div className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-4 mt-4">
          <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
            Mars Golden Hour Times
          </h3>
          <ul className="text-slate-700 dark:text-slate-300 space-y-1">
            <li><strong>Morning:</strong> ~6:00 - 7:30 Mars time</li>
            <li><strong>Evening:</strong> ~17:30 - 19:00 Mars time</li>
          </ul>
          <p className="text-sm text-slate-600 dark:text-slate-400 mt-2">
            (Varies by season and location on Mars)
          </p>
        </div>
      </section>

      {/* Sol vs Earth Date */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          When to Use Sol vs Earth Date
        </h2>
        <div className="grid md:grid-cols-2 gap-6">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-3">
              Use Sol When...
            </h3>
            <ul className="space-y-2 text-slate-700 dark:text-slate-300 text-sm">
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Following a rover&apos;s chronological journey
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Comparing similar mission phases across rovers
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Building mission timelines
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Scientific analysis of rover activities
              </li>
            </ul>
          </div>
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-3">
              Use Earth Date When...
            </h3>
            <ul className="space-y-2 text-slate-700 dark:text-slate-300 text-sm">
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Finding &quot;what did rovers see today?&quot;
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Comparing photos from the same Earth day
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                Building calendars or date-based UIs
              </li>
              <li className="flex gap-2">
                <span className="text-orange-500">•</span>
                News or media applications
              </li>
            </ul>
          </div>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Query by Earth Date
        </h3>
        <CodeBlock
          code={`# Photos from a specific Earth date
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?date_min=2024-01-15&date_max=2024-01-15"

# Photos from a date range
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?date_min=2024-01-01&date_max=2024-01-31"`}
          language="bash"
        />
      </section>

      {/* Response Fields */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Time Fields in Responses
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Each photo includes multiple time fields:
        </p>
        <CodeBlock
          code={`{
  "attributes": {
    "sol": 4728,
    "earth_date": "2025-11-24",
    "date_taken_utc": "2025-11-24T14:23:45Z",
    "date_taken_mars": "Sol-04728M14:23:45.528"
  }
}`}
          language="json"
        />
        <div className="overflow-x-auto mt-4">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Field</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">sol</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mission sol number (integer)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">earth_date</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Earth date (YYYY-MM-DD)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">date_taken_utc</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">UTC timestamp when photo was taken</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">date_taken_mars</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mars local time with sol prefix</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Next steps */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Next Steps
        </h2>
        <ul className="space-y-2">
          <li>
            <Link
              href="/docs/guides/filtering"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Filtering & Pagination &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Combine time filters with other queries
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
              Complete list of query parameters
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
