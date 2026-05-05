using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKCNNET.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsSellerApproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSellerApproved",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 22, 49, 7, 580, DateTimeKind.Local).AddTicks(5419));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSellerApproved",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IsSellerApproved" },
                values: new object[] { new DateTime(2026, 5, 5, 18, 10, 41, 160, DateTimeKind.Local).AddTicks(6060), true });
        }
    }
}
