'use client';

import { useState, useCallback, useEffect } from 'react';
import Image from 'next/image';

interface PhotoResource {
  id: number;
  type: 'photo';
  attributes: {
    nasa_id?: string;
    sol?: number;
    earth_date?: string;
    date_taken_utc?: string;
    date_taken_mars?: string;
    images?: {
      small?: string;
      medium?: string;
      large?: string;
      full?: string;
    };
    dimensions?: {
      width: number;
      height: number;
    };
    sample_type?: string;
    location?: {
      site?: number;
      drive?: number;
    };
    telemetry?: {
      mast_azimuth?: number;
      mast_elevation?: number;
    };
    img_src?: string;
  };
  relationships?: {
    rover?: {
      id: string;
      type: 'rover';
      attributes?: {
        name: string;
        status: string;
      };
    };
    camera?: {
      id: string;
      type: 'camera';
      attributes?: {
        full_name: string;
      };
    };
  };
}

interface PhotosResponse {
  data: PhotoResource[];
  meta?: {
    total_count?: number;
    returned_count: number;
    timestamp: string;
  };
  pagination?: {
    page?: number;
    per_page: number;
    total_pages?: number;
  };
}

interface RateLimitInfo {
  limit: number;
  remaining: number;
  reset: string;
}

interface SearchParams {
  rovers: string;
  cameras: string;
  solMin: string;
  solMax: string;
  dateMin: string;
  dateMax: string;
  sampleType: string;
  site: string;
  drive: string;
  nasaId: string;
}

const API_BASE_URL = 'https://api.marsvista.dev';

const ROVERS = ['curiosity', 'perseverance', 'opportunity', 'spirit'];
const CAMERAS: Record<string, string[]> = {
  curiosity: ['FHAZ', 'RHAZ', 'MAST', 'CHEMCAM', 'MAHLI', 'MARDI', 'NAVCAM'],
  perseverance: [
    'EDL_RUCAM',
    'EDL_RDCAM',
    'EDL_DDCAM',
    'EDL_PUCAM1',
    'EDL_PUCAM2',
    'NAVCAM_LEFT',
    'NAVCAM_RIGHT',
    'MCZ_LEFT',
    'MCZ_RIGHT',
    'FRONT_HAZCAM_LEFT_A',
    'FRONT_HAZCAM_RIGHT_A',
    'REAR_HAZCAM_LEFT',
    'REAR_HAZCAM_RIGHT',
    'SKYCAM',
    'SHERLOC_WATSON',
  ],
  opportunity: ['FHAZ', 'RHAZ', 'NAVCAM', 'PANCAM', 'MINITES'],
  spirit: ['FHAZ', 'RHAZ', 'NAVCAM', 'PANCAM', 'MINITES'],
};
const SAMPLE_TYPES = ['Full', 'Thumbnail', 'Subframe'];

