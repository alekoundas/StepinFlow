using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EndExecutionAsSuccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndExecutionStatus",
                table: "FlowSteps");

            migrationBuilder.AddColumn<bool>(
                name: "EndExecutionAsSuccess",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndExecutionAsSuccess",
                table: "FlowSteps");

            migrationBuilder.AddColumn<int>(
                name: "EndExecutionStatus",
                table: "FlowSteps",
                type: "INTEGER",
                nullable: true);
        }
    }
}
