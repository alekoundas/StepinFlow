using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class TemplateOwnAccuracy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMultiScale",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "AuthoredMonitorId",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "ScaleTolerance",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "TemplateMatchMode",
                table: "FlowStepTemplates");

            migrationBuilder.DropColumn(
                name: "Accuracy",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "AuthoredMonitorDpi",
                table: "FlowStepTemplates",
                newName: "AuthoredFlowAreaWidth");

            migrationBuilder.RenameColumn(
                name: "AuthoredFrameWidth",
                table: "FlowStepTemplates",
                newName: "AuthoredFlowAreaHeight");

            migrationBuilder.RenameColumn(
                name: "AuthoredFrameHeight",
                table: "FlowStepTemplates",
                newName: "AuthoredDpi");

            migrationBuilder.AlterColumn<float>(
                name: "Accuracy",
                table: "FlowStepTemplates",
                type: "REAL",
                nullable: false,
                defaultValue: 0f,
                oldClrType: typeof(float),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BestTemplateId",
                table: "ExecutionSteps",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BestTemplateId",
                table: "ExecutionSteps");

            migrationBuilder.RenameColumn(
                name: "AuthoredFlowAreaWidth",
                table: "FlowStepTemplates",
                newName: "AuthoredMonitorDpi");

            migrationBuilder.RenameColumn(
                name: "AuthoredFlowAreaHeight",
                table: "FlowStepTemplates",
                newName: "AuthoredFrameWidth");

            migrationBuilder.RenameColumn(
                name: "AuthoredDpi",
                table: "FlowStepTemplates",
                newName: "AuthoredFrameHeight");

            migrationBuilder.AlterColumn<float>(
                name: "Accuracy",
                table: "FlowStepTemplates",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "REAL");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultiScale",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AuthoredMonitorId",
                table: "FlowStepTemplates",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "ScaleTolerance",
                table: "FlowStepTemplates",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "TemplateMatchMode",
                table: "FlowStepTemplates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Accuracy",
                table: "FlowSteps",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
