using MarsVista.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

/// <summary>
/// In-memory cache of static reference data (rover and camera name -> id mappings)
/// loaded once at startup (via StaticReferenceCacheWarmer) or lazily on first use.
///
/// Photo queries call the resolver methods to translate user-supplied filter values
/// to integer IDs *before* building the photos query, so EF Core can filter on
/// rover_id / camera_id directly instead of joining via p.Rover.Name.
///
/// Cardinality of the underlying tables differs:
///   - rovers.name is unique (Curiosity, Perseverance, Opportunity, Spirit) and the
///     application treats it as a primary identifier, so rovers are name -> single id.
///   - cameras has a (rover_id, name) unique constraint, NOT name alone. The same
///     name (FHAZ, NAVCAM, RHAZ, PANCAM, etc.) appears on multiple rovers as
///     distinct camera rows. Cameras are therefore name -> list of ids and a
///     name-based camera filter expands to the union of all matching ids - matching
///     the pre-existing public API where ?cameras=FHAZ returned FHAZ photos from
///     every rover that has a FHAZ.
///
/// Normalisation: rover names are stored mixed-case in the DB (`Curiosity`) and
/// matched lower-invariant; camera names are stored upper-case (`FHAZ`) and matched
/// upper-invariant. The dictionary keys reflect those storage conventions so a single
/// allocation handles each lookup.
/// </summary>
public interface IStaticReferenceCache
{
    int? GetRoverIdByName(string name);
    IReadOnlyList<int> GetRoverIdsByNames(IEnumerable<string> names);
    IReadOnlyList<int> GetCameraIdsByName(string name);
    IReadOnlyList<int> GetCameraIdsByNames(IEnumerable<string> names);

    /// <summary>
    /// Force the cache to load now (used by the startup warmer to avoid paying the
    /// double-checked-lock + DB roundtrip cost on the first hot request).
    /// Safe to call concurrently and repeatedly - subsequent calls are no-ops once
    /// the cache has loaded.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);
}

public class StaticReferenceCache : IStaticReferenceCache
{
    private static readonly IReadOnlyList<int> Empty = Array.Empty<int>();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaticReferenceCache> _logger;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
    // volatile so the lock-free fast path on GetRoversOrLoad / GetCamerasOrLoad
    // observes the populated dictionary contents (not just the reference write)
    // on weakly-ordered architectures. On x86_64 this is a no-op; the keyword
    // documents intent and removes the spec-level race.
    private volatile IReadOnlyDictionary<string, int>? _roversByName;
    private volatile IReadOnlyDictionary<string, IReadOnlyList<int>>? _camerasByName;

    public StaticReferenceCache(IServiceScopeFactory scopeFactory, ILogger<StaticReferenceCache> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int? GetRoverIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var rovers = GetRoversOrLoad();
        return rovers.TryGetValue(name.ToLowerInvariant(), out var id) ? id : null;
    }

    public IReadOnlyList<int> GetRoverIdsByNames(IEnumerable<string> names)
    {
        var rovers = GetRoversOrLoad();
        var seen = new HashSet<int>();
        var result = new List<int>();
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (rovers.TryGetValue(name.ToLowerInvariant(), out var id) && seen.Add(id))
            {
                result.Add(id);
            }
        }
        return result;
    }

    public IReadOnlyList<int> GetCameraIdsByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Empty;
        var cameras = GetCamerasOrLoad();
        return cameras.TryGetValue(name.ToUpperInvariant(), out var ids) ? ids : Empty;
    }

    public IReadOnlyList<int> GetCameraIdsByNames(IEnumerable<string> names)
    {
        var cameras = GetCamerasOrLoad();
        var seen = new HashSet<int>();
        var result = new List<int>();
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (cameras.TryGetValue(name.ToUpperInvariant(), out var ids))
            {
                foreach (var id in ids)
                {
                    if (seen.Add(id)) result.Add(id);
                }
            }
        }
        return result;
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_roversByName != null && _camerasByName != null) return;

        await _loadSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_roversByName != null && _camerasByName != null) return;

            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

            var roverRows = await ctx.Rovers
                .AsNoTracking()
                .Select(r => new { r.Id, r.Name })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var cameraRows = await ctx.Cameras
                .AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            ApplyLoad(roverRows.Select(r => (r.Id, r.Name)), cameraRows.Select(c => (c.Id, c.Name)));
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    private IReadOnlyDictionary<string, int> GetRoversOrLoad()
    {
        return _roversByName ?? LoadBlocking().Rovers;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<int>> GetCamerasOrLoad()
    {
        return _camerasByName ?? LoadBlocking().Cameras;
    }

    private (IReadOnlyDictionary<string, int> Rovers, IReadOnlyDictionary<string, IReadOnlyList<int>> Cameras) LoadBlocking()
    {
        // Synchronous fallback used only when a request lands before EnsureLoadedAsync
        // has completed (e.g. integration tests that don't register the startup warmer).
        // The Wait() is acceptable here because it only runs once per process lifetime.
        EnsureLoadedAsync().GetAwaiter().GetResult();
        return (_roversByName!, _camerasByName!);
    }

    private void ApplyLoad(
        IEnumerable<(int Id, string Name)> rovers,
        IEnumerable<(int Id, string Name)> cameras)
    {
        var roverDict = rovers.ToDictionary(r => r.Name.ToLowerInvariant(), r => r.Id);

        var camerasByName = cameras
            .GroupBy(c => c.Name.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<int>)g.Select(c => c.Id).Distinct().ToArray());

        if (roverDict.Count == 0 || camerasByName.Count == 0)
        {
            _logger.LogWarning(
                "StaticReferenceCache load returned empty data ({RoverCount} rovers, {CameraNameCount} distinct camera names) - will retry on next call",
                roverDict.Count, camerasByName.Count);
            return;
        }

        _roversByName = roverDict;
        _camerasByName = camerasByName;

        _logger.LogInformation(
            "StaticReferenceCache loaded: {RoverCount} rovers, {CameraNameCount} distinct camera names ({TotalCameraIds} total ids)",
            roverDict.Count, camerasByName.Count, camerasByName.Sum(kv => kv.Value.Count));
    }
}
