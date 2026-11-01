using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class LaunchArguments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchArguments",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // A launch of an area carried its whole command line. Its program, quoted as it was
            // written, is now what starts the area, the first launch's when several open it and the
            // area does not say already; the rest are the launch's arguments.
            migrationBuilder.Sql(@"
                CREATE TEMP TABLE LaunchLines AS
                SELECT Id, FlowAreaId, Line,
                    CASE
                        WHEN substr(Line, 1, 1) = '""' AND instr(substr(Line, 2), '""') > 0 THEN substr(Line, 1, instr(substr(Line, 2), '""') + 1)
                        WHEN instr(Line, ' ') > 0 THEN substr(Line, 1, instr(Line, ' ') - 1)
                        ELSE Line
                    END AS Program
                FROM (
                    SELECT Id, FlowAreaId, trim(RunCommandValue) AS Line FROM FlowSteps
                    WHERE RunCommandPreset = 'LAUNCH_APP' AND FlowAreaId IS NOT NULL AND trim(RunCommandValue) <> '');");

            migrationBuilder.Sql(@"
                UPDATE FlowAreas SET LaunchCommand = (
                    SELECT l.Program FROM LaunchLines l WHERE l.FlowAreaId = FlowAreas.Id ORDER BY l.Id LIMIT 1)
                WHERE trim(LaunchCommand) = '' AND Id IN (SELECT FlowAreaId FROM LaunchLines);");

            migrationBuilder.Sql(@"
                UPDATE FlowSteps SET
                    LaunchArguments = (SELECT trim(substr(l.Line, length(l.Program) + 1)) FROM LaunchLines l WHERE l.Id = FlowSteps.Id),
                    RunCommandValue = ''
                WHERE Id IN (
                    SELECT l.Id FROM LaunchLines l JOIN FlowAreas a ON a.Id = l.FlowAreaId
                    WHERE a.LaunchCommand = l.Program COLLATE NOCASE);");

            // One that started another program than its area's keeps starting it, without the area:
            // starting the area's instead would start something else with nobody told.
            migrationBuilder.Sql(@"
                UPDATE FlowSteps SET FlowAreaId = NULL
                WHERE Id IN (
                    SELECT l.Id FROM LaunchLines l JOIN FlowAreas a ON a.Id = l.FlowAreaId
                    WHERE a.LaunchCommand <> l.Program COLLATE NOCASE);");

            migrationBuilder.Sql("DROP TABLE LaunchLines;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchArguments",
                table: "FlowSteps");
        }
    }
}
