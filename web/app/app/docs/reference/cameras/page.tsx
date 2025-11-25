import type { Metadata } from 'next';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Cameras API Reference - Mars Vista API',
  description: 'Complete reference for the Cameras endpoint',
};

export default function CamerasReferencePage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Cameras API Reference
      </h1>

      {/* List Cameras */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          List All Cameras
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-2 py-1 rounded">GET /api/v2/cameras</code>
        </p>
        <CodeBlock
          code={`curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/cameras"`}
          language="bash"
        />
      </section>

      {/* Camera List */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Available Cameras
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Different rovers have different cameras. Use the camera abbreviation in queries.
        </p>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Curiosity Cameras
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Abbreviation</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Full Name</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">FHAZ</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Front Hazard Avoidance Camera</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Obstacle detection in front</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">RHAZ</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rear Hazard Avoidance Camera</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Obstacle detection behind</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">MAST</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mast Camera</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">High-res panoramic imaging</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">CHEMCAM</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Chemistry and Camera Complex</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Laser-induced spectroscopy</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">MAHLI</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mars Hand Lens Imager</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Close-up rock/soil images</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">MARDI</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mars Descent Imager</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Descent and ground imaging</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">NAVCAM</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Navigation Camera</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Wide-angle navigation</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-lg font-semibold text-slate-900 dark:text-white mt-6 mb-3">
          Perseverance Cameras
        </h3>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Abbreviation</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Full Name</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">EDL_RUCAM</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rover Up-Look Camera</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">EDL_RDCAM</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rover Down-Look Camera</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">EDL_DDCAM</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Descent Stage Down-Look Camera</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">EDL_PUCAM1</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Parachute Up-Look Camera A</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">EDL_PUCAM2</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Parachute Up-Look Camera B</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">NAVCAM_LEFT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Navigation Camera - Left</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">NAVCAM_RIGHT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Navigation Camera - Right</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">MCZ_LEFT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mastcam-Z Left</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">MCZ_RIGHT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Mastcam-Z Right</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">FRONT_HAZCAM_LEFT_A</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Front Hazard Camera Left A</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">FRONT_HAZCAM_RIGHT_A</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Front Hazard Camera Right A</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">REAR_HAZCAM_LEFT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rear Hazard Camera Left</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">REAR_HAZCAM_RIGHT</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Rear Hazard Camera Right</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Query Example */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Querying by Camera
        </h2>
        <CodeBlock
          code={`# Single camera
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?cameras=NAVCAM"

# Multiple cameras
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?cameras=NAVCAM,MAST,FHAZ"`}
          language="bash"
        />
      </section>
    </div>
  );
}
