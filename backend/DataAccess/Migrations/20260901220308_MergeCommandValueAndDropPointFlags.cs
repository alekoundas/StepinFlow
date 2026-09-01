using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MergeCommandValueAndDropPointFlags : Migration
    {
        /// <summary>The four types the point flags ever applied to.</summary>
        private const string _cursorTypes = "('CURSOR_CLICK', 'CURSOR_RELOCATE', 'CURSOR_DRAG', 'CURSOR_SCROLL')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A CUSTOM step keeps its command in the column about to be dropped, so it moves into
            // the one that survives before that happens.
            migrationBuilder.Sql(
                "UPDATE FlowSteps SET RunCommandPresetValue = RunCommand WHERE RunCommandPreset = 'CUSTOM';");

            // From here on, whichever id is set is the point source. A row that disagrees with its
            // flag would silently start using the other one, so the losing id is cleared first.
            //
            // Cursor types only. WINDOW_RELOCATE keeps its target in FlowPointId and never had a
            // flag to go with it, and CHECK_VALUE and NOTIFY use FlowStepReferenceId the same way -
            // an unscoped update would empty all three.
            migrationBuilder.Sql(
                $"UPDATE FlowSteps SET FlowStepReferenceId = NULL WHERE IsPointCustom = 1 AND FlowStepType IN {_cursorTypes};");

            migrationBuilder.Sql(
                $"UPDATE FlowSteps SET FlowPointId = NULL WHERE IsPointCustom = 0 AND FlowStepType IN {_cursorTypes};");

            migrationBuilder.Sql(
                "UPDATE FlowSteps SET FlowStepReferenceEndId = NULL WHERE IsPointEndCustom = 1 AND FlowStepType = 'CURSOR_DRAG';");

            migrationBuilder.Sql(
                "UPDATE FlowSteps SET FlowPointEndId = NULL WHERE IsPointEndCustom = 0 AND FlowStepType = 'CURSOR_DRAG';");

            migrationBuilder.DropColumn(
                name: "IsPointCustom",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "IsPointEndCustom",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "RunCommand",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "RunCommandPresetValue",
                table: "FlowSteps",
                newName: "RunCommandValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RunCommandValue",
                table: "FlowSteps",
                newName: "RunCommandPresetValue");

            migrationBuilder.AddColumn<bool>(
                name: "IsPointCustom",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPointEndCustom",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RunCommand",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            // Rebuild what the ids now say, so going back does not leave every cursor step reading
            // the wrong source.
            migrationBuilder.Sql(
                $"UPDATE FlowSteps SET IsPointCustom = CASE WHEN FlowPointId IS NOT NULL THEN 1 ELSE 0 END WHERE FlowStepType IN {_cursorTypes};");

            migrationBuilder.Sql(
                "UPDATE FlowSteps SET IsPointEndCustom = CASE WHEN FlowPointEndId IS NOT NULL THEN 1 ELSE 0 END WHERE FlowStepType = 'CURSOR_DRAG';");

            migrationBuilder.Sql(
                "UPDATE FlowSteps SET RunCommand = RunCommandPresetValue, RunCommandPresetValue = '' WHERE RunCommandPreset = 'CUSTOM';");
        }
    }
}
