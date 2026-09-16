using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FlowPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "Flows",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Every existing row lands on Guid.Empty, which the unique index below then rejects
            // the moment a database holds two flows. Backfilled as version 4 UUIDs in the lowercase
            // dashed form EF's SQLite provider reads back.
            migrationBuilder.Sql(@"
                UPDATE Flows SET PublicId = lower(
                    substr(hex(randomblob(4)), 1, 8) || '-' ||
                    substr(hex(randomblob(2)), 1, 4) || '-4' ||
                    substr(hex(randomblob(2)), 2, 3) || '-' ||
                    substr('89ab', 1 + (abs(random()) % 4), 1) ||
                    substr(hex(randomblob(2)), 2, 3) || '-' ||
                    substr(hex(randomblob(6)), 1, 12));");

            migrationBuilder.CreateIndex(
                name: "IX_Flows_PublicId",
                table: "Flows",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flows_PublicId",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "Flows");
        }
    }
}
