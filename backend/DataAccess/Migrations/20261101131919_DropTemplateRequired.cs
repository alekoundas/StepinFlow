using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DropTemplateRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A step's templates are alternatives now. A step that required them all finds any one,
            // which two steps, one inside the other's Success, said the other way.
            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "FlowStepTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "FlowStepTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
