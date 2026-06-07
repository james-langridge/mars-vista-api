using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <summary>
    /// Drop three indexes on the photos table that production usage analysis (story 052a)
    /// found to have zero scans since stats reset. Collectively they consume ~107 MB of
    /// disk and buffer-cache space without serving any query.
    ///
    /// Indexes dropped:
    ///   - idx_photos_aspect_ratio (78 MB)  partial: WHERE aspect_ratio IS NOT NULL
    ///   - ix_photos_height        (27 MB)  partial: WHERE height IS NOT NULL
    ///   - idx_photos_high_quality (2.4 MB) partial: WHERE sample_type='Full' AND width>=1600
    ///
    /// Uses DROP INDEX CONCURRENTLY so live writes are not blocked. Each statement runs
    /// outside the surrounding migration transaction (CONCURRENTLY cannot run inside one).
    ///
    /// Note: idx_photos_aspect_ratio (idx_ prefix) is the manually-created partial index
    /// from migration FixAspectRatioPrecision. EF Core's snapshot previously tracked a
    /// hypothetical default-named ix_photos_aspect_ratio because of HasIndex(e => e.AspectRatio)
    /// in DbContext. Both that HasIndex line and this drop are removed together.
    /// </summary>
    public partial class DropUnusedPhotoIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS idx_photos_aspect_ratio;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS ix_photos_height;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS idx_photos_high_quality;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore using the exact definitions captured from production pg_indexes.
            // CONCURRENTLY so restore is non-blocking on a live database.
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_photos_aspect_ratio
                ON public.photos USING btree (aspect_ratio)
                WHERE aspect_ratio IS NOT NULL;",
                suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_photos_height
                ON public.photos USING btree (height)
                WHERE height IS NOT NULL;",
                suppressTransaction: true);

            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_photos_high_quality
                ON public.photos USING btree (rover_id, sol, camera_id, width, height)
                WHERE sample_type = 'Full' AND width >= 1600;",
                suppressTransaction: true);
        }
    }
}
