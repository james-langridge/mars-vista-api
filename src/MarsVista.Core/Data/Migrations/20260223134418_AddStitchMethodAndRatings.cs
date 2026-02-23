using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStitchMethodAndRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stitch_method",
                table: "stitched_panoramas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "panorama_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    panorama_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_panorama_ratings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_panorama_ratings_panorama_id",
                table: "panorama_ratings",
                column: "panorama_id");

            migrationBuilder.CreateIndex(
                name: "ix_panorama_ratings_panorama_id_client_id",
                table: "panorama_ratings",
                columns: new[] { "panorama_id", "client_id" },
                unique: true);

            migrationBuilder.Sql(
                "ALTER TABLE panorama_ratings ADD CONSTRAINT chk_panorama_ratings_rating CHECK (rating BETWEEN 1 AND 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE panorama_ratings DROP CONSTRAINT IF EXISTS chk_panorama_ratings_rating");

            migrationBuilder.DropTable(
                name: "panorama_ratings");

            migrationBuilder.DropColumn(
                name: "stitch_method",
                table: "stitched_panoramas");
        }
    }
}
