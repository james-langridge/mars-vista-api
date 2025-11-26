import { auth } from '@/server/auth-export';
import { redirect } from 'next/navigation';
import SignOutButton from '@/components/SignOutButton';

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    redirect('/signin');
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex flex-col md:flex-row gap-8">
        <aside className="md:w-64 flex-shrink-0">
          <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700 sticky top-8">
            <div className="mb-6">
              <p className="text-sm text-slate-500 dark:text-slate-400">Signed in as</p>
              <p className="font-medium truncate text-slate-900 dark:text-white">
                {session.user.email}
              </p>
            </div>

            <div className="pt-6 border-t border-slate-200 dark:border-slate-700">
              <SignOutButton />
            </div>
          </div>
        </aside>

        <main className="flex-1">{children}</main>
      </div>
    </div>
  );
}
