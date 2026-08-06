using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSearchAreas_SubFlows_SubFlowId",
                table: "FlowSearchAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_FlowId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_Id",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_ParentFlowStepId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSearchAreas_SubFlowId",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "SubFlowId",
                table: "FlowSearchAreas");

            migrationBuilder.AddColumn<int>(
                name: "FlowLocationEndId",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlowLocationId",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlowStepReferenceEndId",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FlowLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowLocations_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowId_OrderNumber",
                table: "FlowSteps",
                columns: new[] { "FlowId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowLocationEndId",
                table: "FlowSteps",
                column: "FlowLocationEndId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowLocationId",
                table: "FlowSteps",
                column: "FlowLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowStepReferenceEndId",
                table: "FlowSteps",
                column: "FlowStepReferenceEndId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_ParentFlowStepId_OrderNumber",
                table: "FlowSteps",
                columns: new[] { "ParentFlowStepId", "OrderNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_RootId",
                table: "FlowSteps",
                column: "RootId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepImages_Id",
                table: "FlowStepImages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowLocations_FlowId",
                table: "FlowLocations",
                column: "FlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowLocations_FlowLocationEndId",
                table: "FlowSteps",
                column: "FlowLocationEndId",
                principalTable: "FlowLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowLocations_FlowLocationId",
                table: "FlowSteps",
                column: "FlowLocationId",
                principalTable: "FlowLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowSteps",
                column: "FlowSearchAreaId",
                principalTable: "FlowSearchAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceEndId",
                table: "FlowSteps",
                column: "FlowStepReferenceEndId",
                principalTable: "FlowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps",
                column: "FlowStepReferenceId",
                principalTable: "FlowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                table: "FlowSteps",
                column: "ParentFlowStepId",
                principalTable: "FlowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowLocations_FlowLocationEndId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowLocations_FlowLocationId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceEndId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                table: "FlowSteps");

            migrationBuilder.DropTable(
                name: "FlowLocations");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_FlowId_OrderNumber",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_FlowLocationEndId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_FlowLocationId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_FlowStepReferenceEndId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_ParentFlowStepId_OrderNumber",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowSteps_RootId",
                table: "FlowSteps");

            migrationBuilder.DropIndex(
                name: "IX_FlowStepImages_Id",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "FlowLocationEndId",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "FlowLocationId",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "FlowStepReferenceEndId",
                table: "FlowSteps");

            migrationBuilder.AddColumn<int>(
                name: "SubFlowId",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowId",
                table: "FlowSteps",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_Id",
                table: "FlowSteps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_ParentFlowStepId",
                table: "FlowSteps",
                column: "ParentFlowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_SubFlowId",
                table: "FlowSearchAreas",
                column: "SubFlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSearchAreas_SubFlows_SubFlowId",
                table: "FlowSearchAreas",
                column: "SubFlowId",
                principalTable: "SubFlows",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowSteps",
                column: "FlowSearchAreaId",
                principalTable: "FlowSearchAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps",
                column: "FlowStepReferenceId",
                principalTable: "FlowSteps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                table: "FlowSteps",
                column: "ParentFlowStepId",
                principalTable: "FlowSteps",
                principalColumn: "Id");
        }
    }
}
