using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoverIdEarthDateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_photos_rover_id_earth_date",
                table: "photos",
                columns: new[] { "rover_id", "earth_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_photos_rover_id_earth_date",
                table: "photos");
        }
    }
}
