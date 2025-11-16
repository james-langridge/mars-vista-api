using System.Text.Json;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

/// <summary>
/// Scraper for Opportunity rover using PDS (Planetary Data System) index files
/// Parses tab-delimited index files instead of querying JSON APIs
/// </summary>
public class OpportunityScraper : IScraperService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<OpportunityScraper> _logger;
    private readonly PdsIndexParser _parser;

    private const int OpportunityRoverId = 3;  // Database rover ID for Opportunity
    private const int BatchSize = 1000;         // Commit every 1000 photos
    private const int ProgressLogInterval = 10000; // Log progress every 10,000 rows

    public string RoverName => "Opportunity";

    public OpportunityScraper(
        IHttpClientFactory httpClientFactory,
        MarsVistaDbContext context,
        ILogger<OpportunityScraper> logger,
        PdsIndexParser parser)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _parser = parser;
    }

    /// <summary>
    /// Scrape latest photos (not applicable for Opportunity - mission complete)
    /// </summary>
    public async Task<int> ScrapeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ScrapeAsync not applicable for Opportunity (use ScrapeVolumeAsync or ScrapeAllVolumesAsync)");
        return 0;
    }

    /// <summary>
    /// Scrape specific sol (not implemented - use volume-based scraping)
    /// </summary>
    public async Task<int> ScrapeSolAsync(int sol, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ScrapeSolAsync not implemented for Opportunity (use ScrapeVolumeAsync)");
        return 0;
    }

    /// <summary>
    /// Scrape a single PDS volume (e.g., mer1po_0xxx for PANCAM)
    /// </summary>
    public async Task<int> ScrapeVolumeAsync(string volumeName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping Opportunity volume: {VolumeName}", volumeName);

        try
        {
            // Build index file URL
            var indexUrl = PdsBrowseUrlBuilder.BuildIndexUrl("opportunity", volumeName);
            _logger.LogInformation("Downloading index file: {Url}", indexUrl);

            var httpClient = _httpClientFactory.CreateClient("NASA");

            // Stream download index file
            using var stream = await httpClient.GetStreamAsync(indexUrl, cancellationToken);

            // Parse and process rows
            return await ProcessIndexStreamAsync(stream, volumeName, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error downloading volume {VolumeName}: {Message}",
                volumeName, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping volume {VolumeName}", volumeName);
            throw;
        }
    }

    /// <summary>
    /// Scrape all Opportunity volumes (PANCAM, NAVCAM, HAZCAM, MI, DESCENT)
    /// </summary>
    public async Task<int> ScrapeAllVolumesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scraping all Opportunity volumes");

        var volumes = PdsBrowseUrlBuilder.GetVolumesForRover("opportunity");
        var totalInserted = 0;

        foreach (var volume in volumes)
        {
            try
            {
                var inserted = await ScrapeVolumeAsync(volume, cancellationToken);
                totalInserted += inserted;

                _logger.LogInformation(
                    "Completed volume {Volume}: {Count} photos inserted",
                    volume, inserted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scrape volume {Volume}, continuing with next volume", volume);
                // Continue with next volume even if one fails
            }
        }

        _logger.LogInformation(
            "Completed all Opportunity volumes: {Total} total photos inserted",
            totalInserted);

        return totalInserted;
    }

    /// <summary>
    /// Process PDS index file stream and insert photos
    /// </summary>
    private async Task<int> ProcessIndexStreamAsync(
        Stream stream,
        string volumeName,
        CancellationToken cancellationToken)
    {
        var rover = await _context.Rovers
            .Include(r => r.Cameras)
            .FirstOrDefaultAsync(r => r.Id == OpportunityRoverId, cancellationToken);

        if (rover == null)
        {
            throw new InvalidOperationException($"Opportunity rover (ID {OpportunityRoverId}) not found in database");
        }

        var processedCount = 0;
        var insertedCount = 0;
        var skippedCount = 0;
        var batchErrorCount = 0;
        var pendingPhotos = new List<Photo>();
        var pendingNasaIds = new HashSet<string>(); // Track IDs in current batch

        // Load existing NASA IDs into memory for fast duplicate checking
        _logger.LogInformation("Loading existing photo IDs for volume {Volume}...", volumeName);
        var existingNasaIds = await _context.Photos
            .Where(p => p.RoverId == rover.Id)
            .Select(p => p.NasaId)
            .ToHashSetAsync(cancellationToken);
        _logger.LogInformation("Loaded {Count} existing photo IDs", existingNasaIds.Count);

        _logger.LogInformation("Parsing index file for volume {Volume}", volumeName);

        await foreach (var row in _parser.ParseStreamAsync(stream, cancellationToken))
        {
            processedCount++;

            try
            {
                // Check if photo already exists (in-memory lookup is much faster)
                if (existingNasaIds.Contains(row.ProductId))
                {
                    skippedCount++;
                    continue;
                }

                // Check if photo is duplicate within current batch
                if (pendingNasaIds.Contains(row.ProductId))
                {
                    skippedCount++;
                    _logger.LogDebug("Skipping duplicate within batch: {ProductId}", row.ProductId);
                    continue;
                }

                // Map camera name
                var dbCameraName = MerCameraMapper.MapToDbName(row.InstrumentId);

                // Find camera in database
                var camera = rover.Cameras.FirstOrDefault(c =>
                    c.Name.Equals(dbCameraName, StringComparison.OrdinalIgnoreCase));

                if (camera == null)
                {
                    _logger.LogWarning(
                        "Camera not found: {PdsName} (mapped to {DbName}). Skipping photo {ProductId}",
                        row.InstrumentId, dbCameraName, row.ProductId);
                    skippedCount++;
                    continue;
                }

                // Build browse URL
                var browseUrl = PdsBrowseUrlBuilder.BuildBrowseUrl(
                    "opportunity",
                    row.PathName,
                    row.FileName,
                    row.Sol);

                // Convert to Photo entity
                var photo = MapToPhoto(row, rover, camera, browseUrl);
                pendingPhotos.Add(photo);
                pendingNasaIds.Add(row.ProductId);

                // Batch commit
                if (pendingPhotos.Count >= BatchSize)
                {
                    try
                    {
                        await _context.Photos.AddRangeAsync(pendingPhotos, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        insertedCount += pendingPhotos.Count;

                        // Add newly inserted IDs to the existing set for future duplicate checks
                        foreach (var nasaId in pendingNasaIds)
                        {
                            existingNasaIds.Add(nasaId);
                        }

                        _logger.LogInformation(
                            "Volume {Volume}: Committed batch. Total inserted: {Inserted}, Processed: {Processed}",
                            volumeName, insertedCount, processedCount);
                    }
                    catch (Exception batchEx)
                    {
                        batchErrorCount++;
                        _logger.LogError(batchEx,
                            "Batch insert failed at row {Row}. Likely duplicates in batch. Skipping batch and continuing.",
                            processedCount);

                        // Detach failed entities to prevent tracking issues
                        foreach (var failedPhoto in pendingPhotos)
                        {
                            _context.Entry(failedPhoto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                    finally
                    {
                        // Always clear batch regardless of success/failure
                        pendingPhotos.Clear();
                        pendingNasaIds.Clear();
                    }
                }

                // Progress logging
                if (processedCount % ProgressLogInterval == 0)
                {
                    _logger.LogInformation(
                        "Volume {Volume}: Processed {Count} rows, inserted {Inserted}, skipped {Skipped}, batch errors {BatchErrors}",
                        volumeName, processedCount, insertedCount, skippedCount, batchErrorCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing row {Count} (ProductId: {ProductId})",
                    processedCount, row.ProductId);
                skippedCount++;
            }
        }

        // Final commit for remaining photos
        if (pendingPhotos.Any())
        {
            try
            {
                await _context.Photos.AddRangeAsync(pendingPhotos, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                insertedCount += pendingPhotos.Count;
            }
            catch (Exception finalEx)
            {
                batchErrorCount++;
                _logger.LogError(finalEx, "Final batch insert failed. Likely duplicates in batch.");

                // Detach failed entities
                foreach (var failedPhoto in pendingPhotos)
                {
                    _context.Entry(failedPhoto).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }

        _logger.LogInformation(
            "Volume {Volume} complete: Processed {Processed} rows, inserted {Inserted}, skipped {Skipped}, batch errors {BatchErrors}",
            volumeName, processedCount, insertedCount, skippedCount, batchErrorCount);

        return insertedCount;
    }

    /// <summary>
    /// Map PdsIndexRow to Photo entity
    /// Preserves all 55 metadata fields in RawData JSONB column
    /// </summary>
    private static Photo MapToPhoto(PdsIndexRow row, Rover rover, Camera camera, string browseUrl)
    {
        return new Photo
        {
            // Core identification
            NasaId = row.ProductId,

            // Time data
            Sol = row.Sol,
            EarthDate = row.StartTime?.Date,
            DateTakenUtc = row.StartTime ?? DateTime.UtcNow,  // Fallback if null (shouldn't happen)
            DateTakenMars = row.LocalTrueSolarTime ?? "",
            DateReceived = row.EarthReceivedStart,

            // Image URLs (only browse JPG available for MER)
            ImgSrcFull = browseUrl,
            ImgSrcSmall = "",  // No smaller versions available for MER
            ImgSrcMedium = "",
            ImgSrcLarge = "",

            // Image metadata
            Width = row.LineSamples,
            Height = row.Lines,
            SampleType = "full",  // All browse images are full resolution
            FilterName = row.FilterName,

            // Camera telemetry
            MastAz = row.SiteInstrumentAzimuth,
            MastEl = row.SiteInstrumentElevation,

            // Extended telemetry (MER has richer data than active rovers!)
            Attitude = null,  // Not in PDS index
            SpacecraftClock = ParseSpacecraftClock(row.SpacecraftClockStart),
            CameraVector = null,
            CameraPosition = null,
            CameraModelType = null,

            // Location (not available in index files)
            Site = null,
            Drive = null,
            Xyz = null,

            // Metadata
            Title = $"Sol {row.Sol} - {row.FilterName}",
            Caption = $"{camera.FullName} image from sol {row.Sol}",
            Credit = "NASA/JPL-Caltech",

            // Relationships
            RoverId = rover.Id,
            CameraId = camera.Id,

            // Store ALL 55 fields as JSON (100% data preservation)
            RawData = SerializeToJson(row),

            // Timestamps
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Serialize PdsIndexRow to JSON for RawData storage
    /// Preserves all 55 metadata fields
    /// </summary>
    private static JsonDocument SerializeToJson(PdsIndexRow row)
    {
        var json = JsonSerializer.Serialize(row, new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        });

        return JsonDocument.Parse(json);
    }

    /// <summary>
    /// Parse spacecraft clock string to float (if possible)
    /// Format: "128287181.621" → 128287181.621
    /// </summary>
    private static float? ParseSpacecraftClock(string? sclk)
    {
        if (string.IsNullOrWhiteSpace(sclk))
            return null;

        if (float.TryParse(sclk.Trim(), out var value))
            return value;

        return null;
    }
}
