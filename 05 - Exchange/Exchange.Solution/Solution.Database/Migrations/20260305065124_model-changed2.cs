using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solution.Database.Migrations
{
    /// <inheritdoc />
    public partial class modelchanged2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_ProcessedByUserId",
                table: "Transaction");

            migrationBuilder.DropIndex(
                name: "IX_Transaction_ProcessedByUserId",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "ProcessedByUserId",
                table: "Transaction");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedByUserId",
                table: "Transaction",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_ProcessedByUserId",
                table: "Transaction",
                column: "ProcessedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_ProcessedByUserId",
                table: "Transaction",
                column: "ProcessedByUserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
