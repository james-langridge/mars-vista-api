import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Performance Guide - Mars Vista API',
  description: 'Optimization tips and performance expectations for the Mars Vista API',
};

export default function Performance() {
  return (
    <div className="bg-white">
      {/* Hero Section */}
      <div className="bg-gradient-to-b from-slate-900 to-slate-800 text-white py-16">
        <div className="container mx-auto px-6 max-w-6xl">
          <h1 className="text-5xl font-bold mb-4">Performance Guide</h1>
          <p className="text-xl text-slate-300">
            Optimization tips and performance expectations for querying 2 million Mars rover photos
          </p>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-6 py-12 max-w-6xl">

        {/* Overview */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Overview</h2>
          <p className="text-slate-700 text-lg">
            The Mars Vista API provides access to nearly 2 million Mars rover photos. We've optimized
            performance extensively, but some queries naturally take longer due to dataset size and complexity.
            This guide helps you understand what to expect and how to optimize your API usage.
          </p>
        </section>

        {/* Response Times */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Expected Response Times</h2>

          {/* Fast Queries */}
          <div className="mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900 flex items-center gap-2">
              <span className="text-green-500">⚡</span> Fast Queries (&lt; 1 second)
            </h3>
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white border border-slate-200 rounded-lg">
                <thead className="bg-green-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Endpoint</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Typical Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Description</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  <tr>
                    <td className="px-6 py-4 text-sm font-mono text-slate-700">/api/v1/rovers</td>
                    <td className="px-6 py-4 text-sm text-slate-600">50-100ms</td>
                    <td className="px-6 py-4 text-sm text-slate-600">List all rovers</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm font-mono text-slate-700">/api/v1/photos/{'{id}'}</td>
                    <td className="px-6 py-4 text-sm text-slate-600">100-300ms</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Direct lookup by photo ID</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm font-mono text-slate-700">/api/v2/photos (simple)</td>
                    <td className="px-6 py-4 text-sm text-slate-600">200-500ms</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Basic rover/camera filters</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Moderate Queries */}
          <div className="mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900 flex items-center gap-2">
              <span className="text-yellow-500">⚡</span> Moderate Queries (1-2 seconds)
            </h3>
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white border border-slate-200 rounded-lg">
                <thead className="bg-yellow-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Query Type</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Typical Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Optimization Tips</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Date ranges</td>
                    <td className="px-6 py-4 text-sm text-slate-600">500ms-1.5s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Use smaller ranges (&lt; 1 year)</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Sol ranges</td>
                    <td className="px-6 py-4 text-sm text-slate-600">500ms-1.5s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Limit to 100-200 sols</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Combined filters</td>
                    <td className="px-6 py-4 text-sm text-slate-600">1-2s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Multiple filters work together efficiently</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Slower Queries */}
          <div className="mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900 flex items-center gap-2">
              <span className="text-orange-500">⚠️</span> Slower Queries (2-5 seconds)
            </h3>
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white border border-slate-200 rounded-lg">
                <thead className="bg-orange-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Query Type</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Typical Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Why It's Slower</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Image quality filters</td>
                    <td className="px-6 py-4 text-sm text-slate-600">2-3s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Scanning dimensions across 2M photos</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Aspect ratio filters</td>
                    <td className="px-6 py-4 text-sm text-slate-600">2-3s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Calculated field across large dataset</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm text-slate-900">Location queries</td>
                    <td className="px-6 py-4 text-sm text-slate-600">1-3s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Aggregating location data</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          {/* Analysis Queries */}
          <div className="mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900 flex items-center gap-2">
              <span className="text-red-500">🔍</span> Analysis Queries (5-16 seconds)
            </h3>
            <div className="overflow-x-auto">
              <table className="min-w-full bg-white border border-slate-200 rounded-lg">
                <thead className="bg-red-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Endpoint</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Typical Time</th>
                    <th className="px-6 py-3 text-left text-sm font-semibold text-slate-900">Performance Notes</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  <tr>
                    <td className="px-6 py-4 text-sm font-mono text-slate-700">/api/v2/panoramas</td>
                    <td className="px-6 py-4 text-sm text-slate-600">5-16s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Always specify sol_min and sol_max</td>
                  </tr>
                  <tr>
                    <td className="px-6 py-4 text-sm font-mono text-slate-700">/api/v2/photos/stats</td>
                    <td className="px-6 py-4 text-sm text-slate-600">2-5s</td>
                    <td className="px-6 py-4 text-sm text-slate-600">Large aggregations across millions</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </section>

        {/* Optimization Tips */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">8 Ways to Optimize Performance</h2>

          <div className="space-y-6">
            {/* Tip 1 */}
            <div className="bg-green-50 border-l-4 border-green-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">1. Use Specific Filters</h3>
              <p className="text-slate-700 mb-4">
                Narrow your search with multiple parameters instead of broad queries.
              </p>
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-red-700 font-semibold mb-2">❌ Slow - Broad query:</p>
                  <pre className="bg-red-900 text-red-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/photos?rovers=curiosity
# Returns thousands, takes 2-3s`}
                  </pre>
                </div>
                <div>
                  <p className="text-sm text-green-700 font-semibold mb-2">✅ Fast - Narrow query:</p>
                  <pre className="bg-green-900 text-green-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/photos?rovers=curiosity
  &sol_min=1000&sol_max=1100
  &cameras=NAVCAM
# Returns hundreds, takes 0.5-1s`}
                  </pre>
                </div>
              </div>
            </div>

            {/* Tip 2 */}
            <div className="bg-blue-50 border-l-4 border-blue-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">2. Paginate Results</h3>
              <p className="text-slate-700 mb-4">
                Request smaller chunks (10-50 per page) instead of large result sets.
              </p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`# Good practice: 25 per page (default)
GET /api/v2/photos?rovers=curiosity&sol=1000

# Best: Request only what you need
GET /api/v2/photos?rovers=curiosity&sol=1000&per_page=10`}
              </pre>
            </div>

            {/* Tip 3 */}
            <div className="bg-purple-50 border-l-4 border-purple-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">3. Use Field Selection</h3>
              <p className="text-slate-700 mb-4">
                Request only the data you need using <code className="bg-purple-200 px-2 py-1 rounded">field_set</code> or <code className="bg-purple-200 px-2 py-1 rounded">fields</code> parameters.
              </p>
              <div className="space-y-3">
                <div>
                  <p className="text-sm font-semibold mb-1">Minimal (id, sol, medium image):</p>
                  <pre className="bg-slate-900 text-slate-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/photos?field_set=minimal`}
                  </pre>
                </div>
                <div>
                  <p className="text-sm font-semibold mb-1">Custom fields:</p>
                  <pre className="bg-slate-900 text-slate-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/photos?fields=id,img_src,sol,earth_date`}
                  </pre>
                </div>
              </div>
            </div>

            {/* Tip 4 */}
            <div className="bg-orange-50 border-l-4 border-orange-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">4. Cache Responses</h3>
              <p className="text-slate-700 mb-4">
                Mars photo data updates infrequently. Implement caching for better performance.
              </p>
              <ul className="space-y-2 text-slate-700 mb-4">
                <li className="flex items-start gap-2">
                  <span className="text-orange-500 mt-1">•</span>
                  <span><strong>Inactive rovers</strong> (Spirit, Opportunity): Cache for 1 year</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500 mt-1">•</span>
                  <span><strong>Active rovers</strong> (Curiosity, Perseverance): Cache for 1 hour</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="text-orange-500 mt-1">•</span>
                  <span>Use ETags for conditional requests (returns 304 Not Modified if unchanged)</span>
                </li>
              </ul>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`# Initial request
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&sol=1000"
# Response includes: ETag: "abc123"

# Subsequent request with ETag
curl -H "X-API-Key: YOUR_KEY" \\
     -H "If-None-Match: \\"abc123\\"" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=opportunity&sol=1000"
# Returns 304 Not Modified if unchanged`}
              </pre>
            </div>

            {/* Tip 5 */}
            <div className="bg-yellow-50 border-l-4 border-yellow-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">5. Avoid Broad Date/Sol Ranges</h3>
              <p className="text-slate-700 mb-4">
                Large ranges may match hundreds of thousands of photos.
              </p>
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <p className="text-sm text-red-700 font-semibold mb-2">❌ Slow:</p>
                  <pre className="bg-red-900 text-red-100 p-3 rounded text-xs overflow-x-auto">
{`?sol_min=1&sol_max=4683
# Thousands of sols, 5-10s`}
                  </pre>
                </div>
                <div>
                  <p className="text-sm text-green-700 font-semibold mb-2">✅ Fast:</p>
                  <pre className="bg-green-900 text-green-100 p-3 rounded text-xs overflow-x-auto">
{`?sol_min=1000&sol_max=1010
# 10 sols, 0.5-1s`}
                  </pre>
                </div>
              </div>
            </div>

            {/* Tip 6 */}
            <div className="bg-indigo-50 border-l-4 border-indigo-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">6. Use Batch Requests</h3>
              <p className="text-slate-700 mb-4">
                If you need multiple specific photos, use the batch endpoint instead of individual requests.
              </p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`# Fast - Single batch request (300-500ms)
POST /api/v2/photos/batch
{"ids": [123456, 123457, 123458]}

# Slow - Multiple individual requests (900ms + 3x network overhead)
GET /api/v2/photos/123456  # 300ms
GET /api/v2/photos/123457  # 300ms
GET /api/v2/photos/123458  # 300ms`}
              </pre>
            </div>

            {/* Tip 7 */}
            <div className="bg-red-50 border-l-4 border-red-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">7. Optimize Panorama Queries</h3>
              <p className="text-slate-700 mb-4">
                Panorama detection is the most computationally expensive operation. Always specify limits.
              </p>
              <div className="space-y-3">
                <div>
                  <p className="text-sm text-red-700 font-semibold mb-2">❌ Very Slow (60-90s):</p>
                  <pre className="bg-red-900 text-red-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/panoramas?rovers=curiosity
