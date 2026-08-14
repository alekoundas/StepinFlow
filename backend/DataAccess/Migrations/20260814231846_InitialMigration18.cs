using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration18 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropTable(
                name: "FlowLocations");

            migrationBuilder.DropTable(
                name: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "IsLocationCustom",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "IsLocationEndCustom",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "LocationEndX",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "LocationEndY",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "LocationY",
                table: "FlowSteps",
                newName: "IsPointEndCustom");

            migrationBuilder.RenameColumn(
                name: "LocationX",
                table: "FlowSteps",
                newName: "IsPointCustom");

            migrationBuilder.RenameColumn(
                name: "FlowSearchAreaId",
                table: "FlowSteps",
                newName: "FlowPointId");

            migrationBuilder.RenameColumn(
                name: "FlowLocationId",
                table: "FlowSteps",
                newName: "FlowPointEndId");

            migrationBuilder.RenameColumn(
                name: "FlowLocationEndId",
                table: "FlowSteps",
                newName: "FlowAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowSearchAreaId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowPointId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowLocationId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowPointEndId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowLocationEndId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowAreaId");

            migrationBuilder.CreateTable(
                name: "FlowAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    ParentFlowAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    SizingMode = table.Column<string>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false),
                    RatioWidth = table.Column<float>(type: "REAL", nullable: false),
                    RatioHeight = table.Column<float>(type: "REAL", nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePattern = table.Column<string>(type: "TEXT", nullable: false),
                    TitleMatchMode = table.Column<string>(type: "TEXT", nullable: false),
                    InstanceIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    UseClientArea = table.Column<bool>(type: "INTEGER", nullable: false),
                    BrowserType = table.Column<string>(type: "TEXT", nullable: false),
                    TabMatchValue = table.Column<string>(type: "TEXT", nullable: false),
                    TabMatchOn = table.Column<string>(type: "TEXT", nullable: false),
                    MonitorUniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowAreas_FlowAreas_ParentFlowAreaId",
                        column: x => x.ParentFlowAreaId,
                        principalTable: "FlowAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowAreas_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    FlowAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    OffsetMode = table.Column<string>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowPoints_FlowAreas_FlowAreaId",
                        column: x => x.FlowAreaId,
                        principalTable: "FlowAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowPoints_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlowAreas_FlowId",
                table: "FlowAreas",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowAreas_ParentFlowAreaId",
                table: "FlowAreas",
                column: "ParentFlowAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowPoints_FlowAreaId",
                table: "FlowPoints",
                column: "FlowAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowPoints_FlowId",
                table: "FlowPoints",
                column: "FlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowAreas_FlowAreaId",
                table: "FlowSteps",
                column: "FlowAreaId",
                principalTable: "FlowAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowPoints_FlowPointEndId",
                table: "FlowSteps",
                column: "FlowPointEndId",
                principalTable: "FlowPoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSteps_FlowPoints_FlowPointId",
                table: "FlowSteps",
                column: "FlowPointId",
                principalTable: "FlowPoints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowAreas_FlowAreaId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowPoints_FlowPointEndId",
                table: "FlowSteps");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSteps_FlowPoints_FlowPointId",
                table: "FlowSteps");

            migrationBuilder.DropTable(
                name: "FlowPoints");

            migrationBuilder.DropTable(
                name: "FlowAreas");

            migrationBuilder.RenameColumn(
                name: "IsPointEndCustom",
                table: "FlowSteps",
                newName: "LocationY");

            migrationBuilder.RenameColumn(
                name: "IsPointCustom",
                table: "FlowSteps",
                newName: "LocationX");

            migrationBuilder.RenameColumn(
                name: "FlowPointId",
                table: "FlowSteps",
                newName: "FlowSearchAreaId");

            migrationBuilder.RenameColumn(
                name: "FlowPointEndId",
                table: "FlowSteps",
                newName: "FlowLocationId");

            migrationBuilder.RenameColumn(
                name: "FlowAreaId",
                table: "FlowSteps",
                newName: "FlowLocationEndId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowPointId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowSearchAreaId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowPointEndId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowLocationId");

            migrationBuilder.RenameIndex(
                name: "IX_FlowSteps_FlowAreaId",
                table: "FlowSteps",
                newName: "IX_FlowSteps_FlowLocationEndId");

            migrationBuilder.AddColumn<bool>(
                name: "IsLocationCustom",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocationEndCustom",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LocationEndX",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LocationEndY",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "FlowSearchAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentFlowSearchAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    BrowserType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Height = table.Column<int>(type: "INTEGER", nullable: false),
                    InstanceIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    MonitorUniqueId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessName = table.Column<string>(type: "TEXT", nullable: false),
                    RatioHeight = table.Column<float>(type: "REAL", nullable: false),
                    RatioWidth = table.Column<float>(type: "REAL", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false),
                    SizingMode = table.Column<string>(type: "TEXT", nullable: false),
                    TabMatchOn = table.Column<string>(type: "TEXT", nullable: false),
                    TabMatchValue = table.Column<string>(type: "TEXT", nullable: false),
                    TitleMatchMode = table.Column<string>(type: "TEXT", nullable: false),
                    TitlePattern = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    UseClientArea = table.Column<bool>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowSearchAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowSearchAreas_FlowSearchAreas_ParentFlowSearchAreaId",
                        column: x => x.ParentFlowSearchAreaId,
                        principalTable: "FlowSearchAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowSearchAreas_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlowLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowSearchAreaId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocationX = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationY = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OffsetMode = table.Column<string>(type: "TEXT", nullable: false),
                    RatioX = table.Column<float>(type: "REAL", nullable: false),
                    RatioY = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowLocations_FlowSearchAreas_FlowSearchAreaId",
                        column: x => x.FlowSearchAreaId,
                        principalTable: "FlowSearchAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FlowLocations_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FlowLocations_FlowId",
                table: "FlowLocations",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowLocations_FlowSearchAreaId",
                table: "FlowLocations",
                column: "FlowSearchAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_FlowId",
                table: "FlowSearchAreas",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_ParentFlowSearchAreaId",
                table: "FlowSearchAreas",
                column: "ParentFlowSearchAreaId");

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
        }
    }
}
