using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameRunCommandToShell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RunCommandShell",
                table: "FlowSteps",
                newName: "ShellType");

            migrationBuilder.RenameColumn(
                name: "RunCommandPreset",
                table: "FlowSteps",
                newName: "ShellCommandMode");

            migrationBuilder.RenameColumn(
                name: "RunCommandValue",
                table: "FlowSteps",
                newName: "ShellCommandValue");

            migrationBuilder.RenameColumn(
                name: "RunCommandWorkingDirectory",
                table: "FlowSteps",
                newName: "ShellWorkingDirectory");

            migrationBuilder.RenameColumn(
                name: "LaunchArguments",
                table: "FlowSteps",
                newName: "ShellArguments");

            migrationBuilder.RenameColumn(
                name: "SuccessExitCodes",
                table: "FlowSteps",
                newName: "ShellSuccessExitCodes");

            migrationBuilder.RenameColumn(
                name: "ResultSource",
                table: "FlowSteps",
                newName: "ShellResultSource");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShellType",
                table: "FlowSteps",
                newName: "RunCommandShell");

            migrationBuilder.RenameColumn(
                name: "ShellCommandMode",
                table: "FlowSteps",
                newName: "RunCommandPreset");

            migrationBuilder.RenameColumn(
                name: "ShellCommandValue",
                table: "FlowSteps",
                newName: "RunCommandValue");

            migrationBuilder.RenameColumn(
                name: "ShellWorkingDirectory",
                table: "FlowSteps",
                newName: "RunCommandWorkingDirectory");

            migrationBuilder.RenameColumn(
                name: "ShellArguments",
                table: "FlowSteps",
                newName: "LaunchArguments");

            migrationBuilder.RenameColumn(
                name: "ShellSuccessExitCodes",
                table: "FlowSteps",
                newName: "SuccessExitCodes");

            migrationBuilder.RenameColumn(
                name: "ShellResultSource",
                table: "FlowSteps",
                newName: "ResultSource");
        }
    }
}
