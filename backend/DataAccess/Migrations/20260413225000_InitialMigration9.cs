using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonitorName",
                table: "FlowSearchAreas",
                newName: "MonitorUniqueId");

            migrationBuilder.RenameColumn(
                name: "ApplicationName",
                table: "FlowSearchAreas",
                newName: "AppWindowName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonitorUniqueId",
                table: "FlowSearchAreas",
                newName: "MonitorName");

            migrationBuilder.RenameColumn(
                name: "AppWindowName",
                table: "FlowSearchAreas",
                newName: "ApplicationName");
        }
    }
}
