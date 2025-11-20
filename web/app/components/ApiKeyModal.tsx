'use client';

import { useEffect, useRef } from 'react';
import CopyButton from './CopyButton';

interface ApiKeyModalProps {
  apiKey: string;
  isOpen: boolean;
  onClose: () => void;
  isRegenerate?: boolean;
}

export default function ApiKeyModal({ apiKey, isOpen, onClose, isRegenerate = false }: ApiKeyModalProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (isOpen) {
      dialog.showModal();
    } else {
      dialog.close();
    }
  }, [isOpen]);

  const handleClose = () => {
    onClose();
  };

  const handleBackdropClick = (e: React.MouseEvent<HTMLDialogElement>) => {
    // Prevent closing by clicking backdrop - user must explicitly acknowledge
    if (e.target === dialogRef.current) {
      e.preventDefault();
    }
  };

  if (!isOpen) return null;

  return (
    <dialog
      ref={dialogRef}
      onClick={handleBackdropClick}
      className="backdrop:bg-black/80 bg-gray-800 rounded-lg p-0 max-w-2xl w-full border border-gray-700"
    >
      <div className="p-6">
        {/* Header */}
        <div className="mb-6">
          <h2 className="text-2xl font-bold mb-2">
            {isRegenerate ? 'API Key Regenerated!' : 'API Key Generated!'}
          </h2>
          <p className="text-gray-400">
            {isRegenerate
              ? 'Your old API key has been invalidated. Save your new key now.'
              : 'Your API key has been created. Save it now - you won\'t be able to see it again.'}
          </p>
        </div>

        {/* Warning Banner */}
        <div className="bg-yellow-900/20 border border-yellow-700 rounded-lg p-4 mb-6">
          <div className="flex items-start gap-3">
            <span className="text-yellow-500 text-xl flex-shrink-0">⚠️</span>
            <div>
              <p className="text-yellow-400 font-semibold mb-1">
                Save this key immediately
              </p>
              <p className="text-yellow-400/90 text-sm">
                For security reasons, we cannot show you this key again. If you lose it,
                you'll need to regenerate a new one.
              </p>
            </div>
          </div>
        </div>

        {/* API Key Display */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-400 mb-2">
            Your API Key
          </label>
          <div className="flex gap-2">
            <code className="flex-1 bg-gray-900 p-4 rounded-lg font-mono text-sm break-all border border-gray-700">
              {apiKey}
            </code>
            <CopyButton text={apiKey} label="Copy" />
          </div>
        </div>

        {/* Usage Example */}
        <div className="mb-6">
          <h3 className="text-sm font-medium text-gray-400 mb-2">Usage Example</h3>
          <div className="bg-gray-900 p-4 rounded-lg border border-gray-700">
            <code className="text-sm text-gray-300 whitespace-pre-wrap break-all">
{`curl -H "X-API-Key: ${apiKey}" \\
  https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000`}
            </code>
          </div>
        </div>

        {/* Action Button */}
        <div className="flex justify-end">
          <button
            onClick={handleClose}
            className="px-6 py-2 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors"
          >
            I've Saved My Key
          </button>
        </div>
      </div>
    </dialog>
  );
}
