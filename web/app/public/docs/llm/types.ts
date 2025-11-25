/**
 * Mars Vista API TypeScript Type Definitions
 *
 * Copy these types into your project to get full TypeScript support
 * for the Mars Vista API responses.
 *
 * API Documentation: https://marsvista.dev/docs
 * API Reference: https://marsvista.dev/docs/llm/reference.md
 */

// ============================================================================
// API Response Envelope
// ============================================================================

/**
 * Standard API response wrapper for all endpoints
 */
export interface ApiResponse<T> {
  /** The primary data for this response */
  data: T;
  /** Metadata about the response */
  meta?: ResponseMeta;
  /** Pagination information */
  pagination?: PaginationInfo;
  /** Navigation links */
  links?: ResponseLinks;
}

/**
 * Response metadata
 */
export interface ResponseMeta {
  /** Total count of resources matching the query (before pagination) */
  total_count?: number;
  /** Number of resources returned in this response */
  returned_count: number;
  /** The query parameters that were applied */
  query?: Record<string, unknown>;
  /** Response timestamp (ISO 8601) */
  timestamp: string;
}

/**
 * Pagination information
 */
export interface PaginationInfo {
  /** Current page number (1-indexed) */
  page?: number;
  /** Number of items per page */
  per_page: number;
  /** Total number of pages */
  total_pages?: number;
}

/**
 * Navigation links
 */
export interface ResponseLinks {
  /** Link to the current resource/page */
  self: string;
  /** Link to the next page */
  next?: string;
  /** Link to the previous page */
  previous?: string;
  /** Link to the first page */
  first?: string;
  /** Link to the last page */
  last?: string;
}

// ============================================================================
// Photo Resource
// ============================================================================

/**
 * Photo resource
 */
export interface PhotoResource {
  /** Unique photo identifier */
  id: number;
  /** Resource type (always "photo") */
  type: "photo";
  /** Photo attributes */
  attributes: PhotoAttributes;
  /** Related resources (rover, camera) - only included with ?include= parameter */
  relationships?: PhotoRelationships;
  /** Photo-specific computed metadata */
  meta?: PhotoMeta;
}

/**
 * Photo attributes
 */
export interface PhotoAttributes {
  /** NASA's unique identifier for this photo */
  nasa_id?: string;
  /** Mars sol (day) when photo was taken */
  sol?: number;
  /** Earth date when photo was taken (YYYY-MM-DD) */
  earth_date?: string;
  /** UTC timestamp when photo was taken (ISO 8601) */
  date_taken_utc?: string;
  /** Mars local time when photo was taken (e.g., "Sol-1000M14:23:45") */
  date_taken_mars?: string;
  /** Multiple image URLs for different sizes */
  images?: PhotoImages;
  /** Image dimensions */
  dimensions?: PhotoDimensions;
  /** Sample type (e.g., "Full", "Thumbnail", "Subframe") */
  sample_type?: string;
  /** Location where photo was taken */
  location?: PhotoLocation;
  /** Camera telemetry data */
  telemetry?: PhotoTelemetry;
  /** Photo title */
  title?: string;
  /** Photo caption/description */
  caption?: string;
  /** Photo credit (e.g., "NASA/JPL-Caltech") */
  credit?: string;
  /** When this photo was added to our database (ISO 8601) */
  created_at?: string;
  /** @deprecated Legacy field - use images.medium instead */
  img_src?: string;
}

/**
 * Multiple image URLs for different sizes
 */
export interface PhotoImages {
  /** Small size (320px wide) - for thumbnails */
  small?: string;
  /** Medium size (800px wide) - for galleries */
  medium?: string;
  /** Large size (1200px wide) - for detailed viewing */
  large?: string;
  /** Full resolution - for download and analysis */
  full?: string;
}

/**
 * Image dimensions
 */
export interface PhotoDimensions {
  /** Image width in pixels */
  width: number;
  /** Image height in pixels */
  height: number;
}

/**
 * Photo location
 */
export interface PhotoLocation {
  /** Site number (geological location marker) */
  site?: number;
  /** Drive number (rover's drive sequence) */
  drive?: number;
  /** 3D coordinates of rover position */
  coordinates?: PhotoCoordinates;
}

/**
 * 3D coordinates
 */
export interface PhotoCoordinates {
  /** X coordinate */
  x: number;
  /** Y coordinate */
  y: number;
  /** Z coordinate */
  z: number;
}

/**
 * Camera telemetry data
 */
