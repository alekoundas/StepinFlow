using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OcrLanguage",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultExtractPattern",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultSource",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultVariableName",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RunCommandPreset",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RunCommandPresetValue",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RunCommandShell",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RunCommandWorkingDirectory",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuccessExitCodes",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SystemActionType",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrLanguage",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "ResultExtractPattern",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "ResultSource",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "ResultVariableName",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "RunCommandPreset",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "RunCommandPresetValue",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "RunCommandShell",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "RunCommandWorkingDirectory",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "SuccessExitCodes",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "SystemActionType",
                table: "FlowSteps");
        }
    }
}