export default function ExplorerClient() {
  const [apiKey, setApiKey] = useState('');
  const [savedKey, setSavedKey] = useState(false);

  const [searchParams, setSearchParams] = useState<SearchParams>({
    rovers: '',
    cameras: '',
    solMin: '',
    solMax: '',
    dateMin: '',
    dateMax: '',
    sampleType: '',
    site: '',
    drive: '',
    nasaId: '',
  });

  const [results, setResults] = useState<PhotosResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rateLimit, setRateLimit] = useState<RateLimitInfo | null>(null);

  const [viewMode, setViewMode] = useState<'table' | 'json'>('table');
  const [expandedRow, setExpandedRow] = useState<number | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [codeLanguage, setCodeLanguage] = useState<'curl' | 'javascript' | 'python'>('curl');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [copiedCode, setCopiedCode] = useState(false);

  // Load saved API key from localStorage
  useEffect(() => {
    const saved = localStorage.getItem('marsvista_api_key');
    if (saved) {
      setApiKey(saved);
      setSavedKey(true);
    }
  }, []);

  const saveApiKey = () => {
    if (apiKey.trim()) {
      localStorage.setItem('marsvista_api_key', apiKey.trim());
      setSavedKey(true);
    }
  };

  const clearApiKey = () => {
    localStorage.removeItem('marsvista_api_key');
    setApiKey('');
    setSavedKey(false);
  };

  const buildQueryString = useCallback(
    (page = 1) => {
      const params = new URLSearchParams();

      if (searchParams.rovers) params.set('rovers', searchParams.rovers);
      if (searchParams.cameras) params.set('cameras', searchParams.cameras);
      if (searchParams.solMin) params.set('sol_min', searchParams.solMin);
      if (searchParams.solMax) params.set('sol_max', searchParams.solMax);
      if (searchParams.dateMin) params.set('date_min', searchParams.dateMin);
      if (searchParams.dateMax) params.set('date_max', searchParams.dateMax);
      if (searchParams.sampleType) params.set('sample_type', searchParams.sampleType);
      if (searchParams.site) params.set('site', searchParams.site);
      if (searchParams.drive) params.set('drive', searchParams.drive);
      if (searchParams.nasaId) params.set('nasa_id', searchParams.nasaId);

      params.set('include', 'rover,camera');
      params.set('field_set', 'extended');
      params.set('page', page.toString());
      params.set('per_page', '25');

      return params.toString();
    },
    [searchParams]
  );

  const getApiUrl = useCallback(
    (page = 1) => {
      const queryString = buildQueryString(page);
      return `${API_BASE_URL}/api/v2/photos?${queryString}`;
    },
    [buildQueryString]
  );

  const generateCode = useCallback(() => {
    const url = getApiUrl(currentPage);
    const maskedKey = apiKey ? `${apiKey.slice(0, 12)}...` : 'YOUR_API_KEY';

    switch (codeLanguage) {
      case 'curl':
        return `curl -H "X-API-Key: ${maskedKey}" \\
  "${url}"`;

      case 'javascript':
        return `const response = await fetch(
  "${url}",
  {
    headers: { "X-API-Key": "${maskedKey}" }
  }
);
const data = await response.json();
console.log(data);`;

      case 'python': {
        const params = new URLSearchParams(buildQueryString(currentPage));
        const paramsObj: Record<string, string> = {};
        params.forEach((value, key) => {
          paramsObj[key] = value;
        });

        return `import requests

response = requests.get(
    "${API_BASE_URL}/api/v2/photos",
    params=${JSON.stringify(paramsObj, null, 8)},
    headers={"X-API-Key": "${maskedKey}"}
)
data = response.json()
print(data)`;
      }

      default:
        return '';
    }
  }, [codeLanguage, getApiUrl, buildQueryString, currentPage, apiKey]);

  const handleSearch = async (page = 1) => {
    if (!apiKey.trim()) {
      setError('Please enter your API key first');
      return;
    }

    setIsLoading(true);
    setError(null);
    setCurrentPage(page);

    try {
      const response = await fetch(getApiUrl(page), {
        method: 'GET',
        headers: {
          'X-API-Key': apiKey.trim(),
        },
      });

      // Extract rate limit headers
      const limitHeader = response.headers.get('X-RateLimit-Limit');
      const remainingHeader = response.headers.get('X-RateLimit-Remaining');
      const resetHeader = response.headers.get('X-RateLimit-Reset');

      if (limitHeader && remainingHeader) {
        setRateLimit({
          limit: parseInt(limitHeader, 10),
          remaining: parseInt(remainingHeader, 10),
          reset: resetHeader || '',
        });
      }

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.detail || errorData.title || 'Search failed');
      }

      const data: PhotosResponse = await response.json();
      setResults(data);
      setExpandedRow(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Search failed');
      setResults(null);
    } finally {
      setIsLoading(false);
    }
  };

  const handleClear = () => {
    setSearchParams({
      rovers: '',
      cameras: '',
      solMin: '',
      solMax: '',
      dateMin: '',
      dateMax: '',
      sampleType: '',
      site: '',
      drive: '',
      nasaId: '',
    });
    setResults(null);
    setCurrentPage(1);
    setError(null);
  };

  const handleCopyCode = async () => {
    try {
      await navigator.clipboard.writeText(generateCode());
      setCopiedCode(true);
      setTimeout(() => setCopiedCode(false), 2000);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  const availableCameras = searchParams.rovers ? (CAMERAS[searchParams.rovers] ?? []) : [];

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold mb-2 text-slate-900 dark:text-white">API Explorer</h1>
          <p className="text-slate-600 dark:text-slate-300">
            Search Mars rover photos interactively and generate API code
          </p>
        </div>

        {rateLimit && (
          <div className="flex items-center gap-2 bg-slate-100 dark:bg-slate-800 px-4 py-2 rounded-lg border border-slate-200 dark:border-slate-700">
            <div
              className={`w-2 h-2 rounded-full ${rateLimit.remaining > 100 ? 'bg-green-500' : rateLimit.remaining > 0 ? 'bg-yellow-500' : 'bg-red-500'}`}
            ></div>
            <span className="text-sm text-slate-600 dark:text-slate-300">
              <span className="font-semibold">{rateLimit.remaining.toLocaleString()}</span>
              <span className="text-slate-400"> / {rateLimit.limit.toLocaleString()}/hr</span>
            </span>
          </div>
        )}
      </div>

      {/* API Key Input */}
      <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <h2 className="text-lg font-semibold text-slate-900 dark:text-white mb-4">API Key</h2>
        <div className="flex flex-col sm:flex-row gap-3">
          <input
            type="password"
            value={apiKey}
            onChange={(e) => {
              setApiKey(e.target.value);
              setSavedKey(false);
            }}
            placeholder="mv_live_..."
            className="flex-1 px-4 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500 font-mono text-sm"
          />
          <div className="flex gap-2">
            <button
              onClick={saveApiKey}
              disabled={!apiKey.trim() || savedKey}
              className="px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
            >
              {savedKey ? 'Saved' : 'Save'}
            </button>
            <button
              onClick={clearApiKey}
              disabled={!apiKey}
              className="px-4 py-2 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-900 dark:text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed text-sm"
            >
              Clear
            </button>
          </div>
        </div>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-2">
          Your API key is stored locally in your browser.{' '}
          <a href="/api-keys" className="text-orange-600 dark:text-orange-400 hover:underline">
            Get an API key
          </a>
        </p>
      </div>

      {/* Search Form */}
      <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
          {/* Rover Select */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Rover
            </label>
            <select
              value={searchParams.rovers}
              onChange={(e) => {
                setSearchParams({ ...searchParams, rovers: e.target.value, cameras: '' });
              }}
              className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
            >
              <option value="">All Rovers</option>
              {ROVERS.map((rover) => (
                <option key={rover} value={rover}>
                  {rover.charAt(0).toUpperCase() + rover.slice(1)}
                </option>
              ))}
            </select>
          </div>

          {/* Camera Select */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Camera
            </label>
            <select
              value={searchParams.cameras}
              onChange={(e) => setSearchParams({ ...searchParams, cameras: e.target.value })}
              disabled={!searchParams.rovers}
              className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <option value="">All Cameras</option>
              {availableCameras.map((camera) => (
                <option key={camera} value={camera}>
                  {camera}
                </option>
              ))}
            </select>
          </div>

          {/* Sol Min */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Sol Min
            </label>
            <input
              type="number"
              value={searchParams.solMin}
              onChange={(e) => setSearchParams({ ...searchParams, solMin: e.target.value })}
              placeholder="e.g. 1000"
              min="0"
              className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
            />
          </div>

          {/* Sol Max */}
          <div>
            <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
              Sol Max
            </label>
            <input
              type="number"
              value={searchParams.solMax}
              onChange={(e) => setSearchParams({ ...searchParams, solMax: e.target.value })}
              placeholder="e.g. 1100"
              min="0"
              className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
            />
          </div>
        </div>

        {/* Advanced Filters Toggle */}
        <button
          onClick={() => setShowAdvanced(!showAdvanced)}
          className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white mb-4"
        >
          <svg
            className={`w-4 h-4 transition-transform ${showAdvanced ? 'rotate-90' : ''}`}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
          Advanced Filters
        </button>

        {/* Advanced Filters */}
        {showAdvanced && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-4 pt-4 border-t border-slate-200 dark:border-slate-700">
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Date From
              </label>
              <input
                type="date"
                value={searchParams.dateMin}
                onChange={(e) => setSearchParams({ ...searchParams, dateMin: e.target.value })}
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Date To
              </label>
              <input
                type="date"
                value={searchParams.dateMax}
                onChange={(e) => setSearchParams({ ...searchParams, dateMax: e.target.value })}
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Sample Type
              </label>
              <select
                value={searchParams.sampleType}
                onChange={(e) => setSearchParams({ ...searchParams, sampleType: e.target.value })}
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              >
                <option value="">All Types</option>
                {SAMPLE_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                NASA ID
              </label>
              <input
                type="text"
                value={searchParams.nasaId}
                onChange={(e) => setSearchParams({ ...searchParams, nasaId: e.target.value })}
                placeholder="e.g. NLB_1234"
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Site
              </label>
              <input
                type="number"
                value={searchParams.site}
                onChange={(e) => setSearchParams({ ...searchParams, site: e.target.value })}
                placeholder="e.g. 100"
                min="0"
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">
                Drive
              </label>
              <input
                type="number"
                value={searchParams.drive}
                onChange={(e) => setSearchParams({ ...searchParams, drive: e.target.value })}
                placeholder="e.g. 500"
                min="0"
                className="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg text-slate-900 dark:text-white focus:ring-2 focus:ring-orange-500 focus:border-orange-500"
              />
            </div>
          </div>
        )}

        {/* Search Buttons */}
        <div className="flex gap-3">
          <button
            onClick={() => handleSearch(1)}
            disabled={isLoading || !apiKey.trim()}
            className="px-6 py-2 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {isLoading ? (
              <>
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                    fill="none"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Searching...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
                  />
                </svg>
                Search
              </>
            )}
          </button>
          <button
            onClick={handleClear}
            disabled={isLoading}
            className="px-4 py-2 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-900 dark:text-white rounded-lg font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Clear
          </button>
        </div>
      </div>

      {/* Query Preview */}
      <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-6 border border-slate-200 dark:border-slate-700">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-slate-900 dark:text-white">Query Preview</h2>
          <button
            onClick={handleCopyCode}
            className="px-4 py-1.5 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-900 dark:text-white rounded-lg font-medium transition-colors text-sm flex items-center gap-2"
          >
            {copiedCode ? (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                Copied!
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"
                  />
                </svg>
                Copy
              </>
            )}
          </button>
        </div>

        {/* Language Tabs */}
        <div className="flex gap-1 mb-4">
          {(['curl', 'javascript', 'python'] as const).map((lang) => (
            <button
              key={lang}
              onClick={() => setCodeLanguage(lang)}
              className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                codeLanguage === lang
                  ? 'bg-orange-600 text-white'
                  : 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-300 dark:hover:bg-slate-600'
              }`}
            >
              {lang === 'curl' ? 'cURL' : lang === 'javascript' ? 'JavaScript' : 'Python'}
            </button>
          ))}
        </div>

        {/* Code Block */}
        <div className="bg-slate-900 rounded-lg p-4 overflow-x-auto">
          <pre className="text-sm text-slate-100 whitespace-pre-wrap break-all">
            <code>{generateCode()}</code>
          </pre>
        </div>
      </div>

      {/* Error Display */}
      {error && (
        <div className="bg-red-100 dark:bg-red-900/20 border border-red-300 dark:border-red-700 rounded-lg p-4">
          <p className="text-red-600 dark:text-red-400">{error}</p>
        </div>
      )}

      {/* Results */}
      {results && (
        <div className="bg-slate-100 dark:bg-slate-800 rounded-lg border border-slate-200 dark:border-slate-700 overflow-hidden">
          {/* Results Header */}
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 p-4 border-b border-slate-200 dark:border-slate-700">
            <div className="flex items-center gap-4">
              <span className="text-slate-900 dark:text-white font-medium">
                {results.meta?.total_count?.toLocaleString() || results.data.length} photos
              </span>
              {results.pagination && results.pagination.total_pages && (
                <span className="text-sm text-slate-500 dark:text-slate-400">
                  Page {currentPage} of {results.pagination.total_pages}
                </span>
              )}
            </div>

            {/* View Toggle */}
            <div className="flex gap-1">
              <button
                onClick={() => setViewMode('table')}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  viewMode === 'table'
                    ? 'bg-orange-600 text-white'
                    : 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-300 dark:hover:bg-slate-600'
                }`}
              >
                Table
              </button>
              <button
                onClick={() => setViewMode('json')}
                className={`px-3 py-1.5 rounded-lg text-sm font-medium transition-colors ${
                  viewMode === 'json'
                    ? 'bg-orange-600 text-white'
                    : 'bg-slate-200 dark:bg-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-300 dark:hover:bg-slate-600'
                }`}
              >
                JSON
              </button>
            </div>
          </div>

          {/* Table View */}
          {viewMode === 'table' && (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-slate-200 dark:bg-slate-900">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      Sol
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      Earth Date
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      Rover
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      Camera
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      NASA ID
                    </th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-slate-600 dark:text-slate-400 uppercase tracking-wider">
                      Size
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                  {results.data.map((photo) => (
                    <PhotoRow
                      key={photo.id}
                      photo={photo}
                      isExpanded={expandedRow === photo.id}
                      onToggle={() => setExpandedRow(expandedRow === photo.id ? null : photo.id)}
                      formatDate={formatDate}
                    />
                  ))}
                </tbody>
              </table>

              {results.data.length === 0 && (
                <div className="text-center py-12 text-slate-500 dark:text-slate-400">
                  No photos found matching your criteria
                </div>
              )}
            </div>
          )}

          {/* JSON View */}
          {viewMode === 'json' && (
            <div className="p-4">
              <div className="bg-slate-900 rounded-lg p-4 overflow-x-auto max-h-[600px]">
                <pre className="text-sm text-slate-100">
                  <code>{JSON.stringify(results, null, 2)}</code>
                </pre>
              </div>
            </div>
          )}

          {/* Pagination */}
          {results.pagination &&
            results.pagination.total_pages &&
            results.pagination.total_pages > 1 && (
              <div className="flex items-center justify-center gap-2 p-4 border-t border-slate-200 dark:border-slate-700">
                <button
                  onClick={() => handleSearch(1)}
                  disabled={currentPage === 1 || isLoading}
                  className="px-3 py-1.5 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-300 rounded-lg text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  First
                </button>
                <button
                  onClick={() => handleSearch(currentPage - 1)}
                  disabled={currentPage === 1 || isLoading}
                  className="px-3 py-1.5 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-300 rounded-lg text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                <span className="px-4 py-1.5 text-slate-600 dark:text-slate-400 text-sm">
                  {currentPage} / {results.pagination.total_pages}
                </span>
                <button
                  onClick={() => handleSearch(currentPage + 1)}
                  disabled={currentPage === results.pagination?.total_pages || isLoading}
                  className="px-3 py-1.5 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-300 rounded-lg text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
                <button
                  onClick={() => handleSearch(results.pagination?.total_pages || 1)}
                  disabled={currentPage === results.pagination?.total_pages || isLoading}
                  className="px-3 py-1.5 bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-300 rounded-lg text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Last
                </button>
              </div>
            )}
        </div>
      )}

      {/* Empty State */}
      {!results && !isLoading && !error && (
        <div className="bg-slate-100 dark:bg-slate-800 rounded-lg p-12 border border-slate-200 dark:border-slate-700 text-center">
          <svg
            className="w-16 h-16 mx-auto mb-4 text-slate-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={1.5}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
            />
          </svg>
          <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
            Ready to explore
          </h3>
          <p className="text-slate-600 dark:text-slate-400 max-w-md mx-auto">
            Enter your API key above, then use the search form to find Mars rover photos. Try
            selecting a rover and sol range to get started.
          </p>
        </div>
      )}
    </div>
  );
}

