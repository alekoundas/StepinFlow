using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocationTop",
                table: "FlowSearchAreas",
                newName: "Width");

            migrationBuilder.RenameColumn(
                name: "LocationToRight",
                table: "FlowSearchAreas",
                newName: "LocationY");

            migrationBuilder.RenameColumn(
                name: "LocationToBottom",
                table: "FlowSearchAreas",
                newName: "LocationX");

            migrationBuilder.RenameColumn(
                name: "LocationLeft",
                table: "FlowSearchAreas",
                newName: "Height");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationName",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FlowSearchAreaType",
                table: "FlowSearchAreas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MonitorIndex",
                table: "FlowSearchAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationName",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "FlowSearchAreaType",
                table: "FlowSearchAreas");

            migrationBuilder.DropColumn(
                name: "MonitorIndex",
                table: "FlowSearchAreas");

            migrationBuilder.RenameColumn(
                name: "Width",
                table: "FlowSearchAreas",
                newName: "LocationTop");

            migrationBuilder.RenameColumn(
                name: "LocationY",
                table: "FlowSearchAreas",
                newName: "LocationToRight");

            migrationBuilder.RenameColumn(
                name: "LocationX",
                table: "FlowSearchAreas",
                newName: "LocationToBottom");

            migrationBuilder.RenameColumn(
                name: "Height",
                table: "FlowSearchAreas",
                newName: "LocationLeft");
        }
    }
}
