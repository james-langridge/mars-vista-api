'use client'

import { useState } from 'react'
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
import { Skeleton } from '@/components/ui/skeleton'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { compareNasaSol, compareNasaPhoto, compareNasaRange } from '@/lib/api'
import type { NasaComparisonResult, PhotoComparisonResult } from '@/lib/types'
import { Search, CheckCircle2, AlertTriangle, XCircle, RefreshCw } from 'lucide-react'

const ROVERS = ['curiosity', 'perseverance']

export function NasaCompare() {
  const [solRover, setSolRover] = useState('curiosity')
  const [solNumber, setSolNumber] = useState('')
  const [solResult, setSolResult] = useState<NasaComparisonResult | null>(null)
  const [solLoading, setSolLoading] = useState(false)
  const [solError, setSolError] = useState<string | null>(null)

  const [photoNasaId, setPhotoNasaId] = useState('')
  const [photoResult, setPhotoResult] = useState<PhotoComparisonResult | null>(null)
  const [photoLoading, setPhotoLoading] = useState(false)
  const [photoError, setPhotoError] = useState<string | null>(null)

  const [rangeRover, setRangeRover] = useState('curiosity')
  const [rangeStart, setRangeStart] = useState('')
  const [rangeEnd, setRangeEnd] = useState('')
  const [rangeResult, setRangeResult] = useState<{
    rover: string
    startSol: number
    endSol: number
    solsCompared: number
    summary: {
      totalNasaPhotos: number
      totalOurPhotos: number
      totalMissing: number
      totalExtra: number
      matchPercent: number
    }
    sols: Array<{
      sol: number
      nasaCount: number
      ourCount: number
      missing: number
      extra: number
      status: string
    }>
  } | null>(null)
  const [rangeLoading, setRangeLoading] = useState(false)
  const [rangeError, setRangeError] = useState<string | null>(null)

  const handleSolCompare = async () => {
    if (!solNumber) return

    setSolLoading(true)
    setSolError(null)

    try {
      const result = await compareNasaSol(solRover, parseInt(solNumber))
      setSolResult(result)
    } catch (err) {
      setSolError(err instanceof Error ? err.message : 'Comparison failed')
      setSolResult(null)
    } finally {
      setSolLoading(false)
    }
  }

  const handlePhotoCompare = async () => {
    if (!photoNasaId) return

    setPhotoLoading(true)
    setPhotoError(null)

    try {
      const result = await compareNasaPhoto(photoNasaId)
      setPhotoResult(result)
    } catch (err) {
      setPhotoError(err instanceof Error ? err.message : 'Comparison failed')
      setPhotoResult(null)
    } finally {
      setPhotoLoading(false)
    }
  }

  const handleRangeCompare = async () => {
    if (!rangeStart || !rangeEnd) return

    setRangeLoading(true)
    setRangeError(null)

    try {
      const result = await compareNasaRange(rangeRover, parseInt(rangeStart), parseInt(rangeEnd))
      setRangeResult(result)
    } catch (err) {
      setRangeError(err instanceof Error ? err.message : 'Comparison failed')
      setRangeResult(null)
    } finally {
      setRangeLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      {/* Sol Comparison */}
      <Card>
        <CardHeader>
          <CardTitle>Compare by Sol</CardTitle>
          <CardDescription>
            Compare our database with NASA API for a specific rover and sol
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-2">
              <Label>Rover</Label>
              <Select value={solRover} onValueChange={setSolRover}>
                <SelectTrigger className="w-40">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ROVERS.map((r) => (
                    <SelectItem key={r} value={r}>
                      {r.charAt(0).toUpperCase() + r.slice(1)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Sol</Label>
              <Input
                type="number"
                className="w-32"
                placeholder="e.g., 4728"
                value={solNumber}
                onChange={(e) => setSolNumber(e.target.value)}
              />
            </div>
            <Button onClick={handleSolCompare} disabled={solLoading || !solNumber}>
              {solLoading ? (
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Search className="h-4 w-4 mr-2" />
              )}
              Compare
            </Button>
          </div>

          {solError && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertTitle>Error</AlertTitle>
              <AlertDescription>{solError}</AlertDescription>
            </Alert>
          )}

          {solLoading && (
            <div className="space-y-2">
              <Skeleton className="h-24 w-full" />
            </div>
          )}

          {solResult && (
            <div className="space-y-4 mt-4">
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">{solResult.comparison.nasaPhotoCount}</div>
                    <div className="text-sm text-muted-foreground">NASA Photos</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">{solResult.comparison.ourPhotoCount}</div>
                    <div className="text-sm text-muted-foreground">Our Photos</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">{solResult.comparison.matchPercent}%</div>
                    <div className="text-sm text-muted-foreground">Match Rate</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="flex items-center gap-2">
                      {solResult.comparison.status === 'match' ? (
                        <CheckCircle2 className="h-6 w-6 text-green-500" />
                      ) : solResult.comparison.status === 'missing' ? (
                        <AlertTriangle className="h-6 w-6 text-yellow-500" />
                      ) : (
                        <XCircle className="h-6 w-6 text-red-500" />
                      )}
                      <Badge
                        variant={
                          solResult.comparison.status === 'match'
                            ? 'default'
                            : solResult.comparison.status === 'missing'
                              ? 'secondary'
                              : 'destructive'
                        }
                      >
                        {solResult.comparison.status.toUpperCase()}
                      </Badge>
                    </div>
                    <div className="text-sm text-muted-foreground mt-1">Status</div>
                  </CardContent>
                </Card>
              </div>

              {solResult.details.missingNasaIds.length > 0 && (
                <Alert>
                  <AlertTriangle className="h-4 w-4" />
                  <AlertTitle>Missing from our database ({solResult.comparison.missingFromOurs})</AlertTitle>
                  <AlertDescription>
                    <div className="mt-2 font-mono text-xs max-h-32 overflow-auto">
                      {solResult.details.missingNasaIds.join(', ')}
                      {solResult.details.truncatedMissing && ' ...and more'}
                    </div>
                  </AlertDescription>
                </Alert>
              )}

              {solResult.details.extraNasaIds.length > 0 && (
                <Alert variant="destructive">
                  <XCircle className="h-4 w-4" />
                  <AlertTitle>Extra in our database ({solResult.comparison.extraInOurs})</AlertTitle>
                  <AlertDescription>
                    <div className="mt-2 font-mono text-xs max-h-32 overflow-auto">
                      {solResult.details.extraNasaIds.join(', ')}
                      {solResult.details.truncatedExtra && ' ...and more'}
                    </div>
                  </AlertDescription>
                </Alert>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Photo Comparison */}
      <Card>
        <CardHeader>
          <CardTitle>Compare Individual Photo</CardTitle>
          <CardDescription>
            Compare a specific photo by NASA ID to see field-level differences
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-2 flex-1 max-w-md">
              <Label>NASA ID</Label>
              <Input
                placeholder="e.g., NLB_780234567890"
                value={photoNasaId}
                onChange={(e) => setPhotoNasaId(e.target.value)}
              />
            </div>
            <Button onClick={handlePhotoCompare} disabled={photoLoading || !photoNasaId}>
              {photoLoading ? (
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Search className="h-4 w-4 mr-2" />
              )}
              Compare
            </Button>
          </div>

          {photoError && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertTitle>Error</AlertTitle>
              <AlertDescription>{photoError}</AlertDescription>
            </Alert>
          )}

          {photoLoading && (
            <div className="space-y-2">
              <Skeleton className="h-48 w-full" />
            </div>
          )}

          {photoResult && (
            <div className="space-y-4 mt-4">
              <div className="flex gap-4">
                <Badge variant={photoResult.foundInOurs ? 'default' : 'destructive'}>
                  {photoResult.foundInOurs ? 'Found in our DB' : 'Not in our DB'}
                </Badge>
                <Badge variant={photoResult.foundInNasa ? 'default' : 'secondary'}>
                  {photoResult.foundInNasa ? 'Found in NASA API' : 'Not in NASA API'}
                </Badge>
              </div>

              {photoResult.differences.length > 0 ? (
                <div className="rounded-md border">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Field</TableHead>
                        <TableHead>Our Value</TableHead>
                        <TableHead>NASA Value</TableHead>
                        <TableHead>Status</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {photoResult.differences.map((diff) => (
                        <TableRow key={diff.field}>
                          <TableCell className="font-medium">{diff.field}</TableCell>
                          <TableCell className="font-mono text-sm">{diff.ourValue}</TableCell>
                          <TableCell className="font-mono text-sm">{diff.nasaValue}</TableCell>
                          <TableCell>
                            <Badge variant="destructive">Mismatch</Badge>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              ) : (
                <Alert>
                  <CheckCircle2 className="h-4 w-4" />
                  <AlertTitle>All fields match</AlertTitle>
                  <AlertDescription>
                    No differences found between our data and NASA API data
                  </AlertDescription>
                </Alert>
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Card>
                  <CardHeader className="py-3">
                    <CardTitle className="text-sm">Our Database</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm">
                    <pre className="bg-slate-100 dark:bg-slate-800 p-3 rounded-md overflow-auto max-h-64 text-xs">
                      {JSON.stringify(photoResult.ourData, null, 2)}
                    </pre>
                  </CardContent>
                </Card>
                <Card>
                  <CardHeader className="py-3">
                    <CardTitle className="text-sm">NASA API</CardTitle>
                  </CardHeader>
                  <CardContent className="text-sm">
                    <pre className="bg-slate-100 dark:bg-slate-800 p-3 rounded-md overflow-auto max-h-64 text-xs">
                      {JSON.stringify(photoResult.nasaData, null, 2)}
                    </pre>
                  </CardContent>
                </Card>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Range Comparison */}
      <Card>
        <CardHeader>
          <CardTitle>Compare Sol Range</CardTitle>
          <CardDescription>
            Bulk validation across a range of sols (max 50 sols per request)
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4 items-end">
            <div className="space-y-2">
              <Label>Rover</Label>
              <Select value={rangeRover} onValueChange={setRangeRover}>
                <SelectTrigger className="w-40">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ROVERS.map((r) => (
                    <SelectItem key={r} value={r}>
                      {r.charAt(0).toUpperCase() + r.slice(1)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Start Sol</Label>
              <Input
                type="number"
                className="w-32"
                placeholder="e.g., 4700"
                value={rangeStart}
                onChange={(e) => setRangeStart(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>End Sol</Label>
              <Input
                type="number"
                className="w-32"
                placeholder="e.g., 4728"
                value={rangeEnd}
                onChange={(e) => setRangeEnd(e.target.value)}
              />
            </div>
            <Button
              onClick={handleRangeCompare}
              disabled={rangeLoading || !rangeStart || !rangeEnd}
            >
              {rangeLoading ? (
                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Search className="h-4 w-4 mr-2" />
              )}
              Compare Range
            </Button>
          </div>

          {rangeError && (
            <Alert variant="destructive">
              <XCircle className="h-4 w-4" />
              <AlertTitle>Error</AlertTitle>
              <AlertDescription>{rangeError}</AlertDescription>
            </Alert>
          )}

          {rangeLoading && (
            <div className="space-y-2">
              <Skeleton className="h-48 w-full" />
            </div>
          )}

          {rangeResult && (
            <div className="space-y-4 mt-4">
              <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">{rangeResult.solsCompared}</div>
                    <div className="text-sm text-muted-foreground">Sols Compared</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">
                      {rangeResult.summary.totalNasaPhotos.toLocaleString()}
                    </div>
                    <div className="text-sm text-muted-foreground">NASA Photos</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">
                      {rangeResult.summary.totalOurPhotos.toLocaleString()}
                    </div>
                    <div className="text-sm text-muted-foreground">Our Photos</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold">{rangeResult.summary.matchPercent}%</div>
                    <div className="text-sm text-muted-foreground">Match Rate</div>
                  </CardContent>
                </Card>
                <Card>
                  <CardContent className="pt-4">
                    <div className="text-2xl font-bold text-yellow-500">
                      {rangeResult.summary.totalMissing}
                    </div>
                    <div className="text-sm text-muted-foreground">Missing</div>
                  </CardContent>
                </Card>
              </div>

              <div className="rounded-md border max-h-96 overflow-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Sol</TableHead>
                      <TableHead>NASA</TableHead>
                      <TableHead>Ours</TableHead>
                      <TableHead>Missing</TableHead>
                      <TableHead>Extra</TableHead>
                      <TableHead>Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rangeResult.sols.map((sol) => (
                      <TableRow key={sol.sol}>
                        <TableCell className="font-medium">{sol.sol}</TableCell>
                        <TableCell>{sol.nasaCount}</TableCell>
                        <TableCell>{sol.ourCount}</TableCell>
                        <TableCell className={sol.missing > 0 ? 'text-yellow-500' : ''}>
                          {sol.missing}
                        </TableCell>
                        <TableCell className={sol.extra > 0 ? 'text-red-500' : ''}>
                          {sol.extra}
                        </TableCell>
                        <TableCell>
                          <Badge
                            variant={
                              sol.status === 'match'
                                ? 'default'
                                : sol.status === 'missing'
                                  ? 'secondary'
                                  : 'destructive'
                            }
                          >
                            {sol.status}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default NasaCompare