export interface PhotoTelemetry {
  /** Mast azimuth angle (horizontal rotation in degrees) */
  mast_azimuth?: number;
  /** Mast elevation angle (vertical tilt in degrees) */
  mast_elevation?: number;
  /** Spacecraft clock at time of capture */
  spacecraft_clock?: number;
}

/**
 * Photo relationships
 */
export interface PhotoRelationships {
  /** The rover that took this photo */
  rover?: RoverReference;
  /** The camera that took this photo */
  camera?: CameraReference;
}

/**
 * Reference to a rover
 */
export interface RoverReference {
  /** Rover identifier (slug) */
  id: string;
  /** Resource type (always "rover") */
  type: "rover";
  /** Rover attributes (when included) */
  attributes?: {
    name: string;
    status: "active" | "complete";
  };
}

/**
 * Reference to a camera
 */
export interface CameraReference {
  /** Camera identifier (name like "FHAZ", "MAST") */
  id: string;
  /** Resource type (always "camera") */
  type: "camera";
  /** Camera attributes (when included) */
  attributes?: {
    full_name: string;
    photo_count?: number;
  };
}

/**
 * Photo metadata
 */
export interface PhotoMeta {
  /** Whether this photo is part of a panorama sequence */
  is_panorama_part?: boolean;
  /** Panorama sequence identifier if part of a panorama */
  panorama_sequence_id?: string;
  /** Whether this photo has a stereo pair */
  has_stereo_pair?: boolean;
  /** Stereo pair photo ID if available */
  stereo_pair_id?: number;
  /** Lighting conditions (e.g., "golden_hour", "midday", "evening") */
  lighting_conditions?: string;
  /** Number of times rover visited this location */
  location_visits?: number;
}

// ============================================================================
// Rover Resource
// ============================================================================

/**
 * Rover resource
 */
export interface RoverResource {
  /** Rover identifier (slug: curiosity, perseverance, opportunity, spirit) */
  id: RoverSlug;
  /** Resource type (always "rover") */
  type: "rover";
  /** Rover attributes */
  attributes: RoverAttributes;
  /** Related resources */
  relationships?: RoverRelationships;
}

/**
 * Valid rover slugs
 */
export type RoverSlug = "curiosity" | "perseverance" | "opportunity" | "spirit";

/**
 * Rover attributes
 */
export interface RoverAttributes {
  /** Rover name (capitalized) */
  name: string;
  /** Landing date on Mars (YYYY-MM-DD) */
  landing_date: string;
  /** Launch date from Earth (YYYY-MM-DD) */
  launch_date: string;
  /** Mission status */
  status: "active" | "complete";
  /** Maximum sol reached by this rover */
  max_sol: number;
  /** Most recent photo date (YYYY-MM-DD) */
  max_date: string;
  /** Total number of photos in our database */
  total_photos: number;
}

/**
 * Rover relationships
 */
export interface RoverRelationships {
  /** Cameras on this rover */
  cameras?: CameraResource[];
}

// ============================================================================
// Camera Resource
// ============================================================================

/**
 * Camera resource
 */
export interface CameraResource {
  /** Camera identifier (e.g., "FHAZ", "MAST", "NAVCAM") */
  id: string;
  /** Resource type (always "camera") */
  type: "camera";
  /** Camera attributes */
  attributes: CameraAttributes;
}

/**
 * Camera attributes
 */
export interface CameraAttributes {
  /** Camera abbreviation */
  name: string;
  /** Full camera name */
  full_name: string;
}

// ============================================================================
// Manifest Resource
// ============================================================================

/**
 * Rover manifest (photo history by sol)
 */
export interface RoverManifest {
  /** Rover identifier (slug) */
  id: RoverSlug;
  /** Resource type (always "manifest") */
  type: "manifest";
  /** Manifest attributes */
  attributes: ManifestAttributes;
}

/**
 * Manifest attributes
 */
export interface ManifestAttributes {
  /** Rover name */
  name: string;
  /** Landing date (YYYY-MM-DD) */
  landing_date: string;
  /** Launch date (YYYY-MM-DD) */
  launch_date: string;
  /** Mission status */
  status: "active" | "complete";
  /** Maximum sol */
  max_sol: number;
  /** Most recent photo date (YYYY-MM-DD) */
  max_date: string;
  /** Total photos */
  total_photos: number;
  /** Photo counts by sol */
  photos: PhotosBySol[];
}

/**
 * Photos taken on a specific sol
 */
