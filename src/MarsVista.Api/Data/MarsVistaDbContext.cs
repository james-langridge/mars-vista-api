using Microsoft.EntityFrameworkCore;

namespace MarsVista.Api.Data;

public class MarsVistaDbContext : DbContext
{
    public MarsVistaDbContext(DbContextOptions<MarsVistaDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added in future stories
    // Example: public DbSet<Photo> Photos { get; set; }
    // Example: public DbSet<Rover> Rovers { get; set; }
    // Example: public DbSet<Camera> Cameras { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be added here
        // This is where we'll configure:
        // - Table names (snake_case convention)
        // - JSONB columns
        // - Indexes
        // - Relationships
        // - Constraints
    }
}
