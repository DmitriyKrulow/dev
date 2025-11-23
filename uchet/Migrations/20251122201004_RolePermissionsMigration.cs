using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace uchet.Migrations
{
    /// <inheritdoc />
    public partial class RolePermissionsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    ControllerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "ActionName", "ControllerName", "RoleId" },
                values: new object[,]
                {
                    { 1, "Index", "Home", 1 },
                    { 2, "Privacy", "Home", 1 },
                    { 3, "Contact", "Home", 1 },
                    { 4, "Index", "Property", 1 },
                    { 5, "Details", "Property", 1 },
                    { 6, "Create", "Property", 1 },
                    { 7, "Index", "Admin", 1 },
                    { 8, "Index", "Home", 2 },
                    { 9, "Privacy", "Home", 2 },
                    { 10, "Index", "Property", 2 },
                    { 11, "Details", "Property", 2 },
                    { 12, "Create", "Property", 2 },
                    { 13, "Index", "Home", 3 },
                    { 14, "Privacy", "Home", 3 },
                    { 15, "Index", "Property", 3 },
                    { 16, "Details", "Property", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                table: "RolePermissions",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");
        }
    }
}
