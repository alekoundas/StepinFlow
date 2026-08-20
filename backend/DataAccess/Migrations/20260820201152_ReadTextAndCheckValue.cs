using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ReadTextAndCheckValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dropped and added rather than renamed: the two columns hold unrelated things, and
            // scaffolding matched them only because both are strings.
            migrationBuilder.DropColumn(
                name: "ResultVariableName",
                table: "FlowSteps");

            migrationBuilder.AddColumn<string>(
                name: "ConditionTextEnd",
                table: "FlowSteps",
                nullable: false,
                defaultValue: "");

            // The types are stored by name, so rows written before the rename no longer parse.
            migrationBuilder.Sql("UPDATE FlowSteps SET FlowStepType = 'READ_TEXT' WHERE FlowStepType = 'TEXT_SEARCH';");
            migrationBuilder.Sql("UPDATE FlowSteps SET FlowStepType = 'CHECK_VALUE' WHERE FlowStepType = 'VARIABLE_CONDITION';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE FlowSteps SET FlowStepType = 'TEXT_SEARCH' WHERE FlowStepType = 'READ_TEXT';");
            migrationBuilder.Sql("UPDATE FlowSteps SET FlowStepType = 'VARIABLE_CONDITION' WHERE FlowStepType = 'CHECK_VALUE';");

            migrationBuilder.DropColumn(
                name: "ConditionTextEnd",
                table: "FlowSteps");

            migrationBuilder.AddColumn<string>(
                name: "ResultVariableName",
                table: "FlowSteps",
                nullable: false,
                defaultValue: "");
        }
    }
}
