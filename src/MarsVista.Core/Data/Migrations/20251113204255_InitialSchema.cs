using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rovers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    landing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    launch_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_sol = table.Column<int>(type: "integer", nullable: true),
                    max_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_photos = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rovers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cameras",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    rover_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cameras", x => x.id);
                    table.ForeignKey(
                        name: "fk_cameras_rovers_rover_id",
                        column: x => x.rover_id,
                        principalTable: "rovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "photos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nasa_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sol = table.Column<int>(type: "integer", nullable: false),
                    earth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    date_taken_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_taken_mars = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    img_src_small = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    img_src_medium = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    img_src_large = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    img_src_full = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    sample_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    site = table.Column<int>(type: "integer", nullable: true),
                    drive = table.Column<int>(type: "integer", nullable: true),
                    xyz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    mast_az = table.Column<float>(type: "real", nullable: true),
                    mast_el = table.Column<float>(type: "real", nullable: true),
                    camera_vector = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    camera_position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    camera_model_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    attitude = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    spacecraft_clock = table.Column<float>(type: "real", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true),
                    caption = table.Column<string>(type: "text", nullable: true),
                    credit = table.Column<string>(type: "text", nullable: true),
                    date_received = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    filter_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rover_id = table.Column<int>(type: "integer", nullable: false),
                    camera_id = table.Column<int>(type: "integer", nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_photos_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_photos_rovers_rover_id",
                        column: x => x.rover_id,
                        principalTable: "rovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cameras_rover_id_name",
                table: "cameras",
                columns: new[] { "rover_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_photos_camera_id",
                table: "photos",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "ix_photos_date_taken_utc",
                table: "photos",
                column: "date_taken_utc");

            migrationBuilder.CreateIndex(
                name: "ix_photos_earth_date",
                table: "photos",
                column: "earth_date");

            migrationBuilder.CreateIndex(
                name: "ix_photos_mast_az_mast_el",
                table: "photos",
                columns: new[] { "mast_az", "mast_el" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_nasa_id",
                table: "photos",
                column: "nasa_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_photos_rover_id_camera_id_sol",
                table: "photos",
                columns: new[] { "rover_id", "camera_id", "sol" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_rover_id_sol",
                table: "photos",
                columns: new[] { "rover_id", "sol" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_site_drive",
                table: "photos",
                columns: new[] { "site", "drive" });

            migrationBuilder.CreateIndex(
                name: "ix_photos_sol",
                table: "photos",
                column: "sol");

            migrationBuilder.CreateIndex(
                name: "ix_rovers_name",
                table: "rovers",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "photos");

            migrationBuilder.DropTable(
                name: "cameras");

            migrationBuilder.DropTable(
                name: "rovers");
        }
    }
}
