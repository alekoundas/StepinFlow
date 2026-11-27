using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DeletesWaitForCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flows_PublicId",
                table: "Flows");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Flows",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Executions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Flows_PublicId",
                table: "Flows",
                column: "PublicId",
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flows_PublicId",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Executions");

            migrationBuilder.CreateIndex(
                name: "IX_Flows_PublicId",
                table: "Flows",
                column: "PublicId",
                unique: true);
        }
    }
}
