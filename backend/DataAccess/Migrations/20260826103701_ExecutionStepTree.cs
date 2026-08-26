using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionStepTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionSteps_ExecutionId",
                table: "ExecutionSteps");

            migrationBuilder.RenameColumn(
                name: "ResultJson",
                table: "ExecutionSteps",
                newName: "Value");

            migrationBuilder.AddColumn<string>(
                name: "Command",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Depth",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExitCode",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoopPass",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentSequence",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_ExecutionId_Sequence",
                table: "ExecutionSteps",
                columns: new[] { "ExecutionId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionSteps_ExecutionId_Sequence",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Command",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Depth",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "ExitCode",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "LoopPass",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "ParentSequence",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "ExecutionSteps");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "ExecutionSteps",
                newName: "ResultJson");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_ExecutionId",
                table: "ExecutionSteps",
                column: "ExecutionId");
        }
    }
}
