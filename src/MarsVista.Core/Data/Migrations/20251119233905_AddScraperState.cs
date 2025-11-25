using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scraper_states",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rover_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_scraped_sol = table.Column<int>(type: "integer", nullable: false),
                    last_scrape_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_scrape_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    photos_added_last_run = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scraper_states", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scraper_states_rover_name",
                table: "scraper_states",
                column: "rover_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scraper_states");
        }
    }
}