# Analyzes all 675K photos`}
                  </pre>
                </div>
                <div>
                  <p className="text-sm text-yellow-700 font-semibold mb-2">⚠️ Moderate (5-10s):</p>
                  <pre className="bg-yellow-900 text-yellow-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=1100
# Analyzes ~50K photos`}
                  </pre>
                </div>
                <div>
                  <p className="text-sm text-green-700 font-semibold mb-2">✅ Fast (2-5s):</p>
                  <pre className="bg-green-900 text-green-100 p-3 rounded text-xs overflow-x-auto">
{`GET /api/v2/panoramas?rovers=curiosity&sol_min=1000&sol_max=1050&min_photos=10
# Analyzes ~25K photos, filters to substantial panoramas`}
                  </pre>
                </div>
              </div>
            </div>

            {/* Tip 8 */}
            <div className="bg-teal-50 border-l-4 border-teal-500 p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">8. Request Appropriate Image Sizes</h3>
              <p className="text-slate-700 mb-4">
                Our API provides 4 image sizes (small/medium/large/full). Request only what you need.
              </p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`# Get only medium images (smaller response)
GET /api/v2/photos?image_sizes=medium

# Get metadata only (smallest response)
GET /api/v2/photos?exclude_images=true`}
              </pre>
            </div>
          </div>
        </section>

        {/* Why Queries Are Slow */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Understanding Slow Queries</h2>

          <div className="space-y-6">
            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Why Panorama Detection is Slow</h3>
              <p className="text-slate-700 mb-3">Panorama detection involves:</p>
              <ol className="list-decimal list-inside space-y-2 text-slate-700">
                <li>Querying photos by sol range</li>
                <li>Grouping by camera and timestamp proximity</li>
                <li>Analyzing camera angle sequences (azimuth, elevation)</li>
                <li>Calculating angular coverage</li>
                <li>Filtering by minimum photo count</li>
              </ol>
              <p className="text-slate-700 mt-3">
                <strong>Solution:</strong> Always specify <code className="bg-slate-200 px-2 py-1 rounded">sol_min</code>, <code className="bg-slate-200 px-2 py-1 rounded">sol_max</code>, and <code className="bg-slate-200 px-2 py-1 rounded">min_photos</code> parameters.
              </p>
            </div>

            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Why Large Date Ranges are Slow</h3>
              <p className="text-slate-700 mb-3">
                Date range queries like <code className="bg-slate-200 px-2 py-1 rounded">date_min=2020-01-01&date_max=2024-12-31</code> may match hundreds of thousands of photos across hundreds of sols.
              </p>
              <p className="text-slate-700">
                <strong>Solution:</strong> Use smaller date ranges (weeks or months instead of years).
              </p>
            </div>

            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Why Image Quality Filters are Slower</h3>
              <p className="text-slate-700 mb-3">
                Filters like <code className="bg-slate-200 px-2 py-1 rounded">min_width=1920&min_height=1080</code> must check dimensions for all 2M photos.
              </p>
              <p className="text-slate-700">
                <strong>Solution:</strong> Combine with other filters to reduce the dataset first.
              </p>
            </div>
          </div>
        </section>

        {/* Best Practices */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Performance Best Practices</h2>

          <div className="grid md:grid-cols-2 gap-8">
            {/* DO */}
            <div>
              <h3 className="text-2xl font-semibold mb-4 text-green-700">✅ DO</h3>
              <ul className="space-y-3">
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Use specific filters (rover, camera, sol/date ranges)</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Limit results with per_page (10-25 for UI, 50-100 for batch)</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Use field_set=minimal for listings</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Cache responses (especially for inactive rovers)</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Implement ETags for conditional requests</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Use batch endpoints for multiple specific photos</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-green-600 text-xl">•</span>
                  <span className="text-slate-700">Specify sol_min/sol_max for panorama queries</span>
                </li>
              </ul>
            </div>

            {/* DON'T */}
            <div>
              <h3 className="text-2xl font-semibold mb-4 text-red-700">❌ DON'T</h3>
              <ul className="space-y-3">
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Query all rovers without filters</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Use per_page=100 by default</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Request field_set=complete unless necessary</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Query broad date ranges (&gt; 1 year)</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Run panorama detection without sol limits</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Make multiple individual requests when batch is available</span>
                </li>
                <li className="flex items-start gap-3">
                  <span className="text-red-600 text-xl">•</span>
                  <span className="text-slate-700">Ignore ETags and Cache-Control headers</span>
                </li>
              </ul>
            </div>
          </div>
        </section>

        {/* Example: Building a Photo Gallery */}
        <section className="mb-16">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">Example: Optimized Photo Gallery</h2>
          <p className="text-slate-700 mb-6">
            Here's how to build a performant photo gallery application:
          </p>

          <div className="space-y-6">
            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-lg font-semibold mb-3 text-slate-900">Gallery Listing (Fast)</h3>
              <p className="text-sm text-slate-600 mb-3">Thumbnail grid: minimal data, small images only</p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`GET /api/v2/photos?rovers=curiosity&sol_min=1000&sol_max=1100&field_set=minimal&image_sizes=small&per_page=50`}
              </pre>
              <p className="text-sm text-slate-600 mt-2">Response time: 500-800ms</p>
            </div>

            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-lg font-semibold mb-3 text-slate-900">Photo Detail (Fast)</h3>
              <p className="text-sm text-slate-600 mb-3">Detail view: extended data, all image sizes</p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`GET /api/v2/photos/123456?field_set=extended`}
              </pre>
              <p className="text-sm text-slate-600 mt-2">Response time: 200-400ms</p>
            </div>

            <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
              <h3 className="text-lg font-semibold mb-3 text-slate-900">User Filters (Fast)</h3>
              <p className="text-sm text-slate-600 mb-3">User filters by date range and camera</p>
              <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`GET /api/v2/photos?rovers=curiosity&date_min=2024-01-01&date_max=2024-01-31&cameras=MAST,NAVCAM&field_set=minimal&per_page=25`}
              </pre>
              <p className="text-sm text-slate-600 mt-2">Response time: 800ms-1.5s</p>
            </div>
          </div>
        </section>

        {/* Back to Docs */}
        <div className="bg-orange-50 border border-orange-200 rounded-lg p-8">
          <h3 className="text-2xl font-semibold mb-4 text-slate-900">Ready to Start Building?</h3>
          <p className="text-slate-700 mb-6">
            Head back to our API documentation to explore all available endpoints and start querying Mars rover photos.
          </p>
          <Link
            href="/docs"
            className="inline-block px-6 py-3 bg-orange-500 hover:bg-orange-600 text-white rounded-lg font-semibold transition"
          >
            View API Documentation
          </Link>
        </div>

      </div>
    </div>
  );
}
