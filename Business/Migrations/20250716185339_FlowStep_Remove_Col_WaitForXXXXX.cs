using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Migrations
{
    /// <inheritdoc />
    public partial class FlowStep_Remove_Col_WaitForXXXXX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaitForHours",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "WaitForMilliseconds",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "WaitForMinutes",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "WaitForSeconds",
                table: "FlowSteps");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WaitForHours",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WaitForMilliseconds",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WaitForMinutes",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WaitForSeconds",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
