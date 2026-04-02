using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "BuddySubscriptions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "BuddySubscriptions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExternalPaymentRef",
                table: "BuddySubscriptions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "BuddySubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "BuddySubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BuddySubscriptions_ExternalPaymentRef",
                table: "BuddySubscriptions",
                column: "ExternalPaymentRef");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BuddySubscriptions_ExternalPaymentRef",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "ExternalPaymentRef",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "BuddySubscriptions");
        }
    }
}
