import type { Metadata } from 'next';
import { auth } from '@/server/auth-export';
import { redirect } from 'next/navigation';
import SignInForm from '@/components/SignInForm';

export const metadata: Metadata = {
  title: 'Sign In - Mars Vista API',
  description: 'Sign in to manage your Mars Vista API keys',
};

export default async function SignIn() {
  const session = await auth();

  // Redirect to API keys page if already authenticated
  if (session) {
    redirect('/api-keys');
  }

  return (
    <main className="min-h-[calc(100vh-theme(spacing.16))] flex items-center justify-center px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold mb-2 text-slate-900 dark:text-white">
            Sign in to Mars Vista
          </h1>
          <p className="text-slate-600 dark:text-slate-300">
            Access your API keys and start querying Mars rover photos
          </p>
        </div>

        <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-8 border border-slate-200 dark:border-slate-700">
          <SignInForm />
        </div>

        <p className="text-center text-sm text-slate-500 dark:text-slate-400 mt-6">
          Don&apos;t have an account? One will be created automatically when you sign in.
        </p>
      </div>
    </main>
  );
}
