using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class _30october : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "Transactions",
                newName: "UpdatedTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Transactions",
                newName: "CreatedTime");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Cards",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "Cards",
                newName: "UpdatedTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Cards",
                newName: "CreatedTime");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "Accounts",
                newName: "xmin");

            migrationBuilder.RenameColumn(
                name: "UpdatedDate",
                table: "Accounts",
                newName: "UpdatedTime");

            migrationBuilder.RenameColumn(
                name: "CreatedDate",
                table: "Accounts",
                newName: "CreatedTime");

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Cards",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Birthday",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Accounts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedTime",
                table: "Transactions",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedTime",
                table: "Transactions",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Cards",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "UpdatedTime",
                table: "Cards",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedTime",
                table: "Cards",
                newName: "CreatedDate");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Accounts",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "UpdatedTime",
                table: "Accounts",
                newName: "UpdatedDate");

            migrationBuilder.RenameColumn(
                name: "CreatedTime",
                table: "Accounts",
                newName: "CreatedDate");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Cards",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Birthday",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Accounts",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }
    }
}
