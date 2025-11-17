export default function Header() {
  return (
    <header className="bg-gradient-to-r from-red-600 to-red-700 text-white shadow-lg">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold">Mars Vista API</h1>
            <p className="text-red-100 text-sm">Documentation</p>
          </div>
          <nav className="flex gap-4">
            <a
              href="https://marsvista.dev"
              className="px-4 py-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
            >
              Home
            </a>
            <a
              href="https://status.marsvista.dev"
              className="px-4 py-2 bg-white/10 hover:bg-white/20 rounded-lg transition-colors"
            >
              Status
            </a>
          </nav>
        </div>
      </div>
    </header>
  )
}
