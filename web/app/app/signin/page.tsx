import type { Metadata } from 'next';
import SignInForm from '@/components/SignInForm';

export const metadata: Metadata = {
  title: 'Sign In - Mars Vista API',
  description: 'Sign in to your Mars Vista API dashboard',
};

export default function SignIn() {
  return (
    <main className="min-h-[calc(100vh-theme(spacing.16))] flex items-center justify-center px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold mb-2">Sign in to Mars Vista</h1>
          <p className="text-gray-300">
            Access your dashboard to manage API keys and view usage
          </p>
        </div>

        <div className="bg-gray-800 rounded-lg p-8 border border-gray-700">
          <SignInForm />
        </div>

        <p className="text-center text-sm text-gray-400 mt-6">
          Don&apos;t have an account? One will be created automatically when you sign in.
        </p>
      </div>
    </main>
  );
}
