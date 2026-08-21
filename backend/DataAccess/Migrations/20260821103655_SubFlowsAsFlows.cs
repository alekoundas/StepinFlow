using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SubFlowsAsFlows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_SubFlows_SubFlowId",
                table: "FlowSteps");

            // The column is renamed rather than replaced, but it stops pointing at SubFlows and
            // starts pointing at Flows, and the two had separate id sequences. Nothing ever
            // created a SubFlow so this is empty in practice; clearing it makes the migration
            // right whatever the database actually holds.
            migrationBuilder.Sql("UPDATE FlowSteps SET SubFlowId = NULL;");

            migrationBuilder.DropTable(
                name: "SubFlows");

            migrationBuilder.RenameColumn(
                name: "SubFlowId",
                table: "FlowSteps",
                newName: "InvokedFlowId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_SubFlowId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_InvokedFlowId");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubFlow",
                table: "Flows",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_Flows_InvokedFlowId",
                table: "FlowSteps",
                column: "InvokedFlowId",
                principalTable: "Flows",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_Flows_InvokedFlowId",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "IsSubFlow",
                table: "Flows");

            migrationBuilder.RenameColumn(
                name: "InvokedFlowId",
                table: "FlowSteps",
                newName: "SubFlowId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_InvokedFlowId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_SubFlowId");

            migrationBuilder.CreateTable(
                name: "SubFlows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubFlows", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubFlows_Id",
                table: "SubFlows",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_SubFlows_SubFlowId",
                table: "FlowSteps",
                column: "SubFlowId",
                principalTable: "SubFlows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
