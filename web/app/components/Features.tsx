export default function Features() {
  const features = [
    {
      title: 'Four Mars Rovers',
      description: 'Complete image archives from Curiosity, Perseverance, Opportunity, and Spirit missions',
    },
    {
      title: 'NASA-Compatible API',
      description: "Drop-in replacement for NASA's Mars Rover Photos API with identical endpoints",
    },
    {
      title: 'Complete Data Preservation',
      description: 'Full NASA metadata stored in JSONB for 100% data fidelity',
    },
    {
      title: 'Powerful Filtering',
      description: 'Query by sol, Earth date, camera, and more with flexible parameters',
    },
    {
      title: 'Rate Limiting',
      description: 'Fair usage limits with API key support for higher throughput',
    },
    {
      title: 'PostgreSQL Backend',
      description: 'Reliable, performant database with indexed queries and hybrid storage',
    },
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <h2 className="text-3xl sm:text-4xl font-bold text-center mb-12">Features</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        {features.map((feature, index) => (
          <div key={index} className="bg-gray-800 rounded-lg p-6 border border-gray-700">
            <h3 className="text-xl font-semibold mb-3">{feature.title}</h3>
            <p className="text-gray-300">{feature.description}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
