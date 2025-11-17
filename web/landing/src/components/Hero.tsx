export default function Hero() {
  return (
    <div className="relative overflow-hidden">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24 sm:py-32">
        <div className="text-center">
          <h1 className="text-5xl sm:text-7xl font-bold tracking-tight mb-6">
            Mars Vista API
          </h1>
          <p className="text-xl sm:text-2xl text-gray-300 mb-8 max-w-3xl mx-auto">
            Access comprehensive Mars rover imagery from Curiosity, Perseverance, Opportunity, and Spirit missions
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <a
              href="https://docs.marsvista.dev"
              className="px-8 py-3 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors"
            >
              Read Documentation
            </a>
            <a
              href="https://api.marsvista.dev/api/v1/rovers"
              className="px-8 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-medium transition-colors"
            >
              Try API
            </a>
            <a
              href="https://status.marsvista.dev"
              className="px-8 py-3 bg-gray-700 hover:bg-gray-600 rounded-lg font-medium transition-colors"
            >
              Status
            </a>
          </div>
        </div>
      </div>
    </div>
  )
}
