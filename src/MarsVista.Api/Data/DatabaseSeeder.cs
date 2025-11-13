using MarsVista.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Data;

/// <summary>
/// Seeds static reference data (rovers and cameras) into the database
/// </summary>
public class DatabaseSeeder
{
    private readonly MarsVistaDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MarsVistaDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all static reference data. Idempotent - safe to run multiple times.
    /// </summary>
    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        await SeedRoversAsync();
        await SeedCamerasAsync();

        _logger.LogInformation("Database seeding completed");
    }

    private async Task SeedRoversAsync()
    {
        var rovers = new[]
        {
            new Rover
            {
                Name = "Perseverance",
                LandingDate = new DateTime(2021, 2, 18, 0, 0, 0, DateTimeKind.Utc),
                LaunchDate = null,
                Status = "active",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Curiosity",
                LandingDate = new DateTime(2012, 8, 6, 0, 0, 0, DateTimeKind.Utc),
                LaunchDate = null,
                Status = "active",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Opportunity",
                LandingDate = new DateTime(2004, 1, 25, 0, 0, 0, DateTimeKind.Utc),
                LaunchDate = null,
                Status = "complete",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            },
            new Rover
            {
                Name = "Spirit",
                LandingDate = new DateTime(2004, 1, 4, 0, 0, 0, DateTimeKind.Utc),
                LaunchDate = null,
                Status = "complete",
                MaxSol = null,
                MaxDate = null,
                TotalPhotos = 0
            }
        };

        foreach (var rover in rovers)
        {
            var exists = await _context.Rovers.AnyAsync(r => r.Name == rover.Name);

            if (!exists)
            {
                rover.CreatedAt = DateTime.UtcNow;
                rover.UpdatedAt = DateTime.UtcNow;
                _context.Rovers.Add(rover);
                _logger.LogInformation("Seeding rover: {RoverName}", rover.Name);
            }
            else
            {
                _logger.LogDebug("Rover {RoverName} already exists, skipping", rover.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedCamerasAsync()
    {
        var cameraSeedData = new Dictionary<string, (string Name, string FullName)[]>
        {
            ["Perseverance"] = new[]
            {
                ("EDL_RUCAM", "Rover Up-Look Camera"),
                ("EDL_RDCAM", "Rover Down-Look Camera"),
                ("EDL_DDCAM", "Descent Stage Down-Look Camera"),
                ("EDL_PUCAM1", "Parachute Up-Look Camera A"),
                ("EDL_PUCAM2", "Parachute Up-Look Camera B"),
                ("NAVCAM_LEFT", "Navigation Camera - Left"),
                ("NAVCAM_RIGHT", "Navigation Camera - Right"),
                ("MCZ_RIGHT", "Mast Camera Zoom - Right"),
                ("MCZ_LEFT", "Mast Camera Zoom - Left"),
                ("FRONT_HAZCAM_LEFT_A", "Front Hazard Avoidance Camera - Left"),
                ("FRONT_HAZCAM_RIGHT_A", "Front Hazard Avoidance Camera - Right"),
                ("REAR_HAZCAM_LEFT", "Rear Hazard Avoidance Camera - Left"),
                ("REAR_HAZCAM_RIGHT", "Rear Hazard Avoidance Camera - Right"),
                ("SKYCAM", "MEDA Skycam"),
                ("SHERLOC_WATSON", "SHERLOC WATSON Camera"),
                ("SUPERCAM_RMI", "SuperCam Remote Micro Imager"),
                ("LCAM", "Lander Vision System Camera")
            },
            ["Curiosity"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("MAST", "Mast Camera"),
                ("CHEMCAM", "Chemistry and Camera Complex"),
                ("MAHLI", "Mars Hand Lens Imager"),
                ("MARDI", "Mars Descent Imager"),
                ("NAVCAM", "Navigation Camera")
            },
            ["Opportunity"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("NAVCAM", "Navigation Camera"),
                ("PANCAM", "Panoramic Camera"),
                ("MINITES", "Miniature Thermal Emission Spectrometer (Mini-TES)"),
                ("ENTRY", "Entry, Descent, and Landing Camera")
            },
            ["Spirit"] = new[]
            {
                ("FHAZ", "Front Hazard Avoidance Camera"),
                ("RHAZ", "Rear Hazard Avoidance Camera"),
                ("NAVCAM", "Navigation Camera"),
                ("PANCAM", "Panoramic Camera"),
                ("MINITES", "Miniature Thermal Emission Spectrometer (Mini-TES)"),
                ("ENTRY", "Entry, Descent, and Landing Camera")
            }
        };

        // Load all rovers with their cameras
        var rovers = await _context.Rovers
            .Include(r => r.Cameras)
            .ToListAsync();

        foreach (var rover in rovers)
        {
            if (!cameraSeedData.TryGetValue(rover.Name, out var cameras))
            {
                _logger.LogWarning("No camera seed data found for rover: {RoverName}", rover.Name);
                continue;
            }

            foreach (var (name, fullName) in cameras)
            {
                var exists = rover.Cameras.Any(c => c.Name == name);

                if (!exists)
                {
                    var camera = new Camera
                    {
                        Name = name,
                        FullName = fullName,
                        RoverId = rover.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Cameras.Add(camera);
                    _logger.LogInformation("Seeding camera: {CameraName} for {RoverName}", name, rover.Name);
                }
                else
                {
                    _logger.LogDebug("Camera {CameraName} for {RoverName} already exists, skipping",
                        name, rover.Name);
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