interface PhotoRowProps {
  photo: PhotoResource;
  isExpanded: boolean;
  onToggle: () => void;
  formatDate: (date: string) => string;
}

function PhotoRow({ photo, isExpanded, onToggle, formatDate }: PhotoRowProps) {
  return (
    <>
      <tr
        onClick={onToggle}
        className="bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700/50 cursor-pointer transition-colors"
      >
        <td className="px-4 py-3 text-sm text-slate-900 dark:text-white">
          <div className="flex items-center gap-2">
            <svg
              className={`w-4 h-4 transition-transform ${isExpanded ? 'rotate-90' : ''}`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 5l7 7-7 7"
              />
            </svg>
            {photo.attributes.sol}
          </div>
        </td>
        <td className="px-4 py-3 text-sm text-slate-600 dark:text-slate-300">
          {photo.attributes.earth_date ? formatDate(photo.attributes.earth_date) : '-'}
        </td>
        <td className="px-4 py-3 text-sm">
          <span className="text-orange-600 dark:text-orange-400 font-medium">
            {photo.relationships?.rover?.attributes?.name ||
              photo.relationships?.rover?.id ||
              '-'}
          </span>
        </td>
        <td className="px-4 py-3 text-sm text-slate-600 dark:text-slate-300">
          {photo.relationships?.camera?.id || '-'}
        </td>
        <td className="px-4 py-3 text-sm text-slate-600 dark:text-slate-300 font-mono">
          {photo.attributes.nasa_id || '-'}
        </td>
        <td className="px-4 py-3 text-sm text-slate-600 dark:text-slate-300">
          {photo.attributes.dimensions
            ? `${photo.attributes.dimensions.width}x${photo.attributes.dimensions.height}`
            : '-'}
        </td>
      </tr>

      {/* Expanded Row Details */}
      {isExpanded && (
        <tr className="bg-slate-50 dark:bg-slate-900/50">
          <td colSpan={6} className="px-4 py-4">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Image Preview */}
              <div className="relative aspect-square bg-slate-200 dark:bg-slate-800 rounded-lg overflow-hidden max-w-md">
                {(photo.attributes.images?.medium || photo.attributes.img_src) && (
                  <Image
                    src={photo.attributes.images?.medium || photo.attributes.img_src || ''}
                    alt={`Photo ${photo.id}`}
                    fill
                    className="object-contain"
                    unoptimized
                  />
                )}
              </div>

              {/* Photo Details */}
              <div className="space-y-4">
                <div>
                  <h4 className="text-sm font-semibold text-slate-500 dark:text-slate-400 mb-2">
                    Photo Details
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-slate-500 dark:text-slate-400">Photo ID</span>
                      <span className="text-slate-900 dark:text-white font-mono">{photo.id}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500 dark:text-slate-400">NASA ID</span>
                      <span className="text-slate-900 dark:text-white font-mono">
                        {photo.attributes.nasa_id || '-'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500 dark:text-slate-400">Sample Type</span>
                      <span className="text-slate-900 dark:text-white">
                        {photo.attributes.sample_type || '-'}
                      </span>
                    </div>
                    {photo.attributes.location && (
                      <>
                        <div className="flex justify-between">
                          <span className="text-slate-500 dark:text-slate-400">Site</span>
                          <span className="text-slate-900 dark:text-white">
                            {photo.attributes.location.site ?? '-'}
                          </span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-slate-500 dark:text-slate-400">Drive</span>
                          <span className="text-slate-900 dark:text-white">
                            {photo.attributes.location.drive ?? '-'}
                          </span>
                        </div>
                      </>
                    )}
                    {photo.attributes.telemetry && (
                      <>
                        <div className="flex justify-between">
                          <span className="text-slate-500 dark:text-slate-400">Mast Azimuth</span>
                          <span className="text-slate-900 dark:text-white">
                            {photo.attributes.telemetry.mast_azimuth != null
                              ? `${photo.attributes.telemetry.mast_azimuth.toFixed(2)}°`
                              : '-'}
                          </span>
                        </div>
                        <div className="flex justify-between">
                          <span className="text-slate-500 dark:text-slate-400">Mast Elevation</span>
                          <span className="text-slate-900 dark:text-white">
                            {photo.attributes.telemetry.mast_elevation != null
                              ? `${photo.attributes.telemetry.mast_elevation.toFixed(2)}°`
                              : '-'}
                          </span>
                        </div>
                      </>
                    )}
                  </div>
                </div>

                {/* Image URLs */}
                {photo.attributes.images && (
                  <div>
                    <h4 className="text-sm font-semibold text-slate-500 dark:text-slate-400 mb-2">
                      Image URLs
                    </h4>
                    <div className="space-y-1 text-sm">
                      {photo.attributes.images.full && (
                        <a
                          href={photo.attributes.images.full}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="block text-orange-600 dark:text-orange-400 hover:underline truncate"
                        >
                          Full Resolution
                        </a>
                      )}
                      {photo.attributes.images.large && (
                        <a
                          href={photo.attributes.images.large}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="block text-orange-600 dark:text-orange-400 hover:underline truncate"
                        >
                          Large (1200px)
                        </a>
                      )}
                      {photo.attributes.images.medium && (
                        <a
                          href={photo.attributes.images.medium}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="block text-orange-600 dark:text-orange-400 hover:underline truncate"
                        >
                          Medium (800px)
                        </a>
                      )}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}
