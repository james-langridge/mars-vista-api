export default function Loading() {
  return (
    <div className="flex items-center justify-center min-h-[60vh]">
      <div className="text-center">
        <div className="inline-block animate-spin rounded-full h-12 w-12 border-4 border-gray-700 border-t-red-600 mb-4"></div>
        <p className="text-gray-400">Loading...</p>
      </div>
    </div>
  );
}
