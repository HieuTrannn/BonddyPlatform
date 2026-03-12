using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class TripRefactorAndTripRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_City_StartTime",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "BudgetMax",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "BudgetMin",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "MeetingPoint",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "GroupSize",
                table: "Trips",
                newName: "DurationHours");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "Trips",
                newName: "Children");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "StartTime",
                table: "Trips",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Adults",
                table: "Trips",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguages",
                table: "Trips",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Trips",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateTable(
                name: "TripRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuddyId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsInboxOnly = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripRequests_Buddies_BuddyId",
                        column: x => x.BuddyId,
                        principalTable: "Buddies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripRequests_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_City_StartDate",
                table: "Trips",
                columns: new[] { "City", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status",
                table: "Trips",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_BuddyId",
                table: "TripRequests",
                column: "BuddyId");

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_TripId_BuddyId",
                table: "TripRequests",
                columns: new[] { "TripId", "BuddyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TripRequests_TripId_Status",
                table: "TripRequests",
                columns: new[] { "TripId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TripRequests");

            migrationBuilder.DropIndex(
                name: "IX_Trips_City_StartDate",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Status",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Adults",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PreferredLanguages",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "DurationHours",
                table: "Trips",
                newName: "GroupSize");

            migrationBuilder.RenameColumn(
                name: "Children",
                table: "Trips",
                newName: "DurationMinutes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartTime",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMax",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetMin",
                table: "Trips",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingPoint",
                table: "Trips",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Trips",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_City_StartTime",
                table: "Trips",
                columns: new[] { "City", "StartTime" });
        }
    }
}
