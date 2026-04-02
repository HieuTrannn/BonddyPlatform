using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddDowngradeAndCancelScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DowngradeToPackageId",
                table: "BuddySubscriptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelledAtEndOfCycle",
                table: "BuddySubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_BuddySubscriptions_DowngradeToPackageId",
                table: "BuddySubscriptions",
                column: "DowngradeToPackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_BuddySubscriptions_ServicePackages_DowngradeToPackageId",
                table: "BuddySubscriptions",
                column: "DowngradeToPackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BuddySubscriptions_ServicePackages_DowngradeToPackageId",
                table: "BuddySubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BuddySubscriptions_DowngradeToPackageId",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "DowngradeToPackageId",
                table: "BuddySubscriptions");

            migrationBuilder.DropColumn(
                name: "IsCancelledAtEndOfCycle",
                table: "BuddySubscriptions");
        }
    }
}
