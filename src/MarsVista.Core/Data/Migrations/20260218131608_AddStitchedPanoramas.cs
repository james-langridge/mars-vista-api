using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStitchedPanoramas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stitched_panoramas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    panorama_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "processing"),
                    image_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_width = table.Column<int>(type: "integer", nullable: true),
                    image_height = table.Column<int>(type: "integer", nullable: true),
                    image_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    source_photo_count = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stitched_panoramas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stitched_panoramas_panorama_id",
                table: "stitched_panoramas",
                column: "panorama_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stitched_panoramas_status",
                table: "stitched_panoramas",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stitched_panoramas");
        }
    }
}
