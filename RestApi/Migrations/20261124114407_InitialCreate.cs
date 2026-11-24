using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RestApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ranks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    en_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ru_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    min_xp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ranks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nickname = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    NormalizedNickname = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    Coins = table.Column<int>(type: "integer", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Banned = table.Column<bool>(type: "boolean", nullable: false),
                    LoginType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weapon_categories",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    en_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ru_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    icon = table.Column<string>(type: "varchar", nullable: true),
                    sort = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weapon_stats_prop",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    en_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ru_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_stats_prop", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "weapons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    min_rank_id = table.Column<long>(type: "bigint", nullable: false),
                    is_starter = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    sort = table.Column<int>(type: "integer", nullable: false),
                    preview = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    icon = table.Column<string>(type: "varchar", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapons", x => x.id);
                    table.CheckConstraint("ck_weapons_price_nonnegative", "price >= 0");
                    table.ForeignKey(
                        name: "FK_weapons_ranks_min_rank_id",
                        column: x => x.min_rank_id,
                        principalTable: "ranks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weapons_weapon_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "weapon_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_weapons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    weapon_id = table.Column<long>(type: "bigint", nullable: false),
                    equipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_weapons", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_weapons_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_weapons_weapons_weapon_id",
                        column: x => x.weapon_id,
                        principalTable: "weapons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "weapon_stats",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    weapon_id = table.Column<long>(type: "bigint", nullable: false),
                    stat_prop_id = table.Column<long>(type: "bigint", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weapon_stats", x => x.id);
                    table.ForeignKey(
                        name: "FK_weapon_stats_weapon_stats_prop_stat_prop_id",
                        column: x => x.stat_prop_id,
                        principalTable: "weapon_stats_prop",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_weapon_stats_weapons_weapon_id",
                        column: x => x.weapon_id,
                        principalTable: "weapons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ranks",
                columns: new[] { "id", "code", "en_name", "min_xp", "ru_name" },
                values: new object[,]
                {
                    { 1L, "private", "Private", 0, "Рядовой" },
                    { 2L, "corporal", "Corporal", 100, "Ефрейтор" },
                    { 3L, "junior_sergeant", "Junior Sergeant", 250, "Младший сержант" },
                    { 4L, "sergeant", "Sergeant", 450, "Сержант" },
                    { 5L, "senior_sergeant", "Senior Sergeant", 700, "Старший сержант" },
                    { 6L, "sergeant_major", "Sergeant Major", 1000, "Старшина" },
                    { 7L, "warrant_officer", "Warrant Officer", 1400, "Прапорщик" },
                    { 8L, "senior_warrant_officer", "Senior Warrant Officer", 1900, "Старший прапорщик" },
                    { 9L, "junior_lieutenant", "Junior Lieutenant", 2500, "Младший лейтенант" },
                    { 10L, "lieutenant", "Lieutenant", 3200, "Лейтенант" },
                    { 11L, "senior_lieutenant", "Senior Lieutenant", 4000, "Старший лейтенант" },
                    { 12L, "captain", "Captain", 5000, "Капитан" },
                    { 13L, "major", "Major", 6200, "Майор" },
                    { 14L, "lieutenant_colonel", "Lieutenant Colonel", 7600, "Подполковник" },
                    { 15L, "colonel", "Colonel", 9200, "Полковник" },
                    { 16L, "major_general", "Major General", 11000, "Генерал-майор" },
                    { 17L, "lieutenant_general", "Lieutenant General", 13500, "Генерал-лейтенант" },
                    { 18L, "ace", "Ace", 16500, "Ас" },
                    { 19L, "god", "God", 20000, "Бог" }
                });

            migrationBuilder.InsertData(
                table: "weapon_categories",
                columns: new[] { "id", "code", "en_name", "icon", "ru_name", "sort" },
                values: new object[] { 1L, "pistol", "Pistol", null, "Пистолет", 10 });

            migrationBuilder.InsertData(
                table: "weapon_stats_prop",
                columns: new[] { "id", "code", "en_name", "ru_name" },
                values: new object[,]
                {
                    { 1L, "damage", "Damage", "Урон" },
                    { 2L, "fire_rate", "Fire Rate", "Скорострельность" },
                    { 3L, "recoil_control", "Recoil Control", "Контроль отдачи" },
                    { 4L, "magazine_size", "Magazine Size", "Размер магазина" }
                });

            migrationBuilder.InsertData(
                table: "weapons",
                columns: new[] { "id", "category_id", "code", "icon", "is_starter", "min_rank_id", "name", "preview", "price", "sort" },
                values: new object[] { 1L, 1L, "VEX-9", null, true, 1L, "VEX-9", null, 500, 10 });

            migrationBuilder.InsertData(
                table: "weapons",
                columns: new[] { "id", "category_id", "code", "icon", "min_rank_id", "name", "preview", "price", "sort" },
                values: new object[,]
                {
                    { 2L, 1L, "RAVEX-50", null, 2L, "RAVEX-50", null, 1000, 20 },
                    { 3L, 1L, "NEON-X", null, 3L, "NEON-X", null, 2000, 20 }
                });

            migrationBuilder.InsertData(
                table: "weapon_stats",
                columns: new[] { "id", "stat_prop_id", "value", "weapon_id" },
                values: new object[,]
                {
                    { 1L, 1L, 32, 1L },
                    { 2L, 2L, 82, 1L },
                    { 4L, 3L, 70, 1L },
                    { 5L, 4L, 30, 1L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ranks_code",
                table: "ranks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ranks_min_xp",
                table: "ranks",
                column: "min_xp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_weapons_weapon_id",
                table: "user_weapons",
                column: "weapon_id");

            migrationBuilder.CreateIndex(
                name: "ux_user_weapons_user_id_weapon_id",
                table: "user_weapons",
                columns: new[] { "user_id", "weapon_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedNickname",
                table: "users",
                column: "NormalizedNickname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_categories_code",
                table: "weapon_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_stats_stat_prop_id",
                table: "weapon_stats",
                column: "stat_prop_id");

            migrationBuilder.CreateIndex(
                name: "IX_weapon_stats_weapon_id_stat_prop_id",
                table: "weapon_stats",
                columns: new[] { "weapon_id", "stat_prop_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapon_stats_prop_code",
                table: "weapon_stats_prop",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapons_category_id_sort_code",
                table: "weapons",
                columns: new[] { "category_id", "sort", "code" });

            migrationBuilder.CreateIndex(
                name: "IX_weapons_code",
                table: "weapons",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weapons_min_rank_id",
                table: "weapons",
                column: "min_rank_id");

            migrationBuilder.CreateIndex(
                name: "ux_weapons_one_starter",
                table: "weapons",
                column: "is_starter",
                unique: true,
                filter: "is_starter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_weapons");

            migrationBuilder.DropTable(
                name: "weapon_stats");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "weapon_stats_prop");

            migrationBuilder.DropTable(
                name: "weapons");

            migrationBuilder.DropTable(
                name: "ranks");

            migrationBuilder.DropTable(
                name: "weapon_categories");
        }
    }
}
