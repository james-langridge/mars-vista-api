import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'API Documentation - Mars Vista API',
  description: 'Complete API documentation for Mars Vista rover imagery API',
};

export default function Docs() {
  return (
    <div className="bg-white">
      {/* Hero Section */}
      <div className="bg-gradient-to-b from-slate-900 to-slate-800 text-white py-16">
        <div className="container mx-auto px-6 max-w-6xl">
          <h1 className="text-5xl font-bold mb-4">API Documentation</h1>
          <p className="text-xl text-slate-300 mb-8">
            Access Mars rover imagery with powerful filtering, location queries, and advanced features
          </p>
          <div className="flex gap-4">
            <a
              href="https://api.marsvista.dev/swagger"
              target="_blank"
              rel="noopener noreferrer"
              className="px-6 py-3 bg-orange-500 hover:bg-orange-600 text-white rounded-lg font-semibold transition"
            >
              Interactive Swagger UI
            </a>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-6 py-12 max-w-6xl">

        {/* Getting Started */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Getting Started</h2>
          <div className="bg-blue-50 border-l-4 border-blue-500 p-6 mb-6">
            <h3 className="text-xl font-semibold mb-2 text-slate-900">Authentication Required</h3>
            <p className="text-slate-700 mb-3">
              All API requests require an API key. Get yours for free by <Link href="/signin" className="text-blue-600 hover:text-blue-700 font-medium underline">signing in</Link> and visiting your dashboard.
            </p>
            <p className="text-slate-700">
              Include your API key in the <code className="bg-blue-100 px-2 py-1 rounded">X-API-Key</code> header:
            </p>
            <CodeBlock code={`curl -H "X-API-Key: your_api_key_here" \\
  "https://api.marsvista.dev/api/v2/photos"`} />
          </div>
        </section>

        {/* API Versions */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">API Versions</h2>
          <div className="grid md:grid-cols-2 gap-6">
            <div className="border border-orange-300 bg-orange-50 rounded-lg p-6">
              <h3 className="text-2xl font-semibold mb-3 text-slate-900">API v2 (Recommended)</h3>
              <p className="text-slate-700 mb-4">
                Modern REST API with powerful filtering, field selection, HTTP caching, and revolutionary features:
              </p>
              <ul className="space-y-2 text-slate-700">
                <li className="flex items-start gap-2">
                  <span className="text-orange-500">•</span>
                  <span>Query by Mars local time (sunrise/sunset photos)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500">•</span>
                  <span>Location-based queries and journey tracking</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500">•</span>
                  <span>Image quality filters (resolution, aspect ratio)</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500">•</span>
                  <span>Camera angle queries for panorama detection</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500">•</span>
                  <span>Multiple image sizes for progressive loading</span>
                </li>
              </ul>
            </div>
            <div className="border border-slate-300 bg-slate-50 rounded-lg p-6">
              <h3 className="text-2xl font-semibold mb-3 text-slate-900">API v1</h3>
              <p className="text-slate-700 mb-4">
                Drop-in replacement for the{' '}
                <a
                  href="https://github.com/corincerami/mars-photo-api"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:text-blue-700 underline"
                >
                  archived original Mars Rover API
                </a>.
              </p>
              <p className="text-slate-600">
                Use v1 for full compatibility with existing Mars Rover API integrations. For new projects, we recommend v2 for access to enhanced features.
              </p>
            </div>
          </div>
        </section>

        {/* Rate Limits */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Rate Limits</h2>
          <p className="text-slate-700 mb-6">
            All responses include rate limit headers.
          </p>
          <div className="border border-orange-300 bg-orange-50 rounded-lg p-6 max-w-md">
            <ul className="space-y-2 text-slate-700">
              <li>10,000 requests/hour</li>
              <li>100,000 requests/day</li>
              <li>50 concurrent requests</li>
            </ul>
          </div>
        </section>

        {/* Core Endpoints */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Core v2 Endpoints</h2>

          {/* Photos Endpoint */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="photos">Photos Query</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/photos</code> - The unified endpoint for querying Mars rover photos with powerful filtering
            </p>

            {/* Basic Examples */}
            <h4 className="text-xl font-semibold mb-3 text-slate-800">Basic Queries</h4>
            <div className="space-y-6 mb-8">
              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Query multiple rovers</h5>
                <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&per_page=10"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Filter by sol range</h5>
                <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Filter by date range and camera</h5>
                <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?cameras=NAVCAM,FHAZ&date_min=2024-01-01&date_max=2024-12-31"`} />
              </div>
            </div>

            {/* Advanced Features */}
            <h4 className="text-xl font-semibold mb-3 text-slate-800">Advanced Filtering</h4>
            <div className="space-y-6 mb-8">
              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Mars time queries (Golden hour photography)</h5>
                <p className="text-slate-600 mb-2 text-sm">Find photos taken during Mars sunrise or sunset for beautiful lighting</p>
                <CodeBlock code={`# Photos during Mars sunrise (6-7 AM Mars time)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?mars_time_min=M06:00:00&mars_time_max=M07:00:00"

# Or use the golden hour filter
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?mars_time_golden_hour=true"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Location-based queries</h5>
                <p className="text-slate-600 mb-2 text-sm">Query photos by rover position (site/drive coordinates)</p>
                <CodeBlock code={`# Photos at specific location
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204"

# Photos near a location (within 5 drives)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?site=79&drive=1204&location_radius=5"

# Photos along rover's journey
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?site_min=70&site_max=80"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Image quality filters</h5>
                <p className="text-slate-600 mb-2 text-sm">Filter by resolution, aspect ratio, or sample type</p>
                <CodeBlock code={`# High resolution only (1920x1080+)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?min_width=1920&min_height=1080"

# Full quality images only (not thumbnails)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?sample_type=Full"

# Widescreen aspect ratio
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?aspect_ratio=16:9"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Camera angle queries</h5>
                <p className="text-slate-600 mb-2 text-sm">Find photos by camera orientation (useful for panoramas)</p>
                <CodeBlock code={`# Looking at horizon (±5 degrees)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_min=-5&mast_elevation_max=5"

# Looking down at ground
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?mast_elevation_max=-30"

# Specific compass direction (east to south)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?mast_azimuth_min=90&mast_azimuth_max=180"`} />
              </div>
            </div>

            {/* Field Selection */}
            <h4 className="text-xl font-semibold mb-3 text-slate-800">Field Selection & Performance</h4>
            <div className="space-y-6 mb-8">
              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Field set presets</h5>
                <p className="text-slate-600 mb-2 text-sm">Control response size with predefined field sets</p>
                <CodeBlock code={`# Minimal: Just id, sol, and medium image
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?field_set=minimal"

# Extended: Adds location, dimensions, Mars time
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?field_set=extended"

# Scientific: All telemetry and coordinates
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?field_set=scientific"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Custom field selection</h5>
                <CodeBlock code={`# Request specific fields only
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?fields=id,img_src,sol,earth_date"

# Include related resources
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?fields=id,img_src&include=rover,camera"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Multiple image sizes</h5>
                <p className="text-slate-600 mb-2 text-sm">Our API provides 4 image sizes for progressive loading</p>
                <CodeBlock code={`# Only medium and large images (saves bandwidth)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?image_sizes=medium,large"

# Metadata only (no images)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?exclude_images=true"`} />
              </div>
            </div>

            {/* Sorting & Pagination */}
            <h4 className="text-xl font-semibold mb-3 text-slate-800">Sorting & Pagination</h4>
            <div className="space-y-6">
              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Sort results</h5>
                <CodeBlock code={`# Sort by date (newest first), then by camera
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?sort=-earth_date,camera"`} />
              </div>

              <div>
                <h5 className="font-semibold text-slate-800 mb-2">Pagination</h5>
                <CodeBlock code={`# Page 2, 50 items per page (max 100)
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?page=2&per_page=50"`} />
              </div>
            </div>
          </div>

          {/* Rovers Endpoint */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="rovers">Rovers</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/rovers</code> - Get information about all Mars rovers
            </p>
            <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/rovers"`} />

            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/rovers/{`{slug}`}</code> - Get specific rover details
            </p>
            <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/rovers/curiosity"`} />
          </div>

          {/* Cameras Endpoint */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="cameras">Cameras</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/cameras</code> - Get all rover cameras with capabilities
            </p>
            <CodeBlock code={`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/cameras"`} />
          </div>
        </section>

        {/* Advanced Features */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Advanced Features</h2>
          <p className="text-slate-700 mb-6">
            These endpoints leverage the complete NASA Mars Rover data to provide powerful capabilities for exploring Mars imagery.
          </p>

          {/* Panoramas */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="panoramas">Panoramas</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/panoramas</code> - Automatically detected panoramic photo sequences
            </p>
            <p className="text-slate-600 mb-4">
              Our system analyzes camera angles and timing to detect when rovers captured panoramic sequences. Get ordered photo sets ready for stitching.
            </p>
            <CodeBlock code={`# Get all panoramas from Curiosity
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=10"

# Get specific panorama with all photos
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/panoramas/pano_curiosity_1000_14"`} />
          </div>

          {/* Locations */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="locations">Locations</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/locations</code> - Unique locations visited by rovers
            </p>
            <p className="text-slate-600 mb-4">
              Track where rovers have been and how many photos were taken at each location. Perfect for creating journey maps and virtual tours.
            </p>
            <CodeBlock code={`# Get all locations with 50+ photos
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/locations?min_photos=50"

# Get locations from specific sol range
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/locations?rovers=perseverance&sol_min=500&sol_max=1000"`} />
          </div>

          {/* Journey */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="journey">Journey Tracking</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/rovers/{`{slug}`}/journey</code> - Track rover movement over time
            </p>
            <p className="text-slate-600 mb-4">
              Visualize a rover's journey with location coordinates, distance traveled, and photos taken at each stop.
            </p>
            <CodeBlock code={`# Get Curiosity's journey for sols 1000-2000
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/rovers/curiosity/journey?sol_min=1000&sol_max=2000"`} />
          </div>

          {/* Time Machine */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="time-machine">Time Machine</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/time-machine</code> - Photos from the same location at different times
            </p>
            <p className="text-slate-600 mb-4">
              See how a location changed over time by finding all photos taken at the same site/drive coordinates during different rover visits.
            </p>
            <CodeBlock code={`# Photos from site 79, drive 1204 at ~2 PM Mars time
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/time-machine?site=79&drive=1204&mars_time=M14:00:00"`} />
          </div>

          {/* Statistics */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="statistics">Photo Statistics</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">GET /api/v2/photos/stats</code> - Aggregated photo statistics
            </p>
            <p className="text-slate-600 mb-4">
              Get analytics on photo counts grouped by camera, rover, or sol. Perfect for dashboards and data analysis.
            </p>
            <CodeBlock code={`# Camera usage statistics for Curiosity in 2024
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos/stats?rovers=curiosity&group_by=camera&date_min=2024-01-01"`} />
          </div>

          {/* Batch Operations */}
          <div className="mb-10">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900" id="batch">Batch Operations</h3>
            <p className="text-slate-700 mb-4">
              <code className="bg-slate-100 px-2 py-1 rounded">POST /api/v2/photos/batch</code> - Retrieve multiple photos by ID
            </p>
            <p className="text-slate-600 mb-4">
              Efficiently fetch up to 100 photos in a single request.
            </p>
            <CodeBlock code={`curl -X POST -H "X-API-Key: your_key" \\
  -H "Content-Type: application/json" \\
  -d '{"ids": [123456, 123457, 123458]}' \\
  "https://api.marsvista.dev/api/v2/photos/batch"`} />
          </div>
        </section>

        {/* Performance Expectations */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Performance Expectations</h2>
          <p className="text-slate-700 mb-6">
            The Mars Vista API queries a database of nearly 2 million Mars photos. While we've optimized for speed, some queries may take longer depending on complexity.
          </p>

          {/* Response Time Table */}
          <div className="mb-8">
            <h3 className="text-xl font-semibold mb-4 text-slate-900">Expected Response Times</h3>
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white border border-slate-200 rounded-lg">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Query Type</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Typical Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Maximum Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Tips</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Single photo</td>
                    <td className="px-6 py-4 text-sm text-slate-600">{'<'} 0.5s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">1s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Use photo ID directly</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Basic filters</td>
                    <td className="px-6 py-4 text-sm text-slate-600">0.5-1s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">2s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Combine filters for efficiency</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Date/sol ranges</td>
                    <td className="px-6 py-4 text-sm text-slate-600">1-2s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">3s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Use smaller ranges</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Image quality filters</td>
                    <td className="px-6 py-4 text-sm text-slate-600">2-3s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">5s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Combine with other filters</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Panorama detection</td>
                    <td className="px-6 py-4 text-sm text-slate-600">5-10s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">16s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Specify sol ranges</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Performance Tips */}
          <div className="space-y-6">
            <h3 className="text-xl font-semibold text-slate-900">Tips for Better Performance</h3>

            <div className="grid md:grid-cols-2 gap-6">
              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h4 className="font-semibold text-green-900 mb-3 flex items-center gap-2">
                  <span className="text-green-600">✓</span>
                  Use Specific Filters
                </h4>
                <p className="text-green-800 text-sm mb-2">Narrow your search with multiple parameters</p>
                <CodeBlock code={`?rovers=curiosity&sol_min=1000&sol_max=1100&cameras=NAVCAM`} />
              </div>

              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h4 className="font-semibold text-green-900 mb-3 flex items-center gap-2">
                  <span className="text-green-600">✓</span>
                  Paginate Results
                </h4>
                <p className="text-green-800 text-sm mb-2">Request smaller chunks (25-50 per page)</p>
                <CodeBlock code={`?per_page=25&page=1`} />
              </div>

              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h4 className="font-semibold text-green-900 mb-3 flex items-center gap-2">
                  <span className="text-green-600">✓</span>
                  Use Field Sets
                </h4>
                <p className="text-green-800 text-sm mb-2">Request only the data you need</p>
                <CodeBlock code={`?field_set=minimal`} />
              </div>

              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h4 className="font-semibold text-green-900 mb-3 flex items-center gap-2">
                  <span className="text-green-600">✓</span>
                  Cache Responses
                </h4>
                <p className="text-green-800 text-sm mb-2">NASA data updates infrequently - use ETags</p>
                <CodeBlock code={`If-None-Match: "etag-value"`} />
              </div>
            </div>

            {/* Why Some Queries Are Slow */}
            <div className="bg-blue-50 border-l-4 border-blue-500 p-6 mt-6">
              <h4 className="text-lg font-semibold mb-3 text-slate-900">Why Some Queries Are Slow</h4>
              <ul className="space-y-2 text-slate-700">
                <li className="flex items-start gap-2">
                  <span className="text-blue-500 mt-1">•</span>
                  <span><strong>Panorama Detection:</strong> Analyzes photo sequences, camera angles, and timing patterns across thousands of photos</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-blue-500 mt-1">•</span>
                  <span><strong>Large Date Ranges:</strong> Queries spanning years may match hundreds of thousands of photos</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-blue-500 mt-1">•</span>
                  <span><strong>Image Quality Filters:</strong> Filtering by dimensions requires checking metadata for all 2M photos</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-blue-500 mt-1">•</span>
                  <span><strong>Complex Filters:</strong> Multiple conditions across a large dataset require careful query planning</span>
                </li>
              </ul>
            </div>

          </div>
        </section>

        {/* Response Format */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Response Format</h2>
          <p className="text-slate-700 mb-4">
            All v2 endpoints return a consistent JSON:API-inspired structure with data, metadata, pagination, and navigation links.
          </p>
          <CodeBlock code={`{
  "data": [
    {
      "id": 123456,
      "type": "photo",
      "attributes": {
        "nasa_id": "NRF_1646_0813073669",
        "sol": 1000,
        "earth_date": "2015-05-30",
        "date_taken_utc": "2015-05-30T10:23:45Z",
        "date_taken_mars": "Sol-1000M14:23:45",

        // Multiple image sizes for progressive loading
        "images": {
          "small": "https://mars.nasa.gov/.../320.jpg",
          "medium": "https://mars.nasa.gov/.../800.jpg",
          "large": "https://mars.nasa.gov/.../1200.jpg",
          "full": "https://mars.nasa.gov/.../full.png"
        },

        // Image properties
        "dimensions": {
          "width": 1920,
          "height": 1080
        },
        "sample_type": "Full",

        // Location data (site/drive coordinates)
        "location": {
          "site": 79,
          "drive": 1204,
          "coordinates": {
            "x": 35.4362,
            "y": 22.5714,
            "z": -9.46445
          }
        },

        // Camera telemetry
        "telemetry": {
          "mast_azimuth": 156.098,
          "mast_elevation": -10.1652
        }
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "type": "rover"
        },
        "camera": {
          "id": "mast",
          "type": "camera",
          "attributes": {
            "full_name": "Mast Camera"
          }
        }
      }
    }
  ],
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
}`} />
        </section>

        {/* Error Handling */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Error Handling</h2>
          <p className="text-slate-700 mb-4">
            Errors follow RFC 7807 Problem Details format with helpful field-level validation messages.
          </p>
          <CodeBlock code={`{
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
      "example": "2023-01-01"
    }
  ]
}`} />
        </section>

        {/* HTTP Caching */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">HTTP Caching</h2>
          <p className="text-slate-700 mb-4">
            All endpoints support ETags and conditional requests for efficient caching. Photos from inactive rovers (Spirit, Opportunity) can be cached for 1 year.
          </p>
          <CodeBlock code={`# First request - get ETag
curl -v -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?sol=1000"
# Response includes: ETag: "abc123xyz"

# Subsequent request with If-None-Match
curl -H "X-API-Key: your_key" \\
     -H "If-None-Match: \\"abc123xyz\\"" \\
  "https://api.marsvista.dev/api/v2/photos?sol=1000"
# Returns 304 Not Modified if unchanged`} />
        </section>

        {/* Use Cases */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Example Use Cases</h2>

          <div className="space-y-8">
            <div className="bg-slate-50 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Build a Mars Photo Gallery</h3>
              <p className="text-slate-700 mb-4">
                Use field sets and image sizes to create responsive, performant galleries:
              </p>
              <CodeBlock code={`# Thumbnail view: minimal data, small images only
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?field_set=minimal&image_sizes=small&per_page=50"

# Detail view: full data with all image sizes
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos/123456?field_set=extended"`} />
            </div>

            <div className="bg-slate-50 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Track Rover Journey</h3>
              <p className="text-slate-700 mb-4">
                Create an interactive map of where rovers have been:
              </p>
              <CodeBlock code={`# Get journey data with coordinates
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/rovers/perseverance/journey?sol_min=0&sol_max=1000"

# Get photos from specific locations
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/locations?rovers=perseverance"`} />
            </div>

            <div className="bg-slate-50 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Create Panoramas</h3>
              <p className="text-slate-700 mb-4">
                Find and download panoramic sequences for stitching:
              </p>
              <CodeBlock code={`# Get detected panoramas
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/panoramas?rovers=curiosity&min_photos=20"

# Or find them manually with camera angles
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?sol=1000&sample_type=Full&mast_elevation_min=-5&mast_elevation_max=5"`} />
            </div>

            <div className="bg-slate-50 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Scientific Analysis</h3>
              <p className="text-slate-700 mb-4">
                Access complete telemetry and metadata for research:
              </p>
              <CodeBlock code={`# Get all data including camera angles and coordinates
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?field_set=scientific&rovers=curiosity"

# Analyze photography patterns
curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos/stats?group_by=camera&date_min=2024-01-01"`} />
            </div>
          </div>
        </section>

        {/* Additional Resources */}
        <section className="mb-16">
          <div className="bg-orange-50 border border-orange-200 rounded-lg p-8">
            <h2 className="text-2xl font-semibold mb-4 text-slate-900">Additional Resources</h2>
            <ul className="space-y-3 text-slate-700">
              <li>
                <a href="https://api.marsvista.dev/swagger" target="_blank" rel="noopener noreferrer" className="text-orange-600 hover:text-orange-700 font-medium text-lg">
                  Interactive Swagger UI →
                </a>
                <p className="text-slate-600 mt-1">Try all endpoints directly in your browser with live examples</p>
              </li>
              <li>
                <Link href="/dashboard" className="text-orange-600 hover:text-orange-700 font-medium text-lg">
                  Get Your API Key →
                </Link>
                <p className="text-slate-600 mt-1">Sign in to generate your free API key and start exploring Mars</p>
              </li>
            </ul>
          </div>
        </section>

      </div>
    </div>
  );
}
