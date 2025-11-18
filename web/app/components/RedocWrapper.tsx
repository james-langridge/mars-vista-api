'use client';

import { RedocStandalone } from 'redoc';

export default function RedocWrapper() {
  const specUrl = process.env.NEXT_PUBLIC_API_URL
    ? `${process.env.NEXT_PUBLIC_API_URL}/openapi.json`
    : 'https://api.marsvista.dev/openapi.json';

  return (
    <RedocStandalone
      specUrl={specUrl}
      options={{
        theme: {
          colors: {
            primary: {
              main: '#dc2626',
            },
          },
          typography: {
            fontFamily: 'var(--font-geist-sans), system-ui, sans-serif',
            code: {
              fontFamily: 'var(--font-geist-mono), monospace',
            },
          },
        },
        hideDownloadButton: false,
        scrollYOffset: 64,
      }}
    />
  );
}
