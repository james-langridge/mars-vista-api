using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarsVista.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRoverLaunchDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add launch dates for all rovers
            // These were missing from the original seed data
            migrationBuilder.Sql(@"
                UPDATE rovers SET launch_date = '2011-11-26'::timestamp with time zone, updated_at = NOW()
                WHERE LOWER(name) = 'curiosity';

                UPDATE rovers SET launch_date = '2020-07-30'::timestamp with time zone, updated_at = NOW()
                WHERE LOWER(name) = 'perseverance';

                UPDATE rovers SET launch_date = '2003-07-07'::timestamp with time zone, updated_at = NOW()
                WHERE LOWER(name) = 'opportunity';

                UPDATE rovers SET launch_date = '2003-06-10'::timestamp with time zone, updated_at = NOW()
                WHERE LOWER(name) = 'spirit';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert launch dates to NULL
            migrationBuilder.Sql(@"
                UPDATE rovers SET launch_date = NULL, updated_at = NOW();
            ");
        }
    }
}
