import Link from 'next/link';

export default function Hero() {
  return (
    <div className="relative overflow-hidden">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-24 sm:py-32">
        <div className="text-center">
          <h1 className="text-5xl sm:text-7xl font-bold tracking-tight mb-6">
            Mars Vista API
          </h1>
          <p className="text-xl sm:text-2xl text-gray-300 mb-8 max-w-3xl mx-auto">
            The complete archive of Mars rover imagery. Over 1.9 million photos from Curiosity, Perseverance, Opportunity, and Spirit missions. Updated daily.
          </p>
          <div className="flex justify-center">
            <Link
              href="/signin"
              className="px-12 py-4 bg-red-600 hover:bg-red-700 rounded-lg font-semibold text-lg transition-colors shadow-lg"
            >
              Get Started
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}
