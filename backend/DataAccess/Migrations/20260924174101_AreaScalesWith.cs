using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AreaScalesWith : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MonitorUniqueId",
                table: "FlowAreas",
                newName: "MonitorDeviceName");

            migrationBuilder.AddColumn<int>(
                name: "AuthoredDpi",
                table: "FlowAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ScalesWith",
                table: "FlowAreas",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthoredDpi",
                table: "FlowAreas");

            migrationBuilder.DropColumn(
                name: "ScalesWith",
                table: "FlowAreas");

            migrationBuilder.RenameColumn(
                name: "MonitorDeviceName",
                table: "FlowAreas",
                newName: "MonitorUniqueId");
        }
    }
}
