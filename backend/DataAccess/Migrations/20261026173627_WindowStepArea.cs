using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class WindowStepArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A window step names a window area instead of carrying its own process and title. Each
            // one goes to an area of its flow that finds the same window, made when there is none.
            const string LINK = @"
                UPDATE FlowSteps SET FlowAreaId = (
                    SELECT a.Id FROM FlowAreas a
                    WHERE a.FlowId = FlowSteps.RootId AND a.Type = 'APPLICATION'
                      AND a.ProcessName = FlowSteps.ProcessName AND a.TitlePattern = FlowSteps.TitlePattern AND a.TitleMatchMode = FlowSteps.TitleMatchMode
                    ORDER BY a.Id LIMIT 1)
                WHERE FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE') AND FlowAreaId IS NULL;";

            migrationBuilder.Sql(LINK);

            migrationBuilder.Sql(@"
                INSERT INTO FlowAreas (Name, Type, SizingMode, LocationX, LocationY, Width, Height, RatioX, RatioY, RatioWidth, RatioHeight,
                    ProcessName, TitlePattern, TitleMatchMode, UseClientArea, TabMatchValue, TabMatchOn, MonitorDeviceName, FlowId, CreatedOn, AuthoredDpi, IsMain)
                SELECT 'Window of step ' || MIN(Id), 'APPLICATION', 'ABSOLUTE_PX', 0, 0, 0, 0, 0, 0, 0, 0,
                    ProcessName, TitlePattern, TitleMatchMode, 1, '', 'TITLE', '', RootId, datetime('now'), 0, 0
                FROM FlowSteps
                WHERE FlowStepType IN ('WINDOW_FOCUS', 'WINDOW_RESIZE', 'WINDOW_RELOCATE') AND FlowAreaId IS NULL
                  AND (ProcessName <> '' OR TitlePattern <> '')
                GROUP BY RootId, ProcessName, TitlePattern, TitleMatchMode;");

            migrationBuilder.Sql(LINK);

            migrationBuilder.DropColumn(
                name: "ProcessName",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "TitleMatchMode",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "TitlePattern",
                table: "FlowSteps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
