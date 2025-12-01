using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoverWaypoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rover_waypoints",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rover_id = table.Column<int>(type: "integer", nullable: false),
                    frame = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    site = table.Column<int>(type: "integer", nullable: false),
                    drive = table.Column<int>(type: "integer", nullable: true),
                    sol = table.Column<int>(type: "integer", nullable: true),
                    landing_x = table.Column<float>(type: "real", nullable: false),
                    landing_y = table.Column<float>(type: "real", nullable: false),
                    landing_z = table.Column<float>(type: "real", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    elevation = table.Column<float>(type: "real", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rover_waypoints", x => x.id);
                    table.ForeignKey(
                        name: "fk_rover_waypoints_rovers_rover_id",
                        column: x => x.rover_id,
                        principalTable: "rovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_rover_waypoints_rover_site",
                table: "rover_waypoints",
                columns: new[] { "rover_id", "site" });

            migrationBuilder.CreateIndex(
                name: "ix_rover_waypoints_rover_id_site_drive",
                table: "rover_waypoints",
                columns: new[] { "rover_id", "site", "drive" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rover_waypoints");
        }
    }
}
