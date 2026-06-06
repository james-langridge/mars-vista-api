using MarsVista.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Services;

/// <summary>
/// In-memory cache of static reference data (rover and camera name -> id mappings)
/// loaded once on first use.
///
/// Photo queries call <c>GetRoverIdByName</c> / <c>GetCameraIdByName</c> to resolve
/// user-supplied filter values to integer IDs *before* building the photos query,
/// so the EF Core query can filter on <c>rover_id</c> / <c>camera_id</c> directly
/// instead of joining via <c>p.Rover.Name</c>. See story 052a for the analysis -
/// the join-based filter was triggering a backward sweep of <c>ix_photos_sol</c>
/// that pulled 4 GB of disk pages per call.
///
/// Loaded lazily under a double-checked lock; treats an empty result as transient
/// (does not cache empty) so we recover if first call lands during DB warmup.
/// </summary>
public interface IStaticReferenceCache
{
    int? GetRoverIdByName(string name);
    int? GetCameraIdByName(string name);
    IReadOnlyList<int> GetRoverIdsByNames(IEnumerable<string> names);
    IReadOnlyList<int> GetCameraIdsByNames(IEnumerable<string> names);
}

public class StaticReferenceCache : IStaticReferenceCache
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaticReferenceCache> _logger;
    private readonly object _initLock = new();
    private IReadOnlyDictionary<string, int>? _roversByName;
    private IReadOnlyDictionary<string, int>? _camerasByName;

    public StaticReferenceCache(IServiceScopeFactory scopeFactory, ILogger<StaticReferenceCache> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int? GetRoverIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var data = GetData();
        return data.Rovers.TryGetValue(name.ToLowerInvariant(), out var id) ? id : null;
    }

    public int? GetCameraIdByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var data = GetData();
        return data.Cameras.TryGetValue(name.ToUpperInvariant(), out var id) ? id : null;
    }

    public IReadOnlyList<int> GetRoverIdsByNames(IEnumerable<string> names)
    {
        var data = GetData();
        return ResolveIds(names, data.Rovers, n => n.ToLowerInvariant());
    }

    public IReadOnlyList<int> GetCameraIdsByNames(IEnumerable<string> names)
    {
        var data = GetData();
        return ResolveIds(names, data.Cameras, n => n.ToUpperInvariant());
    }

    private static List<int> ResolveIds(
        IEnumerable<string> names,
        IReadOnlyDictionary<string, int> lookup,
        Func<string, string> normalise)
    {
        var seen = new HashSet<int>();
        var result = new List<int>();
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (lookup.TryGetValue(normalise(name), out var id) && seen.Add(id))
            {
                result.Add(id);
            }
        }
        return result;
    }

    private (IReadOnlyDictionary<string, int> Rovers, IReadOnlyDictionary<string, int> Cameras) GetData()
    {
        var rovers = _roversByName;
        var cameras = _camerasByName;
        if (rovers != null && cameras != null) return (rovers, cameras);

        lock (_initLock)
        {
            if (_roversByName != null && _camerasByName != null)
                return (_roversByName, _camerasByName);

            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<MarsVistaDbContext>();

            var loadedRovers = ctx.Rovers
                .AsNoTracking()
                .Select(r => new { r.Id, r.Name })
                .AsEnumerable()
                .ToDictionary(r => r.Name.ToLowerInvariant(), r => r.Id);

            var loadedCameras = ctx.Cameras
                .AsNoTracking()
                .Select(c => new { c.Id, c.Name })
                .AsEnumerable()
                .ToDictionary(c => c.Name.ToUpperInvariant(), c => c.Id);

            if (loadedRovers.Count == 0 || loadedCameras.Count == 0)
            {
                _logger.LogWarning(
                    "StaticReferenceCache load returned empty data ({RoverCount} rovers, {CameraCount} cameras) - will retry on next call",
                    loadedRovers.Count, loadedCameras.Count);
                return (loadedRovers, loadedCameras);
            }

            _roversByName = loadedRovers;
            _camerasByName = loadedCameras;

            _logger.LogInformation(
                "StaticReferenceCache loaded: {RoverCount} rovers, {CameraCount} cameras",
                loadedRovers.Count, loadedCameras.Count);

            return (loadedRovers, loadedCameras);
        }
    }
}
