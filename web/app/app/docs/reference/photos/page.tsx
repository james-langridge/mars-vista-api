import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Photos API Reference - Mars Vista API',
  description: 'Complete reference for the Photos endpoint',
};

export default function PhotosReferencePage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Photos API Reference
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v2/photos</code>
      </p>

      {/* Overview */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Overview
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Query Mars rover photos with powerful filtering, sorting, and pagination.
          This is the primary endpoint for accessing the photo database.
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity&per_page=10&include=rover,camera"`}
          language="bash"
        />
      </section>

      {/* Query Parameters */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Query Parameters
        </h2>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Basic Filters
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">rovers</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Comma-separated: curiosity, perseverance, opportunity, spirit</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">cameras</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Comma-separated camera names: NAVCAM, FHAZ, RHAZ, MAST, etc.</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">sol_min</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Minimum sol number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">sol_max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Maximum sol number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">date_min</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Minimum Earth date (YYYY-MM-DD)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">date_max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Maximum Earth date (YYYY-MM-DD)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Location Filters
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">site</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Exact site number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">drive</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Exact drive number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">site_min / site_max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Site range for journey tracking</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">location_radius</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Proximity search radius (requires site + drive)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Mars Time Filters
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">mars_time_min</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Minimum Mars local time (MHH:MM:SS)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">mars_time_max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Maximum Mars local time (MHH:MM:SS)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">mars_time_golden_hour</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">boolean</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Filter for sunrise/sunset photos</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Image Quality Filters
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">min_width / min_height</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Minimum image dimensions</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">sample_type</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Full, Subframe, or Thumbnail</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">aspect_ratio_min / max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">number</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Aspect ratio range (width/height)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Camera Angle Filters
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">mast_elevation_min / max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">number</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Camera vertical angle (-90 to +90 degrees)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">mast_azimuth_min / max</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">number</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Camera compass direction (0-360 degrees)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Pagination & Sorting
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Default</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">page</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">1</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Page number</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">per_page</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">integer</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">25</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Items per page (max 100)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">sort</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">-earth_date</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Sort field (prefix - for desc)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Response Control
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Parameter</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">include</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Include related resources: rover, camera</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">field_set</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Field preset: minimal, standard, extended, scientific, complete</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">fields</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Custom field selection (comma-separated)</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">image_sizes</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">string</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Which sizes to include: small, medium, large, full</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono text-sm">exclude_images</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">boolean</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Return metadata only (no image URLs)</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Response Format */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Response Format
        </h2>
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
        "date_taken_utc": "2025-11-24T14:23:45Z",
        "date_taken_mars": "Sol-04728M14:23:45.528",
        "images": {
          "small": "https://mars.nasa.gov/..._320.jpg",
          "medium": "https://mars.nasa.gov/..._800.jpg",
          "large": "https://mars.nasa.gov/..._1200.jpg",
          "full": "https://mars.nasa.gov/..._full.jpg"
        },
        "dimensions": { "width": 1920, "height": 1080 },
        "sample_type": "Full",
        "title": "Sol 4728: Navigation Camera",
        "caption": "This image was taken by...",
        "credit": "NASA/JPL-Caltech",
        "location": {
          "site": 79,
          "drive": 1204,
          "coordinates": { "x": 35.4, "y": 22.5, "z": -9.4 }
        },
        "telemetry": {
          "mast_azimuth": 156.098,
          "mast_elevation": -10.165
        }
      },
      "relationships": {
        "rover": {
          "id": "curiosity",
          "type": "rover",
          "attributes": { "name": "Curiosity", "status": "active" }
        },
        "camera": {
          "id": "NAVCAM",
          "type": "camera",
          "attributes": { "full_name": "Navigation Camera" }
        }
      }
    }
  ],
  "meta": {
    "total_count": 682660,
    "returned_count": 1,
    "timestamp": "2025-11-25T12:00:00Z"
  },
  "pagination": {
    "page": 1,
    "per_page": 25,
    "total_pages": 27307
  },
  "links": {
    "self": "https://api.marsvista.dev/api/v2/photos?page=1",
    "next": "https://api.marsvista.dev/api/v2/photos?page=2",
    "first": "https://api.marsvista.dev/api/v2/photos?page=1",
    "last": "https://api.marsvista.dev/api/v2/photos?page=27307"
  }
}`}
          language="json"
        />
      </section>

      {/* Related Endpoints */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Related Endpoints
        </h2>
        <ul className="space-y-2">
          <li>
            <Link href="/docs/reference/rovers" className="text-orange-600 dark:text-orange-400 hover:underline">
              GET /api/v2/rovers &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">List all rovers</span>
          </li>
          <li>
            <Link href="/docs/reference/cameras" className="text-orange-600 dark:text-orange-400 hover:underline">
              GET /api/v2/cameras &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">List all cameras</span>
          </li>
          <li>
            <Link href="/docs/reference/errors" className="text-orange-600 dark:text-orange-400 hover:underline">
              Error Handling &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">Error response formats</span>
          </li>
        </ul>
      </section>
    </div>
  );
}
