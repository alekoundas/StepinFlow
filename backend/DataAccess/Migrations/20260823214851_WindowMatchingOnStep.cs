using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class WindowMatchingOnStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrowserType",
                table: "FlowAreas");

            migrationBuilder.DropColumn(
                name: "InstanceIndex",
                table: "FlowAreas");

            migrationBuilder.AddColumn<string>(
                name: "ProcessName",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleMatchMode",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitlePattern",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // TitleMatchMode is stored as text and the added column defaults to an empty string,
            // which is not a value the enum can parse. Every row gets the enum's own default first.
            migrationBuilder.Sql(@"
                UPDATE FlowSteps SET TitleMatchMode = 'CONTAINS';
            ");

            // Window steps used to name their window through an APPLICATION area. The matcher moves
            // onto the step, so it has to be copied across - without this every window step in every
            // saved flow silently stops matching, with no error, just a step that never finds its
            // window.
            //
            // FlowAreaId is cleared afterwards because these types no longer read it, and leaving it
            // set would keep counting them against the area's "used by" total.
            migrationBuilder.Sql(@"
                UPDATE FlowSteps
                SET ProcessName    = COALESCE((SELECT a.ProcessName    FROM FlowAreas a WHERE a.Id = FlowSteps.FlowAreaId), ''),
                    TitlePattern   = COALESCE((SELECT a.TitlePattern   FROM FlowAreas a WHERE a.Id = FlowSteps.FlowAreaId), ''),
                    TitleMatchMode = COALESCE((SELECT a.TitleMatchMode FROM FlowAreas a WHERE a.Id = FlowSteps.FlowAreaId), 'CONTAINS')
                WHERE FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE');
            ");

            migrationBuilder.Sql(@"
                UPDATE FlowSteps
                SET FlowAreaId = NULL
                WHERE FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessName",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "TitleMatchMode",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "TitlePattern",
                table: "FlowSteps");

            migrationBuilder.AddColumn<string>(
                name: "BrowserType",
                table: "FlowAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InstanceIndex",
                table: "FlowAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
