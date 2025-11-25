import Link from 'next/link';

export default function Footer() {
  return (
    <footer className="border-t border-gray-800 mt-16">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          <div>
            <h3 className="font-semibold mb-4">Resources</h3>
            <ul className="space-y-2">
              <li>
                <Link href="/" className="text-gray-400 hover:text-white transition-colors">
                  Home
                </Link>
              </li>
              <li>
                <Link href="/docs" className="text-gray-400 hover:text-white transition-colors">
                  Documentation
                </Link>
              </li>
              <li>
                <a
                  href="https://status.marsvista.dev"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-gray-400 hover:text-white transition-colors"
                >
                  Status
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
              Advanced Mars rover imagery API with location search, Mars time filtering, and complete NASA metadata from all four rovers.
            </p>
          </div>
        </div>
        <div className="mt-8 pt-8 border-t border-gray-800 text-center text-gray-400">
          <p>
            Built by{' '}
            <a
              href="https://langridge.dev"
              className="text-gray-300 hover:text-white transition-colors"
              target="_blank"
              rel="noopener noreferrer"
            >
              James Langridge
            </a>
            . The source code is available on{' '}
            <a
              href="https://github.com/james-langridge/mars-vista-api"
              className="text-gray-300 hover:text-white transition-colors"
              target="_blank"
              rel="noopener noreferrer"
            >
              GitHub
            </a>
            .
          </p>
        </div>
      </div>
    </footer>
  );
}
