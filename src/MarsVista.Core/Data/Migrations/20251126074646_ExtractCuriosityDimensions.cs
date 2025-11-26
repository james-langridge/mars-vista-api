using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtractCuriosityDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =================================================================
            // PHASE 1: Extract EXACT dimensions from Curiosity subframe_rect
            // =================================================================
            // Format is "(x, y, width, height)" - extract width (3rd) and height (4th)
            // This covers ~210K photos with exact dimension data
            // Note: regexp_matches returns a set, so we need to use a subquery
            migrationBuilder.Sql(@"
                UPDATE photos p
                SET
                    width = extracted.w,
                    height = extracted.h,
                    updated_at = NOW()
                FROM (
                    SELECT id,
                           (regexp_matches(raw_data->>'subframe_rect', '\((\d+),(\d+),(\d+),(\d+)\)'))[3]::int as w,
                           (regexp_matches(raw_data->>'subframe_rect', '\((\d+),(\d+),(\d+),(\d+)\)'))[4]::int as h
                    FROM photos
                    WHERE rover_id = 2
                      AND width IS NULL
                      AND raw_data->>'subframe_rect' IS NOT NULL
                      AND raw_data->>'subframe_rect' != ''
                ) extracted
                WHERE p.id = extracted.id;
            ");

            // =================================================================
            // PHASE 2: Estimate dimensions for remaining Curiosity photos
            // =================================================================
            // For photos without subframe_rect, estimate based on sample_type
            // These are conservative estimates (underestimate rather than overestimate)
            migrationBuilder.Sql(@"
                UPDATE photos
                SET
                    width = CASE
                        WHEN sample_type = 'thumbnail' THEN 160
                        WHEN sample_type = 'subframe' THEN 1024
                        WHEN sample_type = 'full' THEN 1024
                        WHEN sample_type = 'downsampled' THEN 800
                        WHEN sample_type = 'chemcam prc' THEN 1024
                        WHEN sample_type = 'mixed' THEN 1024
                        ELSE 512
                    END,
                    height = CASE
                        WHEN sample_type = 'thumbnail' THEN 144
                        WHEN sample_type = 'subframe' THEN 1024
                        WHEN sample_type = 'full' THEN 1024
                        WHEN sample_type = 'downsampled' THEN 600
                        WHEN sample_type = 'chemcam prc' THEN 1024
                        WHEN sample_type = 'mixed' THEN 1024
                        ELSE 512
                    END,
                    updated_at = NOW()
                WHERE rover_id = 2
                  AND width IS NULL;
            ");

            // =================================================================
            // PHASE 3: Delete thumbnails from all rovers
            // =================================================================

            // Delete Curiosity thumbnails (by sample_type)
            // ~244K rows - these are 160x144 or smaller, not useful
            migrationBuilder.Sql(@"
                DELETE FROM photos
                WHERE rover_id = 2
                  AND sample_type = 'thumbnail';
            ");

            // Delete Opportunity tiny thumbnails (64x64 and smaller)
            // ~353K rows
            migrationBuilder.Sql(@"
                DELETE FROM photos
                WHERE rover_id = 3
                  AND width <= 64;
            ");

            // Delete Spirit tiny thumbnails (64x64 and smaller)
            // ~186K rows
            migrationBuilder.Sql(@"
                DELETE FROM photos
                WHERE rover_id = 4
                  AND width <= 64;
            ");

            // =================================================================
            // PHASE 4: Update partial indexes for high-quality photos
            // =================================================================
            // The idx_photos_high_quality index may now be more useful
            // with Curiosity photos having dimensions. Rebuild to update stats.
            migrationBuilder.Sql(@"
                REINDEX INDEX idx_photos_high_quality;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: This migration deletes data and cannot be fully reversed.
            // The Down migration only clears the dimension data that was set.
            // Deleted thumbnails cannot be restored without re-scraping.

            // Clear Curiosity dimensions (set back to NULL)
            migrationBuilder.Sql(@"
                UPDATE photos
                SET
                    width = NULL,
                    height = NULL,
                    updated_at = NOW()
                WHERE rover_id = 2;
            ");

            // Note: Deleted thumbnails (~783K rows) cannot be restored.
            // To restore, you would need to:
            // 1. Restore from a database backup, OR
            // 2. Re-run the scrapers for affected sols
        }
    }
}
