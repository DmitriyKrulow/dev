using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchet.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryNumberToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InventoryNumber",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryNumber",
                table: "Properties");
        }
    }
}
