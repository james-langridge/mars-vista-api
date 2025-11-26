import Image from 'next/image';

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

interface LatestPhotosProps {
  photos: Photo[];
}

export default function LatestPhotos({ photos }: LatestPhotosProps) {
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    });
  };

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16">
      <h2 className="text-3xl sm:text-4xl font-bold text-center mb-4 text-slate-900 dark:text-white">
        Latest from Mars
      </h2>
      <p className="text-center text-slate-600 dark:text-slate-300 mb-12 max-w-2xl mx-auto">
        The newest images transmitted from NASA&apos;s Mars rovers, updated daily
      </p>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {photos.map((photo) => (
          <div
            key={photo.id}
            className="group relative bg-slate-100 dark:bg-slate-800 rounded-lg overflow-hidden border border-slate-200 dark:border-slate-700 hover:border-orange-500 dark:hover:border-orange-500 transition-all duration-300 hover:shadow-lg hover:shadow-orange-500/20"
          >
            {/* Image Container with aspect ratio */}
            <div className="relative aspect-square overflow-hidden">
              <Image
                src={photo.imgSrc}
                alt={`${photo.rover.name} - ${photo.camera.fullName} - Sol ${photo.sol}`}
                fill
                sizes="(max-width: 640px) 100vw, (max-width: 1024px) 50vw, (max-width: 1280px) 33vw, 25vw"
                className="object-cover group-hover:scale-105 transition-transform duration-300"
                unoptimized // NASA images are external, so we can't optimize them
              />

              {/* Overlay with camera info on hover */}
              <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/0 to-black/0 opacity-0 group-hover:opacity-100 transition-opacity duration-300">
                <div className="absolute bottom-0 left-0 right-0 p-4">
                  <p className="text-xs text-gray-300 mb-1">{photo.camera.fullName}</p>
                  <p className="text-xs text-gray-400">Sol {photo.sol.toLocaleString()}</p>
                </div>
              </div>
            </div>

            {/* Info bar at bottom */}
            <div className="p-3 bg-slate-100 dark:bg-slate-800">
              <div className="flex items-center justify-between">
                <span className="text-sm font-semibold text-orange-600 dark:text-orange-400">
                  {photo.rover.name}
                </span>
                <span className="text-xs text-slate-500 dark:text-slate-400">
                  {formatDate(photo.earthDate)}
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>

      {photos.length === 0 && (
        <div className="text-center text-slate-500 dark:text-slate-400 py-12">
          <p>No photos available at the moment. Check back soon!</p>
        </div>
      )}
    </div>
  );
}