export interface PhotosBySol {
  /** Mars sol number */
  sol: number;
  /** Earth date (YYYY-MM-DD) */
  earth_date: string;
  /** Total photos taken on this sol */
  total_photos: number;
  /** Cameras that took photos on this sol */
  cameras: string[];
}

// ============================================================================
// Statistics
// ============================================================================

/**
 * Photo statistics response
 */
export interface PhotoStatisticsResponse {
  /** Total photos matching the query */
  total_photos: number;
  /** Grouped statistics */
  groups: StatisticsGroup[];
}

/**
 * Statistics group
 */
export interface StatisticsGroup {
  /** Group key (camera name, rover slug, or sol number) */
  key: string;
  /** Count of photos in this group */
  count: number;
}

// ============================================================================
// Error Response
// ============================================================================

/**
 * API error response (RFC 7807 Problem Details)
 */
export interface ApiError {
  /** Error type URI */
  type: string;
  /** Error title */
  title: string;
  /** HTTP status code */
  status: number;
  /** Detailed error message */
  detail: string;
  /** Request path that caused the error */
  instance: string;
  /** Field-level validation errors */
  errors?: ValidationError[];
}

/**
 * Validation error for a specific field
 */
export interface ValidationError {
  /** Field name that has the error */
  field: string;
  /** The invalid value that was provided */
  value: string;
  /** Error message */
  message: string;
  /** Example of a valid value */
  example?: string;
}

// ============================================================================
// Query Parameters
// ============================================================================

/**
 * Photo query parameters
 */
export interface PhotoQueryParams {
  /** Comma-separated rover names */
  rovers?: string;
  /** Comma-separated camera names */
  cameras?: string;
  /** Minimum sol */
  sol_min?: number;
  /** Maximum sol */
  sol_max?: number;
  /** Minimum date (YYYY-MM-DD) */
  date_min?: string;
  /** Maximum date (YYYY-MM-DD) */
  date_max?: string;
  /** Site number */
  site?: number;
  /** Drive number */
  drive?: number;
  /** Minimum site number */
  site_min?: number;
  /** Maximum site number */
  site_max?: number;
  /** Location search radius */
  location_radius?: number;
  /** Minimum image width */
  min_width?: number;
  /** Minimum image height */
  min_height?: number;
  /** Sample type filter */
  sample_type?: "Full" | "Thumbnail" | "Subframe";
  /** Minimum Mars local time (M06:00:00 format) */
  mars_time_min?: string;
  /** Maximum Mars local time */
  mars_time_max?: string;
  /** Filter for golden hour photos */
  mars_time_golden_hour?: boolean;
  /** Minimum camera elevation angle */
  mast_elevation_min?: number;
  /** Maximum camera elevation angle */
  mast_elevation_max?: number;
  /** Minimum camera azimuth angle */
  mast_azimuth_min?: number;
  /** Maximum camera azimuth angle */
  mast_azimuth_max?: number;
  /** Related resources to include */
  include?: "rover" | "camera" | "rover,camera";
  /** Specific fields to return */
  fields?: string;
  /** Preset field groups */
  field_set?: "minimal" | "standard" | "extended" | "scientific" | "complete";
  /** Image sizes to include */
  image_sizes?: string;
  /** Sort order */
  sort?: string;
  /** Page number */
  page?: number;
  /** Results per page (max 100) */
  per_page?: number;
}

/**
 * Batch photo request body
 */
export interface BatchPhotoRequest {
  /** Photo IDs to retrieve (max 100) */
  ids: number[];
}

// ============================================================================
// Typed API Responses
// ============================================================================

/** Response for GET /api/v2/photos */
export type PhotosResponse = ApiResponse<PhotoResource[]>;

/** Response for GET /api/v2/photos/{id} */
export type PhotoResponse = ApiResponse<PhotoResource>;

/** Response for GET /api/v2/rovers */
export type RoversResponse = ApiResponse<RoverResource[]>;

/** Response for GET /api/v2/rovers/{slug} */
export type RoverResponse = ApiResponse<RoverResource>;

/** Response for GET /api/v2/rovers/{slug}/manifest */
export type ManifestResponse = ApiResponse<RoverManifest>;

/** Response for GET /api/v2/rovers/{slug}/cameras */
export type CamerasResponse = ApiResponse<CameraResource[]>;

/** Response for GET /api/v2/photos/stats */
export type StatsResponse = ApiResponse<PhotoStatisticsResponse>;
