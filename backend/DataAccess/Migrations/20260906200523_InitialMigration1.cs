using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlowStepLastGoodScreenshotHistories_FlowStepId",
                table: "FlowStepLastGoodScreenshotHistories");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepLastGoodScreenshotHistories_FlowStepId_ViewportWidth_ViewportHeight",
                table: "FlowStepLastGoodScreenshotHistories",
                columns: new[] { "FlowStepId", "ViewportWidth", "ViewportHeight" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FlowStepLastGoodScreenshotHistories_FlowStepId_ViewportWidth_ViewportHeight",
                table: "FlowStepLastGoodScreenshotHistories");

            migrationBuilder.CreateIndex(
                name: "IX_FlowStepLastGoodScreenshotHistories_FlowStepId",
                table: "FlowStepLastGoodScreenshotHistories",
                column: "FlowStepId");
        }
    }
}
