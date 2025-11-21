import Link from 'next/link';

export default function SwaggerPage() {
  return (
    <div className="min-h-screen bg-gradient-to-b from-neutral-900 to-black flex items-center justify-center p-4">
      <div className="max-w-2xl w-full bg-neutral-800 border border-neutral-700 rounded-lg p-8 text-center">
        <h1 className="text-3xl font-bold text-white mb-4">API Documentation</h1>
        <p className="text-neutral-300 mb-8">
          View our interactive API documentation powered by Swagger UI
        </p>
        <a
          href="https://api.marsvista.dev/swagger"
          target="_blank"
          rel="noopener noreferrer"
          className="inline-block bg-orange-600 hover:bg-orange-700 text-white font-semibold px-8 py-3 rounded-lg transition-colors"
        >
          Open API Documentation
        </a>
      </div>
    </div>
  );
}
