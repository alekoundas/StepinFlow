using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropStageHotkey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The recording's stage key is gone, Stage on the bar stays. A key the enum no longer has
            // would not load as a setting.
            migrationBuilder.Sql(@"DELETE FROM AppSettings WHERE ""Key"" = 'RECORDING_HOTKEY_STAGE';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Which key it was moved to is not kept, so it is back at its default, F8.
        }
    }
}
