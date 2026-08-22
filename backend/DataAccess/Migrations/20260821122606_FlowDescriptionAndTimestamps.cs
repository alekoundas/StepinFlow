using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FlowDescriptionAndTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Flows");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "FlowSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "FlowStepImages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Flows",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Flows",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "FlowPoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "FlowAreas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "ExecutionSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Executions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "FlowSteps");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "FlowStepImages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Flows");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "FlowPoints");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "FlowAreas");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "ExecutionSteps");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Executions");

            migrationBuilder.AddColumn<int>(
                name: "OrderNumber",
                table: "Flows",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
