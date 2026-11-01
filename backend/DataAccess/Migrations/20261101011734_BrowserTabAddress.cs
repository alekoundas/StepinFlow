using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BrowserTabAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A tab is found by its browser and its address. A title only narrowed which window of
            // the browser, and a browser's title is its front tab's, so a tab behind was never found
            // through one.
            migrationBuilder.Sql("UPDATE FlowAreas SET TitlePattern = '' WHERE Type = 'BROWSER_TAB';");

            // No saved tab was found by its title. One that was keeps the title as its address, where
            // the form shows it and it finds nothing until it is changed.
            migrationBuilder.DropColumn(
                name: "TabMatchOn",
                table: "FlowAreas");

            migrationBuilder.RenameColumn(
                name: "TabMatchValue",
                table: "FlowAreas",
                newName: "TabAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TabAddress",
                table: "FlowAreas",
                newName: "TabMatchValue");

            migrationBuilder.AddColumn<string>(
                name: "TabMatchOn",
                table: "FlowAreas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
