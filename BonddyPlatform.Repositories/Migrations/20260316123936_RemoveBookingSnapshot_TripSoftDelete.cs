using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBookingSnapshot_TripSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripAdults",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripChildren",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TripDurationHours",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Bookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<int>(
                name: "TripDurationHours",
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
        }
    }
}
