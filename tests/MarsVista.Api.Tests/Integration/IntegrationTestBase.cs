using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MarsVista.Api.Data;
using MarsVista.Api.Entities;
using Npgsql;

namespace MarsVista.Api.Tests.Integration;

/// <summary>
/// Base class for integration tests that use a real PostgreSQL database.
/// Creates a unique test database for each test class, applies migrations,
/// and seeds required data.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected MarsVistaDbContext DbContext { get; private set; } = null!;
    protected ServiceProvider ServiceProvider { get; private set; } = null!;

    private string _testDatabaseName = null!;
    private string _connectionString = null!;
    private string _masterConnectionString = null!;

    public async Task InitializeAsync()
    {
        // 1. Create unique test database name
        _testDatabaseName = $"marsvista_test_{Guid.NewGuid():N}";

        // 2. Get base connection string from configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var baseConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found in configuration");

        // 3. Create master and test connection strings
        // The master connection string connects to an existing database (marsvista_dev)
        // so we can execute CREATE DATABASE commands
        _masterConnectionString = baseConnectionString;

        // The test connection string connects to our unique test database
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        builder.Database = _testDatabaseName;
        _connectionString = builder.ToString();

        // 4. Create test database
        await CreateDatabaseAsync();

        // 5. Set up services and apply migrations
        ServiceProvider = CreateServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<MarsVistaDbContext>();

        // 6. Apply EF Core migrations
        await DbContext.Database.MigrateAsync();

        // 7. Seed required test data (rovers and cameras)
        await SeedRequiredDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up: Dispose context and drop test database
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }

        if (ServiceProvider != null)
        {
            await ServiceProvider.DisposeAsync();
        }

        await DropDatabaseAsync();
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE {_testDatabaseName}";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        try
        {
            // Terminate all connections to the test database first
            await using var connection = new NpgsqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = '{_testDatabaseName}'
                  AND pid <> pg_backend_pid();

                DROP DATABASE IF EXISTS {_testDatabaseName};
            ";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            // Log but don't throw - cleanup is best effort
            Console.WriteLine($"Failed to drop test database {_testDatabaseName}: {ex.Message}");
        }
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddDbContext<MarsVistaDbContext>(options =>
        {
            options.UseNpgsql(_connectionString)
                .UseSnakeCaseNamingConvention()
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        ConfigureServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method to register additional services needed for your tests
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Default implementation - override in derived classes to add services
    }

    /// <summary>
    /// Seeds rovers and cameras required for most integration tests.
    /// Override SeedAdditionalDataAsync to add test-specific data.
    /// </summary>
    private async Task SeedRequiredDataAsync()
    {
        var now = DateTime.UtcNow;

        // Add rovers
        var curiosity = new Rover
        {
            Id = 1,
            Name = "Curiosity",
            LandingDate = new DateTime(2012, 8, 6, 0, 0, 0, DateTimeKind.Utc),
            LaunchDate = new DateTime(2011, 11, 26, 0, 0, 0, DateTimeKind.Utc),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };

        var perseverance = new Rover
        {
            Id = 2,
            Name = "Perseverance",
            LandingDate = new DateTime(2021, 2, 18, 0, 0, 0, DateTimeKind.Utc),
            LaunchDate = new DateTime(2020, 7, 30, 0, 0, 0, DateTimeKind.Utc),
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };

        DbContext.Rovers.AddRange(curiosity, perseverance);

        // Add cameras
        var fhaz = new Camera
        {
            Id = 1,
            Name = "FHAZ",
            FullName = "Front Hazard Avoidance Camera",
            RoverId = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var mast = new Camera
        {
            Id = 2,
            Name = "MAST",
            FullName = "Mast Camera",
            RoverId = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var navcam = new Camera
        {
            Id = 3,
            Name = "NAVCAM",
            FullName = "Navigation Camera",
            RoverId = 2,
            CreatedAt = now,
            UpdatedAt = now
        };

        DbContext.Cameras.AddRange(fhaz, mast, navcam);

        await DbContext.SaveChangesAsync();

        // Allow derived classes to seed additional data
        await SeedAdditionalDataAsync();
    }

    /// <summary>
    /// Override this method to seed test-specific data
    /// </summary>
    protected virtual Task SeedAdditionalDataAsync()
    {
        return Task.CompletedTask;
    }
}
