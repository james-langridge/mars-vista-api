'use client'

import { useState, useCallback } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import { Skeleton } from '@/components/ui/skeleton'
import { searchPhotos } from '@/lib/api'
import type { PhotoSearchParams, PhotoSearchResponse, PhotoResource } from '@/lib/types'
import { JsonViewer } from '@/components/ui/json-viewer'
import {
  Search,
  Download,
  ExternalLink,
  ChevronDown,
  ChevronUp,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Copy,
  Check,
  TableIcon,
  Code,
} from 'lucide-react'

const ROVERS = ['curiosity', 'perseverance', 'opportunity', 'spirit']
const CAMERAS = [
  'FHAZ',
  'RHAZ',
  'MAST',
  'NAVCAM',
  'CHEMCAM',
  'MAHLI',
  'MARDI',
  'EDL_RUCAM',
  'EDL_RDCAM',
  'EDL_DDCAM',
  'EDL_PUCAM1',
  'EDL_PUCAM2',
  'FRONT_HAZCAM_LEFT_A',
  'FRONT_HAZCAM_RIGHT_A',
  'REAR_HAZCAM_LEFT',
  'REAR_HAZCAM_RIGHT',
  'NAVCAM_LEFT',
  'NAVCAM_RIGHT',
  'MCZ_LEFT',
  'MCZ_RIGHT',
  'SUPERCAM_RMI',
  'SKYCAM',
  'SHERLOC_WATSON',
]
const SAMPLE_TYPES = ['Full', 'Subframe', 'Thumbnail', 'Sub-frame', 'Downsampled']
const PAGE_SIZES = [10, 25, 50, 100]

