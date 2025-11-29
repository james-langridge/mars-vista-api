using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ClearCuriosityDuplicateImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DISABLED: This migration times out on Railway (~730K rows).
            // Run manually in batches after deployment:
            //
            // UPDATE photos
            // SET img_src_small = '', img_src_medium = '', img_src_large = '', updated_at = NOW()
            // WHERE rover_id = 2 AND img_src_small != ''
            // LIMIT 50000;
            //
            // Repeat until 0 rows affected.

            // No-op - migration marked as applied without running
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore img_src_small from img_src_full for Curiosity
            // (This is the old behavior - copying the URL to small)
            migrationBuilder.Sql(@"
                UPDATE photos
                SET img_src_small = img_src_full, updated_at = NOW()
                WHERE rover_id = 2 AND img_src_full != '';
            ");
        }
    }
}
