'use client';

import { RedocStandalone } from 'redoc';

export default function RedocWrapper() {
  // Use bundled OpenAPI spec from public folder
  // This is faster (no network request) and works offline
  const specUrl = '/openapi.json';

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
