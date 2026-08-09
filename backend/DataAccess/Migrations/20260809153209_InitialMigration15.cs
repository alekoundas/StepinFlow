using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClickAnchor",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "Anchor",
                table: "FlowLocations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClickAnchor",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Anchor",
                table: "FlowLocations",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
