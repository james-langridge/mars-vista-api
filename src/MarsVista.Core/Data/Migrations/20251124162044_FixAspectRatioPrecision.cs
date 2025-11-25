using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAspectRatioPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the aspect_ratio column if it exists (handles partial migration failure)
            // This cleans up any previous attempts that may have partially succeeded
            migrationBuilder.Sql("ALTER TABLE photos DROP COLUMN IF EXISTS aspect_ratio;");

            // Recreate with correct precision: DECIMAL(10,3) instead of DECIMAL(6,3)
            // This allows aspect ratios up to 9,999,999.999 (handles extreme panoramas)
            // Previous DECIMAL(6,3) only allowed up to 999.999, causing overflow on wide images
            migrationBuilder.Sql(@"
                ALTER TABLE photos
                ADD COLUMN aspect_ratio DECIMAL(10,3)
                GENERATED ALWAYS AS (
                    CASE
                        WHEN height IS NOT NULL AND height > 0
                        THEN ROUND((width::decimal / height), 3)
                        ELSE NULL
                    END
                ) STORED;
            ");

            // Recreate the index
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_aspect_ratio
                ON photos(aspect_ratio)
                WHERE aspect_ratio IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clean up: drop index and column
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_aspect_ratio;");
            migrationBuilder.Sql("ALTER TABLE photos DROP COLUMN IF EXISTS aspect_ratio;");
        }
    }
}
