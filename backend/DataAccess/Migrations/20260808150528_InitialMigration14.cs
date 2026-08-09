using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlowStepImages_Id",
                table: "FlowStepImages");

            migrationBuilder.DropIndex(
                name: "IX_FlowSearchAreas_Id",
                table: "FlowSearchAreas");

            migrationBuilder.RenameColumn(
                name: "WindowName",
                table: "FlowSteps",
                newName: "TemplateMatchMode");

            migrationBuilder.RenameColumn(
                name: "LoopOnMultipleFindings",
                table: "FlowStepImages",
                newName: "OrderNumber");

            migrationBuilder.RenameColumn(
                name: "AppWindowName",
                table: "FlowSearchAreas",
                newName: "TitlePattern");

            migrationBuilder.AddColumn<float>(
                name: "Accuracy",
                table: "FlowSteps",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "ImageSearchMode",
                table: "FlowSteps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "LoopOnMultipleFindings",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxMatches",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PollIntervalMilliseconds",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutMilliseconds",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "TemplateMatchMode",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Accuracy",
                table: "FlowStepImages",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultiScale",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredFrameHeight",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredFrameWidth",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AuthoredMonitorDpi",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AuthoredMonitorId",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClickAnchor",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ClickOffsetX",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClickOffsetY",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "ScaleTolerance",
                table: "FlowStepImages",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "BrowserType",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "InstanceIndex",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentFlowSearchAreaId",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessName",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "RatioHeight",
                table: "FlowSearchAreas",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RatioWidth",
                table: "FlowSearchAreas",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RatioX",
                table: "FlowSearchAreas",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RatioY",
                table: "FlowSearchAreas",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "SizingMode",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TabMatchOn",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TabMatchValue",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitleMatchMode",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UseClientArea",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Anchor",
                table: "FlowLocations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FlowSearchAreaId",
                table: "FlowLocations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffsetMode",
                table: "FlowLocations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "RatioX",
                table: "FlowLocations",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "RatioY",
                table: "FlowLocations",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_ParentFlowSearchAreaId",
                table: "FlowSearchAreas",
                column: "ParentFlowSearchAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_FlowLocations_FlowSearchAreaId",
                table: "FlowLocations",
                column: "FlowSearchAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_FlowLocations_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowLocations",
                column: "FlowSearchAreaId",
                principalTable: "FlowSearchAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FlowSearchAreas_FlowSearchAreas_ParentFlowSearchAreaId",
                table: "FlowSearchAreas",
                column: "ParentFlowSearchAreaId",
                principalTable: "FlowSearchAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FlowLocations_FlowSearchAreas_FlowSearchAreaId",
                table: "FlowLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_FlowSearchAreas_FlowSearchAreas_ParentFlowSearchAreaId",
                table: "FlowSearchAreas");

            migrationBuilder.DropIndex(
                name: "IX_FlowSearchAreas_ParentFlowSearchAreaId",
                table: "FlowSearchAreas");

            migrationBuilder.DropIndex(
                name: "IX_FlowLocations_FlowSearchAreaId",
                table: "FlowLocations");

            migrationBuilder.DropColumn(
                name: "Accuracy",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "ImageSearchMode",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "LoopOnMultipleFindings",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "MaxMatches",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "PollIntervalMilliseconds",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "TimeoutMilliseconds",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "AllowMultiScale",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "AuthoredFrameHeight",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "AuthoredFrameWidth",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "AuthoredMonitorDpi",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "AuthoredMonitorId",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "ClickAnchor",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "ClickOffsetX",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "ClickOffsetY",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "ScaleTolerance",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "BrowserType",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "InstanceIndex",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "ParentFlowSearchAreaId",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "ProcessName",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "RatioHeight",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "RatioWidth",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "RatioX",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "RatioY",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "SizingMode",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "TabMatchOn",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "TabMatchValue",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "TitleMatchMode",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "UseClientArea",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "Anchor",
                table: "FlowLocations");

            migrationBuilder.DropColumn(
                name: "FlowSearchAreaId",
                table: "FlowLocations");

            migrationBuilder.DropColumn(
                name: "OffsetMode",
                table: "FlowLocations");

            migrationBuilder.DropColumn(
                name: "RatioX",
                table: "FlowLocations");

            migrationBuilder.DropColumn(
                name: "RatioY",
                table: "FlowLocations");

            migrationBuilder.RenameColumn(
                name: "TemplateMatchMode",
                table: "FlowSteps",
                newName: "WindowName");

            migrationBuilder.RenameColumn(
                name: "OrderNumber",
                table: "FlowStepImages",
                newName: "LoopOnMultipleFindings");

            migrationBuilder.RenameColumn(
                name: "TitlePattern",
                table: "FlowSearchAreas",
                newName: "AppWindowName");

            migrationBuilder.AlterColumn<int>(
                name: "TemplateMatchMode",
                table: "FlowStepImages",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "Accuracy",
                table: "FlowStepImages",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepImages_Id",
                table: "FlowStepImages",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlowSearchAreas_Id",
                table: "FlowSearchAreas",
                column: "Id",
                unique: true);
        }
    }
}
