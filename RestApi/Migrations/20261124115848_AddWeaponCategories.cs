using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestApi.Migrations
{
    /// <inheritdoc />
    public partial class AddWeaponCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "weapon_categories",
                columns: new[] { "id", "code", "en_name", "icon", "ru_name", "sort" },
                values: new object[,]
                {
                    { 2L, "ar", "Assault Rifle", null, "Штурмовая винтовка", 20 },
                    { 3L, "smg", "Submachine Gun", null, "Пистолет-пулемёт", 30 },
                    { 4L, "shotgun", "Shotgun", null, "Дробовик", 40 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 4L);
        }
    }
}
