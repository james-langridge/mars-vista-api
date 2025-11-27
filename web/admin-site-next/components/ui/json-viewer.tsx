'use client'

import { useState } from 'react'
import { Button } from './button'
import { Copy, Check } from 'lucide-react'

interface JsonViewerProps {
  data: unknown
  maxHeight?: string
}

export function JsonViewer({ data, maxHeight = '500px' }: JsonViewerProps) {
  const [copied, setCopied] = useState(false)

  const copyToClipboard = async () => {
    await navigator.clipboard.writeText(JSON.stringify(data, null, 2))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div className="relative">
      <Button
        variant="secondary"
        size="sm"
        className="absolute top-2 right-2 z-10 bg-slate-700 hover:bg-slate-600 text-slate-100"
        onClick={copyToClipboard}
      >
        {copied ? (
          <>
            <Check className="h-4 w-4 mr-1" />
            Copied!
          </>
        ) : (
          <>
            <Copy className="h-4 w-4 mr-1" />
            Copy JSON
          </>
        )}
      </Button>
      <pre
        className="bg-slate-950 text-slate-50 p-4 pt-12 rounded-md overflow-auto text-xs font-mono"
        style={{ maxHeight }}
      >
        <JsonSyntaxHighlight data={data} />
      </pre>
    </div>
  )
}

function JsonSyntaxHighlight({ data }: { data: unknown }) {
  const highlight = (obj: unknown, indent = 0): React.ReactNode => {
    const spaces = '  '.repeat(indent)

    if (obj === null) {
      return <span className="text-orange-400">null</span>
    }

    if (typeof obj === 'boolean') {
      return <span className="text-orange-400">{obj.toString()}</span>
    }

    if (typeof obj === 'number') {
      return <span className="text-cyan-400">{obj}</span>
    }

    if (typeof obj === 'string') {
      // Check if it's a URL
      if (obj.startsWith('http://') || obj.startsWith('https://')) {
        return (
          <span className="text-green-400">
            &quot;<a href={obj} target="_blank" rel="noopener noreferrer" className="underline hover:text-green-300">{obj}</a>&quot;
          </span>
        )
      }
      return <span className="text-green-400">&quot;{obj}&quot;</span>
    }

    if (Array.isArray(obj)) {
      if (obj.length === 0) {
        return <span className="text-slate-400">[]</span>
      }

      return (
        <>
          <span className="text-slate-400">[</span>
          {'\n'}
          {obj.map((item, i) => (
            <span key={i}>
              {spaces}  {highlight(item, indent + 1)}
              {i < obj.length - 1 ? <span className="text-slate-400">,</span> : null}
              {'\n'}
            </span>
          ))}
          {spaces}<span className="text-slate-400">]</span>
        </>
      )
    }

    if (typeof obj === 'object') {
      const entries = Object.entries(obj)
      if (entries.length === 0) {
        return <span className="text-slate-400">{'{}'}</span>
      }

      return (
        <>
          <span className="text-slate-400">{'{'}</span>
          {'\n'}
          {entries.map(([key, value], i) => (
            <span key={key}>
              {spaces}  <span className="text-purple-400">&quot;{key}&quot;</span>
              <span className="text-slate-400">: </span>
              {highlight(value, indent + 1)}
              {i < entries.length - 1 ? <span className="text-slate-400">,</span> : null}
              {'\n'}
            </span>
          ))}
          {spaces}<span className="text-slate-400">{'}'}</span>
        </>
      )
    }

    return <span>{String(obj)}</span>
  }

  return <>{highlight(data)}</>
}

export default JsonViewer
