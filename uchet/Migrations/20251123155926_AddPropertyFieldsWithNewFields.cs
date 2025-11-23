using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchet.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyFieldsWithNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BalanceDate",
                table: "Properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Properties",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMaintenanceDate",
                table: "Properties",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsagePeriod",
                table: "Properties",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                table: "PropertyTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { 4, "Расходные материалы", "Расходники" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PropertyTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "BalanceDate",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "LastMaintenanceDate",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "UsagePeriod",
                table: "Properties");
        }
    }
}
