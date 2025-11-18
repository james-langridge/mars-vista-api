import Link from 'next/link';

export default function QuickStart() {
  const exampleCode = `# Get photos from Curiosity rover on sol 1000
curl "https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000"

# Get photos from specific camera
curl "https://api.marsvista.dev/api/v1/rovers/perseverance/photos?sol=100&camera=navcam"

# Get photos by Earth date
curl "https://api.marsvista.dev/api/v1/rovers/opportunity/photos?earth_date=2015-06-03"`;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <h2 className="text-3xl sm:text-4xl font-bold text-center mb-12">Quick Start</h2>
      <div className="bg-gray-900 rounded-lg p-6 border border-gray-700">
        <pre className="text-sm sm:text-base overflow-x-auto">
          <code className="text-green-400">{exampleCode}</code>
        </pre>
      </div>
      <div className="mt-8 text-center">
        <p className="text-gray-300 mb-4">
          For detailed API documentation, examples, and guides, visit the documentation site.
        </p>
        <Link href="/docs" className="text-red-500 hover:text-red-400 font-medium">
          View Full Documentation →
        </Link>
      </div>
    </div>
  );
}
