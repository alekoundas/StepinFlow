using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Migrations
{
    /// <inheritdoc />
    public partial class Execution_Remove_Useless_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Executions_Executions_ChildLoopExecutionId",
                table: "Executions");

            migrationBuilder.DropIndex(
                name: "IX_Executions_ChildLoopExecutionId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ChildLoopExecutionId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "IsExecuted",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ParentLoopExecutionId",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "ResultImage",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "RunFor",
                table: "Executions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChildLoopExecutionId",
                table: "Executions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExecuted",
                table: "Executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ParentLoopExecutionId",
                table: "Executions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ResultImage",
                table: "Executions",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RunFor",
                table: "Executions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ChildLoopExecutionId",
                table: "Executions",
                column: "ChildLoopExecutionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Executions_Executions_ChildLoopExecutionId",
                table: "Executions",
                column: "ChildLoopExecutionId",
                principalTable: "Executions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
