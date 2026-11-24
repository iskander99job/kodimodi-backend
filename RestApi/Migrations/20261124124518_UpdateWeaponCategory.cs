using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWeaponCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 1L,
                column: "ru_name",
                value: "Pistol");

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "en_name", "ru_name" },
                values: new object[] { "AR", "AR" });

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "en_name", "ru_name" },
                values: new object[] { "SMG", "SMG" });

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 4L,
                column: "ru_name",
                value: "Shotgun");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 1L,
                column: "ru_name",
                value: "Пистолет");

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 2L,
                columns: new[] { "en_name", "ru_name" },
                values: new object[] { "Assault Rifle", "Штурмовая винтовка" });

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 3L,
                columns: new[] { "en_name", "ru_name" },
                values: new object[] { "Submachine Gun", "Пистолет-пулемёт" });

            migrationBuilder.UpdateData(
                table: "weapon_categories",
                keyColumn: "id",
                keyValue: 4L,
                column: "ru_name",
                value: "Дробовик");
        }
    }
}
