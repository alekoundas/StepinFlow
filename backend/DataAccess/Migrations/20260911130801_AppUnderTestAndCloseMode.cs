using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AppUnderTestAndCloseMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppCloseMode",
                table: "Flows",
                type: "TEXT",
                nullable: false,
                defaultValue: "LEAVE");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flows_FlowAreas_AppUnderTestAreaId",
                table: "Flows");

            migrationBuilder.DropIndex(
                name: "IX_Flows_AppUnderTestAreaId",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "AppCloseMode",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "AppUnderTestAreaId",
                table: "Flows");
        }
    }
}
