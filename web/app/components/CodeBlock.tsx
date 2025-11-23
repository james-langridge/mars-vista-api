import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { vscDarkPlus } from 'react-syntax-highlighter/dist/esm/styles/prism';

interface CodeBlockProps {
  code: string;
  language?: string;
}

export default function CodeBlock({ code, language = 'bash' }: CodeBlockProps) {
  return (
    <SyntaxHighlighter
      language={language}
      style={vscDarkPlus}
      customStyle={{
        borderRadius: '0.375rem',
        padding: '1rem',
        fontSize: '0.875rem',
        margin: 0,
      }}
      showLineNumbers={false}
    >
      {code}
    </SyntaxHighlighter>
  );
}
