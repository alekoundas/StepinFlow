using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameExecutionScreenshotLimitSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The key is stored as text and is the primary key, so renaming the enum member strands
            // any row already written under the old one.
            migrationBuilder.Sql("UPDATE AppSettings SET Key = 'EXECUTION_SCREENSHOT_LIMIT' WHERE Key = 'EXECUTION_SEARCH_SCREENSHOT_LIMIT';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AppSettings SET Key = 'EXECUTION_SEARCH_SCREENSHOT_LIMIT' WHERE Key = 'EXECUTION_SCREENSHOT_LIMIT';");
        }
    }
}
