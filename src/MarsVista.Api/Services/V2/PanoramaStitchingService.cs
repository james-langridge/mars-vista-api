using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

public class PanoramaStitchingService : IPanoramaStitchingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPanoramaService _panoramaService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PanoramaStitchingService> _logger;
    private readonly string _stitchedImagesPath;
    private readonly string _pythonScriptPath;

    // Limit concurrent stitch jobs (CPU-intensive)
    private static readonly SemaphoreSlim _stitchSemaphore = new(2);

    public PanoramaStitchingService(
        IServiceScopeFactory scopeFactory,
        IPanoramaService panoramaService,
        IHttpClientFactory httpClientFactory,
        ILogger<PanoramaStitchingService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _panoramaService = panoramaService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _stitchedImagesPath = configuration["StitchedImagesPath"]
            ?? Environment.GetEnvironmentVariable("STITCHED_IMAGES_PATH")
            ?? "./data/stitched";
        _pythonScriptPath = Path.Combine(AppContext.BaseDirectory, "scripts", "stitch_panorama.py");

        // Fall back to project-relative path for local dev
        if (!File.Exists(_pythonScriptPath))
        {
            _pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "stitch_panorama.py");
        }
    }

    public async Task<StitchStatusResponse> GetStitchStatusAsync(
        string panoramaId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        var record = await dbContext.StitchedPanoramas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId, cancellationToken);

        if (record == null)
            return new StitchStatusResponse { Status = "not_started" };

        return ToResponse(record, panoramaId);
    }

    public async Task<StitchStatusResponse> RequestStitchAsync(
        string panoramaId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        // Check existing record
        var existing = await dbContext.StitchedPanoramas
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId, cancellationToken);

        if (existing != null)
        {
            // If completed or processing, return current status
            if (existing.Status is "completed" or "processing")
                return ToResponse(existing, panoramaId);

            // If failed, allow retry by resetting status
            existing.Status = "processing";
            existing.ErrorMessage = null;
            existing.CompletedAt = null;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Verify panorama exists before creating record
            var panoramaResource = await _panoramaService.GetPanoramaByIdAsync(panoramaId, cancellationToken);
            if (panoramaResource == null)
                return new StitchStatusResponse { Status = "not_found" };

            existing = new StitchedPanorama
            {
                PanoramaId = panoramaId,
                Status = "processing",
                CreatedAt = DateTime.UtcNow
            };
            dbContext.StitchedPanoramas.Add(existing);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Fire-and-forget background task
        _ = Task.Run(() => ExecuteStitchAsync(panoramaId));

        return new StitchStatusResponse { Status = "processing" };
    }

    public async Task<string?> GetStitchedImagePathAsync(
        string panoramaId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        var record = await dbContext.StitchedPanoramas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId && s.Status == "completed", cancellationToken);

        if (record?.ImagePath == null)
            return null;

        var fullPath = Path.Combine(_stitchedImagesPath, record.ImagePath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    private async Task ExecuteStitchAsync(string panoramaId)
    {
        await _stitchSemaphore.WaitAsync();
        try
        {
            _logger.LogInformation("Starting stitch for panorama {PanoramaId}", panoramaId);

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
            var panoramaService = scope.ServiceProvider.GetRequiredService<IPanoramaService>();

            // Get photo entities for this panorama
            var photos = await panoramaService.GetPanoramaPhotosAsync(panoramaId);
            if (photos == null || photos.Count < 2)
            {
                await SetFailedAsync(dbContext, panoramaId, "Panorama not found or has fewer than 2 photos");
                return;
            }

            // Select best photo per unique azimuth position
            var selectedPhotos = SelectPhotosForStitching(photos);
            _logger.LogInformation("Selected {Count} photos for stitching panorama {PanoramaId}",
                selectedPhotos.Count, panoramaId);

            if (selectedPhotos.Count < 2)
            {
                await SetFailedAsync(dbContext, panoramaId, "Fewer than 2 unique azimuth positions");
                return;
            }

            // Download source images to temp directory
            var tempDir = Path.Combine(Path.GetTempPath(), $"stitch_{panoramaId}_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var imagePaths = await DownloadPhotosAsync(selectedPhotos, tempDir);
                if (imagePaths.Count < 2)
                {
                    await SetFailedAsync(dbContext, panoramaId, "Failed to download enough source images");
                    return;
                }

                // Call Python stitcher
                var outputPath = Path.Combine(_stitchedImagesPath, $"{panoramaId}.jpg");
                Directory.CreateDirectory(_stitchedImagesPath);

                var result = await RunPythonStitcherAsync(imagePaths, outputPath);

                if (result.Status == "success")
                {
                    var record = await dbContext.StitchedPanoramas
                        .FirstAsync(s => s.PanoramaId == panoramaId);
                    record.Status = "completed";
                    record.ImagePath = $"{panoramaId}.jpg";
                    record.ImageWidth = result.Width;
                    record.ImageHeight = result.Height;
                    record.ImageSizeBytes = result.SizeBytes;
                    record.SourcePhotoCount = selectedPhotos.Count;
                    record.CompletedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Stitch completed for {PanoramaId}: {Width}x{Height}, {SizeBytes} bytes",
                        panoramaId, result.Width, result.Height, result.SizeBytes);
                }
                else
                {
                    await SetFailedAsync(dbContext, panoramaId, result.Error ?? "Unknown stitching error");
                }
            }
            finally
            {
                // Clean up temp directory
                try { Directory.Delete(tempDir, true); }
                catch { /* best effort */ }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stitch failed for panorama {PanoramaId}", panoramaId);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
                await SetFailedAsync(dbContext, panoramaId, ex.Message);
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to update stitch record for {PanoramaId}", panoramaId);
            }
        }
        finally
        {
            _stitchSemaphore.Release();
        }
    }

    public static List<Photo> SelectPhotosForStitching(List<Photo> photos)
    {
        // Group by rounded azimuth (nearest degree)
        var byAzimuth = photos
            .Where(p => p.MastAz.HasValue)
            .GroupBy(p => Math.Round(p.MastAz!.Value))
            .OrderBy(g => g.Key);

        var selected = new List<Photo>();
        foreach (var group in byAzimuth)
        {
            // Prefer RGB/Bayer filter for natural color
            var best = group
                .OrderByDescending(p =>
                    p.FilterName != null &&
                    (p.FilterName.Contains("RGB", StringComparison.OrdinalIgnoreCase) ||
                     p.FilterName.Contains("Bayer", StringComparison.OrdinalIgnoreCase)) ? 1 : 0)
                .ThenByDescending(p => p.Width ?? 0) // Then prefer larger images
                .First();
            selected.Add(best);
        }

        return selected;
    }

    private async Task<List<string>> DownloadPhotosAsync(List<Photo> photos, string tempDir)
    {
        var client = _httpClientFactory.CreateClient("NASA");
        var paths = new List<string>();

        foreach (var photo in photos)
        {
            var url = photo.ImgSrcFull;
            if (string.IsNullOrEmpty(url))
            {
                url = photo.ImgSrcLarge;
                if (string.IsNullOrEmpty(url)) continue;
            }

            // Normalize to HTTPS
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url[7..];

            try
            {
                var fileName = $"{paths.Count:D3}_{photo.Id}.jpg";
                var filePath = Path.Combine(tempDir, fileName);

                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                await using var fs = File.Create(filePath);
                await response.Content.CopyToAsync(fs);

                paths.Add(filePath);
                _logger.LogDebug("Downloaded photo {PhotoId} to {Path}", photo.Id, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download photo {PhotoId} from {Url}", photo.Id, url);
            }
        }

        return paths;
    }

    private async Task<StitchResult> RunPythonStitcherAsync(List<string> imagePaths, string outputPath)
    {
        var input = JsonSerializer.Serialize(new
        {
            image_paths = imagePaths,
            output_path = outputPath
        });

        var startInfo = new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = _pythonScriptPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderr = await process.StandardError.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);

        if (!string.IsNullOrEmpty(stderr))
            _logger.LogWarning("Python stitcher stderr: {Stderr}", stderr);

        try
        {
            return JsonSerializer.Deserialize<StitchResult>(stdout)
                ?? new StitchResult { Status = "failed", Error = "Empty response from stitcher" };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse stitcher output: {Output}", stdout);
            return new StitchResult { Status = "failed", Error = $"Invalid stitcher output: {stdout}" };
        }
    }

    private static async Task SetFailedAsync(MarsVistaDbContext dbContext, string panoramaId, string error)
    {
        var record = await dbContext.StitchedPanoramas
            .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId);
        if (record != null)
        {
            record.Status = "failed";
            record.ErrorMessage = error.Length > 2000 ? error[..2000] : error;
            record.CompletedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
    }

    private StitchStatusResponse ToResponse(StitchedPanorama record, string panoramaId)
    {
        return new StitchStatusResponse
        {
            Status = record.Status,
            ImageUrl = record.Status == "completed"
                ? $"/api/v2/panoramas/{panoramaId}/stitch/image"
                : null,
            Width = record.ImageWidth,
            Height = record.ImageHeight,
            SizeBytes = record.ImageSizeBytes,
            Error = record.ErrorMessage
        };
    }

    private record StitchResult
    {
        public string Status { get; init; } = string.Empty;
        public int? Width { get; init; }
        public int? Height { get; init; }
        public long? SizeBytes { get; init; }
        public string? Error { get; init; }
    }
}
