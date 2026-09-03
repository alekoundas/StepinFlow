using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScreenshotRingSizeSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The screenshot ring is gone, so its setting is gone with it. The row has to go too:
            // keys are stored as the enum's name, and a saved row whose name no longer exists fails
            // to materialise - which takes the whole settings query down, not just that one row.
            migrationBuilder.Sql("DELETE FROM \"AppSettings\" WHERE \"Key\" = 'EXECUTION_SCREENSHOT_RING_SIZE';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to put back. The value was a setting for a feature that no longer exists, and
            // anyone rolling back gets the catalog default.
        }
    }
}
