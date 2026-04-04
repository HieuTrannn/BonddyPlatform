using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOsOrderCodeToBuddySubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PayOsOrderCode",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PayOsOrderCode",
                table: "BuddySubscriptions",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayOsOrderCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayOsOrderCode",
                table: "BuddySubscriptions");
        }
    }
}
