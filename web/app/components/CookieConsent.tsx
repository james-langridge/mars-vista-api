'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';

const COOKIE_CONSENT_KEY = 'marsvista-cookie-consent';

export default function CookieConsent() {
  const [showBanner, setShowBanner] = useState(false);

  useEffect(() => {
    // Check if user has already acknowledged cookies
    const consent = localStorage.getItem(COOKIE_CONSENT_KEY);
    if (!consent) {
      setShowBanner(true);
    }
  }, []);

  const handleAccept = () => {
    localStorage.setItem(COOKIE_CONSENT_KEY, 'accepted');
    setShowBanner(false);
  };

  if (!showBanner) {
    return null;
  }

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 p-4 bg-slate-900 dark:bg-slate-800 border-t border-slate-700">
      <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-4">
        <div className="text-sm text-slate-300">
          <p>
            We use essential cookies for authentication. No tracking or advertising cookies.{' '}
            <Link
              href="/privacy#cookies"
              className="text-orange-400 hover:text-orange-300 underline"
            >
              Learn more
            </Link>
          </p>
        </div>
        <button
          onClick={handleAccept}
          className="px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium transition-colors text-sm whitespace-nowrap"
        >
          Got it
        </button>
      </div>
    </div>
  );
}
