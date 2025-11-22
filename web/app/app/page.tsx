import Hero from '@/components/Hero';
import Statistics from '@/components/Statistics';
import LatestPhotos from '@/components/LatestPhotos';

interface DatabaseStatistics {
  total_photos: number;
  photos_added_last_7_days: number;
  rover_count: number;
  earliest_photo_date: string;
  latest_photo_date: string;
  last_scrape_at: string;
}

interface Camera {
  id: number;
  name: string;
  fullName: string;
}

interface Rover {
  id: number;
  name: string;
  landingDate: string;
  launchDate: string;
  status: string;
}

interface Photo {
  id: number;
  sol: number;
  camera: Camera;
  imgSrc: string;
  earthDate: string;
  rover: Rover;
}

interface PhotosResponse {
  photos: Photo[];
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

async function getLatestPhotos(): Promise<Photo[]> {
  try {
    const res = await fetch(
      'https://api.marsvista.dev/api/v1/rovers/perseverance/latest?per_page=10&format=camelCase',
      {
        headers: {
          'X-API-Key': process.env.MARSVISTA_API_KEY || '',
        },
        next: { revalidate: 86400 }, // Revalidate once per day (24 hours)
      }
    );

    if (!res.ok) {
      console.error('Failed to fetch latest photos:', res.status);
      return [];
    }

    const data: PhotosResponse = await res.json();
    return data.photos;
  } catch (error) {
    console.error('Error fetching latest photos:', error);
    return [];
  }
}

export default async function Home() {
  const [stats, photos] = await Promise.all([
    getStatistics(),
    getLatestPhotos(),
  ]);

  return (
    <main>
      <Hero />
      {stats && <Statistics stats={stats} />}
      <LatestPhotos photos={photos} />
    </main>
  );
}
