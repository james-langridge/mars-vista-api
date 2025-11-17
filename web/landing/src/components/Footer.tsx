export default function Footer() {
  return (
    <footer className="border-t border-gray-800 mt-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div>
            <h3 className="font-semibold mb-4">Resources</h3>
            <ul className="space-y-2">
              <li>
                <a href="https://docs.marsvista.dev" className="text-gray-400 hover:text-white transition-colors">
                  Documentation
                </a>
              </li>
              <li>
                <a href="https://api.marsvista.dev/api/v1/rovers" className="text-gray-400 hover:text-white transition-colors">
                  API Reference
                </a>
              </li>
              <li>
                <a href="https://status.marsvista.dev" className="text-gray-400 hover:text-white transition-colors">
                  Status Page
                </a>
              </li>
            </ul>
          </div>
          <div>
            <h3 className="font-semibold mb-4">Rovers</h3>
            <ul className="space-y-2 text-gray-400">
              <li>Curiosity (2012-present)</li>
              <li>Perseverance (2021-present)</li>
              <li>Opportunity (2004-2018)</li>
              <li>Spirit (2004-2010)</li>
            </ul>
          </div>
          <div>
            <h3 className="font-semibold mb-4">About</h3>
            <p className="text-gray-400">
              Open-source Mars rover imagery API, recreating NASA's Mars Rover Photos API with enhanced data preservation and performance.
            </p>
          </div>
        </div>
        <div className="mt-8 pt-8 border-t border-gray-800 text-center text-gray-400">
          <p>Mars Vista API - Powered by NASA's Mars rover imagery</p>
        </div>
      </div>
    </footer>
  )
}
