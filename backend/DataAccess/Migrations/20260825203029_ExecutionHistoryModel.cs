using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionHistoryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExecutionSteps_Id",
                table: "ExecutionSteps");

            migrationBuilder.DropIndex(
                name: "IX_Executions_FlowId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_Id",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "CurrentStepPath",
                table: "Executions");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ExecutionSteps",
                newName: "StartedOn");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "ExecutionSteps",
                newName: "ResultJson");

            migrationBuilder.RenameColumn(
                name: "CheckpointStepCount",
                table: "Executions",
                newName: "StepCount");

            migrationBuilder.AlterColumn<int>(
                name: "FlowStepId",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "DurationMilliseconds",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FlowStepType",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MatchCount",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchIndex",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultImagePath",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorFlowStepId",
                table: "Executions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "Executions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FlowStructureHash",
                table: "Executions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HistoryLevel",
                table: "Executions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_FlowStepId",
                table: "ExecutionSteps",
                column: "FlowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_FlowId_CreatedOn",
                table: "Executions",
                columns: new[] { "FlowId", "CreatedOn" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExecutionSteps_FlowSteps_FlowStepId",
                table: "ExecutionSteps",
                column: "FlowStepId",
                principalTable: "FlowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Status was a free string ("Running", "Pending") and is an enum now. Nothing has ever
            // executed a flow in this database so both tables should be empty, but a row written by
            // hand would otherwise be a value the enum cannot parse.
            migrationBuilder.Sql(@"
                UPDATE Executions
                SET Status = 'ABANDONED'
                WHERE Status NOT IN ('RUNNING', 'COMPLETED', 'STOPPED', 'ERRORED', 'ABANDONED');
            ");

            migrationBuilder.Sql(@"
                UPDATE Executions SET HistoryLevel = 'STEPS' WHERE HistoryLevel = '';
            ");

            migrationBuilder.Sql(@"
                UPDATE ExecutionSteps
                SET Outcome = 'FAILURE'
                WHERE Outcome NOT IN ('SUCCESS', 'FAILURE');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExecutionSteps_FlowSteps_FlowStepId",
                table: "ExecutionSteps");

            migrationBuilder.DropIndex(
                name: "IX_ExecutionSteps_FlowStepId",
                table: "ExecutionSteps");

            migrationBuilder.DropIndex(
                name: "IX_Executions_FlowId_CreatedOn",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "DurationMilliseconds",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "FlowStepType",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "MatchCount",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "MatchIndex",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "ResultImagePath",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "ErrorFlowStepId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "FlowStructureHash",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "HistoryLevel",
                table: "Executions");

            migrationBuilder.RenameColumn(
                name: "StartedOn",
                table: "ExecutionSteps",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ResultJson",
                table: "ExecutionSteps",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "StepCount",
                table: "Executions",
                newName: "CheckpointStepCount");

            migrationBuilder.AlterColumn<int>(
                name: "FlowStepId",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStepPath",
                table: "Executions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSteps_Id",
                table: "ExecutionSteps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_FlowId",
                table: "Executions",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Id",
                table: "Executions",
                column: "Id",
                unique: true);
        }
    }
}
