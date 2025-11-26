'use client';

import { useState } from 'react';
import { signIn } from 'next-auth/react';

export default function SignInForm() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      const result = await signIn('resend', {
        email,
        redirect: false,
        callbackUrl: '/api-keys',
      });

      if (result?.error) {
        setError('Failed to send magic link. Please try again.');
        setIsLoading(false);
      } else {
        setIsSubmitted(true);
      }
    } catch (err) {
      setError('An error occurred. Please try again.');
      setIsLoading(false);
    }
  }

  if (isSubmitted) {
    return (
      <div className="bg-green-100 dark:bg-green-900/20 border border-green-300 dark:border-green-700 rounded-lg p-6 text-center">
        <svg
          className="w-16 h-16 text-green-500 mx-auto mb-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
          />
        </svg>
        <h2 className="text-2xl font-bold mb-2 text-slate-900 dark:text-white">Check your email</h2>
        <p className="text-slate-600 dark:text-slate-300 mb-4">
          We&apos;ve sent a magic link to <strong>{email}</strong>
        </p>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          Click the link in the email to sign in and access your API keys. The link will expire in
          24 hours.
        </p>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      <div>
        <label
          htmlFor="email"
          className="block text-sm font-medium mb-2 text-slate-900 dark:text-white"
        >
          Email address
        </label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="w-full px-4 py-3 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-600 focus:border-transparent text-slate-900 dark:text-white"
          placeholder="you@example.com"
          disabled={isLoading}
        />
      </div>

      {error && (
        <div className="bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-700 rounded-lg p-4 text-red-600 dark:text-red-400">
          {error}
        </div>
      )}

      <button
        type="submit"
        disabled={isLoading}
        className="w-full px-6 py-3 bg-orange-600 hover:bg-orange-700 disabled:bg-slate-400 dark:disabled:bg-slate-700 disabled:cursor-not-allowed text-white rounded-lg font-medium transition-colors"
      >
        {isLoading ? 'Sending magic link...' : 'Send magic link'}
      </button>

      <p className="text-sm text-slate-500 dark:text-slate-400 text-center">
        We&apos;ll email you a magic link for a password-free sign in.
      </p>
    </form>
  );
}
