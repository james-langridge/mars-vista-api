import type { MDXComponents } from 'mdx/types';
import CodeBlock from '@/components/CodeBlock';
import Link from 'next/link';

// Custom components for MDX rendering
export function useMDXComponents(components: MDXComponents): MDXComponents {
  return {
    // Override default elements with styled versions
    h1: ({ children }) => (
      <h1 className="text-4xl font-bold text-slate-900 dark:text-white mb-6 mt-8 first:mt-0">
        {children}
      </h1>
    ),
    h2: ({ children }) => (
      <h2 className="text-3xl font-bold text-slate-900 dark:text-white mb-4 mt-8 pb-2 border-b border-slate-200 dark:border-slate-700">
        {children}
      </h2>
    ),
    h3: ({ children }) => (
      <h3 className="text-2xl font-semibold text-slate-900 dark:text-white mb-3 mt-6">
        {children}
      </h3>
    ),
    h4: ({ children }) => (
      <h4 className="text-xl font-semibold text-slate-800 dark:text-slate-200 mb-2 mt-4">
        {children}
      </h4>
    ),
    p: ({ children }) => (
      <p className="text-slate-700 dark:text-slate-300 mb-4 leading-7">
        {children}
      </p>
    ),
    a: ({ href, children }) => {
      const isExternal = href?.startsWith('http');
      if (isExternal) {
        return (
          <a
            href={href}
            target="_blank"
            rel="noopener noreferrer"
            className="text-orange-600 dark:text-orange-400 hover:text-orange-700 dark:hover:text-orange-300 underline"
          >
            {children}
          </a>
        );
      }
      return (
        <Link
          href={href || '#'}
          className="text-orange-600 dark:text-orange-400 hover:text-orange-700 dark:hover:text-orange-300 underline"
        >
          {children}
        </Link>
      );
    },
    ul: ({ children }) => (
      <ul className="list-disc list-inside text-slate-700 dark:text-slate-300 mb-4 space-y-2 ml-4">
        {children}
      </ul>
    ),
    ol: ({ children }) => (
      <ol className="list-decimal list-inside text-slate-700 dark:text-slate-300 mb-4 space-y-2 ml-4">
        {children}
      </ol>
    ),
    li: ({ children }) => (
      <li className="leading-7">{children}</li>
    ),
    blockquote: ({ children }) => (
      <blockquote className="border-l-4 border-orange-500 pl-4 py-2 my-4 bg-orange-50 dark:bg-orange-900/20 text-slate-700 dark:text-slate-300 italic">
        {children}
      </blockquote>
    ),
    code: ({ children, className }) => {
      // Check if this is a code block (has language class) or inline code
      const isCodeBlock = className?.includes('language-');
      if (isCodeBlock) {
        const language = className?.replace('language-', '') || 'bash';
        return <CodeBlock code={String(children).trim()} language={language} />;
      }
      // Inline code
      return (
        <code className="bg-slate-100 dark:bg-slate-800 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded text-sm font-mono">
          {children}
        </code>
      );
    },
    pre: ({ children }) => {
      // The code component handles the actual rendering
      return <div className="my-4">{children}</div>;
    },
    table: ({ children }) => (
      <div className="overflow-x-auto my-6">
        <table className="min-w-full border border-slate-200 dark:border-slate-700 rounded-lg">
          {children}
        </table>
      </div>
    ),
    thead: ({ children }) => (
      <thead className="bg-slate-100 dark:bg-slate-800">{children}</thead>
    ),
    tbody: ({ children }) => (
      <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
        {children}
      </tbody>
    ),
    tr: ({ children }) => <tr>{children}</tr>,
    th: ({ children }) => (
      <th className="px-4 py-3 text-left text-sm font-semibold text-slate-900 dark:text-white">
        {children}
      </th>
    ),
    td: ({ children }) => (
      <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">
        {children}
      </td>
    ),
    hr: () => (
      <hr className="my-8 border-t border-slate-200 dark:border-slate-700" />
    ),
    // Pass through any custom components
    ...components,
  };
}
