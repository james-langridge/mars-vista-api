using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usage_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    response_time_ms = table.Column<int>(type: "integer", nullable: false),
                    photos_returned = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usage_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_usage_events_created_at",
                table: "usage_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_usage_events_status_code",
                table: "usage_events",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "ix_usage_events_user_email",
                table: "usage_events",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "ix_usage_events_user_email_created_at",
                table: "usage_events",
                columns: new[] { "user_email", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usage_events");
        }
    }
}
