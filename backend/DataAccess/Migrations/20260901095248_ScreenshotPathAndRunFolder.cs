using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScreenshotPathAndRunFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResultImagePath",
                table: "ExecutionSteps");

            migrationBuilder.RenameColumn(
                name: "SearchImagePath",
                table: "ExecutionSteps",
                newName: "ScreenshotPath");

            migrationBuilder.AddColumn<string>(
                name: "ScreenshotFolderName",
                table: "Executions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScreenshotFolderName",
                table: "Executions");

            migrationBuilder.RenameColumn(
                name: "ScreenshotPath",
                table: "ExecutionSteps",
                newName: "SearchImagePath");

            migrationBuilder.AddColumn<string>(
                name: "ResultImagePath",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);
        }
    }
}
