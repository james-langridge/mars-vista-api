import DocsSidebar from '@/components/docs/DocsSidebar';
import MobileDocsSidebar from '@/components/docs/MobileDocsSidebar';
import OnThisPage from '@/components/docs/OnThisPage';

export default function DocsLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="bg-white dark:bg-slate-900 min-h-screen">
      <div className="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex">
          {/* Left sidebar */}
          <DocsSidebar />

          {/* Main content */}
          <main className="flex-1 min-w-0 py-8 lg:pl-8 lg:pr-8">
            <article className="max-w-none">
              {children}
            </article>
          </main>

          {/* Right sidebar - On this page */}
          <OnThisPage />
        </div>
      </div>

      {/* Mobile sidebar */}
      <MobileDocsSidebar />
    </div>
  );
}
