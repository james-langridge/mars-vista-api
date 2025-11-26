interface DatabaseStatistics {
  total_photos: number;
  photos_added_last_7_days: number;
  rover_count: number;
  earliest_photo_date: string;
  latest_photo_date: string;
  last_scrape_at: string;
}

interface StatisticsProps {
  stats: DatabaseStatistics;
}

export default function Statistics({ stats }: StatisticsProps) {

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
      title: 'Unique Photos',
      value: formatNumber(stats.total_photos),
      description: 'Complete archive spanning 4 Mars rovers across 20+ years of exploration',
      fontSize: 'text-4xl',
    },
    {
      title: 'New This Week',
      value: formatNumber(stats.photos_added_last_7_days),
      description: 'Fresh images fetched daily from NASA\'s latest rover transmissions',
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
      <h2 className="text-3xl sm:text-4xl font-bold text-center mb-12 text-slate-900 dark:text-white">
        Mars Vista by the Numbers
      </h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        {statistics.map((stat, index) => (
          <div
            key={index}
            className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700 text-center"
          >
            <div className={`${stat.fontSize} font-bold text-orange-600 dark:text-orange-500 mb-2`}>
              {stat.value}
            </div>
            <h3 className="text-xl font-semibold text-slate-900 dark:text-white mb-3">{stat.title}</h3>
            <p className="text-slate-600 dark:text-slate-300 text-sm">{stat.description}</p>
          </div>
        ))}
      </div>
    </div>
  );
}
