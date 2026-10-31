using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixAccountUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId1",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AppUserId1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "AppUserId1",
                table: "Accounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppUserId1",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AppUserId1",
                table: "Accounts",
                column: "AppUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId1",
                table: "Accounts",
                column: "AppUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
