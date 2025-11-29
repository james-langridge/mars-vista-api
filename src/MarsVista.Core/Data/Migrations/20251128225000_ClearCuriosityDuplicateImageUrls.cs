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
            // Clear img_src_small for Curiosity photos
            // NASA only provides one URL per photo for Curiosity (unlike Perseverance).
            // Previously we stored the same URL in both small and full, which was confusing.
            // Now we only store in full, consistent with Spirit/Opportunity.
            migrationBuilder.Sql(@"
                UPDATE photos
                SET img_src_small = '', updated_at = NOW()
                WHERE rover_id = 2 AND img_src_small != '';
            ");
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
