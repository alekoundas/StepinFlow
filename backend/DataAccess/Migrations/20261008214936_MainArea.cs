using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MainArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flows_FlowAreas_AppUnderTestAreaId",
                table: "Flows");

            migrationBuilder.DropIndex(
                name: "IX_Flows_AppUnderTestAreaId",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "AppUnderTestAreaId",
                table: "Flows");

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "FlowAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "FlowAreas");

            migrationBuilder.AddColumn<int>(
                name: "AppUnderTestAreaId",
                table: "Flows",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flows_AppUnderTestAreaId",
                table: "Flows",
                column: "AppUnderTestAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flows_FlowAreas_AppUnderTestAreaId",
                table: "Flows",
                column: "AppUnderTestAreaId",
                principalTable: "FlowAreas",
                principalColumn: "Id");
        }
    }
}
