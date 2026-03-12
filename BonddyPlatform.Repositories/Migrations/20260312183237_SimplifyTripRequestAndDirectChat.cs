using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyTripRequestAndDirectChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TripRequests_TripId_Status",
                table: "TripRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TripRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TripRequests");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "TripRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TripRequestId",
                table: "ChatRooms",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_ExpiresAt",
                table: "TripRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_TripId",
                table: "TripRequests",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatRooms_TripRequestId",
                table: "ChatRooms",
                column: "TripRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatRooms_TripRequests_TripRequestId",
                table: "ChatRooms",
                column: "TripRequestId",
                principalTable: "TripRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatRooms_TripRequests_TripRequestId",
                table: "ChatRooms");

            migrationBuilder.DropIndex(
                name: "IX_TripRequests_ExpiresAt",
                table: "TripRequests");

            migrationBuilder.DropIndex(
                name: "IX_TripRequests_TripId",
                table: "TripRequests");

            migrationBuilder.DropIndex(
                name: "IX_ChatRooms_TripRequestId",
                table: "ChatRooms");

            migrationBuilder.DropColumn(
                name: "TripRequestId",
                table: "ChatRooms");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "TripRequests",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TripRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TripRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_TripId_Status",
                table: "TripRequests",
                columns: new[] { "TripId", "Status" });
        }
    }
}
