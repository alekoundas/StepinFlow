using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SubFlowIdOnFlowStep : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_Flows_InvokedFlowId",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "InvokedFlowId",
                table: "FlowSteps",
                newName: "SubFlowId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_InvokedFlowId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_SubFlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_Flows_SubFlowId",
                table: "FlowSteps",
                column: "SubFlowId",
                principalTable: "Flows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_Flows_SubFlowId",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "SubFlowId",
                table: "FlowSteps",
                newName: "InvokedFlowId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_SubFlowId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_InvokedFlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_Flows_InvokedFlowId",
                table: "FlowSteps",
                column: "InvokedFlowId",
                principalTable: "Flows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
