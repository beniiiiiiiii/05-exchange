using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solution.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelchanged : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRate_User_CreatedByUserId",
                table: "ExchangeRate");

            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRate_User_ModifiedByUserId",
                table: "ExchangeRate");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRate_CreatedByUserId",
                table: "ExchangeRate");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRate_ModifiedByUserId",
                table: "ExchangeRate");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ExchangeRate");

            migrationBuilder.DropColumn(
                name: "ModifiedByUserId",
                table: "ExchangeRate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ExchangeRate",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByUserId",
                table: "ExchangeRate",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRate_CreatedByUserId",
                table: "ExchangeRate",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRate_ModifiedByUserId",
                table: "ExchangeRate",
                column: "ModifiedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRate_User_CreatedByUserId",
                table: "ExchangeRate",
                column: "CreatedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRate_User_ModifiedByUserId",
                table: "ExchangeRate",
                column: "ModifiedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
