import type { Metadata } from 'next';
import { Geist, Geist_Mono } from 'next/font/google';
import './globals.css';
import SiteHeaderWrapper from '@/components/SiteHeaderWrapper';
import Footer from '@/components/Footer';
import CookieConsent from '@/components/CookieConsent';
import { ThemeProvider } from '@/components/docs/ThemeProvider';

const geistSans = Geist({
  variable: '--font-geist-sans',
  subsets: ['latin'],
});

const geistMono = Geist_Mono({
  variable: '--font-geist-mono',
  subsets: ['latin'],
});

export const metadata: Metadata = {
  title: 'Mars Vista API - Mars Rover Imagery API',
  description: 'Access comprehensive Mars rover imagery from Curiosity, Perseverance, Opportunity, and Spirit missions',
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={`${geistSans.variable} ${geistMono.variable} antialiased min-h-screen bg-white dark:bg-slate-900 text-slate-900 dark:text-white`}>
        <ThemeProvider>
          <SiteHeaderWrapper />
          {children}
          <Footer />
          <CookieConsent />
        </ThemeProvider>
      </body>
    </html>
  );
}
