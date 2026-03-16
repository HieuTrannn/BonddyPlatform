using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class BookingFlowRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_CreatedByUserId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "BookingSurcharges");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BuddyId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CreatedByUserId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BaseRatePerHour",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SurchargesAmount",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TipAmount",
                table: "Bookings",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ScheduledStartTime",
                table: "Bookings",
                newName: "PaymentDeadline");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "Bookings",
                newName: "TripDurationHours");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Bookings",
                newName: "TripRequestId");

            migrationBuilder.AddColumn<Guid>(
                name: "BookingId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BookedAdults",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BookedChildren",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BookedDate",
                table: "Bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "BookedDurationHours",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "BookedStartTime",
                table: "Bookings",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "ChatRoomId",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Bookings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Excludes",
                table: "Bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Includes",
                table: "Bookings",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NoteForCustomer",
                table: "Bookings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TravelerUserId",
                table: "Bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "TripAdults",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TripChildren",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TripNotes",
                table: "Bookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TripStartDate",
                table: "Bookings",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TripStartTime",
                table: "Bookings",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateIndex(
                name: "IX_Messages_BookingId",
                table: "Messages",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BuddyId_Status",
                table: "Bookings",
                columns: new[] { "BuddyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TravelerUserId_Status",
                table: "Bookings",
                columns: new[] { "TravelerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TripRequestId",
                table: "Bookings",
                column: "TripRequestId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TripRequests_TripRequestId",
                table: "Bookings",
                column: "TripRequestId",
                principalTable: "TripRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_TravelerUserId",
                table: "Bookings",
                column: "TravelerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Bookings_BookingId",
                table: "Messages",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TripRequests_TripRequestId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_TravelerUserId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Bookings_BookingId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_BookingId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BuddyId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TravelerUserId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TripRequestId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "BookedAdults",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookedChildren",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookedDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookedDurationHours",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BookedStartTime",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ChatRoomId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Excludes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Includes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "NoteForCustomer",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TravelerUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripAdults",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripChildren",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripNotes",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripStartDate",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripStartTime",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "TripRequestId",
                table: "Bookings",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "TripDurationHours",
                table: "Bookings",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Bookings",
                newName: "TipAmount");

            migrationBuilder.RenameColumn(
                name: "PaymentDeadline",
                table: "Bookings",
                newName: "ScheduledStartTime");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseRatePerHour",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SurchargesAmount",
                table: "Bookings",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "BookingSurcharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSurcharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingSurcharges_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BuddyId",
                table: "Bookings",
                column: "BuddyId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CreatedByUserId",
                table: "Bookings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_CreatedAt",
                table: "Bookings",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingSurcharges_BookingId",
                table: "BookingSurcharges",
                column: "BookingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Trips_TripId",
                table: "Bookings",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_CreatedByUserId",
                table: "Bookings",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
