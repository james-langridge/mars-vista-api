using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Api.DTOs.V2;
using MarsVista.Core.Helpers;
using MarsVista.Api.Models.V2;

namespace MarsVista.Api.Services.V2;

/// <summary>
/// Implementation of time machine service
/// Allows viewing the same location at different times
/// </summary>
public class TimeMachineService : ITimeMachineService
{
    private readonly MarsVistaDbContext _context;
    private readonly IPhotoQueryServiceV2 _photoService;
    private readonly ILogger<TimeMachineService> _logger;

    private const float MarsTimeToleranceHours = 0.5f; // 30 minutes tolerance

    public TimeMachineService(
        MarsVistaDbContext context,
        IPhotoQueryServiceV2 photoService,
        ILogger<TimeMachineService> logger)
    {
        _context = context;
        _photoService = photoService;
        _logger = logger;
    }

    public async Task<TimeMachineResponse> GetTimeMachinePhotosAsync(
        int site,
        int drive,
        string? rover = null,
        string? marsTime = null,
        string? camera = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        // Build query for photos at this location
        var query = _context.Photos
            .Where(p => p.Site == site && p.Drive == drive);

        // Apply optional filters
        if (!string.IsNullOrWhiteSpace(rover))
        {
            query = query.Where(p => p.Rover.Name.ToLower() == rover.ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(camera))
        {
            query = query.Where(p => p.Camera.Name.ToUpper() == camera.ToUpperInvariant());
        }

        // Get all photos at this location
        var photos = await query
            .Include(p => p.Rover)
            .Include(p => p.Camera)
            .OrderBy(p => p.Sol)
            .ThenBy(p => p.DateTakenUtc)
            .ToListAsync(cancellationToken);

        // Get total stats for this location
        var totalPhotos = photos.Count;
        var uniqueSols = photos.Select(p => p.Sol).Distinct().Count();

        // Filter by Mars time if specified
        if (!string.IsNullOrWhiteSpace(marsTime) &&
            MarsTimeHelper.TryParseMarsTime(marsTime, out var targetTime))
        {
            photos = photos
                .Where(p =>
                {
                    if (string.IsNullOrEmpty(p.DateTakenMars))
                        return false;

                    if (!MarsTimeHelper.TryExtractTimeFromTimestamp(p.DateTakenMars, out var photoTime))
                        return false;

                    // Check if within tolerance
                    var timeDiff = Math.Abs((photoTime - targetTime).TotalHours);
                    return timeDiff <= MarsTimeToleranceHours;
                })
                .ToList();
        }

        // Group by sol and pick one representative photo per sol
        var timeMachineEntries = photos
            .GroupBy(p => p.Sol)
            .Select(g =>
            {
                // Pick the photo closest to the target Mars time if specified
                // Otherwise, pick the first photo of the day
                var photo = g.First();

                // Extract Mars time for this photo
                string? marsTimeStr = null;
                if (!string.IsNullOrEmpty(photo.DateTakenMars) &&
                    MarsTimeHelper.TryExtractTimeFromTimestamp(photo.DateTakenMars, out var photoTime))
                {
                    marsTimeStr = MarsTimeHelper.FormatMarsTime(photoTime);
                }

                // Determine lighting conditions
                string? lightingConditions = null;
                if (!string.IsNullOrEmpty(photo.DateTakenMars))
                {
                    lightingConditions = MarsTimeHelper.GetLightingConditions(photo.DateTakenMars);
                }

                return new
                {
                    Photo = photo,
                    MarsTime = marsTimeStr,
                    LightingConditions = lightingConditions
                };
            })
            .OrderBy(e => e.Photo.Sol)
            .Take(limit ?? 100)
            .ToList();

        // Convert to resources
        var queryParams = new PhotoQueryParameters
        {
            IncludeList = new List<string> { "rover", "camera" }
        };

        var timeMachineResources = timeMachineEntries.Select(e =>
        {
            var photoResource = MapPhotoToResource(e.Photo, queryParams);

            return new TimeMachineResource
            {
                Sol = e.Photo.Sol,
                EarthDate = e.Photo.EarthDate?.ToString("yyyy-MM-dd"),
                MarsTime = e.MarsTime,
                Photo = photoResource,
                LightingConditions = e.LightingConditions
            };
        }).ToList();

        return new TimeMachineResponse
        {
            Location = new TimeMachineLocation
            {
                Site = site,
                Drive = drive,
                TotalVisits = uniqueSols,
                TotalPhotos = totalPhotos
            },
            Data = timeMachineResources,
            Meta = new ResponseMeta
            {
                TotalCount = timeMachineResources.Count,
                ReturnedCount = timeMachineResources.Count
            }
        };
    }

    /// <summary>
    /// Map Photo entity to PhotoResource (simplified version of PhotoQueryServiceV2 mapping)
    /// </summary>
    private PhotoResource MapPhotoToResource(Photo photo, PhotoQueryParameters parameters)
    {
        // Build images object
        var images = new PhotoImages
        {
            Small = !string.IsNullOrEmpty(photo.ImgSrcSmall) ? photo.ImgSrcSmall : null,
            Medium = !string.IsNullOrEmpty(photo.ImgSrcMedium) ? photo.ImgSrcMedium : null,
            Large = !string.IsNullOrEmpty(photo.ImgSrcLarge) ? photo.ImgSrcLarge : null,
            Full = !string.IsNullOrEmpty(photo.ImgSrcFull) ? photo.ImgSrcFull : null
        };

        var attributes = new PhotoAttributes
        {
            NasaId = photo.NasaId,
            Sol = photo.Sol,
            EarthDate = photo.EarthDate?.ToString("yyyy-MM-dd"),
            DateTakenUtc = photo.DateTakenUtc,
            DateTakenMars = photo.DateTakenMars,
            Images = images,
            ImgSrc = photo.ImgSrcLarge
        };

        PhotoRelationships? relationships = null;
        if (parameters.IncludeList.Contains("rover") || parameters.IncludeList.Contains("camera"))
        {
            relationships = new PhotoRelationships
            {
                Rover = new ResourceReference
                {
                    Id = photo.Rover.Name.ToLowerInvariant(),
                    Type = "rover"
                },
                Camera = new CameraReference
                {
                    Id = photo.Camera.Name,
                    Type = "camera",
                    Attributes = new CameraAttributes
                    {
                        FullName = photo.Camera.FullName
                    }
                }
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
}
