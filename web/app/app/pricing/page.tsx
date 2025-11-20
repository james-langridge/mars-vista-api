import Link from 'next/link';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Pricing - Mars Vista API',
  description: 'Choose the right plan for your Mars rover imagery needs',
};

export default function Pricing() {
  const tiers = [
    {
      name: 'Free',
      price: '$0',
      period: 'forever',
      description: 'Perfect for getting started with Mars rover imagery',
      features: [
        '1,000 requests per hour',
        '10,000 requests per day',
        '5 concurrent requests',
        'All rovers and cameras',
        'Full NASA metadata',
        'Community support',
      ],
      cta: 'Get Started',
      href: '/signin',
      highlighted: false,
      badge: 'Matches NASA\'s limit',
    },
    {
      name: 'Pro',
      price: '$20',
      period: 'per month',
      description: 'For developers building production applications',
      features: [
        '10,000 requests per hour',
        '100,000 requests per day',
        '25 concurrent requests',
        'All rovers and cameras',
        'Full NASA metadata',
        'Priority support',
        'Usage analytics dashboard',
      ],
      cta: 'Contact Sales',
      href: 'mailto:marsvista@langridge.dev',
      highlighted: true,
      badge: '10x NASA\'s limit',
    },
  ];

  return (
    <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <div className="text-center mb-16">
        <h1 className="text-4xl sm:text-5xl font-bold mb-4">Pricing</h1>
        <p className="text-xl text-gray-300 max-w-2xl mx-auto">
          Choose the plan that fits your needs. All plans include access to our complete Mars rover imagery database.
        </p>
        <p className="text-lg text-red-400 mt-4 font-medium">
          Free tier matches NASA&apos;s gateway, Pro tier offers 10x the limits
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-4xl mx-auto mb-16">
        {tiers.map((tier) => (
          <div
            key={tier.name}
            className={`rounded-lg p-8 border ${
              tier.highlighted
                ? 'border-red-600 bg-gray-800 relative'
                : 'border-gray-700 bg-gray-800/50'
            }`}
          >
            {tier.highlighted && (
              <div className="absolute -top-4 left-1/2 transform -translate-x-1/2 bg-red-600 text-white px-3 py-1 rounded-full text-sm font-medium">
                Production Ready
              </div>
            )}
            {tier.badge && (
              <div className="inline-block bg-blue-900/50 border border-blue-700 text-blue-300 px-2 py-1 rounded text-xs font-medium mb-4">
                {tier.badge}
              </div>
            )}
            <div className="mb-6">
              <h3 className="text-2xl font-bold mb-2">{tier.name}</h3>
              <div className="flex items-baseline mb-2">
                <span className="text-4xl font-bold">{tier.price}</span>
                {tier.period && <span className="text-gray-400 ml-2">{tier.period}</span>}
              </div>
              <p className="text-gray-300">{tier.description}</p>
            </div>

            <ul className="space-y-3 mb-8">
              {tier.features.map((feature, index) => (
                <li key={index} className="flex items-start">
                  <svg
                    className="w-5 h-5 text-green-500 mr-2 mt-0.5 flex-shrink-0"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <span className="text-gray-300">{feature}</span>
                </li>
              ))}
            </ul>

            <Link
              href={tier.href}
              className={`block w-full text-center px-6 py-3 rounded-lg font-medium transition-colors ${
                tier.highlighted
                  ? 'bg-red-600 hover:bg-red-700 text-white'
                  : 'bg-gray-700 hover:bg-gray-600 text-white'
              }`}
            >
              {tier.cta}
            </Link>
          </div>
        ))}
      </div>
    </main>
  );
}
