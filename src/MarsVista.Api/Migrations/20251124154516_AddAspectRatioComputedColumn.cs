using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAspectRatioComputedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add computed column for aspect ratio
            migrationBuilder.Sql(@"
                ALTER TABLE photos
                ADD COLUMN aspect_ratio DECIMAL(6,3)
                GENERATED ALWAYS AS (
                    CASE
                        WHEN height IS NOT NULL AND height > 0
                        THEN ROUND((width::decimal / height), 3)
                        ELSE NULL
                    END
                ) STORED;
            ");

            // Create index on computed column
            migrationBuilder.Sql(@"
                CREATE INDEX idx_photos_aspect_ratio
                ON photos(aspect_ratio)
                WHERE aspect_ratio IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_photos_aspect_ratio;");
            migrationBuilder.Sql("ALTER TABLE photos DROP COLUMN IF EXISTS aspect_ratio;");
        }
    }
}
