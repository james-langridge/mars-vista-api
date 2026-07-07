using Microsoft.EntityFrameworkCore;
using MarsVista.Core.Entities;

namespace MarsVista.Core.Data;

public class MarsVistaDbContext : DbContext
{
    public MarsVistaDbContext(DbContextOptions<MarsVistaDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Rover> Rovers { get; set; }
    public DbSet<Camera> Cameras { get; set; }
    public DbSet<Photo> Photos { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<RateLimit> RateLimits { get; set; }
    public DbSet<UsageEvent> UsageEvents { get; set; }
    public DbSet<UsageEventMonthly> UsageEventsMonthly { get; set; }
    public DbSet<ScraperState> ScraperStates { get; set; }
    public DbSet<ScraperJobHistory> ScraperJobHistories { get; set; }
    public DbSet<ScraperJobRoverDetails> ScraperJobRoverDetails { get; set; }
    public DbSet<RoverWaypoint> RoverWaypoints { get; set; }
    public DbSet<SolCompleteness> SolCompleteness { get; set; }
    public DbSet<StitchedPanorama> StitchedPanoramas { get; set; }
    public DbSet<PanoramaRating> PanoramaRatings { get; set; }
    public DbSet<PhotoRating> PhotoRatings { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically update UpdatedAt timestamp for modified entities
        var entries = ChangeTracker.Entries<ITimestamped>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Automatically update UpdatedAt timestamp for modified entities
        var entries = ChangeTracker.Entries<ITimestamped>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChanges();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Rover configuration
        modelBuilder.Entity<Rover>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20);

            // Timestamps with default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Camera configuration
        modelBuilder.Entity<Camera>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.RoverId, e.Name }).IsUnique();

            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();

