using Microsoft.EntityFrameworkCore;
using MarsVista.Api.Entities;

namespace MarsVista.Api.Data;

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
    }
}
