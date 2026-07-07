using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPanoramasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "panoramas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    panorama_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rover_id = table.Column<int>(type: "integer", nullable: false),
                    sol = table.Column<int>(type: "integer", nullable: false),
                    sequence_index = table.Column<int>(type: "integer", nullable: false),
                    camera_id = table.Column<int>(type: "integer", nullable: false),
                    mars_time_start = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    mars_time_end = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    total_photos = table.Column<int>(type: "integer", nullable: false),
                    coverage_degrees = table.Column<float>(type: "real", nullable: false),
                    avg_elevation = table.Column<float>(type: "real", nullable: false),
                    unique_positions = table.Column<int>(type: "integer", nullable: false),
                    avg_position_spacing = table.Column<float>(type: "real", nullable: true),
                    quality_tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_multi_row = table.Column<bool>(type: "boolean", nullable: false),
                    elevation_tier_count = table.Column<int>(type: "integer", nullable: false),
                    azimuth_column_count = table.Column<int>(type: "integer", nullable: false),
                    min_elevation = table.Column<float>(type: "real", nullable: true),
                    max_elevation = table.Column<float>(type: "real", nullable: true),
                    site = table.Column<int>(type: "integer", nullable: true),
                    drive = table.Column<int>(type: "integer", nullable: true),
                    coordinate_x = table.Column<float>(type: "real", nullable: true),
                    coordinate_y = table.Column<float>(type: "real", nullable: true),
                    coordinate_z = table.Column<float>(type: "real", nullable: true),
                    photo_ids = table.Column<int[]>(type: "integer[]", nullable: false),
                    detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_panoramas", x => x.id);
                    table.ForeignKey(
                        name: "fk_panoramas_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_panoramas_rovers_rover_id",
                        column: x => x.rover_id,
                        principalTable: "rovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_camera_id",
                table: "panoramas",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_coverage_degrees",
                table: "panoramas",
                column: "coverage_degrees");

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_panorama_id",
                table: "panoramas",
                column: "panorama_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_rover_id_sol",
                table: "panoramas",
                columns: new[] { "rover_id", "sol" });

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_rover_id_sol_sequence_index",
                table: "panoramas",
                columns: new[] { "rover_id", "sol", "sequence_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_panoramas_total_photos",
                table: "panoramas",
                column: "total_photos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "panoramas");
        }
    }
}
