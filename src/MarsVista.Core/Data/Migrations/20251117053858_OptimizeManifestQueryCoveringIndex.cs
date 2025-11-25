using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeManifestQueryCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old index that only had (rover_id, sol)
            migrationBuilder.DropIndex(
                name: "ix_photos_rover_id_sol",
                table: "photos");

            // Create a covering index with all columns needed by the manifest query
            // This allows PostgreSQL to do an index-only scan without touching the table
            migrationBuilder.CreateIndex(
                name: "ix_photos_rover_id_sol_covering",
                table: "photos",
                columns: new[] { "rover_id", "sol", "earth_date", "camera_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the covering index
            migrationBuilder.DropIndex(
                name: "ix_photos_rover_id_sol_covering",
                table: "photos");

            // Restore the old index
            migrationBuilder.CreateIndex(
                name: "ix_photos_rover_id_sol",
                table: "photos",
                columns: new[] { "rover_id", "sol" });
        }
    }
}
