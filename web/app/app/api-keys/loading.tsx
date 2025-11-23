export default function DashboardLoading() {
  return (
    <div className="space-y-8">
      <div className="h-12 bg-gray-800 rounded-lg animate-pulse"></div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="h-32 bg-gray-800 rounded-lg animate-pulse"></div>
        <div className="h-32 bg-gray-800 rounded-lg animate-pulse"></div>
        <div className="h-32 bg-gray-800 rounded-lg animate-pulse"></div>
      </div>
      <div className="h-64 bg-gray-800 rounded-lg animate-pulse"></div>
    </div>
  );
}
