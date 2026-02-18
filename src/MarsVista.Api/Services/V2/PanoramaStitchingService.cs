using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Data;
using MarsVista.Core.Entities;
using MarsVista.Api.DTOs.V2;

namespace MarsVista.Api.Services.V2;

public class PanoramaStitchingService : IPanoramaStitchingService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PanoramaStitchingService> _logger;
    private readonly string _stitchedImagesPath;
    private readonly string _pythonScriptPath;
    private readonly CancellationTokenSource _shutdownCts = new();

    // Limit concurrent stitch jobs (CPU-intensive)
    private static readonly SemaphoreSlim _stitchSemaphore = new(2);

    public PanoramaStitchingService(
        IServiceScopeFactory scopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<PanoramaStitchingService> logger,
        IConfiguration configuration,
        IHostApplicationLifetime lifetime)
    {
        _scopeFactory = scopeFactory;
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

        if (!File.Exists(_pythonScriptPath))
        {
            _logger.LogWarning("Python stitching script not found at {Path}. Stitching will fail at runtime.", _pythonScriptPath);
        }

        lifetime.ApplicationStopping.Register(() => _shutdownCts.Cancel());

        // Mark orphaned "processing" records as failed on startup
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
                var orphaned = await dbContext.StitchedPanoramas
                    .Where(s => s.Status == "processing")
                    .ToListAsync();
                foreach (var record in orphaned)
                {
                    record.Status = "failed";
                    record.ErrorMessage = "Interrupted by application restart";
                    record.CompletedAt = DateTime.UtcNow;
                }
                if (orphaned.Count > 0)
                {
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Marked {Count} orphaned stitch jobs as failed", orphaned.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up orphaned stitch records on startup");
            }
        });
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
            var panoramaService = scope.ServiceProvider.GetRequiredService<IPanoramaService>();
            var panoramaResource = await panoramaService.GetPanoramaByIdAsync(panoramaId, cancellationToken);
            if (panoramaResource == null)
                return new StitchStatusResponse { Status = "not_found" };

            existing = new StitchedPanorama
            {
                PanoramaId = panoramaId,
                Status = "processing",
                CreatedAt = DateTime.UtcNow
            };
            dbContext.StitchedPanoramas.Add(existing);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrent insert won the race - return the existing record
                var raced = await dbContext.StitchedPanoramas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.PanoramaId == panoramaId, cancellationToken);
                if (raced != null)
                    return ToResponse(raced, panoramaId);
                throw;
            }
        }

        // Fire-and-forget background task with shutdown awareness
        _ = Task.Run(() => ExecuteStitchAsync(panoramaId, _shutdownCts.Token));

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

    private async Task ExecuteStitchAsync(string panoramaId, CancellationToken shutdownToken)
    {
        if (!await _stitchSemaphore.WaitAsync(TimeSpan.FromMinutes(10), shutdownToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();
            await SetFailedAsync(dbContext, panoramaId, "Timed out waiting for available stitching slot");
            return;
        }
        try
        {
            var sw = Stopwatch.StartNew();
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
                var imagePaths = await DownloadPhotosAsync(selectedPhotos, tempDir, shutdownToken);
                if (imagePaths.Count < 2)
                {
                    await SetFailedAsync(dbContext, panoramaId, "Failed to download enough source images");
                    return;
                }

                // Call Python stitcher
                var outputPath = Path.Combine(_stitchedImagesPath, $"{panoramaId}.jpg");
                Directory.CreateDirectory(_stitchedImagesPath);

                var result = await RunPythonStitcherAsync(imagePaths, outputPath, shutdownToken);

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
                        "Stitch completed for {PanoramaId}: {Width}x{Height}, {SizeBytes} bytes in {Elapsed}ms",
                        panoramaId, result.Width, result.Height, result.SizeBytes, sw.ElapsedMilliseconds);
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

    private async Task<List<string>> DownloadPhotosAsync(List<Photo> photos, string tempDir, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("NASA");
        using var downloadSemaphore = new SemaphoreSlim(4);
        var results = new (int index, string? path)[photos.Count];

        var tasks = photos.Select(async (photo, index) =>
        {
            var url = photo.ImgSrcFull;
            if (string.IsNullOrEmpty(url))
            {
                url = photo.ImgSrcLarge;
                if (string.IsNullOrEmpty(url)) return;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url[7..];

            await downloadSemaphore.WaitAsync(ct);
            try
            {
                var fileName = $"{index:D3}_{photo.Id}.jpg";
                var filePath = Path.Combine(tempDir, fileName);

                using var response = await client.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                await using var fs = File.Create(filePath);
                await response.Content.CopyToAsync(fs, ct);

                results[index] = (index, filePath);
                _logger.LogDebug("Downloaded photo {PhotoId} to {Path}", photo.Id, filePath);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download photo {PhotoId} from {Url}", photo.Id, url);
            }
            finally
            {
                downloadSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // Return in order, filtering out failed downloads
        return results.Where(r => r.path != null).OrderBy(r => r.index).Select(r => r.path!).ToList();
    }

    private async Task<StitchResult> RunPythonStitcherAsync(List<string> imagePaths, string outputPath, CancellationToken ct = default)
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
        _logger.LogInformation("Starting Python stitcher: {Script} with {Count} images, output: {Output}",
            _pythonScriptPath, imagePaths.Count, outputPath);
        process.Start();

        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();

        // Read stdout and stderr in parallel to avoid pipe buffer deadlock.
        // Don't pass CancellationToken — pipe reads can't be cancelled in .NET.
        // Instead, kill the process on timeout to unblock the reads.
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var readTask = Task.WhenAll(stdoutTask, stderrTask);
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), ct);

        var completed = await Task.WhenAny(readTask, timeoutTask);
        if (completed != readTask)
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* best effort */ }
            // Killing unblocks the pipe reads — wait briefly for them to finish
            await Task.WhenAny(readTask, Task.Delay(5000));
            return new StitchResult { Status = "failed", Error = "Stitching timed out after 5 minutes" };
        }

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;
        await process.WaitForExitAsync(ct);

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
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;
        [JsonPropertyName("width")]
        public int? Width { get; init; }
        [JsonPropertyName("height")]
        public int? Height { get; init; }
        [JsonPropertyName("size_bytes")]
        public long? SizeBytes { get; init; }
        [JsonPropertyName("error")]
        public string? Error { get; init; }
    }
}
