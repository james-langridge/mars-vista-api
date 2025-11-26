import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Privacy Policy - Mars Vista API',
  description: 'Privacy policy for Mars Vista API - how we collect, use, and protect your data',
};

export default function PrivacyPolicy() {
  const lastUpdated = 'November 26, 2025';

  return (
    <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <h1 className="text-3xl font-bold mb-2 text-slate-900 dark:text-white">Privacy Policy</h1>
      <p className="text-slate-500 dark:text-slate-400 mb-8">Last updated: {lastUpdated}</p>

      <div className="prose prose-slate dark:prose-invert max-w-none">
        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Overview</h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            Mars Vista API (&quot;we&quot;, &quot;our&quot;, or &quot;the Service&quot;) provides access to Mars
            rover imagery. This privacy policy explains how we collect, use, and protect your personal
            information when you use our website and API.
          </p>
          <p className="text-slate-600 dark:text-slate-300">
            We are committed to protecting your privacy and being transparent about our data practices.
            We collect only the minimum data necessary to provide our service.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            Information We Collect
          </h2>

          <h3 className="text-lg font-medium mb-2 text-slate-800 dark:text-slate-200">
            Account Information
          </h3>
          <ul className="list-disc pl-6 mb-4 text-slate-600 dark:text-slate-300 space-y-1">
            <li>
              <strong>Email address:</strong> Required for authentication via magic link sign-in
            </li>
            <li>
              <strong>Account creation date:</strong> When you first signed up
            </li>
          </ul>

          <h3 className="text-lg font-medium mb-2 text-slate-800 dark:text-slate-200">
            API Key Information
          </h3>
          <ul className="list-disc pl-6 mb-4 text-slate-600 dark:text-slate-300 space-y-1">
            <li>
              <strong>API key hash:</strong> A one-way hash of your API key (we cannot retrieve your
              actual key)
            </li>
            <li>
              <strong>Key metadata:</strong> Creation date, last used date, active status
            </li>
          </ul>

          <h3 className="text-lg font-medium mb-2 text-slate-800 dark:text-slate-200">Usage Data</h3>
          <ul className="list-disc pl-6 mb-4 text-slate-600 dark:text-slate-300 space-y-1">
            <li>
              <strong>Rate limit counters:</strong> Hourly and daily request counts per API key (stored
              temporarily in Redis)
            </li>
            <li>
              <strong>Request timestamps:</strong> When API requests are made (for rate limiting only)
            </li>
          </ul>

          <h3 className="text-lg font-medium mb-2 text-slate-800 dark:text-slate-200">
            Technical Data
          </h3>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-1">
            <li>
              <strong>Session cookies:</strong> Used to maintain your logged-in state on the website
            </li>
            <li>
              <strong>Server logs:</strong> May temporarily include IP addresses for security and
              debugging (automatically deleted)
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            How We Use Your Information
          </h2>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Authentication:</strong> To verify your identity and provide secure access to your
              account
            </li>
            <li>
              <strong>API access:</strong> To validate your API key and authorize requests
            </li>
            <li>
              <strong>Rate limiting:</strong> To enforce fair usage limits and prevent abuse
            </li>
            <li>
              <strong>Service communication:</strong> To send magic link sign-in emails (transactional
              only)
            </li>
            <li>
              <strong>Security:</strong> To detect and prevent unauthorized access or abuse
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            What We Don&apos;t Do
          </h2>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>We do not sell your personal information to third parties</li>
            <li>We do not use your data for advertising or marketing</li>
            <li>We do not track your activity across other websites</li>
            <li>We do not share your email with other users</li>
            <li>We do not send promotional emails (only transactional authentication emails)</li>
          </ul>
        </section>

        <section id="cookies" className="mb-8 scroll-mt-20">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Cookies</h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            We use only essential cookies required for the website to function:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Session cookie:</strong> Maintains your logged-in state (expires when you sign out
              or after inactivity)
            </li>
            <li>
              <strong>CSRF token:</strong> Protects against cross-site request forgery attacks
            </li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            We do not use analytics cookies, advertising cookies, or any third-party tracking cookies.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Data Retention</h2>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Account data:</strong> Retained while your account is active, deleted upon account
              deletion
            </li>
            <li>
              <strong>API key data:</strong> Retained while your account is active, deleted upon account
              deletion
            </li>
            <li>
              <strong>Rate limit data:</strong> Automatically expires after 24 hours
            </li>
            <li>
              <strong>Server logs:</strong> Automatically deleted after 30 days
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            Third-Party Services
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            We use the following third-party services to operate Mars Vista API:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Resend:</strong> Email delivery for magic link authentication (
              <a
                href="https://resend.com/legal/privacy-policy"
                className="text-orange-600 dark:text-orange-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
              >
                Privacy Policy
              </a>
              )
            </li>
            <li>
              <strong>Railway:</strong> Cloud hosting infrastructure (
              <a
                href="https://railway.app/legal/privacy"
                className="text-orange-600 dark:text-orange-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
              >
                Privacy Policy
              </a>
              )
            </li>
            <li>
              <strong>Vercel:</strong> Website hosting (
              <a
                href="https://vercel.com/legal/privacy-policy"
                className="text-orange-600 dark:text-orange-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
              >
                Privacy Policy
              </a>
              )
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Your Rights</h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            You have the following rights regarding your personal data:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Access:</strong> Request a copy of your personal data
            </li>
            <li>
              <strong>Correction:</strong> Request correction of inaccurate data
            </li>
            <li>
              <strong>Deletion:</strong> Delete your account and all associated data from your{' '}
              <Link
                href="/api-keys"
                className="text-orange-600 dark:text-orange-400 hover:underline"
              >
                account settings
              </Link>
            </li>
            <li>
              <strong>Export:</strong> Request an export of your data in a portable format
            </li>
            <li>
              <strong>Withdraw consent:</strong> Stop using the service at any time
            </li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            To exercise these rights, please contact us at the email address below or use the account
            deletion feature in your dashboard.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            International Users
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            Mars Vista API is operated from Germany. If you are accessing our service from
            the European Union, European Economic Area, or other regions with data protection laws, your
            personal data may be transferred to and processed in countries outside your region.
          </p>
          <p className="text-slate-600 dark:text-slate-300">
            For EU/EEA users: We process your data under the legal basis of contract performance
            (providing the service you signed up for) and legitimate interests (security and abuse
            prevention). You have rights under GDPR including access, rectification, erasure, and data
            portability.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Security</h2>
          <p className="text-slate-600 dark:text-slate-300">
            We implement appropriate technical and organizational measures to protect your personal
            data, including:
          </p>
          <ul className="list-disc pl-6 mt-4 text-slate-600 dark:text-slate-300 space-y-2">
            <li>API keys are stored as SHA-256 hashes (we cannot retrieve your actual key)</li>
            <li>All data is transmitted over HTTPS/TLS encryption</li>
            <li>Database access is restricted and authenticated</li>
            <li>Regular security updates and monitoring</li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            Changes to This Policy
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            We may update this privacy policy from time to time. We will notify you of any material
            changes by posting the new policy on this page and updating the &quot;Last updated&quot;
            date. We encourage you to review this policy periodically.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">Contact Us</h2>
          <p className="text-slate-600 dark:text-slate-300">
            If you have questions about this privacy policy or your personal data, please contact us:
          </p>
          <ul className="list-disc pl-6 mt-4 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              GitHub:{' '}
              <a
                href="https://github.com/james-langridge/mars-vista-api/issues"
                className="text-orange-600 dark:text-orange-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
              >
                Open an issue
              </a>
            </li>
            <li>
              Website:{' '}
              <a
                href="https://langridge.dev"
                className="text-orange-600 dark:text-orange-400 hover:underline"
                target="_blank"
                rel="noopener noreferrer"
              >
                langridge.dev
              </a>
            </li>
          </ul>
        </section>
      </div>
    </main>
  );
}
