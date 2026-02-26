using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "photo_ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    photo_id = table.Column<int>(type: "integer", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_photo_ratings", x => x.id);
                    table.ForeignKey(
                        name: "fk_photo_ratings_photos_photo_id",
                        column: x => x.photo_id,
                        principalTable: "photos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_photo_ratings_photo_id",
                table: "photo_ratings",
                column: "photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_photo_ratings_photo_id_client_id",
                table: "photo_ratings",
                columns: new[] { "photo_id", "client_id" },
                unique: true);

            migrationBuilder.Sql(
                "ALTER TABLE photo_ratings ADD CONSTRAINT chk_photo_ratings_rating CHECK (rating BETWEEN 1 AND 5)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE photo_ratings DROP CONSTRAINT IF EXISTS chk_photo_ratings_rating");

            migrationBuilder.DropTable(
                name: "photo_ratings");
        }
    }
}
