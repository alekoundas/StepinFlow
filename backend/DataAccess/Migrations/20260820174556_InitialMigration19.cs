using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoopOnMultipleFindings",
                table: "FlowSteps");

            migrationBuilder.RenameColumn(
                name: "ImageSearchMode",
                table: "FlowSteps",
                newName: "SearchMode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SearchMode",
                table: "FlowSteps",
                newName: "ImageSearchMode");

            migrationBuilder.AddColumn<bool>(
                name: "LoopOnMultipleFindings",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
