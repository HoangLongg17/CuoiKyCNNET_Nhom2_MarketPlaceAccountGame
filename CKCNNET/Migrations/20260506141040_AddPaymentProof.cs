using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CKCNNET.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Purchases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentProofs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerId = table.Column<int>(type: "int", nullable: false),
                    GameAccountId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProofs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentProofs_GameAccounts_GameAccountId",
                        column: x => x.GameAccountId,
                        principalTable: "GameAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentProofs_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 6, 21, 10, 36, 862, DateTimeKind.Local).AddTicks(6197));

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProofs_BuyerId",
                table: "PaymentProofs",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProofs_GameAccountId",
                table: "PaymentProofs",
                column: "GameAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentProofs");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Purchases");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 5, 22, 49, 7, 580, DateTimeKind.Local).AddTicks(5419));
        }
    }
}
