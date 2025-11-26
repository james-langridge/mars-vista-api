import type { Metadata } from 'next';
import Link from 'next/link';

export const metadata: Metadata = {
  title: 'Terms of Service - Mars Vista API',
  description: 'Terms of service for Mars Vista API',
};

export default function TermsOfService() {
  const lastUpdated = 'November 26, 2025';

  return (
    <main className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
      <h1 className="text-3xl font-bold mb-2 text-slate-900 dark:text-white">Terms of Service</h1>
      <p className="text-slate-500 dark:text-slate-400 mb-8">Last updated: {lastUpdated}</p>

      <div className="prose prose-slate dark:prose-invert max-w-none">
        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            1. Acceptance of Terms
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            By accessing or using the Mars Vista API (&quot;Service&quot;), you agree to be bound by
            these Terms of Service. If you do not agree to these terms, please do not use the Service.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            2. Description of Service
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            Mars Vista API provides programmatic access to Mars rover imagery from NASA missions
            including Curiosity, Perseverance, Opportunity, and Spirit. The Service includes the API
            endpoints, documentation, and web dashboard for API key management.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            3. Account Registration
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">To use the API, you must:</p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>Provide a valid email address</li>
            <li>Generate an API key through the dashboard</li>
            <li>Keep your API key confidential and secure</li>
            <li>Not share your API key with others or publish it in public repositories</li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            You are responsible for all activity that occurs under your API key. If you believe your key
            has been compromised, regenerate it immediately from your dashboard.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            4. Acceptable Use
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">You agree to use the Service only for lawful purposes. You must not:</p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              Attempt to circumvent rate limits or abuse the Service through excessive requests
            </li>
            <li>Use automated tools to create multiple accounts or API keys</li>
            <li>
              Interfere with or disrupt the Service or servers or networks connected to the Service
            </li>
            <li>Attempt to gain unauthorized access to any part of the Service</li>
            <li>Use the Service for any illegal or unauthorized purpose</li>
            <li>Resell or redistribute the Service without authorization</li>
            <li>
              Misrepresent your identity or affiliation when using the Service
            </li>
          </ul>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">5. Rate Limits</h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            The Service enforces rate limits to ensure fair usage for all users:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>
              <strong>Hourly limit:</strong> 10,000 requests per hour
            </li>
            <li>
              <strong>Daily limit:</strong> 100,000 requests per day
            </li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            Rate limit information is included in response headers. If you exceed these limits, requests
            will be rejected with a 429 status code until the limit resets. See our{' '}
            <Link
              href="/docs/guides/rate-limits"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              rate limits guide
            </Link>{' '}
            for more information.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            6. Intellectual Property
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            <strong>Mars rover imagery:</strong> All Mars rover images are courtesy of NASA/JPL-Caltech
            and are in the public domain. You may use these images freely, but we encourage attribution
            to NASA/JPL-Caltech.
          </p>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            <strong>API and Service:</strong> The Mars Vista API code, documentation, and service are
            the property of the Service operator. The API is open source and available under the MIT
            License on{' '}
            <a
              href="https://github.com/james-langridge/mars-vista-api"
              className="text-orange-600 dark:text-orange-400 hover:underline"
              target="_blank"
              rel="noopener noreferrer"
            >
              GitHub
            </a>
            .
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            7. Service Availability
          </h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            We strive to maintain high availability but do not guarantee uninterrupted access to the
            Service. The Service may be temporarily unavailable due to:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>Planned maintenance (we will try to provide advance notice)</li>
            <li>Unplanned outages or technical difficulties</li>
            <li>Updates to the Service</li>
            <li>Factors beyond our control</li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            Check our{' '}
            <a
              href="https://status.marsvista.dev"
              className="text-orange-600 dark:text-orange-400 hover:underline"
              target="_blank"
              rel="noopener noreferrer"
            >
              status page
            </a>{' '}
            for current service status.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            8. Modifications to Service
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            We reserve the right to modify, suspend, or discontinue the Service (or any part thereof) at
            any time, with or without notice. We will not be liable to you or any third party for any
            modification, suspension, or discontinuation of the Service.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">9. Termination</h2>
          <p className="text-slate-600 dark:text-slate-300 mb-4">
            We may terminate or suspend your access to the Service immediately, without prior notice or
            liability, for any reason, including:
          </p>
          <ul className="list-disc pl-6 text-slate-600 dark:text-slate-300 space-y-2">
            <li>Breach of these Terms</li>
            <li>Abuse of the Service or rate limit evasion</li>
            <li>Requests from law enforcement</li>
            <li>Extended periods of inactivity</li>
          </ul>
          <p className="text-slate-600 dark:text-slate-300 mt-4">
            You may terminate your account at any time by deleting it from your{' '}
            <Link
              href="/api-keys"
              className="text-orange-600 dark:text-orange-400 hover:underline"
            >
              account settings
            </Link>
            .
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            10. Disclaimer of Warranties
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            THE SERVICE IS PROVIDED &quot;AS IS&quot; AND &quot;AS AVAILABLE&quot; WITHOUT WARRANTIES OF
            ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO IMPLIED WARRANTIES OF
            MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. WE DO NOT WARRANT
            THAT THE SERVICE WILL BE UNINTERRUPTED, SECURE, OR ERROR-FREE, OR THAT ANY DEFECTS WILL BE
            CORRECTED.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            11. Limitation of Liability
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            TO THE MAXIMUM EXTENT PERMITTED BY LAW, IN NO EVENT SHALL WE BE LIABLE FOR ANY INDIRECT,
            INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES, OR ANY LOSS OF PROFITS OR REVENUES,
            WHETHER INCURRED DIRECTLY OR INDIRECTLY, OR ANY LOSS OF DATA, USE, GOODWILL, OR OTHER
            INTANGIBLE LOSSES, RESULTING FROM (A) YOUR USE OR INABILITY TO USE THE SERVICE; (B) ANY
            UNAUTHORIZED ACCESS TO OR USE OF OUR SERVERS; (C) ANY INTERRUPTION OR CESSATION OF THE
            SERVICE; OR (D) ANY BUGS, VIRUSES, OR OTHER HARMFUL CODE THAT MAY BE TRANSMITTED THROUGH THE
            SERVICE.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            12. Indemnification
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            You agree to indemnify and hold harmless the Service operator from and against any claims,
            damages, losses, liabilities, costs, and expenses (including reasonable attorney&apos;s
            fees) arising from or relating to your use of the Service or your violation of these Terms.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            13. Governing Law
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            These Terms shall be governed by and construed in accordance with the laws of England and
            Wales, without regard to its conflict of law provisions.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            14. Changes to Terms
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            We reserve the right to update these Terms at any time. We will notify you of any material
            changes by posting the new Terms on this page and updating the &quot;Last updated&quot;
            date. Your continued use of the Service after any changes constitutes acceptance of the new
            Terms.
          </p>
        </section>

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">15. Contact</h2>
          <p className="text-slate-600 dark:text-slate-300">
            If you have questions about these Terms, please contact us:
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

        <section className="mb-8">
          <h2 className="text-xl font-semibold mb-4 text-slate-900 dark:text-white">
            16. Privacy Policy
          </h2>
          <p className="text-slate-600 dark:text-slate-300">
            Your use of the Service is also governed by our{' '}
            <Link href="/privacy" className="text-orange-600 dark:text-orange-400 hover:underline">
              Privacy Policy
            </Link>
            , which describes how we collect, use, and protect your personal information.
          </p>
        </section>
      </div>
    </main>
  );
}
