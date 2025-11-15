using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorIdle.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillLearningAndEquipmentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProfessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxHp = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackRateAPS = table.Column<double>(type: "REAL", nullable: false),
                    DamagePerAttack = table.Column<int>(type: "INTEGER", nullable: false),
                    HastePercent = table.Column<double>(type: "REAL", nullable: false),
                    SpecialIntervalSec = table.Column<double>(type: "REAL", nullable: false),
                    SpecialDamage = table.Column<int>(type: "INTEGER", nullable: false),
                    CritChancePercent = table.Column<double>(type: "REAL", nullable: false),
                    CritMultiplier = table.Column<double>(type: "REAL", nullable: false),
                    VariancePct = table.Column<double>(type: "REAL", nullable: false),
                    ReviveSec = table.Column<double>(type: "REAL", nullable: false),
                    Professions = table.Column<string>(type: "TEXT", nullable: false),
                    ActiveCombatProfessionId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "warrior"),
                    LearnedSkills = table.Column<string>(type: "TEXT", nullable: false),
                    EquippedSkillsByProfession = table.Column<string>(type: "TEXT", nullable: false),
                    Inventory = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxCharacterSlots = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    UsedCharacterSlots = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId",
                table: "Characters",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_UserId_Name",
                table: "Characters",
                columns: new[] { "UserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
