using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SharedFlowTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlowTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    TemplateImage = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ClickOffsetX = table.Column<int>(type: "INTEGER", nullable: false),
                    ClickOffsetY = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredFlowAreaWidth = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredFlowAreaHeight = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthoredDpi = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowTemplates_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "FlowTemplateId",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // The pictures move before their columns go. Rows of one flow with the same name and
            // the same picture, click point and capture become one template; a copy that differs
            // keeps a template of its own under a new name, so no step searches for anything it
            // did not search for before. Each template takes the id of the row it came from, which
            // is how the links find it.
            migrationBuilder.Sql(@"
                UPDATE FlowStepTemplates SET Name = 'template-' || Id || '.png' WHERE Name IS NULL OR Name = '';");

            migrationBuilder.Sql(@"
                CREATE TEMP TABLE TemplateGroups AS
                SELECT t.Id AS FlowStepTemplateId,
                       s.RootId AS FlowId,
                       (SELECT MIN(t2.Id)
                          FROM FlowStepTemplates t2
                          INNER JOIN FlowSteps s2 ON s2.Id = t2.FlowStepId
                         WHERE s2.RootId = s.RootId
                           AND lower(t2.Name) = lower(t.Name)
                           AND t2.TemplateImage IS t.TemplateImage
                           AND t2.ClickOffsetX = t.ClickOffsetX
                           AND t2.ClickOffsetY = t.ClickOffsetY
                           AND t2.AuthoredFlowAreaWidth = t.AuthoredFlowAreaWidth
                           AND t2.AuthoredFlowAreaHeight = t.AuthoredFlowAreaHeight
                           AND t2.AuthoredDpi = t.AuthoredDpi) AS PictureRowId
                  FROM FlowStepTemplates t
                  INNER JOIN FlowSteps s ON s.Id = t.FlowStepId;");

            migrationBuilder.Sql(@"
                INSERT INTO FlowTemplates (Id, Name, TemplateImage, Thumbnail, ClickOffsetX, ClickOffsetY, AuthoredFlowAreaWidth, AuthoredFlowAreaHeight, AuthoredDpi, FlowId, CreatedOn, UpdatedOn)
                SELECT t.Id,
                       CASE
                           WHEN t.Id = (SELECT MIN(t3.Id)
                                          FROM FlowStepTemplates t3
                                          INNER JOIN FlowSteps s3 ON s3.Id = t3.FlowStepId
                                         WHERE s3.RootId = g.FlowId AND lower(t3.Name) = lower(t.Name)) THEN t.Name
                           WHEN lower(t.Name) LIKE '%.png' THEN substr(t.Name, 1, length(t.Name) - 4) || '-' || t.Id || '.png'
                           ELSE t.Name || '-' || t.Id
                       END,
                       t.TemplateImage, t.Thumbnail, t.ClickOffsetX, t.ClickOffsetY,
                       t.AuthoredFlowAreaWidth, t.AuthoredFlowAreaHeight, t.AuthoredDpi,
                       g.FlowId, t.CreatedOn, t.UpdatedOn
                  FROM FlowStepTemplates t
                  INNER JOIN (SELECT DISTINCT PictureRowId, FlowId FROM TemplateGroups) g ON g.PictureRowId = t.Id;");

            migrationBuilder.Sql(@"
                UPDATE FlowStepTemplates
                   SET FlowTemplateId = (SELECT g.PictureRowId FROM TemplateGroups g WHERE g.FlowStepTemplateId = FlowStepTemplates.Id);");

            migrationBuilder.Sql(@"
                DROP TABLE TemplateGroups;");

            migrationBuilder.DropColumn(
                name: "AuthoredDpi",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "AuthoredFlowAreaHeight",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "AuthoredFlowAreaWidth",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "ClickOffsetX",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "ClickOffsetY",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "TemplateImage",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "FlowStepTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepTemplates_FlowTemplateId",
                table: "FlowStepTemplates",
                column: "FlowTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowTemplates_FlowId",
                table: "FlowTemplates",
                column: "FlowId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowStepTemplates_FlowTemplates_FlowTemplateId",
                table: "FlowStepTemplates",
                column: "FlowTemplateId",
                principalTable: "FlowTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuthoredDpi",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredFlowAreaHeight",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredFlowAreaWidth",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClickOffsetX",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClickOffsetY",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "FlowStepTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "TemplateImage",
                table: "FlowStepTemplates",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Thumbnail",
                table: "FlowStepTemplates",
                type: "BLOB",
                nullable: true);

            // Every link gets its own copy of the picture back, as each row had one before.
            migrationBuilder.Sql(@"
                UPDATE FlowStepTemplates
                   SET Name = (SELECT f.Name FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       TemplateImage = (SELECT f.TemplateImage FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       Thumbnail = (SELECT f.Thumbnail FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       ClickOffsetX = (SELECT f.ClickOffsetX FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       ClickOffsetY = (SELECT f.ClickOffsetY FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       AuthoredFlowAreaWidth = (SELECT f.AuthoredFlowAreaWidth FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       AuthoredFlowAreaHeight = (SELECT f.AuthoredFlowAreaHeight FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId),
                       AuthoredDpi = (SELECT f.AuthoredDpi FROM FlowTemplates f WHERE f.Id = FlowStepTemplates.FlowTemplateId);");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowStepTemplates_FlowTemplates_FlowTemplateId",
                table: "FlowStepTemplates");

            migrationBuilder.DropIndex(
                name: "IX_FlowStepTemplates_FlowTemplateId",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "FlowTemplateId",
                table: "FlowStepTemplates");

            migrationBuilder.DropTable(
                name: "FlowTemplates");
        }
    }
}
