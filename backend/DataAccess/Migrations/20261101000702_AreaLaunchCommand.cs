using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AreaLaunchCommand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchCommand",
                table: "FlowAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // An area a launch opens is started by that launch's command line, the first one when
            // several open it. The launches keep theirs, which still win over the area's.
            migrationBuilder.Sql(@"
                UPDATE FlowAreas SET LaunchCommand = (
                    SELECT s.RunCommandValue FROM FlowSteps s
                    WHERE s.FlowAreaId = FlowAreas.Id AND s.RunCommandPreset = 'LAUNCH_APP' AND TRIM(s.RunCommandValue) <> ''
                    ORDER BY s.Id LIMIT 1)
                WHERE EXISTS (
                    SELECT 1 FROM FlowSteps s
                    WHERE s.FlowAreaId = FlowAreas.Id AND s.RunCommandPreset = 'LAUNCH_APP' AND TRIM(s.RunCommandValue) <> '');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchCommand",
                table: "FlowAreas");
        }
    }
}