            // Foreign key to Rover
            entity.HasOne(e => e.Rover)
                .WithMany(r => r.Cameras)
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // Photo configuration
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NasaId).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => e.Sol);
            entity.HasIndex(e => e.EarthDate);
            entity.HasIndex(e => e.DateTakenUtc);
            entity.HasIndex(e => new { e.RoverId, e.Sol });
            entity.HasIndex(e => new { e.RoverId, e.CameraId, e.Sol });

            // Indexes for advanced features
            entity.HasIndex(e => new { e.Site, e.Drive });
            entity.HasIndex(e => new { e.MastAz, e.MastEl });

            // String length constraints
            entity.Property(e => e.NasaId).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DateTakenMars).HasMaxLength(100);
            entity.Property(e => e.ImgSrcSmall).HasMaxLength(500);
            entity.Property(e => e.ImgSrcMedium).HasMaxLength(500);
            entity.Property(e => e.ImgSrcLarge).HasMaxLength(500);
            entity.Property(e => e.ImgSrcFull).HasMaxLength(500);
            entity.Property(e => e.SampleType).HasMaxLength(50);
            entity.Property(e => e.Xyz).HasMaxLength(200);
            entity.Property(e => e.CameraVector).HasMaxLength(200);
            entity.Property(e => e.CameraPosition).HasMaxLength(200);
            entity.Property(e => e.CameraModelType).HasMaxLength(100);
            entity.Property(e => e.Attitude).HasMaxLength(200);
            entity.Property(e => e.FilterName).HasMaxLength(100);

            // JSONB column for raw NASA data
            entity.Property(e => e.RawData)
                .HasColumnType("jsonb");

            // Computed column for aspect ratio (stored).
            // DECIMAL(10,3) defined in migration SQL to handle extreme panoramas.
            // Index on this column was removed in 2026-06 (story 052a): partial index
            // `idx_photos_aspect_ratio WHERE aspect_ratio IS NOT NULL` had zero scans
            // and 78 MB of dead weight in the buffer cache.
            entity.Property(e => e.AspectRatio)
                .HasComputedColumnSql("CASE WHEN height IS NOT NULL AND height > 0 THEN ROUND((width::decimal / height), 3) ELSE NULL END", stored: true);

            // Foreign keys
            entity.HasOne(e => e.Rover)
                .WithMany(r => r.Photos)
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Camera)
                .WithMany(c => c.Photos)
                .HasForeignKey(e => e.CameraId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ApiKey configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);

            // One user can have one API key (enforced by unique constraint on email)
            entity.HasIndex(e => e.UserEmail).IsUnique();

            // Fast lookup by API key hash during authentication
            entity.HasIndex(e => e.ApiKeyHash).IsUnique();

            // String length constraints
            entity.Property(e => e.UserEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ApiKeyHash).HasMaxLength(64).IsRequired(); // SHA-256 = 64 hex chars
            entity.Property(e => e.Tier).HasMaxLength(20).HasDefaultValue("free");
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("user");

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RateLimit configuration
        modelBuilder.Entity<RateLimit>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: one record per user per window
            entity.HasIndex(e => new { e.UserEmail, e.WindowStart, e.WindowType }).IsUnique();

            // Fast lookup by user and window type
            entity.HasIndex(e => new { e.UserEmail, e.WindowType });

            // String length constraints
            entity.Property(e => e.UserEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.WindowType).HasMaxLength(10).IsRequired();
        });

        // UsageEvent configuration
        modelBuilder.Entity<UsageEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes for common admin queries
            entity.HasIndex(e => e.UserEmail);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.StatusCode);
            entity.HasIndex(e => new { e.UserEmail, e.CreatedAt });

            // String length constraints
            entity.Property(e => e.UserEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Tier).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(500).IsRequired();
            entity.Property(e => e.QueryString).HasMaxLength(2000);
            entity.Property(e => e.ErrorDetail).HasMaxLength(2000);

            // Timestamp with default value
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // UsageEventMonthly configuration (retention rollup of usage_events)
        modelBuilder.Entity<UsageEventMonthly>(entity =>
        {
            entity.ToTable("usage_events_monthly");

            entity.HasKey(e => e.Id);

            // The retention job's ON CONFLICT target: one row per
            // (month, user, endpoint, tier) that accumulates across runs.
            entity.HasIndex(e => new { e.Month, e.UserEmail, e.Endpoint, e.Tier })
                .IsUnique();

            // String length constraints (match usage_events)
            entity.Property(e => e.UserEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Tier).HasMaxLength(20).IsRequired();

            // Timestamps with default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ScraperState configuration
        modelBuilder.Entity<ScraperState>(entity =>
        {
            entity.HasKey(e => e.Id);

            // One state record per rover
            entity.HasIndex(e => e.RoverName).IsUnique();

            // String length constraints
            entity.Property(e => e.RoverName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.LastScrapeStatus).HasMaxLength(20).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            // Timestamps with default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ScraperJobHistory configuration
        modelBuilder.Entity<ScraperJobHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index for fast queries by start time (most recent first)
            entity.HasIndex(e => e.JobStartedAt).HasDatabaseName("idx_scraper_job_history_started");

            // String length constraints
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();

            // Timestamp with default value
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ScraperJobRoverDetails configuration
        modelBuilder.Entity<ScraperJobRoverDetails>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Index for fast queries by job
            entity.HasIndex(e => e.JobHistoryId).HasDatabaseName("idx_scraper_job_rover_details_job");

            // Index for fast queries by rover
            entity.HasIndex(e => e.RoverName).HasDatabaseName("idx_scraper_job_rover_details_rover");

            // String length constraints
            entity.Property(e => e.RoverName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired();

            // Foreign key with cascade delete
            entity.HasOne(e => e.JobHistory)
                .WithMany(h => h.RoverDetails)
                .HasForeignKey(e => e.JobHistoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamp with default value
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // RoverWaypoint configuration
        modelBuilder.Entity<RoverWaypoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: one waypoint per rover/site/drive combination
            entity.HasIndex(e => new { e.RoverId, e.Site, e.Drive }).IsUnique();

            // Index for traverse queries (ordered by site, drive)
            entity.HasIndex(e => new { e.RoverId, e.Site }).HasDatabaseName("idx_rover_waypoints_rover_site");

            // String length constraints
            entity.Property(e => e.Frame).HasMaxLength(10).IsRequired();

            // Foreign key to Rover
            entity.HasOne(e => e.Rover)
                .WithMany()
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // StitchedPanorama configuration
        modelBuilder.Entity<StitchedPanorama>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.PanoramaId).IsUnique();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.PanoramaId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired()
                .HasDefaultValue("processing");
            entity.Property(e => e.StitchMethod).HasMaxLength(30);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // PanoramaRating configuration
        modelBuilder.Entity<PanoramaRating>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.PanoramaId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.ClientId).HasMaxLength(100).IsRequired();

            // One rating per panorama per client
            entity.HasIndex(e => new { e.PanoramaId, e.ClientId }).IsUnique();

            // Fast lookup by panorama for aggregate queries
            entity.HasIndex(e => e.PanoramaId);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // PhotoRating configuration
        modelBuilder.Entity<PhotoRating>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Rating).IsRequired();
            entity.Property(e => e.ClientId).HasMaxLength(100).IsRequired();

            // One rating per photo per client
            entity.HasIndex(e => new { e.PhotoId, e.ClientId }).IsUnique();

            // Fast lookup by photo for aggregate queries
            entity.HasIndex(e => e.PhotoId);

            // Foreign key to Photo
            entity.HasOne(e => e.Photo)
                .WithMany(p => p.Ratings)
                .HasForeignKey(e => e.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // SolCompleteness configuration
        modelBuilder.Entity<SolCompleteness>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: one record per rover/sol combination
            entity.HasIndex(e => new { e.RoverId, e.Sol }).IsUnique();

            // Index for querying by status (find failed/pending sols)
            entity.HasIndex(e => new { e.RoverId, e.ScrapeStatus });

            // String length constraints
            entity.Property(e => e.ScrapeStatus).HasMaxLength(20).IsRequired();

            // Foreign key to Rover
            entity.HasOne(e => e.Rover)
                .WithMany()
                .HasForeignKey(e => e.RoverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Timestamps
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
