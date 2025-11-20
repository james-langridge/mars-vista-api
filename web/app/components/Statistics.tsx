'use client';

import { useEffect, useState } from 'react';

interface DatabaseStatistics {
  total_photos: number;
  photos_added_last_7_days: number;
  rover_count: number;
  earliest_photo_date: string;
  latest_photo_date: string;
  last_scrape_at: string;
}

export default function Statistics() {
  const [stats, setStats] = useState<DatabaseStatistics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchStatistics() {
      try {
        const response = await fetch('https://api.marsvista.dev/api/v1/statistics');
        if (!response.ok) {
          throw new Error('Failed to fetch statistics');
        }
        const data = await response.json();
        setStats(data);
      } catch (err) {
        console.error('Error fetching statistics:', err);
        setError('Unable to load statistics');
      } finally {
        setLoading(false);
      }
    }

    fetchStatistics();
  }, []);

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <h2 className="text-3xl sm:text-4xl font-bold text-center mb-12">Mars Vista by the Numbers</h2>
        <div className="text-center text-gray-400">Loading statistics...</div>
      </div>
    );
  }

  if (error || !stats) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
        <h2 className="text-3xl sm:text-4xl font-bold text-center mb-12">Mars Vista by the Numbers</h2>
        <div className="text-center text-gray-400">{error || 'No statistics available'}</div>
      </div>
    );
  }

  const formatNumber = (num: number) => {
    return num.toLocaleString('en-US');
  };

  const formatDateRange = (startDate: string, endDate: string) => {
    const start = new Date(startDate);
    const end = new Date(endDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    const startFormatted = start.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });

    // Check if end date is today or yesterday
    const endDateOnly = new Date(end);
    endDateOnly.setHours(0, 0, 0, 0);

    let endFormatted;
    if (endDateOnly.getTime() === today.getTime()) {
      endFormatted = 'today';
    } else if (endDateOnly.getTime() === yesterday.getTime()) {
      endFormatted = 'yesterday';
    } else {
      endFormatted = end.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      });
    }

    return `${startFormatted} - ${endFormatted}`;
  };

  const statistics = [
    {
      title: 'Total Photos',
      value: formatNumber(stats.total_photos),
      description: 'Complete archive spanning 4 Mars rovers across 20+ years of exploration',
      fontSize: 'text-4xl',
    },
    {
      title: 'New Photos This Week',
      value: formatNumber(stats.photos_added_last_7_days),
      description: 'Fresh images automatically scraped daily from NASA\'s latest rover transmissions',
      fontSize: 'text-4xl',
    },
    {
      title: 'Rovers',
      value: stats.rover_count.toString(),
      description: 'Curiosity, Perseverance, Opportunity, and Spirit missions fully supported',
      fontSize: 'text-4xl',
    },
    {
      title: 'Archive Timeline',
      value: formatDateRange(stats.earliest_photo_date, stats.latest_photo_date),
      description: 'From Spirit\'s first landing to today\'s latest photos from the red planet',
      fontSize: 'text-2xl',
    },
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <h2 className="text-3xl sm:text-4xl font-bold text-center mb-4">Mars Vista by the Numbers</h2>
      <p className="text-center text-gray-300 mb-12 max-w-2xl mx-auto">
        Real-time statistics from our comprehensive Mars rover photo archive, updated daily
      </p>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
        {statistics.map((stat, index) => (
          <div key={index} className="bg-gray-800 rounded-lg p-6 border border-gray-700 text-center">
            <div className={`${stat.fontSize} font-bold text-red-500 mb-2`}>{stat.value}</div>
            <h3 className="text-xl font-semibold mb-3">{stat.title}</h3>
            <p className="text-gray-300 text-sm">{stat.description}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
