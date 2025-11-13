using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarsVista.Api.Data;

public class MarsVistaDbContextFactory : IDesignTimeDbContextFactory<MarsVistaDbContext>
{
    public MarsVistaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MarsVistaDbContext>();

        // Use the connection string from appsettings.json
        // In a real scenario, you might read this from environment variables
        var connectionString = "Host=localhost;Port=5432;Database=marsvista_dev;Username=marsvista;Password=marsvista_dev_password";

        optionsBuilder.UseNpgsql(connectionString);

        return new MarsVistaDbContext(optionsBuilder.Options);
    }
}
