'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';

interface AccountSettingsProps {
  userEmail: string;
}

export default function AccountSettings({ userEmail }: AccountSettingsProps) {
  const router = useRouter();
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [confirmEmail, setConfirmEmail] = useState('');
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDelete = async () => {
    if (confirmEmail !== userEmail) {
      setError('Email does not match');
      return;
    }

    setIsDeleting(true);
    setError(null);

    try {
      const response = await fetch('/api/account/delete', {
        method: 'DELETE',
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to delete account');
      }

      // Redirect to home page after successful deletion
      router.push('/?deleted=true');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete account');
      setIsDeleting(false);
    }
  };

  return (
    <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700">
      <h2 className="text-xl font-bold mb-4 text-slate-900 dark:text-white">Account Settings</h2>

      <div className="space-y-4">
        {/* Account Info */}
        <div>
          <label className="block text-sm font-medium text-slate-500 dark:text-slate-400 mb-2">
            Email
          </label>
          <div className="bg-white dark:bg-slate-900 p-3 rounded-lg border border-slate-200 dark:border-slate-700">
            <span className="text-slate-900 dark:text-white">{userEmail}</span>
          </div>
        </div>

        {/* Data & Privacy */}
        <div className="border-t border-slate-200 dark:border-slate-700 pt-4">
          <h3 className="font-semibold mb-2 text-slate-900 dark:text-white">Data & Privacy</h3>
          <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
            We store your email address for authentication and link it to your API key. Read our{' '}
            <Link href="/privacy" className="text-orange-600 dark:text-orange-400 hover:underline">
              Privacy Policy
            </Link>{' '}
            for details.
          </p>
        </div>

        {/* Danger Zone */}
        <div className="border-t border-slate-200 dark:border-slate-700 pt-4">
          <h3 className="font-semibold mb-2 text-red-600 dark:text-red-400">Danger Zone</h3>

          {!showDeleteConfirm ? (
            <div>
              <p className="text-sm text-slate-600 dark:text-slate-400 mb-3">
                Permanently delete your account and all associated data, including your API key.
              </p>
              <button
                onClick={() => setShowDeleteConfirm(true)}
                className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium transition-colors text-sm"
              >
                Delete Account
              </button>
            </div>
          ) : (
            <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-lg p-4 space-y-4">
              <div className="flex items-start gap-3">
                <svg
                  className="w-6 h-6 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                  />
                </svg>
                <div>
                  <p className="text-red-700 dark:text-red-300 font-medium">
                    This action cannot be undone
                  </p>
                  <p className="text-sm text-red-600 dark:text-red-400 mt-1">
                    This will permanently delete your account, API key, and all associated data.
                  </p>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-red-700 dark:text-red-300 mb-2">
                  Type your email to confirm: <span className="font-mono">{userEmail}</span>
                </label>
                <input
                  type="email"
                  value={confirmEmail}
                  onChange={(e) => setConfirmEmail(e.target.value)}
                  placeholder="your@email.com"
                  className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-red-300 dark:border-red-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-red-500 focus:border-red-500"
                />
              </div>

              {error && (
                <p className="text-red-600 dark:text-red-400 text-sm">{error}</p>
              )}

              <div className="flex gap-3">
                <button
                  onClick={handleDelete}
                  disabled={isDeleting || confirmEmail !== userEmail}
                  className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg font-medium transition-colors text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {isDeleting ? 'Deleting...' : 'Permanently Delete Account'}
                </button>
                <button
                  onClick={() => {
                    setShowDeleteConfirm(false);
                    setConfirmEmail('');
                    setError(null);
                  }}
                  disabled={isDeleting}
                  className="px-4 py-2 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-900 dark:text-white rounded-lg font-medium transition-colors text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
