using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Partial index for active rovers (Curiosity=2, Perseverance=4)
            // Speeds up: GET /api/v2/photos?rovers=curiosity
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_active_rovers_sol
                ON photos(rover_id, sol, camera_id)
                WHERE rover_id IN (2, 4);
            ");

            // Partial index for high-quality photos
            // Speeds up: GET /api/v2/photos?min_width=1920&sample_type=Full
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_high_quality
                ON photos(rover_id, sol, camera_id, width, height)
                WHERE sample_type = 'Full' AND width >= 1600;
            ");

            // Partial index for photos with camera telemetry (used in panorama detection)
            // Speeds up: GET /api/v2/panoramas
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_panorama_telemetry
                ON photos(site, drive, mast_az, mast_el, rover_id, sol)
                WHERE mast_az IS NOT NULL
                  AND mast_el IS NOT NULL
                  AND site IS NOT NULL
                  AND drive IS NOT NULL;
            ");

            // Partial index for photos with location data
            // Speeds up: GET /api/v2/locations
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_with_location
                ON photos(site, drive, rover_id, sol)
                WHERE site IS NOT NULL AND drive IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_active_rovers_sol;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_high_quality;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_panorama_telemetry;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_with_location;");
        }
    }
}
