import type { Metadata } from 'next';
import RedocWrapper from '@/components/RedocWrapper';

export const metadata: Metadata = {
  title: 'API Documentation - Mars Vista API',
  description: 'Complete API documentation for Mars Vista rover imagery API',
};

export default function Docs() {
  return (
    <div className="bg-white">
      {/* Hero Section */}
      <div className="bg-gradient-to-b from-slate-900 to-slate-800 text-white py-16">
        <div className="container mx-auto px-6 max-w-5xl">
          <h1 className="text-5xl font-bold mb-4">API Documentation</h1>
          <p className="text-xl text-slate-300 mb-8">
            Comprehensive guide to accessing Mars rover imagery with our powerful API
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
            <a
              href="/pricing"
              className="px-6 py-3 bg-slate-700 hover:bg-slate-600 text-white rounded-lg font-semibold transition"
            >
              View Pricing
            </a>
          </div>
        </div>
      </div>

      {/* API v2 Information Section */}
      <div className="container mx-auto px-6 py-12 max-w-5xl">
        <section className="mb-12">
          <h2 className="text-3xl font-bold mb-6 text-slate-900">API v2 - Latest Version</h2>
          <p className="text-lg text-slate-700 mb-6">
            Our redesigned v2 API offers improved performance, better developer experience, and enhanced features.
          </p>

          {/* Key Improvements */}
          <div className="bg-slate-50 rounded-lg p-6 mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900">Key Improvements</h3>
            <div className="grid md:grid-cols-2 gap-4">
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">Multi-Rover Queries</h4>
                  <p className="text-slate-600">Query photos from multiple rovers in a single request</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">Flexible Filtering</h4>
                  <p className="text-slate-600">Advanced filters for sol, date, camera, and more</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">Field Selection</h4>
                  <p className="text-slate-600">Request only the fields you need with sparse fieldsets</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">HTTP Caching</h4>
                  <p className="text-slate-600">Built-in ETags and Cache-Control for optimal performance</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">JSON:API Format</h4>
                  <p className="text-slate-600">Standardized response structure with relationships</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <span className="text-orange-500 text-2xl">✓</span>
                <div>
                  <h4 className="font-semibold text-slate-900">Better Error Messages</h4>
                  <p className="text-slate-600">Detailed validation errors with field-level feedback</p>
                </div>
              </div>
            </div>
          </div>

          {/* v1 API Notice */}
          <div className="mb-8">
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
              <h3 className="text-xl font-semibold mb-3 text-slate-900">Looking for v1 API?</h3>
              <p className="text-slate-700 mb-3">
                We maintain API v1 for backward compatibility with NASA's original Mars Photo API format.
              </p>
              <p className="text-slate-700">
                For v1 documentation, see the{' '}
                <a
                  href="https://github.com/corincerami/mars-photo-api"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:text-blue-700 font-medium underline"
                >
                  original NASA Mars Photo API repository
                </a>
                . Our v1 endpoints are fully compatible with the documented format.
              </p>
            </div>
          </div>

          {/* Code Examples */}
          <div className="mb-8">
            <h3 className="text-2xl font-semibold mb-4 text-slate-900">Quick Start Examples</h3>
            <div className="space-y-4">
              <div>
                <h4 className="font-semibold text-slate-800 mb-2">Query multiple rovers</h4>
                <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&per_page=10"`}
                </pre>
              </div>
              <div>
                <h4 className="font-semibold text-slate-800 mb-2">Filter by date range and camera</h4>
                <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?cameras=NAVCAM,FHAZ&date_min=2024-01-01&date_max=2024-12-31"`}
                </pre>
              </div>
              <div>
                <h4 className="font-semibold text-slate-800 mb-2">Request specific fields only</h4>
                <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`curl -H "X-API-Key: your_key" \\
  "https://api.marsvista.dev/api/v2/photos?fields=id,img_src,sol&include=rover"`}
                </pre>
              </div>
              <div>
                <h4 className="font-semibold text-slate-800 mb-2">Use HTTP caching with ETags</h4>
                <pre className="bg-slate-900 text-slate-100 p-4 rounded text-sm overflow-x-auto">
{`curl -H "X-API-Key: your_key" \\
     -H "If-None-Match: \\"etag-value\\"" \\
  "https://api.marsvista.dev/api/v2/photos?sol=1000"
# Returns 304 Not Modified if unchanged`}
                </pre>
              </div>
            </div>
          </div>

          {/* Links */}
          <div className="bg-orange-50 border border-orange-200 rounded-lg p-6">
            <h3 className="text-xl font-semibold mb-3 text-slate-900">Additional Resources</h3>
            <ul className="space-y-2 text-slate-700">
              <li>
                <a href="https://api.marsvista.dev/swagger" target="_blank" rel="noopener noreferrer" className="text-orange-600 hover:text-orange-700 font-medium">
                  Interactive Swagger UI →
                </a>
                <span className="text-slate-600"> - Try API endpoints directly in your browser</span>
              </li>
              <li>
                <a href="#examples" className="text-orange-600 hover:text-orange-700 font-medium">
                  Code Examples →
                </a>
                <span className="text-slate-600"> - See v2 features in action with curl examples</span>
              </li>
              <li>
                <a href="/pricing" className="text-orange-600 hover:text-orange-700 font-medium">
                  API Pricing & Rate Limits →
                </a>
                <span className="text-slate-600"> - Free tier available with 1,000 requests/hour</span>
              </li>
            </ul>
          </div>
        </section>
      </div>

      {/* Redoc Documentation */}
      <div className="border-t border-slate-200">
        <RedocWrapper />
      </div>
    </div>
  );
}
