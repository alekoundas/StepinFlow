using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameUseClientAreaToIgnoreWindowBorders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UseClientArea",
                table: "FlowAreas",
                newName: "IgnoreWindowBorders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgnoreWindowBorders",
                table: "FlowAreas",
                newName: "UseClientArea");
        }
    }
}
