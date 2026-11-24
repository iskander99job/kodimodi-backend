using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedRemainingWeaponStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "weapon_stats",
                columns: new[] { "id", "stat_prop_id", "value", "weapon_id" },
                values: new object[,]
                {
                    { 6L, 1L, 46, 2L },
                    { 7L, 2L, 57, 2L },
                    { 8L, 3L, 64, 2L },
                    { 9L, 4L, 12, 2L },
                    { 10L, 1L, 28, 3L },
                    { 11L, 2L, 91, 3L },
                    { 12L, 3L, 76, 3L },
                    { 13L, 4L, 24, 3L }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 11L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 12L);

            migrationBuilder.DeleteData(
                table: "weapon_stats",
                keyColumn: "id",
                keyValue: 13L);
        }
    }
}
