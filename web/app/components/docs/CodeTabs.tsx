'use client';

import { useState } from 'react';
import CodeBlock from '../CodeBlock';

interface CodeExample {
  language: string;
  label: string;
  code: string;
}

interface CodeTabsProps {
  examples: CodeExample[];
}

export default function CodeTabs({ examples }: CodeTabsProps) {
  const [activeTab, setActiveTab] = useState(0);

  if (examples.length === 0) return null;

  return (
    <div className="my-4">
      {/* Tabs */}
      <div className="flex border-b border-slate-200 dark:border-slate-700 bg-slate-100 dark:bg-slate-800 rounded-t-lg">
        {examples.map((example, index) => (
          <button
            key={example.label}
            onClick={() => setActiveTab(index)}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              index === activeTab
                ? 'text-orange-600 dark:text-orange-400 border-b-2 border-orange-600 dark:border-orange-400 -mb-px bg-white dark:bg-slate-900'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            {example.label}
          </button>
        ))}
      </div>

      {/* Code block */}
      <div className="rounded-t-none overflow-hidden">
        <CodeBlock
          code={examples[activeTab].code}
          language={examples[activeTab].language}
        />
      </div>
    </div>
  );
}

// Pre-built example sets for common use cases
export function ApiExampleTabs({ endpoint, params = {} }: { endpoint: string; params?: Record<string, string> }) {
  const queryString = Object.entries(params)
    .map(([key, value]) => `${key}=${value}`)
    .join('&');
  const fullUrl = `https://api.marsvista.dev${endpoint}${queryString ? `?${queryString}` : ''}`;

  const examples: CodeExample[] = [
    {
      label: 'cURL',
      language: 'bash',
      code: `curl -H "X-API-Key: YOUR_API_KEY" \\
  "${fullUrl}"`,
    },
    {
      label: 'JavaScript',
      language: 'javascript',
      code: `const response = await fetch(
  '${fullUrl}',
  {
    headers: {
      'X-API-Key': process.env.MARS_VISTA_API_KEY
    }
  }
);
const data = await response.json();
console.log(data);`,
    },
    {
      label: 'Python',
      language: 'python',
      code: `import requests
import os

response = requests.get(
    '${fullUrl}',
    headers={'X-API-Key': os.environ['MARS_VISTA_API_KEY']}
)
data = response.json()
print(data)`,
    },
    {
      label: 'TypeScript',
      language: 'typescript',
      code: `interface MarsVistaResponse {
  data: Photo[];
  meta: { total_count: number; returned_count: number };
  pagination: { page: number; per_page: number; total_pages: number };
}

const response = await fetch(
  '${fullUrl}',
  {
    headers: {
      'X-API-Key': process.env.MARS_VISTA_API_KEY!
    }
  }
);
const data: MarsVistaResponse = await response.json();`,
    },
  ];

  return <CodeTabs examples={examples} />;
}
