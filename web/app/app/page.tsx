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

// Component interfaces (what LatestPhotos expects)
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

// v2 API response interfaces
interface V2PhotoResource {
  id: number;
  attributes: {
    sol: number;
    earth_date: string;
    images?: {
      medium?: string;
      large?: string;
      full?: string;
    };
    img_src?: string;
  };
  relationships?: {
    rover?: {
      id: string;
      attributes?: {
        name?: string;
        landing_date?: string;
        launch_date?: string;
        status?: string;
      };
    };
    camera?: {
      id: string;
      attributes?: {
        full_name?: string;
      };
    };
  };
}

interface V2PhotosResponse {
  data: V2PhotoResource[];
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
    // Fetch latest photos across all active rovers, sorted by date descending
    const res = await fetch(
      'https://api.marsvista.dev/api/v2/photos?rovers=curiosity,perseverance&sort=-earth_date&per_page=12&include=rover,camera',
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

    const data: V2PhotosResponse = await res.json();

    // Transform v2 API response to component format
    return data.data.map((photo): Photo => ({
      id: photo.id,
      sol: photo.attributes.sol,
      earthDate: photo.attributes.earth_date,
      // Prefer medium for gallery display, fall back through sizes (Curiosity only has full)
      imgSrc: photo.attributes.images?.medium || photo.attributes.images?.large || photo.attributes.images?.full || photo.attributes.img_src || '',
      camera: {
        id: 0,
        name: photo.relationships?.camera?.id || '',
        fullName: photo.relationships?.camera?.attributes?.full_name || photo.relationships?.camera?.id || '',
      },
      rover: {
        id: 0,
        // Capitalize rover name (v2 returns lowercase slug like "curiosity")
        name: (photo.relationships?.rover?.id || '').charAt(0).toUpperCase() + (photo.relationships?.rover?.id || '').slice(1),
        landingDate: photo.relationships?.rover?.attributes?.landing_date || '',
        launchDate: photo.relationships?.rover?.attributes?.launch_date || '',
        status: photo.relationships?.rover?.attributes?.status || '',
      },
    }));
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
