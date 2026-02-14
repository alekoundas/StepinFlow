using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Flows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Flows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubFlows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubFlows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlowSearchAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LocationLeft = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationTop = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationToRight = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationToBottom = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubFlowId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowSearchAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowSearchAreas_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSearchAreas_SubFlows_SubFlowId",
                        column: x => x.SubFlowId,
                        principalTable: "SubFlows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FlowSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FlowStepType = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationEndX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationEndY = table.Column<int>(type: "INTEGER", nullable: false),
                    WaitForMilliseconds = table.Column<int>(type: "INTEGER", nullable: false),
                    LoopCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLoopInfinite = table.Column<bool>(type: "INTEGER", nullable: false),
                    RunCommand = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionText = table.Column<string>(type: "TEXT", nullable: false),
                    ConditionType = table.Column<string>(type: "TEXT", nullable: false),
                    WindowName = table.Column<string>(type: "TEXT", nullable: false),
                    WindowHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    WindowWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyboardInputText = table.Column<string>(type: "TEXT", nullable: false),
                    KeyboardInputType = table.Column<string>(type: "TEXT", nullable: true),
                    IsLocationCustom = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocationEndCustom = table.Column<bool>(type: "INTEGER", nullable: false),
                    CursorActionType = table.Column<string>(type: "TEXT", nullable: true),
                    CursorButtonType = table.Column<string>(type: "TEXT", nullable: true),
                    CursorScrollDirectionType = table.Column<string>(type: "TEXT", nullable: true),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubFlowId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowSearchAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentFlowStepId = table.Column<int>(type: "INTEGER", nullable: true),
                    FlowStepReferenceId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSearchAreas_FlowSearchAreaId",
                        column: x => x.FlowSearchAreaId,
                        principalTable: "FlowSearchAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSteps_FlowStepReferenceId",
                        column: x => x.FlowStepReferenceId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSteps_FlowSteps_ParentFlowStepId",
                        column: x => x.ParentFlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FlowSteps_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlowSteps_SubFlows_SubFlowId",
                        column: x => x.SubFlowId,
                        principalTable: "SubFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowStepImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateMatchMode = table.Column<int>(type: "INTEGER", nullable: true),
                    TemplateImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Accuracy = table.Column<float>(type: "REAL", nullable: false),
                    LoopOnMultipleFindings = table.Column<bool>(type: "INTEGER", nullable: false),
                    FlowStepId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowStepImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowStepImages_FlowSteps_FlowStepId",
                        column: x => x.FlowStepId,
                        principalTable: "FlowSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Flows_Id",
                table: "Flows",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_FlowId",
                table: "FlowSearchAreas",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_Id",
                table: "FlowSearchAreas",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_SubFlowId",
                table: "FlowSearchAreas",
                column: "SubFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepImages_FlowStepId",
                table: "FlowStepImages",
                column: "FlowStepId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepImages_Id",
                table: "FlowStepImages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowId",
                table: "FlowSteps",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowSearchAreaId",
                table: "FlowSteps",
                column: "FlowSearchAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSteps_FlowStepReferenceId",
                table: "FlowSteps",
                column: "FlowStepReferenceId");

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
                name: "IX_FlowSteps_SubFlowId",
                table: "FlowSteps",
                column: "SubFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_SubFlows_Id",
                table: "SubFlows",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlowStepImages");

            migrationBuilder.DropTable(
                name: "FlowSteps");

            migrationBuilder.DropTable(
                name: "FlowSearchAreas");

            migrationBuilder.DropTable(
                name: "Flows");

            migrationBuilder.DropTable(
                name: "SubFlows");
        }
    }
}
