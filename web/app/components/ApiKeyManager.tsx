'use client';

import { useState, useEffect } from 'react';
import ApiKeyModal from './ApiKeyModal';

interface ApiKeyInfo {
  tier: string;
  createdAt?: string;
  lastUsedAt?: string | null;
  isActive?: boolean;
}

export default function ApiKeyManager() {
  const [apiKeyInfo, setApiKeyInfo] = useState<ApiKeyInfo | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showRegenerateConfirm, setShowRegenerateConfirm] = useState(false);

  // Modal state
  const [showModal, setShowModal] = useState(false);
  const [modalApiKey, setModalApiKey] = useState<string>('');
  const [isRegenerateModal, setIsRegenerateModal] = useState(false);

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
      // Store only metadata, not the key itself
      setApiKeyInfo({
        tier: data.tier,
        createdAt: data.createdAt,
        lastUsedAt: data.lastUsedAt,
        isActive: data.isActive,
      });
    } catch (err) {
      console.error('Failed to fetch API key:', err);
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

      // Store metadata
      setApiKeyInfo({
        tier: data.tier,
        createdAt: data.createdAt,
        lastUsedAt: null,
        isActive: true,
      });

      // Show modal with full key
      setModalApiKey(data.apiKey);
      setIsRegenerateModal(false);
      setShowModal(true);
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

      // Store metadata
      setApiKeyInfo({
        tier: data.tier,
        createdAt: data.createdAt,
        lastUsedAt: null,
        isActive: true,
      });

      // Show modal with full key
      setModalApiKey(data.apiKey);
      setIsRegenerateModal(true);
      setShowModal(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to regenerate API key');
    } finally {
      setIsLoading(false);
    }
  };

  const handleModalClose = () => {
    setShowModal(false);
    setModalApiKey(''); // Clear key from memory
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
    <>
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
            {/* API Key Status */}
            <div>
              <label className="block text-sm font-medium text-gray-400 mb-2">
                Status
              </label>
              <div className="flex items-center gap-2 bg-gray-900 p-3 rounded-lg border border-gray-700">
                <span className="text-green-500">✓</span>
                <span className="font-medium">API Key Active</span>
              </div>
              <p className="text-sm text-gray-500 mt-2">
                Your API key is configured and ready to use. For security, we cannot display it again.
              </p>
            </div>

            {/* Metadata */}
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

            {/* Usage Example */}
            <div className="border-t border-gray-700 pt-4">
              <h3 className="font-semibold mb-2">Usage Example</h3>
              <div className="bg-gray-900 p-4 rounded-lg border border-gray-700">
                <code className="text-sm text-gray-300 whitespace-pre-wrap break-all">
{`curl -H "X-API-Key: YOUR_API_KEY" \\
  https://api.marsvista.dev/api/v1/rovers/curiosity/photos?sol=1000`}
                </code>
              </div>
            </div>

            {/* Regenerate Section */}
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
          </div>
        )}
      </div>

      {/* Modal for displaying API key */}
      <ApiKeyModal
        apiKey={modalApiKey}
        isOpen={showModal}
        onClose={handleModalClose}
        isRegenerate={isRegenerateModal}
      />
    </>
  );
}
