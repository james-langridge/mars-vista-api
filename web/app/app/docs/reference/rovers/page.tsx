import type { Metadata } from 'next';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Rovers API Reference - Mars Vista API',
  description: 'Complete reference for the Rovers endpoint',
};

export default function RoversReferencePage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Rovers API Reference
      </h1>

      {/* List Rovers */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          List All Rovers
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v2/rovers</code>
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/rovers"`}
          language="bash"
        />
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Response
        </h3>
        <CodeBlock
          code={`{
  "data": [
    {
      "id": "curiosity",
      "type": "rover",
      "attributes": {
        "name": "Curiosity",
        "landing_date": "2012-08-06",
        "launch_date": "2011-11-26",
        "status": "active",
        "max_sol": 4728,
        "max_date": "2025-11-24",
        "total_photos": 682660
      }
    },
    {
      "id": "perseverance",
      "type": "rover",
      "attributes": {
        "name": "Perseverance",
        "landing_date": "2021-02-18",
        "launch_date": "2020-07-30",
        "status": "active",
        "max_sol": 1382,
        "max_date": "2025-11-24",
        "total_photos": 215840
      }
    },
    {
      "id": "opportunity",
      "type": "rover",
      "attributes": {
        "name": "Opportunity",
        "landing_date": "2004-01-25",
        "launch_date": "2003-07-07",
        "status": "complete",
        "max_sol": 5111,
        "max_date": "2018-06-11",
        "total_photos": 198439
      }
    },
    {
      "id": "spirit",
      "type": "rover",
      "attributes": {
        "name": "Spirit",
        "landing_date": "2004-01-04",
        "launch_date": "2003-06-10",
        "status": "complete",
        "max_sol": 2208,
        "max_date": "2010-03-21",
        "total_photos": 124550
      }
    }
  ],
  "meta": {
    "total_count": 4,
    "timestamp": "2025-11-25T12:00:00Z"
  }
}`}
          language="json"
        />
      </section>

      {/* Get Single Rover */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Get Single Rover
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v2/rovers/{'{slug}'}</code>
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/rovers/curiosity"`}
          language="bash"
        />
        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Response
        </h3>
        <CodeBlock
          code={`{
  "data": {
    "id": "curiosity",
    "type": "rover",
    "attributes": {
      "name": "Curiosity",
      "landing_date": "2012-08-06",
      "launch_date": "2011-11-26",
      "status": "active",
      "max_sol": 4728,
      "max_date": "2025-11-24",
      "total_photos": 682660
    },
    "relationships": {
      "cameras": [
        { "id": "FHAZ", "attributes": { "full_name": "Front Hazard Avoidance Camera" } },
        { "id": "RHAZ", "attributes": { "full_name": "Rear Hazard Avoidance Camera" } },
        { "id": "MAST", "attributes": { "full_name": "Mast Camera" } },
        { "id": "NAVCAM", "attributes": { "full_name": "Navigation Camera" } },
        { "id": "CHEMCAM", "attributes": { "full_name": "Chemistry and Camera Complex" } },
        { "id": "MAHLI", "attributes": { "full_name": "Mars Hand Lens Imager" } },
        { "id": "MARDI", "attributes": { "full_name": "Mars Descent Imager" } }
      ]
    }
  }
}`}
          language="json"
        />
      </section>

      {/* Rover Details */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Available Rovers
        </h2>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Slug</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Name</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Status</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Landing</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">curiosity</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Curiosity</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-sm">active</span></td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Aug 6, 2012</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">perseverance</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Perseverance</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-sm">active</span></td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Feb 18, 2021</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">opportunity</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Opportunity</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400 rounded text-sm">complete</span></td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Jan 25, 2004</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">spirit</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Spirit</td>
                <td className="px-4 py-3"><span className="px-2 py-1 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-400 rounded text-sm">complete</span></td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Jan 4, 2004</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}
