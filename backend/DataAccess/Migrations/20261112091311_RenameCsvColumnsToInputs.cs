using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameCsvColumnsToInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlowInputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecretValue = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowInputs_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Every row comes along with its id. A secret had no value saved before now.
            migrationBuilder.Sql(
                """
                INSERT INTO FlowInputs (Id, Name, DefaultValue, IsSecret, SecretValue, OrderNumber, FlowId, CreatedOn, UpdatedOn)
                SELECT Id, Name, DefaultValue, IsSecret, '', OrderNumber, FlowId, CreatedOn, UpdatedOn
                FROM FlowCsvColumns;
                """);

            migrationBuilder.DropTable(
                name: "FlowCsvColumns");

            // The unique index is new, since its configuration was never applied: a name two inputs
            // of one flow share keeps the first and numbers the rest, so the index can be made.
            migrationBuilder.Sql(
                """
                UPDATE FlowInputs
                SET Name = Name || ' ' || Id
                WHERE Id NOT IN (SELECT MIN(Id) FROM FlowInputs GROUP BY FlowId, Name);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FlowInputs_FlowId_Name",
                table: "FlowInputs",
                columns: new[] { "FlowId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlowCsvColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FlowId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OrderNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlowCsvColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlowCsvColumns_Flows_FlowId",
                        column: x => x.FlowId,
                        principalTable: "Flows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // A secret's saved value has nowhere to go back to.
            migrationBuilder.Sql(
                """
                INSERT INTO FlowCsvColumns (Id, FlowId, CreatedOn, DefaultValue, IsSecret, Name, OrderNumber, UpdatedOn)
                SELECT Id, FlowId, CreatedOn, DefaultValue, IsSecret, Name, OrderNumber, UpdatedOn
                FROM FlowInputs;
                """);

            migrationBuilder.DropTable(
                name: "FlowInputs");

            migrationBuilder.CreateIndex(
                name: "IX_FlowCsvColumns_FlowId",
                table: "FlowCsvColumns",
                column: "FlowId");
        }
    }
}
