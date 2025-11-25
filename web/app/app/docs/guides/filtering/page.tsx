import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Filtering & Pagination - Mars Vista API',
  description: 'Master advanced query techniques for filtering and paginating Mars photos',
};

export default function FilteringPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Filtering & Pagination
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Master advanced query techniques to find exactly the photos you need.
      </p>

      {/* Basic Filtering */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Basic Filtering
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          Filter by Rover
        </h3>
        <CodeBlock
          code={`# Single rover
curl "...?rovers=curiosity"

# Multiple rovers
curl "...?rovers=curiosity,perseverance"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Filter by Camera
        </h3>
        <CodeBlock
          code={`# Single camera
curl "...?cameras=NAVCAM"

# Multiple cameras
curl "...?cameras=NAVCAM,FHAZ,RHAZ"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Filter by Date
        </h3>
        <CodeBlock
          code={`# By sol range
curl "...?sol_min=1000&sol_max=1100"

# By Earth date range
curl "...?date_min=2024-01-01&date_max=2024-01-31"

# Single date (min = max)
curl "...?date_min=2024-01-15&date_max=2024-01-15"`}
          language="bash"
        />
      </section>

      {/* Combining Filters */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Combining Filters
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Combine multiple filters with <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">&amp;</code>. All filters use AND logic.
        </p>
        <CodeBlock
          code={`# Curiosity NAVCAM photos from Sol 1000
curl "...?rovers=curiosity&cameras=NAVCAM&sol_min=1000&sol_max=1000"

# All rovers, high-res photos from January 2024
curl "...?date_min=2024-01-01&date_max=2024-01-31&min_width=1920&sample_type=Full"`}
          language="bash"
        />
      </section>

      {/* Advanced Filters */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Advanced Filters
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          Location-Based Queries
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          Filter by rover position using site/drive coordinates:
        </p>
        <CodeBlock
          code={`# Exact location
curl "...?site=79&drive=1204"

# Location range (rover's journey)
curl "...?site_min=70&site_max=80"

# Proximity search (within radius)
curl "...?site=79&drive=1204&location_radius=5"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Mars Time Queries
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          Filter by Mars local time when photos were taken:
        </p>
        <CodeBlock
          code={`# Morning photos (6 AM - 10 AM Mars time)
curl "...?mars_time_min=M06:00:00&mars_time_max=M10:00:00"

# Golden hour (sunrise/sunset)
curl "...?mars_time_golden_hour=true"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Image Quality Filters
        </h3>
        <CodeBlock
          code={`# High resolution only
curl "...?min_width=1920&min_height=1080"

# Specific sample type
curl "...?sample_type=Full"

# Aspect ratio range (widescreen)
curl "...?aspect_ratio_min=1.5&aspect_ratio_max=2.0"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Camera Angle Queries
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          Filter by mast camera orientation:
        </p>
        <CodeBlock
          code={`# Horizon shots (elevation near 0)
curl "...?mast_elevation_min=-5&mast_elevation_max=5"

# Looking down (negative elevation)
curl "...?mast_elevation_max=-30"

# Specific compass direction (azimuth 0-360)
curl "...?mast_azimuth_min=90&mast_azimuth_max=180"  # East to South`}
          language="bash"
        />
      </section>

      {/* Sorting */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Sorting Results
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Use the <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">sort</code> parameter to order results.
          Prefix with <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">-</code> for descending order.
        </p>
        <CodeBlock
          code={`# Newest first (default)
curl "...?sort=-earth_date"

# Oldest first
curl "...?sort=earth_date"

# By sol (ascending)
curl "...?sort=sol"

# Multiple sort fields
curl "...?sort=-earth_date,camera"`}
          language="bash"
        />
        <div className="overflow-x-auto mt-4">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Sort Field</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">earth_date</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Earth date taken</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">sol</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mission sol number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">camera</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Camera name</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">id</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Photo ID</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Pagination */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Pagination
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Results are paginated. Use <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">page</code> and
          <code className="bg-slate-100 dark:bg-slate-800 px-1 rounded">per_page</code> to navigate.
        </p>
        <CodeBlock
          code={`# First page, 25 items (default)
curl "...?page=1"

# Second page, 50 items per page
curl "...?page=2&per_page=50"

# Maximum page size (100)
curl "...?per_page=100"`}
          language="bash"
        />

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Pagination Response
        </h3>
        <p className="text-slate-700 dark:text-slate-300 mb-3">
          Every response includes pagination metadata and navigation links:
        </p>
        <CodeBlock
          code={`{
  "data": [...],
  "meta": {
    "total_count": 15234,
    "returned_count": 25
  },
  "pagination": {
    "page": 1,
    "per_page": 25,
    "total_pages": 610
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/photos?page=1",
    "next": "https://api.marsvista.dev/api/v2/photos?page=2",
    "first": "https://api.marsvista.dev/api/v2/photos?page=1",
    "last": "https://api.marsvista.dev/api/v2/photos?page=610"
  }
}`}
          language="json"
        />
        <div className="bg-blue-50 dark:bg-blue-900/20 border-l-4 border-blue-500 p-4 mt-4">
          <p className="text-blue-800 dark:text-blue-200 font-medium">Tip</p>
          <p className="text-blue-700 dark:text-blue-300 text-sm">
            Use the URLs in <code className="bg-blue-100 dark:bg-blue-900/30 px-1 rounded">links</code> for navigation.
            They preserve your filters and sorting.
          </p>
        </div>
      </section>

      {/* Field Selection */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Field Selection
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Control response size by selecting specific fields or using presets.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-3">
          Field Set Presets
        </h3>
        <div className="overflow-x-auto mb-4">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Preset</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Includes</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">minimal</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">id, sol, medium image only</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">standard</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Basic photo data + all image sizes (default)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">extended</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Standard + location, dimensions, Mars time</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">scientific</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">All telemetry, camera angles, coordinates</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">complete</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Everything including raw NASA data</td>
              </tr>
            </tbody>
          </table>
        </div>
        <CodeBlock
          code={`# Use preset
curl "...?field_set=minimal"

# Custom field selection
curl "...?fields=id,sol,earth_date,images"

# Include related resources
curl "...?include=rover,camera"`}
          language="bash"
        />
      </section>

      {/* Common Patterns */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Common Query Patterns
        </h2>
        <div className="space-y-6">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Latest Photos from All Active Rovers
            </h3>
            <CodeBlock
              code={`curl "...?rovers=curiosity,perseverance&sort=-earth_date&per_page=20&include=rover,camera"`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              High-Quality Panoramic Candidates
            </h3>
            <CodeBlock
              code={`curl "...?sample_type=Full&mast_elevation_min=-5&mast_elevation_max=5&min_width=1920&cameras=NAVCAM,MAST"`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Golden Hour Beauty Shots
            </h3>
            <CodeBlock
              code={`curl "...?mars_time_golden_hour=true&rovers=curiosity&sample_type=Full&sort=-earth_date"`}
              language="bash"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2">
              Rover Journey Through Location Range
            </h3>
            <CodeBlock
              code={`curl "...?rovers=perseverance&site_min=1&site_max=50&field_set=extended&sort=sol"`}
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
              href="/docs/reference/photos"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Photos Reference &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Complete parameter documentation
            </span>
          </li>
          <li>
            <Link
              href="/docs/guides/rate-limits"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Rate Limits &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Optimize your API usage
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
