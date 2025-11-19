'use client';

import { useState, useEffect } from 'react';
import CopyButton from './CopyButton';

interface ApiKeyInfo {
  apiKey?: string;
  maskedKey?: string;
  tier: string;
  createdAt?: string;
  lastUsedAt?: string | null;
}

export default function ApiKeyManager() {
  const [apiKeyInfo, setApiKeyInfo] = useState<ApiKeyInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showKey, setShowKey] = useState(false);
  const [showRegenerateConfirm, setShowRegenerateConfirm] = useState(false);

  useEffect(() => {
    fetchApiKey();
  }, []);

  const fetchApiKey = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await fetch('/api/keys/current');

      if (response.status === 404) {
        // No API key exists yet
        setApiKeyInfo(null);
        return;
      }

      if (!response.ok) {
        throw new Error('Failed to fetch API key');
      }

      const data = await response.json();
      console.log('✅ API Key data received:', data);
      console.log('  - maskedKey:', data.maskedKey);
      console.log('  - tier:', data.tier);
      setApiKeyInfo(data);
    } catch (err) {
      console.error('❌ Failed to fetch API key:', err);
      setError(err instanceof Error ? err.message : 'Failed to fetch API key');
    } finally {
      setIsLoading(false);
    }
  };

  const generateKey = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await fetch('/api/keys/generate', {
        method: 'POST',
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to generate API key');
      }

      const data = await response.json();
      setApiKeyInfo(data);
      setShowKey(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate API key');
    } finally {
      setIsLoading(false);
    }
  };

  const regenerateKey = async () => {
    try {
      setIsLoading(true);
      setError(null);
      setShowRegenerateConfirm(false);
      const response = await fetch('/api/keys/regenerate', {
        method: 'POST',
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || 'Failed to regenerate API key');
      }

      const data = await response.json();
      setApiKeyInfo(data);
      setShowKey(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to regenerate API key');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading && !apiKeyInfo) {
    return (
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <h2 className="text-xl font-bold mb-4">API Key</h2>
        <p className="text-gray-400">Loading...</p>
      </div>
    );
  }

  return (
    <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
      <h2 className="text-xl font-bold mb-4">API Key</h2>

      {error && (
        <div className="bg-red-900/20 border border-red-700 rounded-lg p-4 mb-4">
          <p className="text-red-400">{error}</p>
        </div>
      )}

      {!apiKeyInfo ? (
        <div className="space-y-4">
          <p className="text-gray-400">
            Generate an API key to start making requests to the Mars Vista API.
          </p>
          <button
            onClick={generateKey}
            disabled={isLoading}
            className="px-6 py-2 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? 'Generating...' : 'Generate API Key'}
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-400 mb-2">
              Your API Key
            </label>
            <div className="flex gap-2">
              <code className="flex-1 bg-gray-900 p-3 rounded-lg font-mono text-sm break-all">
                {(() => {
                  const display = showKey && apiKeyInfo.apiKey ? apiKeyInfo.apiKey : apiKeyInfo.maskedKey;
                  console.log('🔍 Rendering key display:', { showKey, hasApiKey: !!apiKeyInfo.apiKey, maskedKey: apiKeyInfo.maskedKey, displaying: display });
                  return display || '(no key data)';
                })()}
              </code>
              {apiKeyInfo.apiKey && (
                <CopyButton text={apiKeyInfo.apiKey} label="Copy" />
              )}
            </div>            {!showKey && apiKeyInfo.apiKey && (
              <button
                onClick={() => setShowKey(true)}
                className="text-sm text-blue-400 hover:text-blue-300 mt-2"
              >
                Show full key
              </button>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <span className="text-gray-400">Tier:</span>
              <span className="ml-2 font-medium capitalize">{apiKeyInfo.tier}</span>
            </div>
            <div>
              <span className="text-gray-400">Created:</span>
              <span className="ml-2 font-medium">
                {apiKeyInfo.createdAt ? new Date(apiKeyInfo.createdAt).toLocaleDateString() : 'N/A'}
              </span>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <h3 className="font-semibold mb-2">Usage Example</h3>
            <div className="bg-gray-900 p-4 rounded-lg">
              <code className="text-sm text-gray-300 whitespace-pre-wrap break-all">
{`curl -H "X-API-Key: ${apiKeyInfo.apiKey || 'YOUR_API_KEY'}" \\
  https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000`}
              </code>
            </div>
          </div>

          <div className="border-t border-gray-700 pt-4">
            {!showRegenerateConfirm ? (
              <button
                onClick={() => setShowRegenerateConfirm(true)}
                disabled={isLoading}
                className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg font-medium transition-colors text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Regenerate API Key
              </button>
            ) : (
              <div className="bg-yellow-900/20 border border-yellow-700 rounded-lg p-4 space-y-3">
                <p className="text-yellow-400 text-sm">
                  Are you sure you want to regenerate your API key? Your old key will stop working immediately.
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={regenerateKey}
                    disabled={isLoading}
                    className="px-4 py-2 bg-red-600 hover:bg-red-700 rounded-lg font-medium transition-colors text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isLoading ? 'Regenerating...' : 'Yes, Regenerate'}
                  </button>
                  <button
                    onClick={() => setShowRegenerateConfirm(false)}
                    disabled={isLoading}
                    className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg font-medium transition-colors text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Cancel
                  </button>
                </div>
              </div>
            )}
          </div>

          {showKey && apiKeyInfo.apiKey && (
            <div className="bg-blue-900/20 border border-blue-700 rounded-lg p-4">
              <p className="text-blue-400 text-sm">
                <strong>Important:</strong> This is the only time you'll see your full API key.
                Make sure to copy it now. If you lose it, you'll need to regenerate a new one.
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
