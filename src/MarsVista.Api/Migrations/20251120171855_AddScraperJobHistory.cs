using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScraperJobHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scraper_job_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    job_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    total_rovers_attempted = table.Column<int>(type: "integer", nullable: false),
                    total_rovers_succeeded = table.Column<int>(type: "integer", nullable: false),
                    total_photos_added = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scraper_job_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scraper_job_rover_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    job_history_id = table.Column<int>(type: "integer", nullable: false),
                    rover_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    start_sol = table.Column<int>(type: "integer", nullable: false),
                    end_sol = table.Column<int>(type: "integer", nullable: false),
                    sols_attempted = table.Column<int>(type: "integer", nullable: false),
                    sols_succeeded = table.Column<int>(type: "integer", nullable: false),
                    sols_failed = table.Column<int>(type: "integer", nullable: false),
                    photos_added = table.Column<int>(type: "integer", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    failed_sols = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scraper_job_rover_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_scraper_job_rover_details_scraper_job_histories_job_history",
                        column: x => x.job_history_id,
                        principalTable: "scraper_job_histories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_scraper_job_history_started",
                table: "scraper_job_histories",
                column: "job_started_at");

            migrationBuilder.CreateIndex(
                name: "idx_scraper_job_rover_details_job",
                table: "scraper_job_rover_details",
                column: "job_history_id");

            migrationBuilder.CreateIndex(
                name: "idx_scraper_job_rover_details_rover",
                table: "scraper_job_rover_details",
                column: "rover_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scraper_job_rover_details");

            migrationBuilder.DropTable(
                name: "scraper_job_histories");
        }
    }
}
