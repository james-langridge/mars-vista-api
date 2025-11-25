using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarsTimeHourColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mars_time_hour",
                table: "photos",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_photos_mars_time_hour",
                table: "photos",
                column: "mars_time_hour");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_photos_mars_time_hour",
                table: "photos");

            migrationBuilder.DropColumn(
                name: "mars_time_hour",
                table: "photos");
        }
    }
}
