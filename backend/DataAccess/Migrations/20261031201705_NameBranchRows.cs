using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NameBranchRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // An import or a recording saved its branch rows with no name, and the tree showed them blank.
            migrationBuilder.Sql(@"UPDATE FlowSteps SET Name = 'Success' WHERE FlowStepType = 'SUCCESS' AND Name = '';");

            migrationBuilder.Sql(@"UPDATE FlowSteps SET Name = 'Failure' WHERE FlowStepType = 'FAILURE' AND Name = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Which rows had no name is not kept, and a named branch is what a form always made.
        }
    }
}
