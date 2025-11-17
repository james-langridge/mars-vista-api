export default function Header() {
  return (
    <header className="bg-gradient-to-r from-[#d14524] to-[#e95e3e] shadow-lg">
      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex items-center gap-3">
          <div className="text-4xl">🔴</div>
          <div>
            <h1 className="text-3xl font-bold text-white">Mars Vista Status</h1>
            <p className="text-white/90 mt-1">
              Real-time system status and uptime monitoring
            </p>
          </div>
        </div>
      </div>
    </header>
  )
}
