using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonitorIndex",
                table: "FlowSearchAreas",
                newName: "MonitorName");

            migrationBuilder.RenameColumn(
                name: "FlowSearchAreaType",
                table: "FlowSearchAreas",
                newName: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "FlowSearchAreas",
                newName: "FlowSearchAreaType");

            migrationBuilder.RenameColumn(
                name: "MonitorName",
                table: "FlowSearchAreas",
                newName: "MonitorIndex");
        }
    }
}
