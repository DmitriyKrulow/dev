using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace uchet.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyTypeAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedUserId",
                table: "Properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PropertyTypeId",
                table: "Properties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QRCode",
                table: "Properties",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PropertyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Locations",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Главный офис", "Офис 101" },
                    { 2, "Основной склад", "Склад A" },
                    { 3, "Зал для встреч", "Конференц-зал" }
                });

            migrationBuilder.InsertData(
                table: "PropertyTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Электронные устройства", "Электроника" },
                    { 2, "Офисная мебель", "Мебель" },
                    { 3, "Транспортные средства", "Транспорт" }
                });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Edit", "Property" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Delete", "Property", 1 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Import", "Property", 1 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ControllerName", "RoleId" },
                values: new object[] { "Admin", 1 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Index", "Home" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Privacy", "Home" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "ControllerName", "RoleId" },
                values: new object[] { "Property", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Details", "Property", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "ActionName", "RoleId" },
                values: new object[] { "Create", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "ActionName", "RoleId" },
                values: new object[] { "Edit", 2 });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "ActionName", "ControllerName", "RoleId" },
                values: new object[,]
                {
                    { 17, "Index", "Home", 3 },
                    { 18, "Privacy", "Home", 3 },
                    { 19, "Index", "Property", 3 },
                    { 20, "Details", "Property", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Properties_AssignedUserId",
                table: "Properties",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_PropertyTypeId",
                table: "Properties",
                column: "PropertyTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_PropertyTypes_PropertyTypeId",
                table: "Properties",
                column: "PropertyTypeId",
                principalTable: "PropertyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Users_AssignedUserId",
                table: "Properties",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_PropertyTypes_PropertyTypeId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Users_AssignedUserId",
                table: "Properties");

            migrationBuilder.DropTable(
                name: "PropertyTypes");

            migrationBuilder.DropIndex(
                name: "IX_Properties_AssignedUserId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_PropertyTypeId",
                table: "Properties");

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Locations",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PropertyTypeId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "QRCode",
                table: "Properties");

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Index", "Admin" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Index", "Home", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Privacy", "Home", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ControllerName", "RoleId" },
                values: new object[] { "Property", 2 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Details", "Property" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ActionName", "ControllerName" },
                values: new object[] { "Create", "Property" });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "ControllerName", "RoleId" },
                values: new object[] { "Home", 3 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "ActionName", "ControllerName", "RoleId" },
                values: new object[] { "Privacy", "Home", 3 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "ActionName", "RoleId" },
                values: new object[] { "Index", 3 });

            migrationBuilder.UpdateData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "ActionName", "RoleId" },
                values: new object[] { "Details", 3 });
        }
    }
}
