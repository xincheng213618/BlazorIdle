using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorIdle.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseStateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PurchaseState",
                table: "Characters",
                type: "TEXT",
                nullable: false,
                defaultValue: "{\"perDayCountsByChar\":{},\"perAccountCounts\":{},\"perCharacterCounts\":{},\"lastDailyReset\":\"" + System.DateOnly.FromDateTime(System.DateTime.Now).ToString("O") + "\"}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PurchaseState",
                table: "Characters");
        }
    }
}
