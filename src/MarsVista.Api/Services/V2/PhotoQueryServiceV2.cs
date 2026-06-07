using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Api.DTOs.V2;
using MarsVista.Api.Services;
using MarsVista.Core.Entities;
using MarsVista.Core.Helpers;
using MarsVista.Api.Models.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of v2 photo query service
/// Handles complex filtering, pagination, sorting, and field selection
/// </summary>
public class PhotoQueryServiceV2 : IPhotoQueryServiceV2
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<PhotoQueryServiceV2> _logger;
    private readonly IStaticReferenceCache _referenceCache;

    public PhotoQueryServiceV2(
        MarsVistaDbContext context,
        ILogger<PhotoQueryServiceV2> logger,
        IStaticReferenceCache referenceCache)
    {
        _context = context;
        _logger = logger;
        _referenceCache = referenceCache;
    }

    public async Task<ApiResponse<List<PhotoResource>>> QueryPhotosAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Build query with ALL filters (including Mars time) at the database level
        var query = BuildQuery(parameters);

        // Get total count for pagination metadata
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, parameters);

        // Apply pagination
        var skip = (parameters.PageNumber - 1) * parameters.PageSize;
        var paginatedQuery = query.Skip(skip).Take(parameters.PageSize);

        // Eager load related entities if needed
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            paginatedQuery = paginatedQuery
                .Include(p => p.Rover)
                .Include(p => p.Camera);
        }

        // Execute query (everything happens in database now!)
        var photos = await paginatedQuery.ToListAsync(cancellationToken);

        // Batch-load rating info when needed (extended+ field set, or rating filter/sort active)
        var includeRatings = ShouldIncludeRatings(parameters);
        var ratingsMap = new Dictionary<int, PhotoRatingInfo>();
        if (includeRatings && photos.Count > 0)
        {
            var photoIds = photos.Select(p => p.Id).ToList();
            ratingsMap = await GetRatingInfoBatchAsync(photoIds, cancellationToken);
        }

        // Map to DTOs
        var photoDtos = photos.Select(p => MapToPhotoResource(p, parameters,
            ratingsMap.GetValueOrDefault(p.Id))).ToList();

        // Build pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)parameters.PageSize);

        var response = new ApiResponse<List<PhotoResource>>(photoDtos)
        {
            Meta = new ResponseMeta
            {
                TotalCount = totalCount,
                ReturnedCount = photoDtos.Count,
                Query = BuildQueryMetadata(parameters)
            },
            Pagination = new PaginationInfo
            {
                Page = parameters.PageNumber,
                PerPage = parameters.PageSize,
                TotalPages = totalPages
            }
        };

        return response;
    }

    public async Task<PhotoResource?> GetPhotoByIdAsync(
        int id,
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Photos.Where(p => p.Id == id);

        // Eager load if requested
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            query = query.Include(p => p.Rover).Include(p => p.Camera);
        }

        var photo = await query.FirstOrDefaultAsync(cancellationToken);
        if (photo == null) return null;

        // Always include rating data for single-photo responses
        var ratingInfo = await GetRatingInfoAsync(id, cancellationToken);

        return MapToPhotoResource(photo, parameters, ratingInfo);
    }

    public async Task<List<PhotoResource>> GetPhotosByIdsAsync(
        List<int> ids,
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Photos.Where(p => ids.Contains(p.Id));

        // Eager load if requested
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            query = query.Include(p => p.Rover).Include(p => p.Camera);
        }

        // Maintain the order of the requested IDs
        var photos = await query.ToListAsync(cancellationToken);

        // Create a dictionary for quick lookup
        var photoDict = photos.ToDictionary(p => p.Id);

        // Return photos in the order they were requested (skip missing ones)
        return ids
            .Where(id => photoDict.ContainsKey(id))
            .Select(id => MapToPhotoResource(photoDict[id], parameters))
            .ToList();
    }

    public async Task<int> GetPhotoCountAsync(
        PhotoQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(parameters);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<PhotoStatisticsResponse> GetStatisticsAsync(
        PhotoQueryParameters parameters,
        string groupBy,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(parameters);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get date range
        var minDate = await query.MinAsync(p => (DateTime?)p.EarthDate, cancellationToken);
        var maxDate = await query.MaxAsync(p => (DateTime?)p.EarthDate, cancellationToken);

        var response = new PhotoStatisticsResponse
        {
            TotalPhotos = totalCount,
            Period = new PeriodInfo
            {
                From = minDate?.ToString("yyyy-MM-dd"),
                To = maxDate?.ToString("yyyy-MM-dd")
            }
        };

        // Group by the requested dimension - all return unified StatisticsGroup list
        response.Groups = groupBy.ToLower() switch
        {
            "camera" => await GetCameraStatistics(query, totalCount, cancellationToken),
            "rover" => await GetRoverStatistics(query, totalCount, cancellationToken),
            "sol" => await GetSolStatistics(query, totalCount, cancellationToken),
            _ => new List<StatisticsGroup>()
        };

        return response;
    }

    /// <summary>
    /// Get statistics grouped by camera
    /// </summary>
    private async Task<List<StatisticsGroup>> GetCameraStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Camera.Name)
            .Select(g => new
            {
                Key = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Key,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by rover
    /// </summary>
    private async Task<List<StatisticsGroup>> GetRoverStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => p.Rover.Name)
            .Select(g => new
            {
                Key = g.Key,
                Count = g.Count(),
                MinSol = g.Min(p => p.Sol),
                MaxSol = g.Max(p => p.Sol)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Key,
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0,
            AvgPerSol = s.MaxSol > s.MinSol ? Math.Round(s.Count / (double)(s.MaxSol - s.MinSol + 1), 1) : 0
        }).ToList();
    }

    /// <summary>
    /// Get statistics grouped by sol (limited to top 100 sols)
    /// </summary>
    private async Task<List<StatisticsGroup>> GetSolStatistics(
        IQueryable<Photo> query,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var stats = await query
            .GroupBy(p => new { p.Sol, p.EarthDate })
            .Select(g => new
            {
                Sol = g.Key.Sol,
                EarthDate = g.Key.EarthDate,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(100) // Limit to top 100 sols to avoid huge responses
            .ToListAsync(cancellationToken);

        return stats.Select(s => new StatisticsGroup
        {
            Key = s.Sol.ToString(),
            Count = s.Count,
            Percentage = totalCount > 0 ? Math.Round((s.Count / (double)totalCount) * 100, 1) : 0,
            EarthDate = s.EarthDate?.ToString("yyyy-MM-dd")
        }).ToList();
    }

    private static bool ShouldIncludeRatings(PhotoQueryParameters parameters)
    {
        // Include when field_set is extended or higher
        if (parameters.FieldSetParsed.HasValue && parameters.FieldSetParsed.Value >= FieldSetType.Extended)
            return true;

        // Include when rating filters or sort are active
        if (parameters.MinRating.HasValue || parameters.MinRatingCount.HasValue)
            return true;

        if (parameters.SortFields.Any(s => s.Field is "rating" or "rating_count"))
            return true;

        return false;
    }

    private async Task<PhotoRatingInfo?> GetRatingInfoAsync(int photoId, CancellationToken cancellationToken)
    {
        var aggregate = await _context.PhotoRatings
            .AsNoTracking()
            .Where(r => r.PhotoId == photoId)
            .GroupBy(r => r.PhotoId)
            .Select(g => new { Avg = g.Average(r => r.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);

        if (aggregate == null) return new PhotoRatingInfo { Average = null, Count = 0 };

        return new PhotoRatingInfo
        {
            Average = Math.Round(aggregate.Avg, 1),
            Count = aggregate.Count
        };
    }

    private async Task<Dictionary<int, PhotoRatingInfo>> GetRatingInfoBatchAsync(
        List<int> photoIds,
        CancellationToken cancellationToken)
    {
        var aggregates = await _context.PhotoRatings
            .AsNoTracking()
            .Where(r => photoIds.Contains(r.PhotoId))
            .GroupBy(r => r.PhotoId)
            .Select(g => new { PhotoId = g.Key, Avg = g.Average(r => r.Rating), Count = g.Count() })
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(
            a => a.PhotoId,
            a => new PhotoRatingInfo
            {
                Average = Math.Round(a.Avg, 1),
                Count = a.Count
            });
    }

    public async Task<bool> PhotoExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Photos.AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<RatingResponse> UpsertRatingAsync(
        int photoId,
        string clientId,
        int rating,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.PhotoRatings
            .AsTracking()
            .FirstOrDefaultAsync(r => r.PhotoId == photoId && r.ClientId == clientId, cancellationToken);

        if (existing != null)
        {
            existing.Rating = rating;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.PhotoRatings.Add(new PhotoRating
            {
                PhotoId = photoId,
                Rating = rating,
                ClientId = clientId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (existing == null)
        {
            // Concurrent insert won the race — reload and update
            _context.ChangeTracker.Clear();
            var conflict = await _context.PhotoRatings
                .AsTracking()
                .FirstAsync(r => r.PhotoId == photoId && r.ClientId == clientId, cancellationToken);
            conflict.Rating = rating;
            conflict.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return await GetRatingAsync(photoId, clientId, cancellationToken);
    }

    public async Task<RatingResponse> GetRatingAsync(
        int photoId,
        string? clientId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.PhotoRatings
            .AsNoTracking()
            .Where(r => r.PhotoId == photoId)
            .GroupBy(r => 1)
            .Select(g => new
            {
                Avg = g.Average(r => r.Rating),
                Count = g.Count(),
                UserRating = clientId != null
                    ? g.Where(r => r.ClientId == clientId).Select(r => (int?)r.Rating).FirstOrDefault()
                    : (int?)null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new RatingResponse
        {
            AverageRating = result != null ? Math.Round(result.Avg, 1) : 0,
            RatingCount = result?.Count ?? 0,
            UserRating = result?.UserRating
        };
    }

    /// <summary>
    /// Build the base filtered query
    /// </summary>
    private IQueryable<Photo> BuildQuery(PhotoQueryParameters parameters)
    {
        var query = _context.Photos.AsNoTracking().AsQueryable();

        // Filter by NASA ID (partial match, case-insensitive)
        if (!string.IsNullOrWhiteSpace(parameters.NasaId))
        {
            var nasaIdPattern = parameters.NasaId.Trim();
            query = query.Where(p => EF.Functions.ILike(p.NasaId, $"%{nasaIdPattern}%"));
        }

        // Filter by rovers - resolve names to IDs and filter on rover_id directly.
        // See story 052a: filtering via `p.Rover.Name.ToLower()` joined to rovers caused
        // the planner to backward-scan ix_photos_sol (4 GB of buffer reads per call).
        // Scalar = is also critical for the single-rover case (the planner does not
        // use ix_photos_rover_id_sol_covering when it sees `rover_id = ANY(ARRAY[x])`).
        if (parameters.RoverList.Count > 0)
        {
            var roverIds = _referenceCache.GetRoverIdsByNames(parameters.RoverList);
            if (roverIds.Count == 0)
            {
                query = query.Where(p => false);
            }
            else if (roverIds.Count == 1)
            {
                var roverId = roverIds[0];
                query = query.Where(p => p.RoverId == roverId);
            }
            else
            {
                query = query.Where(p => roverIds.Contains(p.RoverId));
            }
        }

        // Filter by cameras - same pattern as rovers above.
        if (parameters.CameraList.Count > 0)
        {
            var cameraIds = _referenceCache.GetCameraIdsByNames(parameters.CameraList);
            if (cameraIds.Count == 0)
            {
                query = query.Where(p => false);
            }
            else if (cameraIds.Count == 1)
            {
                var cameraId = cameraIds[0];
                query = query.Where(p => p.CameraId == cameraId);
            }
            else
            {
                query = query.Where(p => cameraIds.Contains(p.CameraId));
            }
        }

        // Filter by sol range
        if (parameters.SolMin.HasValue)
        {
            query = query.Where(p => p.Sol >= parameters.SolMin.Value);
        }

        if (parameters.SolMax.HasValue)
        {
            query = query.Where(p => p.Sol <= parameters.SolMax.Value);
        }

        // Filter by date range
        if (parameters.DateMinParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate >= parameters.DateMinParsed.Value);
        }

        if (parameters.DateMaxParsed.HasValue)
        {
            query = query.Where(p => p.EarthDate <= parameters.DateMaxParsed.Value);
        }

        // Mars time filtering (database-level)
        if (parameters.MarsTimeGoldenHour == true)
        {
            // Golden hour: hours 5-7 (morning) and 17-19 (evening)
            // Use indexed MarsTimeHour column for efficient golden hour queries
            query = query.Where(p => p.MarsTimeHour.HasValue &&
                (p.MarsTimeHour.Value >= 5 && p.MarsTimeHour.Value <= 7 ||
                 p.MarsTimeHour.Value >= 17 && p.MarsTimeHour.Value <= 19));
        }
        else if (parameters.MarsTimeMinParsed.HasValue || parameters.MarsTimeMaxParsed.HasValue)
        {
            // Minute-level precision filtering using DateTakenMars string comparison
            // DateTakenMars format: "Sol-04287M12:28:29.589" - the 'M' is always at position 9
            // Format time as HH:MM:SS for lexicographic comparison

            if (parameters.MarsTimeMinParsed.HasValue)
            {
                var minTimeStr = $"{parameters.MarsTimeMinParsed.Value.Hours:D2}:{parameters.MarsTimeMinParsed.Value.Minutes:D2}:{parameters.MarsTimeMinParsed.Value.Seconds:D2}";
                // Extract time from DateTakenMars starting at position 10 (after 'M'), 8 chars for HH:MM:SS
                query = query.Where(p => !string.IsNullOrEmpty(p.DateTakenMars) &&
                    p.DateTakenMars.Length >= 18 &&
                    p.DateTakenMars.Substring(10, 8).CompareTo(minTimeStr) >= 0);
            }

            if (parameters.MarsTimeMaxParsed.HasValue)
            {
                var maxTimeStr = $"{parameters.MarsTimeMaxParsed.Value.Hours:D2}:{parameters.MarsTimeMaxParsed.Value.Minutes:D2}:{parameters.MarsTimeMaxParsed.Value.Seconds:D2}";
                query = query.Where(p => !string.IsNullOrEmpty(p.DateTakenMars) &&
                    p.DateTakenMars.Length >= 18 &&
                    p.DateTakenMars.Substring(10, 8).CompareTo(maxTimeStr) <= 0);
            }
        }

        // Filter by location - site/drive ranges
        if (parameters.SiteMin.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value >= parameters.SiteMin.Value);
        }

        if (parameters.SiteMax.HasValue)
        {
            query = query.Where(p => p.Site.HasValue && p.Site.Value <= parameters.SiteMax.Value);
        }

        if (parameters.DriveMin.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value >= parameters.DriveMin.Value);
        }

        if (parameters.DriveMax.HasValue)
        {
            query = query.Where(p => p.Drive.HasValue && p.Drive.Value <= parameters.DriveMax.Value);
        }

        // Location proximity search (requires site, drive, and radius)
        if (parameters.Site.HasValue && parameters.Drive.HasValue && parameters.LocationRadius.HasValue)
        {
            var targetSite = parameters.Site.Value;
            var targetDrive = parameters.Drive.Value;
            var radius = parameters.LocationRadius.Value;

            query = query.Where(p =>
                p.Site.HasValue && p.Drive.HasValue &&
                p.Site.Value == targetSite &&
                p.Drive.Value >= targetDrive - radius &&
                p.Drive.Value <= targetDrive + radius);
        }

        // Filter by image dimensions
        if (parameters.MinWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value >= parameters.MinWidth.Value);
        }

        if (parameters.MaxWidth.HasValue)
        {
            query = query.Where(p => p.Width.HasValue && p.Width.Value <= parameters.MaxWidth.Value);
        }

        if (parameters.MinHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value >= parameters.MinHeight.Value);
        }

        if (parameters.MaxHeight.HasValue)
        {
            query = query.Where(p => p.Height.HasValue && p.Height.Value <= parameters.MaxHeight.Value);
        }

        // Filter by sample type
        if (parameters.SampleTypeList.Count > 0)
        {
            query = query.Where(p => parameters.SampleTypeList.Contains(p.SampleType));
        }

        // Filter by aspect ratio using indexed computed column (50%+ faster)
        if (parameters.AspectRatioParsed.HasValue)
        {
            var (targetWidth, targetHeight) = parameters.AspectRatioParsed.Value;
            var aspectRatio = (decimal)targetWidth / targetHeight;
            var tolerance = 0.1m; // 10% tolerance for aspect ratio matching

            // For 16:9 (1.777), accept ratios between 1.6 and 1.95
            var minRatio = aspectRatio - tolerance;
            var maxRatio = aspectRatio + tolerance;

            // Use indexed AspectRatio column instead of calculating in query
            query = query.Where(p => p.AspectRatio.HasValue &&
                                   p.AspectRatio.Value >= minRatio &&
                                   p.AspectRatio.Value <= maxRatio);
        }

        // Filter by camera angles
        if (parameters.MastElevationMin.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value >= parameters.MastElevationMin.Value);
        }

        if (parameters.MastElevationMax.HasValue)
        {
            query = query.Where(p => p.MastEl.HasValue && p.MastEl.Value <= parameters.MastElevationMax.Value);
        }

        if (parameters.MastAzimuthMin.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value >= parameters.MastAzimuthMin.Value);
        }

        if (parameters.MastAzimuthMax.HasValue)
        {
            query = query.Where(p => p.MastAz.HasValue && p.MastAz.Value <= parameters.MastAzimuthMax.Value);
        }

        // Filter by rating - pre-aggregate qualifying photo_ids in a single subquery
        // (HashAggregate over photo_ratings) instead of the correlated form
        // `p.Ratings.Average(...) >= X`, which EF Core translates to a per-row
        // subquery executed once per photo in the outer query. With 1.58M photos
        // that was ~5.5 s and ~23 GB of buffer reads to return zero rows.
        if (parameters.MinRating.HasValue)
        {
            var minRating = parameters.MinRating.Value;
            var qualifyingByRating = _context.PhotoRatings
                .GroupBy(r => r.PhotoId)
                .Where(g => g.Average(r => (double)r.Rating) >= minRating)
                .Select(g => g.Key);
            query = query.Where(p => qualifyingByRating.Contains(p.Id));
        }

        if (parameters.MinRatingCount.HasValue)
        {
            var minCount = parameters.MinRatingCount.Value;
            var qualifyingByCount = _context.PhotoRatings
                .GroupBy(r => r.PhotoId)
                .Where(g => g.Count() >= minCount)
                .Select(g => g.Key);
            query = query.Where(p => qualifyingByCount.Contains(p.Id));
        }

        return query;
    }

    /// <summary>
    /// Apply sorting to the query
    /// </summary>
    private IQueryable<Photo> ApplySorting(IQueryable<Photo> query, PhotoQueryParameters parameters)
    {
        if (parameters.SortFields.Count == 0)
        {
            // Default sort: most recent first
            return query.OrderByDescending(p => p.DateTakenUtc);
        }

        IOrderedQueryable<Photo>? orderedQuery = null;

        foreach (var sortField in parameters.SortFields)
        {
            var isDescending = sortField.Direction == SortDirection.Descending;

            // First sort field uses OrderBy/OrderByDescending
            // Subsequent fields use ThenBy/ThenByDescending
            if (orderedQuery == null)
            {
                orderedQuery = sortField.Field switch
                {
                    "id" => isDescending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
                    "sol" => isDescending ? query.OrderByDescending(p => p.Sol) : query.OrderBy(p => p.Sol),
                    "earth_date" => isDescending ? query.OrderByDescending(p => p.EarthDate) : query.OrderBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? query.OrderByDescending(p => p.DateTakenUtc) : query.OrderBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? query.OrderByDescending(p => p.Camera.Name) : query.OrderBy(p => p.Camera.Name),
                    "created_at" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    // TODO: same correlated-aggregate antipattern as the
                    // rating filter above - p.Ratings.Average / .Count
                    // compiles to a per-row subquery. Tolerable today because
                    // photo_ratings has < 100 rows and no current traffic
                    // uses sort=rating. Rewrite via a projected join when
                    // the table grows.
                    "rating" => isDescending
                        ? query.OrderByDescending(p => p.Ratings.Average(r => (double?)r.Rating) ?? 0)
                        : query.OrderBy(p => p.Ratings.Any()
                            ? p.Ratings.Average(r => (double?)r.Rating)
                            : (double?)double.MaxValue),
                    "rating_count" => isDescending
                        ? query.OrderByDescending(p => p.Ratings.Count())
                        : query.OrderBy(p => p.Ratings.Count()),
                    _ => isDescending ? query.OrderByDescending(p => p.DateTakenUtc) : query.OrderBy(p => p.DateTakenUtc)
                };
            }
            else
            {
                orderedQuery = sortField.Field switch
                {
                    "id" => isDescending ? orderedQuery.ThenByDescending(p => p.Id) : orderedQuery.ThenBy(p => p.Id),
                    "sol" => isDescending ? orderedQuery.ThenByDescending(p => p.Sol) : orderedQuery.ThenBy(p => p.Sol),
                    "earth_date" => isDescending ? orderedQuery.ThenByDescending(p => p.EarthDate) : orderedQuery.ThenBy(p => p.EarthDate),
                    "date_taken_utc" => isDescending ? orderedQuery.ThenByDescending(p => p.DateTakenUtc) : orderedQuery.ThenBy(p => p.DateTakenUtc),
                    "camera" => isDescending ? orderedQuery.ThenByDescending(p => p.Camera.Name) : orderedQuery.ThenBy(p => p.Camera.Name),
                    "created_at" => isDescending ? orderedQuery.ThenByDescending(p => p.CreatedAt) : orderedQuery.ThenBy(p => p.CreatedAt),
                    // TODO: same correlated-aggregate antipattern as the
                    // primary-sort branch above - see note there.
                    "rating" => isDescending
                        ? orderedQuery.ThenByDescending(p => p.Ratings.Average(r => (double?)r.Rating) ?? 0)
                        : orderedQuery.ThenBy(p => p.Ratings.Any()
                            ? p.Ratings.Average(r => (double?)r.Rating)
                            : (double?)double.MaxValue),
                    "rating_count" => isDescending
                        ? orderedQuery.ThenByDescending(p => p.Ratings.Count())
                        : orderedQuery.ThenBy(p => p.Ratings.Count()),
                    _ => orderedQuery
                };
            }
        }

        return orderedQuery ?? query.OrderByDescending(p => p.DateTakenUtc);
    }

    /// <summary>
    /// Map Photo entity to PhotoResource DTO with field selection
    /// </summary>
    private PhotoResource MapToPhotoResource(Photo photo, PhotoQueryParameters parameters, PhotoRatingInfo? ratingInfo = null)
    {
        var hasFieldSelection = parameters.FieldList.Count > 0;
        var includeRover = parameters.IncludeList.Contains("rover");
        var includeCamera = parameters.IncludeList.Contains("camera");

        // Helper to check if a field should be included
        bool ShouldInclude(string field) => !hasFieldSelection || parameters.FieldList.Contains(field);

        // Helper to check if any field in a group should be included
        bool ShouldIncludeAny(params string[] fields) => !hasFieldSelection || fields.Any(f => parameters.FieldList.Contains(f));

        // Build images object (nested structure)
        PhotoImages? images = null;
        if (ShouldIncludeAny("images", "img_src_small", "img_src_medium", "img_src_large", "img_src_full"))
        {
            images = new PhotoImages
            {
                Small = !string.IsNullOrEmpty(photo.ImgSrcSmall) ? photo.ImgSrcSmall : null,
                Medium = !string.IsNullOrEmpty(photo.ImgSrcMedium) ? photo.ImgSrcMedium : null,
                Large = !string.IsNullOrEmpty(photo.ImgSrcLarge) ? photo.ImgSrcLarge : null,
                Full = !string.IsNullOrEmpty(photo.ImgSrcFull) ? photo.ImgSrcFull : null
            };
        }

        // Build dimensions object
        PhotoDimensions? dimensions = null;
        if (ShouldIncludeAny("dimensions", "width", "height") && photo.Width.HasValue && photo.Height.HasValue)
        {
            dimensions = new PhotoDimensions
            {
                Width = photo.Width.Value,
                Height = photo.Height.Value
            };
        }

        // Build location object with coordinates (only if there's actual location data)
        PhotoLocation? location = null;
        if (ShouldIncludeAny("location", "site", "drive", "xyz"))
        {
            // Only create location if we have site, drive, or XYZ coordinates
            if (photo.Site.HasValue || photo.Drive.HasValue || !string.IsNullOrEmpty(photo.Xyz))
            {
                PhotoCoordinates? coordinates = null;
                if (!string.IsNullOrEmpty(photo.Xyz))
                {
                    // Parse XYZ string "(35.4362,22.5714,-9.46445)" to coordinates
                    if (MarsVista.Core.Helpers.MarsTimeHelper.TryParseXYZ(photo.Xyz, out var parsed))
                    {
                        coordinates = new PhotoCoordinates
                        {
                            X = parsed.X,
                            Y = parsed.Y,
                            Z = parsed.Z
                        };
                    }
                }

                location = new PhotoLocation
                {
                    Site = photo.Site,
                    Drive = photo.Drive,
                    Coordinates = coordinates
                };
            }
        }

        // Build telemetry object
        PhotoTelemetry? telemetry = null;
        if (ShouldIncludeAny("telemetry", "mast_az", "mast_el", "mast_azimuth", "mast_elevation", "spacecraft_clock"))
        {
            if (photo.MastAz.HasValue || photo.MastEl.HasValue || photo.SpacecraftClock.HasValue)
            {
                telemetry = new PhotoTelemetry
                {
                    MastAzimuth = photo.MastAz,
                    MastElevation = photo.MastEl,
                    SpacecraftClock = photo.SpacecraftClock
                };
            }
        }

        // Include raw_data only for "complete" field set
        object? rawData = null;
        if (parameters.FieldSetParsed == FieldSetType.Complete && photo.RawData != null)
        {
            // Convert JsonDocument to object for serialization
            rawData = System.Text.Json.JsonSerializer.Deserialize<object>(photo.RawData.RootElement.GetRawText());
        }

        var attributes = new PhotoAttributes
        {
            NasaId = ShouldInclude("nasa_id") ? photo.NasaId : null,
            Sol = ShouldInclude("sol") ? photo.Sol : null,
            EarthDate = ShouldInclude("earth_date") ? photo.EarthDate?.ToString("yyyy-MM-dd") : null,
            DateTakenUtc = ShouldInclude("date_taken_utc") ? photo.DateTakenUtc : null,
            DateTakenMars = ShouldInclude("date_taken_mars") ? photo.DateTakenMars : null,
            Images = images,
            Dimensions = dimensions,
            SampleType = ShouldInclude("sample_type") ? photo.SampleType : null,
            Location = location,
            Telemetry = telemetry,
            Title = ShouldInclude("title") ? photo.Title : null,
            Caption = ShouldInclude("caption") ? photo.Caption : null,
            Credit = ShouldInclude("credit") ? photo.Credit : null,
            CreatedAt = ShouldInclude("created_at") ? photo.CreatedAt : null,
            // Rating info (single-photo or extended+ field set)
            Rating = ratingInfo,
            // Legacy field for backwards compatibility
            ImgSrc = ShouldInclude("img_src") ? photo.ImgSrcLarge : null,
            // Raw NASA data (only for complete field set)
            RawData = rawData
        };

        PhotoRelationships? relationships = null;

        if (includeRover || includeCamera)
        {
            // Defensive null checks: Include() can fail to populate relationships due to
            // transient database issues, query timeouts, or connection pool edge cases.
            // When a relationship is null but was requested, log a warning and omit it.
            ResourceReference? roverRef = null;
            if (includeRover)
            {
                if (photo.Rover != null)
                {
                    roverRef = new ResourceReference
                    {
                        Id = photo.Rover.Name.ToLowerInvariant(),
                        Type = "rover",
                        Attributes = new
                        {
                            name = photo.Rover.Name,
                            status = photo.Rover.Status
                        }
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "Photo {PhotoId} has null Rover navigation property despite Include() - " +
                        "possible transient database issue (rover_id={RoverId})",
                        photo.Id, photo.RoverId);
                }
            }

            CameraReference? cameraRef = null;
            if (includeCamera)
            {
                if (photo.Camera != null)
                {
                    cameraRef = new CameraReference
                    {
                        Id = photo.Camera.Name,
                        Type = "camera",
                        Attributes = new CameraAttributes
                        {
                            FullName = photo.Camera.FullName
                        }
                    };
                }
                else
                {
                    _logger.LogWarning(
                        "Photo {PhotoId} has null Camera navigation property despite Include() - " +
                        "possible transient database issue (camera_id={CameraId})",
                        photo.Id, photo.CameraId);
                }
            }

            relationships = new PhotoRelationships
            {
                Rover = roverRef,
                Camera = cameraRef
            };
        }

        return new PhotoResource
        {
            Id = photo.Id,
            Type = "photo",
            Attributes = attributes,
            Relationships = relationships
        };
    }

    /// <summary>
    /// Build query metadata for response
    /// </summary>
    private Dictionary<string, object> BuildQueryMetadata(PhotoQueryParameters parameters)
    {
        var metadata = new Dictionary<string, object>();

        if (parameters.RoverList.Count > 0)
            metadata["rovers"] = parameters.RoverList;

        if (parameters.CameraList.Count > 0)
            metadata["cameras"] = parameters.CameraList;

        if (parameters.SolMin.HasValue)
            metadata["sol_min"] = parameters.SolMin.Value;

        if (parameters.SolMax.HasValue)
            metadata["sol_max"] = parameters.SolMax.Value;

        if (parameters.DateMin != null)
            metadata["date_min"] = parameters.DateMin;

        if (parameters.DateMax != null)
            metadata["date_max"] = parameters.DateMax;

        // Mars time filters
        if (!string.IsNullOrWhiteSpace(parameters.MarsTimeMin))
            metadata["mars_time_min"] = parameters.MarsTimeMin;

        if (!string.IsNullOrWhiteSpace(parameters.MarsTimeMax))
            metadata["mars_time_max"] = parameters.MarsTimeMax;

        if (parameters.MarsTimeGoldenHour == true)
            metadata["mars_time_golden_hour"] = true;

        // Location filters
        if (parameters.Site.HasValue)
            metadata["site"] = parameters.Site.Value;

        if (parameters.SiteMin.HasValue && !parameters.Site.HasValue)
            metadata["site_min"] = parameters.SiteMin.Value;

        if (parameters.SiteMax.HasValue && !parameters.Site.HasValue)
            metadata["site_max"] = parameters.SiteMax.Value;

        if (parameters.Drive.HasValue)
            metadata["drive"] = parameters.Drive.Value;

        if (parameters.DriveMin.HasValue && !parameters.Drive.HasValue)
            metadata["drive_min"] = parameters.DriveMin.Value;

        if (parameters.DriveMax.HasValue && !parameters.Drive.HasValue)
            metadata["drive_max"] = parameters.DriveMax.Value;

        if (parameters.LocationRadius.HasValue)
            metadata["location_radius"] = parameters.LocationRadius.Value;

        // Rating filters
        if (parameters.MinRating.HasValue)
            metadata["min_rating"] = parameters.MinRating.Value;

        if (parameters.MinRatingCount.HasValue)
            metadata["min_rating_count"] = parameters.MinRatingCount.Value;

        return metadata;
    }

}
