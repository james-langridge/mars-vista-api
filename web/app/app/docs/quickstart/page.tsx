import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';
import CodeTabs from '@/components/docs/CodeTabs';

export const metadata: Metadata = {
  title: 'Quick Start - Mars Vista API',
  description: 'Make your first Mars Vista API request in under 5 minutes',
};

export default function QuickStartPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Quick Start
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Make your first API request in under 5 minutes.
      </p>

      {/* Step 1 */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4 flex items-center gap-3">
          <span className="flex-shrink-0 w-8 h-8 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full flex items-center justify-center text-sm font-bold">
            1
          </span>
          Get Your API Key
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          All API requests require an API key. Sign in to get yours for free:
        </p>
        <div className="flex gap-4 mb-4">
          <Link
            href="/signin"
            className="px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium transition-colors"
          >
            Sign In & Get API Key
          </Link>
        </div>
        <p className="text-sm text-slate-600 dark:text-slate-400">
          Already have a key? Skip to step 2.
        </p>
      </section>

      {/* Step 2 */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4 flex items-center gap-3">
          <span className="flex-shrink-0 w-8 h-8 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full flex items-center justify-center text-sm font-bold">
            2
          </span>
          Make Your First Request
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Query the latest photos from Curiosity rover:
        </p>
        <CodeTabs
          examples={[
            {
              label: 'cURL',
              language: 'bash',
              code: `curl -H "X-API-Key: YOUR_API_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=5&include=rover,camera"`,
            },
            {
              label: 'JavaScript',
              language: 'javascript',
              code: `const response = await fetch(
  'https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=5&include=rover,camera',
  {
    headers: {
      'X-API-Key': process.env.MARS_VISTA_API_KEY
    }
  }
);
const data = await response.json();`,
            },
            {
              label: 'Python',
              language: 'python',
              code: `import requests
import os

response = requests.get(
    'https://api.marsvista.dev/api/v2/photos',
    params={
        'rovers': 'curiosity',
        'per_page': 5,
        'include': 'rover,camera'
    },
    headers={'X-API-Key': os.environ['MARS_VISTA_API_KEY']}
)
data = response.json()`,
            },
            {
              label: 'TypeScript',
              language: 'typescript',
              code: `interface Photo {
  id: number;
  type: string;
  attributes: {
    sol: number;
    earth_date: string;
    images: { small: string | null; medium: string | null; large: string | null; full: string };
  };
}

const response = await fetch(
  'https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=5&include=rover,camera',
  { headers: { 'X-API-Key': process.env.MARS_VISTA_API_KEY! } }
);
const data = await response.json() as { data: Photo[] };`,
            },
          ]}
        />
      </section>

      {/* Step 3 */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4 flex items-center gap-3">
          <span className="flex-shrink-0 w-8 h-8 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full flex items-center justify-center text-sm font-bold">
            3
          </span>
          Explore the Response
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          You&apos;ll receive a JSON response with photo data:
        </p>
        <CodeBlock
          code={`{
  "data": [
    {
      "id": 2544294,
      "type": "photo",
      "attributes": {
        "nasa_id": "1533956",
        "sol": 4728,
        "earth_date": "2025-11-24",
        "images": {
          "small": "https://mars.nasa.gov/..._320.jpg",
          "medium": "https://mars.nasa.gov/..._800.jpg",
          "large": "https://mars.nasa.gov/..._1200.jpg",
          "full": "https://mars.nasa.gov/..._full.jpg"
        }
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "attributes": { "name": "Curiosity", "status": "active" }
        },
        "camera": {
          "id": "NAVCAM",
          "attributes": { "full_name": "Navigation Camera" }
        }
      }
    }
  ],
  "meta": { "total_count": 682660, "returned_count": 5 },
  "pagination": { "page": 1, "per_page": 5, "total_pages": 136532 }
}`}
          language="json"
        />
      </section>

      {/* Key concepts */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Key Concepts
        </h2>
        <div className="grid md:grid-cols-2 gap-4">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              The <code className="text-orange-600 dark:text-orange-400">include</code> Parameter
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Always use <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">include=rover,camera</code> to get rover and camera details in the response.
              Without it, the <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">relationships</code> object will be empty.
            </p>
          </div>
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Image Sizes (Vary by Rover)
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              <strong>Perseverance:</strong> All 4 sizes (<code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">small</code>,
              <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">medium</code>,
              <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">large</code>,
              <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">full</code>).
              <strong> Other rovers:</strong> Only <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">full</code>.
              Always use <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">full</code> as fallback.
            </p>
          </div>
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Sol vs Earth Date
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              A &quot;sol&quot; is a Martian day (~24h 39m). Each rover counts sols from landing.
              Sol 1000 for Curiosity is a different Earth date than Sol 1000 for Perseverance.
            </p>
          </div>
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Pagination
            </h3>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Results are paginated. Use <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">page</code> and
              <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">per_page</code> (max 100) to navigate.
              The <code className="bg-slate-200 dark:bg-slate-700 px-1 rounded">links</code> object provides URLs for next/prev pages.
            </p>
          </div>
        </div>
      </section>

      {/* Common queries */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Common Queries
        </h2>
        <div className="space-y-6">
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Photos from a specific sol
            </h3>
            <CodeBlock
              code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1000&include=rover,camera"`}
              language="bash"
            />
          </div>
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Photos from a specific Earth date
            </h3>
            <CodeBlock
              code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?date_min=2024-01-15&date_max=2024-01-15&include=rover,camera"`}
              language="bash"
            />
          </div>
          <div>
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Photos from multiple rovers
            </h3>
            <CodeBlock
              code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&per_page=10&include=rover,camera"`}
              language="bash"
            />
          </div>
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
              href="/docs/authentication"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Authentication &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Learn about rate limits and managing your API key
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
              Complete guide to querying photos
            </span>
          </li>
          <li>
            <Link
              href="/docs/guides/mars-time"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Understanding Mars Time &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Deep dive into sols and Mars local time
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
