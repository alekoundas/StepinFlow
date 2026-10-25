using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ExecutionCommit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommitSha",
                table: "Executions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasUncommittedChanges",
                table: "Executions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Script",
                table: "Executions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommitSha",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "HasUncommittedChanges",
                table: "Executions");

            migrationBuilder.DropColumn(
                name: "Script",
                table: "Executions");
        }
    }
}
