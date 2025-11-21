import Hero from '@/components/Hero';
import Statistics from '@/components/Statistics';

interface DatabaseStatistics {
  total_photos: number;
  photos_added_last_7_days: number;
  rover_count: number;
  earliest_photo_date: string;
  latest_photo_date: string;
  last_scrape_at: string;
}

async function getStatistics(): Promise<DatabaseStatistics | null> {
  try {
    const res = await fetch('https://api.marsvista.dev/api/v1/statistics', {
      next: { revalidate: 86400 }, // Revalidate once per day (24 hours)
    });

    if (!res.ok) {
      console.error('Failed to fetch statistics:', res.status);
      return null;
    }

    return res.json();
  } catch (error) {
    console.error('Error fetching statistics:', error);
    return null;
  }
}

export default async function Home() {
  const stats = await getStatistics();

  return (
    <main>
      <Hero />
      {stats && <Statistics stats={stats} />}
    </main>
  );
}
