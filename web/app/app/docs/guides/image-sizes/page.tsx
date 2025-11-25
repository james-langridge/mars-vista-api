import type { Metadata } from 'next';
import Link from 'next/link';
import CodeBlock from '@/components/CodeBlock';

export const metadata: Metadata = {
  title: 'Image Sizes Guide - Mars Vista API',
  description: 'Learn about the different image sizes available and when to use each one',
};

export default function ImageSizesPage() {
  return (
    <div>
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-4">
        Image Sizes Guide
      </h1>
      <p className="text-xl text-slate-600 dark:text-slate-400 mb-8">
        Each Mars photo is available in multiple sizes for different use cases.
      </p>

      {/* Available Sizes */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Available Sizes
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          The API provides 4 image sizes for each photo:
        </p>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Size</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Max Width</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Typical File Size</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Best For</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">small</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">320px</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">~10-30 KB</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Thumbnails, lists</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">medium</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">800px</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">~50-150 KB</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Previews, galleries</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">large</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">1200px</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">~100-300 KB</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Full-screen views</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">full</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Original</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">~200 KB - 2 MB</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Downloads, analysis</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      {/* Response Format */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Response Format
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Image URLs are returned in the <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded">images</code> object:
        </p>
        <CodeBlock
          code={`{
  "data": [{
    "attributes": {
      "images": {
        "small": "https://mars.nasa.gov/msl-raw-images/..._320.jpg",
        "medium": "https://mars.nasa.gov/msl-raw-images/..._800.jpg",
        "large": "https://mars.nasa.gov/msl-raw-images/..._1200.jpg",
        "full": "https://mars.nasa.gov/msl-raw-images/..._full.jpg"
      }
    }
  }]
}`}
          language="json"
        />
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border-l-4 border-yellow-500 p-4 mt-4">
          <p className="text-yellow-800 dark:text-yellow-200 font-medium">Note on Legacy Field</p>
          <p className="text-yellow-700 dark:text-yellow-300 text-sm">
            The <code className="bg-yellow-100 dark:bg-yellow-900/30 px-1 rounded">img_src</code> field is deprecated and returns an empty string.
            Always use the <code className="bg-yellow-100 dark:bg-yellow-900/30 px-1 rounded">images</code> object instead.
          </p>
        </div>
      </section>

      {/* When to Use Each Size */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          When to Use Each Size
        </h2>

        <div className="space-y-6">
          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2 flex items-center gap-2">
              <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 rounded text-sm font-mono">small</span>
              Thumbnails & Lists
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Use for thumbnail grids, search results, and mobile lists. Loads quickly and saves bandwidth.
            </p>
            <CodeBlock
              code={`// React example: Thumbnail grid
{photos.map(photo => (
  <img
    src={photo.attributes.images.small}
    alt={\`Sol \${photo.attributes.sol}\`}
    className="w-20 h-20 object-cover"
  />
))}`}
              language="jsx"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2 flex items-center gap-2">
              <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 rounded text-sm font-mono">medium</span>
              Galleries & Previews
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Best for photo galleries, card layouts, and preview images. Good balance of quality and size.
            </p>
            <CodeBlock
              code={`// React example: Gallery card
<div className="max-w-md">
  <img
    src={photo.attributes.images.medium}
    alt={\`Sol \${photo.attributes.sol}\`}
    className="w-full rounded-lg"
  />
</div>`}
              language="jsx"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2 flex items-center gap-2">
              <span className="px-2 py-1 bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400 rounded text-sm font-mono">large</span>
              Full-Screen & Detail Views
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Use for lightboxes, modal views, and desktop full-width displays. High quality without huge file sizes.
            </p>
            <CodeBlock
              code={`// React example: Lightbox view
<dialog className="fixed inset-0 bg-black">
  <img
    src={photo.attributes.images.large}
    alt={\`Sol \${photo.attributes.sol}\`}
    className="max-w-full max-h-full object-contain"
  />
</dialog>`}
              language="jsx"
            />
          </div>

          <div className="bg-slate-50 dark:bg-slate-800 rounded-lg p-4">
            <h3 className="font-semibold text-slate-900 dark:text-white mb-2 flex items-center gap-2">
              <span className="px-2 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 rounded text-sm font-mono">full</span>
              Downloads & Analysis
            </h3>
            <p className="text-slate-700 dark:text-slate-300 mb-3">
              Original NASA image. Use for downloads, scientific analysis, or printing. Can be very large.
            </p>
            <CodeBlock
              code={`// Download button
<a
  href={photo.attributes.images.full}
  download={\`mars-\${photo.id}.jpg\`}
>
  Download Full Resolution
</a>`}
              language="jsx"
            />
          </div>
        </div>
      </section>

      {/* Progressive Loading */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Progressive Loading Pattern
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          For the best user experience, implement progressive loading: show a small image first, then load larger sizes on demand.
        </p>
        <CodeBlock
          code={`// React progressive loading example
function MarsPhoto({ photo }) {
  const [src, setSrc] = useState(photo.attributes.images.small);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    // Preload large image
    const img = new Image();
    img.src = photo.attributes.images.large;
    img.onload = () => {
      setSrc(photo.attributes.images.large);
      setLoaded(true);
    };
  }, [photo]);

  return (
    <img
      src={src}
      className={\`transition-opacity \${loaded ? 'opacity-100' : 'opacity-70'}\`}
      alt={\`Mars photo from Sol \${photo.attributes.sol}\`}
    />
  );
}`}
          language="jsx"
        />
      </section>

      {/* Filtering by Size */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Requesting Specific Sizes
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          To reduce response size, you can request only the image sizes you need:
        </p>
        <CodeBlock
          code={`# Only small and medium images
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?image_sizes=small,medium"

# Metadata only (no images)
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?exclude_images=true"`}
          language="bash"
        />
      </section>

      {/* Dimensions */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Image Dimensions
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          Each photo includes dimension information in the <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded">dimensions</code> object:
        </p>
        <CodeBlock
          code={`{
  "attributes": {
    "dimensions": {
      "width": 1920,
      "height": 1080
    },
    "aspect_ratio": 1.78  // computed: width / height
  }
}`}
          language="json"
        />
        <p className="text-slate-700 dark:text-slate-300 mt-4 mb-4">
          You can filter by dimensions:
        </p>
        <CodeBlock
          code={`# High resolution only (1920x1080+)
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?min_width=1920&min_height=1080"

# Widescreen photos only
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?aspect_ratio_min=1.5&aspect_ratio_max=2.0"`}
          language="bash"
        />
      </section>

      {/* Sample Types */}
      <section className="mb-10">
        <h2 className="text-2xl font-bold text-slate-900 dark:text-white mb-4">
          Sample Types
        </h2>
        <p className="text-slate-700 dark:text-slate-300 mb-4">
          NASA categorizes images by their sample type. Filter to get only the quality you need:
        </p>
        <div className="overflow-x-auto">
          <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Type</th>
                <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">Description</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">Full</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Full resolution image</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">Subframe</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Cropped region of interest</td>
              </tr>
              <tr>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300 font-mono">Thumbnail</td>
                <td className="px-4 py-3 text-slate-700 dark:text-slate-300">Low-res preview (often grayscale)</td>
              </tr>
            </tbody>
          </table>
        </div>
        <CodeBlock
          code={`# Full quality images only
curl -H "X-API-Key: YOUR_KEY" \\
  "https://api.marsvista.dev/api/v2/photos?sample_type=Full"`}
          language="bash"
        />
      </section>

      {/* Next steps */}
      <section className="bg-orange-50 dark:bg-orange-900/20 rounded-lg p-6">
        <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
          Next Steps
        </h2>
        <ul className="space-y-2">
          <li>
            <Link
              href="/docs/guides/filtering"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Filtering & Pagination &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              Learn advanced query techniques
            </span>
          </li>
          <li>
            <Link
              href="/docs/reference/photos"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              Photos Reference &rarr;
            </Link>
            <span className="text-slate-600 dark:text-slate-400 ml-2">
              All photo query parameters
            </span>
          </li>
        </ul>
      </section>
    </div>
  );
}
