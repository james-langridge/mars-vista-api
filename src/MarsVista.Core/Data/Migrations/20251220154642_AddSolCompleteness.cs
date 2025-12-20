using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSolCompleteness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sol_completeness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    rover_id = table.Column<int>(type: "integer", nullable: false),
                    sol = table.Column<int>(type: "integer", nullable: false),
                    photo_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    nasa_expected_count = table.Column<int>(type: "integer", nullable: true),
                    scrape_status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "pending"),
                    last_scrape_attempt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_success_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sol_completeness", x => x.id);
                    table.ForeignKey(
                        name: "fk_sol_completeness_rovers_rover_id",
                        column: x => x.rover_id,
                        principalTable: "rovers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sol_completeness_rover_id_sol",
                table: "sol_completeness",
                columns: new[] { "rover_id", "sol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sol_completeness_rover_id_scrape_status",
                table: "sol_completeness",
                columns: new[] { "rover_id", "scrape_status" });

            // Backfill from existing photos - mark all sols with photos as 'success'
            migrationBuilder.Sql(@"
                INSERT INTO sol_completeness (id, rover_id, sol, photo_count, scrape_status, last_success_at, attempt_count, consecutive_failures, created_at, updated_at)
                SELECT
                    gen_random_uuid(),
                    rover_id,
                    sol,
                    COUNT(*) as photo_count,
                    'success' as scrape_status,
                    NOW() as last_success_at,
                    1 as attempt_count,
                    0 as consecutive_failures,
                    NOW() as created_at,
                    NOW() as updated_at
                FROM photos
                GROUP BY rover_id, sol
                ON CONFLICT (rover_id, sol) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sol_completeness");
        }
    }
}
