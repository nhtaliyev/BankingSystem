using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelsChanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Accounts_AccountId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Cards_CardId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Cards_CardNumber",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "Cards");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "Transactions",
                newName: "ToAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_CardId",
                table: "Transactions",
                newName: "IX_Transactions_ToAccountId");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Transactions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FromAccountId",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Cards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CardNumberEncrypted",
                table: "Cards",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardNumberHash",
                table: "Cards",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CardNumberLast4",
                table: "Cards",
                type: "character(4)",
                fixedLength: true,
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Cards",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Accounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AppUserId1",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "Accounts",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Accounts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FromAccountId",
                table: "Transactions",
                column: "FromAccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_FromSide",
                table: "Transactions",
                sql: "(\"FromAccountId\" IS NULL OR \"FromCardId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_ToSide",
                table: "Transactions",
                sql: "(\"ToAccountId\" IS NULL OR \"ToCardId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardNumberHash",
                table: "Cards",
                column: "CardNumberHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AppUserId1",
                table: "Accounts",
                column: "AppUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId",
                table: "Accounts",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId1",
                table: "Accounts",
                column: "AppUserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Accounts_AccountId",
                table: "Cards",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_FromAccountId",
                table: "Transactions",
                column: "FromAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Accounts_ToAccountId",
                table: "Transactions",
                column: "ToAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId1",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Accounts_AccountId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_FromAccountId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Accounts_ToAccountId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FromAccountId",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_FromSide",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_ToSide",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Cards_CardNumberHash",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_AppUserId1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FromAccountId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CardNumberEncrypted",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardNumberHash",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardNumberLast4",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiryTime",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AppUserId1",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "ToAccountId",
                table: "Transactions",
                newName: "CardId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ToAccountId",
                table: "Transactions",
                newName: "IX_Transactions_CardId");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "Transactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Cards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "Cards",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Accounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "Currency",
                table: "Accounts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardNumber",
                table: "Cards",
                column: "CardNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_AspNetUsers_AppUserId",
                table: "Accounts",
                column: "AppUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Accounts_AccountId",
                table: "Cards",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Cards_CardId",
                table: "Transactions",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }
    }
}