export function PhotoSearch() {
  const [params, setParams] = useState<PhotoSearchParams>({
    per_page: 25,
    page: 1,
    field_set: 'extended',
    include: 'rover,camera',
    sort: '-sol',
  })
  const [results, setResults] = useState<PhotoSearchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set())
  const [copiedId, setCopiedId] = useState<string | null>(null)
  const [viewMode, setViewMode] = useState<'table' | 'json'>('table')

  const handleSearch = useCallback(
    async (newParams?: Partial<PhotoSearchParams>) => {
      const searchParams = { ...params, ...newParams }
      setParams(searchParams)
      setLoading(true)
      setError(null)

      try {
        const data = await searchPhotos(searchParams)
        setResults(data)
        setExpandedRows(new Set())
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Search failed')
        setResults(null)
      } finally {
        setLoading(false)
      }
    },
    [params]
  )

  const toggleRow = (id: string) => {
    const newExpanded = new Set(expandedRows)
    if (newExpanded.has(id)) {
      newExpanded.delete(id)
    } else {
      newExpanded.add(id)
    }
    setExpandedRows(newExpanded)
  }

  const copyToClipboard = async (text: string, id: string) => {
    await navigator.clipboard.writeText(text)
    setCopiedId(id)
    setTimeout(() => setCopiedId(null), 2000)
  }

  const exportToCsv = () => {
    if (!results?.data.length) return

    const headers = [
      'ID',
      'NASA ID',
      'Sol',
      'Earth Date',
      'Rover',
      'Camera',
      'Width',
      'Height',
      'Sample Type',
      'Site',
      'Drive',
      'Image URL',
    ]

    const rows = results.data.map((photo) => {
      const attrs = photo.attributes
      return [
        photo.id,
        attrs.nasa_id,
        attrs.sol,
        attrs.earth_date,
        getRoverName(photo),
        getCameraName(photo),
        attrs.dimensions?.width || '',
        attrs.dimensions?.height || '',
        attrs.sample_type || '',
        attrs.location?.site || '',
        attrs.location?.drive || '',
        attrs.images?.full || '',
      ].join(',')
    })

    const csv = [headers.join(','), ...rows].join('\n')
    const blob = new Blob([csv], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `mars-photos-export-${new Date().toISOString().split('T')[0]}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div className="space-y-6">
      {/* Search Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Database Search</span>
            {results && (
              <Button variant="outline" size="sm" onClick={exportToCsv}>
                <Download className="h-4 w-4 mr-2" />
                Export CSV
              </Button>
            )}
          </CardTitle>
          <CardDescription>Search photos by any combination of filters</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Rover Select */}
            <div className="space-y-2">
              <Label>Rover</Label>
              <Select
                value={params.rovers || 'all'}
                onValueChange={(v) => setParams({ ...params, rovers: v === 'all' ? undefined : v })}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All rovers" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All rovers</SelectItem>
                  {ROVERS.map((r) => (
                    <SelectItem key={r} value={r}>
                      {r.charAt(0).toUpperCase() + r.slice(1)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Camera Select */}
            <div className="space-y-2">
              <Label>Camera</Label>
              <Select
                value={params.cameras || 'all'}
                onValueChange={(v) =>
                  setParams({ ...params, cameras: v === 'all' ? undefined : v })
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="All cameras" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All cameras</SelectItem>
                  {CAMERAS.map((c) => (
                    <SelectItem key={c} value={c}>
                      {c}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Sample Type */}
            <div className="space-y-2">
              <Label>Sample Type</Label>
              <Select
                value={params.sample_type || 'all'}
                onValueChange={(v) =>
                  setParams({ ...params, sample_type: v === 'all' ? undefined : v })
                }
              >
                <SelectTrigger>
                  <SelectValue placeholder="All types" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All types</SelectItem>
                  {SAMPLE_TYPES.map((t) => (
                    <SelectItem key={t} value={t}>
                      {t}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Page Size */}
            <div className="space-y-2">
              <Label>Per Page</Label>
              <Select
                value={params.per_page?.toString() || '25'}
                onValueChange={(v) => setParams({ ...params, per_page: parseInt(v), page: 1 })}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZES.map((size) => (
                    <SelectItem key={size} value={size.toString()}>
                      {size} per page
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Sol Range */}
            <div className="space-y-2">
              <Label>Sol Min</Label>
              <Input
                type="number"
                placeholder="0"
                value={params.sol_min ?? ''}
                onChange={(e) =>
                  setParams({
                    ...params,
                    sol_min: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
              />
            </div>
            <div className="space-y-2">
              <Label>Sol Max</Label>
              <Input
                type="number"
                placeholder="9999"
                value={params.sol_max ?? ''}
                onChange={(e) =>
                  setParams({
                    ...params,
                    sol_max: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
              />
            </div>

            {/* Date Range */}
            <div className="space-y-2">
              <Label>Date Min</Label>
              <Input
                type="date"
                value={params.date_min ?? ''}
                onChange={(e) => setParams({ ...params, date_min: e.target.value || undefined })}
              />
            </div>
            <div className="space-y-2">
              <Label>Date Max</Label>
              <Input
                type="date"
                value={params.date_max ?? ''}
                onChange={(e) => setParams({ ...params, date_max: e.target.value || undefined })}
              />
            </div>

            {/* NASA ID Search */}
            <div className="space-y-2 md:col-span-2">
              <Label>NASA ID (partial match)</Label>
              <Input
                placeholder="e.g., NLB_780234 or CR0_"
                value={params.nasa_id ?? ''}
                onChange={(e) => setParams({ ...params, nasa_id: e.target.value || undefined })}
              />
            </div>

            {/* Site/Drive */}
            <div className="space-y-2">
              <Label>Site</Label>
              <Input
                type="number"
                placeholder="e.g., 79"
                value={params.site ?? ''}
                onChange={(e) =>
                  setParams({
                    ...params,
                    site: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
              />
            </div>
            <div className="space-y-2">
              <Label>Drive</Label>
              <Input
                type="number"
                placeholder="e.g., 1204"
                value={params.drive ?? ''}
                onChange={(e) =>
                  setParams({
                    ...params,
                    drive: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
              />
            </div>
          </div>

          <div className="mt-6 flex gap-4">
            <Button onClick={() => handleSearch({ page: 1 })} disabled={loading}>
              <Search className="h-4 w-4 mr-2" />
              {loading ? 'Searching...' : 'Search'}
            </Button>
            <Button
              variant="outline"
              onClick={() => {
                setParams({ per_page: 25, page: 1, field_set: 'extended', include: 'rover,camera', sort: '-sol' })
                setResults(null)
                setError(null)
              }}
            >
              Clear
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Error Display */}
      {error && (
        <Card className="border-red-200 bg-red-50 dark:bg-red-900/20">
          <CardContent className="pt-6">
            <p className="text-red-600 dark:text-red-400">{error}</p>
          </CardContent>
        </Card>
      )}

      {/* Results */}
      {loading && !results && (
        <Card>
          <CardContent className="pt-6">
            <div className="space-y-4">
              <Skeleton className="h-8 w-48" />
              <Skeleton className="h-64 w-full" />
            </div>
          </CardContent>
        </Card>
      )}

      {results && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between flex-wrap gap-4">
              <CardTitle>
                Results: {results.meta.total_count.toLocaleString()} photos
                <span className="text-sm font-normal text-muted-foreground ml-2">
                  (returned {results.meta.returned_count})
                </span>
              </CardTitle>
              <div className="flex items-center gap-4">
                {/* View Toggle */}
                <div className="flex items-center border rounded-md">
                  <Button
                    variant={viewMode === 'table' ? 'secondary' : 'ghost'}
                    size="sm"
                    onClick={() => setViewMode('table')}
                    className="rounded-r-none"
                  >
                    <TableIcon className="h-4 w-4 mr-1" />
                    Table
                  </Button>
                  <Button
                    variant={viewMode === 'json' ? 'secondary' : 'ghost'}
                    size="sm"
                    onClick={() => setViewMode('json')}
                    className="rounded-l-none"
                  >
                    <Code className="h-4 w-4 mr-1" />
                    JSON
                  </Button>
                </div>
                {/* Pagination */}
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSearch({ page: 1 })}
                    disabled={results.pagination.page === 1 || loading}
                  >
                    <ChevronsLeft className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSearch({ page: results.pagination.page - 1 })}
                    disabled={!results.links?.previous || loading}
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </Button>
                  <span className="text-sm px-2">
                    Page {results.pagination.page} of {results.pagination.total_pages}
                  </span>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSearch({ page: results.pagination.page + 1 })}
                    disabled={!results.links?.next || loading}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSearch({ page: results.pagination.total_pages })}
                    disabled={results.pagination.page === results.pagination.total_pages || loading}
                  >
                    <ChevronsRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            </div>
          </CardHeader>
          <CardContent>
            {viewMode === 'json' ? (
              <JsonViewer data={results} maxHeight="600px" />
            ) : (
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[50px]"></TableHead>
                      <TableHead>Sol</TableHead>
                      <TableHead>Earth Date</TableHead>
                      <TableHead>Rover</TableHead>
                      <TableHead>Camera</TableHead>
                      <TableHead>NASA ID</TableHead>
                      <TableHead>Dimensions</TableHead>
                      <TableHead>Type</TableHead>
                      <TableHead className="w-[100px]">Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {results.data.map((photo) => {
                      const photoId = String(photo.id)
                      return (
                        <PhotoRow
                          key={photoId}
                          photo={photo}
                          isExpanded={expandedRows.has(photoId)}
                          onToggle={() => toggleRow(photoId)}
                          onCopy={(text) => copyToClipboard(text, photoId)}
                          copied={copiedId === photoId}
                        />
                      )
                    })}
                  </TableBody>
                </Table>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}

interface PhotoRowProps {
  photo: PhotoResource
  isExpanded: boolean
  onToggle: () => void
  onCopy: (text: string) => void
  copied: boolean
}

// Helper to get rover name from relationships
function getRoverName(photo: PhotoResource): string {
  return photo.relationships?.rover?.attributes?.name || photo.relationships?.rover?.id || '-'
}

// Helper to get camera name from relationships or title fallback
function getCameraName(photo: PhotoResource): string {
  // Use camera relationship if available
  if (photo.relationships?.camera?.attributes?.full_name) {
    return photo.relationships.camera.attributes.full_name
  }
  if (photo.relationships?.camera?.id) {
    return photo.relationships.camera.id
  }
  // Fallback: try to extract from title like "Sol 4729: Right Navigation Camera"
  if (photo.attributes.title) {
    const match = photo.attributes.title.match(/:\s*(.+)$/)
    if (match) return match[1]
  }
  return '-'
}

// Helper to get sample type
function getSampleType(attrs: PhotoResource['attributes']): string {
  return attrs.sample_type || '-'
}

// Helper to get credit
function getCredit(attrs: PhotoResource['attributes']): string {
  return attrs.credit || '-'
}

function PhotoRow({ photo, isExpanded, onToggle, onCopy, copied }: PhotoRowProps) {
  const attrs = photo.attributes

  return (
    <>
      <TableRow className="cursor-pointer hover:bg-muted/50" onClick={onToggle}>
        <TableCell>
          {isExpanded ? (
            <ChevronUp className="h-4 w-4" />
          ) : (
            <ChevronDown className="h-4 w-4" />
          )}
        </TableCell>
        <TableCell className="font-medium">{attrs.sol}</TableCell>
        <TableCell>{attrs.earth_date}</TableCell>
        <TableCell>
          <Badge variant="outline">{getRoverName(photo)}</Badge>
        </TableCell>
        <TableCell>{getCameraName(photo)}</TableCell>
        <TableCell className="font-mono text-xs">{attrs.nasa_id}</TableCell>
        <TableCell>
          {attrs.dimensions?.width && attrs.dimensions?.height
            ? `${attrs.dimensions.width}x${attrs.dimensions.height}`
            : '-'}
        </TableCell>
        <TableCell>
          <Badge variant="secondary">{getSampleType(attrs)}</Badge>
        </TableCell>
        <TableCell onClick={(e) => e.stopPropagation()}>
          <div className="flex gap-1">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onCopy(attrs.nasa_id)}
              title="Copy NASA ID"
            >
              {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
            </Button>
            {attrs.images?.full && (
              <Button
                variant="ghost"
                size="sm"
                asChild
                title="View image"
              >
                <a href={attrs.images.full} target="_blank" rel="noopener noreferrer">
                  <ExternalLink className="h-3 w-3" />
                </a>
              </Button>
            )}
          </div>
        </TableCell>
      </TableRow>
      {isExpanded && (
        <TableRow>
          <TableCell colSpan={9} className="bg-muted/30">
            <ExpandedPhotoDetails photo={photo} />
          </TableCell>
        </TableRow>
      )}
    </>
  )
}

function ExpandedPhotoDetails({ photo }: { photo: PhotoResource }) {
  const attrs = photo.attributes
  const [showRawData, setShowRawData] = useState(false)

  // Calculate aspect ratio if dimensions exist but aspect_ratio doesn't
  const aspectRatio = attrs.dimensions?.aspect_ratio
    ?? (attrs.dimensions?.width && attrs.dimensions?.height
        ? attrs.dimensions.width / attrs.dimensions.height
        : null)

  return (
    <div className="p-4 space-y-4">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
        <div>
          <span className="text-muted-foreground">Database ID:</span>
          <span className="ml-2 font-mono">{photo.id}</span>
        </div>
        <div>
          <span className="text-muted-foreground">NASA ID:</span>
          <span className="ml-2 font-mono">{attrs.nasa_id}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Sol:</span>
          <span className="ml-2">{attrs.sol}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Earth Date:</span>
          <span className="ml-2">{attrs.earth_date}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Rover:</span>
          <span className="ml-2">{getRoverName(photo)}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Camera:</span>
          <span className="ml-2">{getCameraName(photo)}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Dimensions:</span>
          <span className="ml-2">
            {attrs.dimensions?.width && attrs.dimensions?.height
              ? `${attrs.dimensions.width}x${attrs.dimensions.height}`
              : '-'}
          </span>
        </div>
        <div>
          <span className="text-muted-foreground">Aspect Ratio:</span>
          <span className="ml-2">{aspectRatio?.toFixed(2) || '-'}</span>
        </div>
        {attrs.location && (
          <>
            <div>
              <span className="text-muted-foreground">Site:</span>
              <span className="ml-2">{attrs.location.site ?? '-'}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Drive:</span>
              <span className="ml-2">{attrs.location.drive ?? '-'}</span>
            </div>
          </>
        )}
        {(attrs.mars_time || attrs.date_taken_mars) && (
          <>
            <div>
              <span className="text-muted-foreground">Mars Time:</span>
              <span className="ml-2">{attrs.mars_time?.date_taken_mars || attrs.date_taken_mars || '-'}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Local Time:</span>
              <span className="ml-2">{attrs.mars_time?.local_time || '-'}</span>
            </div>
          </>
        )}
        {attrs.telemetry && (
          <>
            <div>
              <span className="text-muted-foreground">Mast Azimuth:</span>
              <span className="ml-2">{attrs.telemetry.mast_azimuth?.toFixed(1) ?? '-'}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Mast Elevation:</span>
              <span className="ml-2">{attrs.telemetry.mast_elevation?.toFixed(1) ?? '-'}</span>
            </div>
          </>
        )}
        <div>
          <span className="text-muted-foreground">Sample Type:</span>
          <span className="ml-2">{getSampleType(attrs)}</span>
        </div>
        <div>
          <span className="text-muted-foreground">Credit:</span>
          <span className="ml-2">{getCredit(attrs)}</span>
        </div>
        {attrs.title && (
          <div className="col-span-2">
            <span className="text-muted-foreground">Title:</span>
            <span className="ml-2">{attrs.title}</span>
          </div>
        )}
        {attrs.caption && (
          <div className="col-span-2 md:col-span-4">
            <span className="text-muted-foreground">Caption:</span>
            <span className="ml-2">{attrs.caption}</span>
          </div>
        )}
      </div>

      {attrs.images && (
        <div className="flex gap-2 pt-2">
          {attrs.images.small && (
            <Button variant="outline" size="sm" asChild>
              <a href={attrs.images.small} target="_blank" rel="noopener noreferrer">
                Small
              </a>
            </Button>
          )}
          {attrs.images.medium && (
            <Button variant="outline" size="sm" asChild>
              <a href={attrs.images.medium} target="_blank" rel="noopener noreferrer">
                Medium
              </a>
            </Button>
          )}
          {attrs.images.large && (
            <Button variant="outline" size="sm" asChild>
              <a href={attrs.images.large} target="_blank" rel="noopener noreferrer">
                Large
              </a>
            </Button>
          )}
          {attrs.images.full && (
            <Button variant="outline" size="sm" asChild>
              <a href={attrs.images.full} target="_blank" rel="noopener noreferrer">
                Full Resolution
              </a>
            </Button>
          )}
        </div>
      )}

      {attrs.raw_data && (
        <Collapsible open={showRawData} onOpenChange={setShowRawData}>
          <CollapsibleTrigger asChild>
            <Button variant="outline" size="sm">
              {showRawData ? 'Hide' : 'Show'} Raw NASA Data
              {showRawData ? (
                <ChevronUp className="h-4 w-4 ml-2" />
              ) : (
                <ChevronDown className="h-4 w-4 ml-2" />
              )}
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent className="mt-2">
            <pre className="bg-slate-900 text-slate-100 p-4 rounded-md overflow-auto max-h-96 text-xs">
              {JSON.stringify(attrs.raw_data, null, 2)}
            </pre>
          </CollapsibleContent>
        </Collapsible>
      )}

      {photo.links && (
        <div className="flex gap-2 pt-2">
          <Button variant="outline" size="sm" asChild>
            <a href={photo.links.self} target="_blank" rel="noopener noreferrer">
              <ExternalLink className="h-4 w-4 mr-2" />
              API Link
            </a>
          </Button>
        </div>
      )}
    </div>
  )
}

export default PhotoSearch
