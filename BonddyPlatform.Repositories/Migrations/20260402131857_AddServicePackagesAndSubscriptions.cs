using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BonddyPlatform.Repositories.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackagesAndSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PricePerMonth = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    HasChatAccess = table.Column<bool>(type: "boolean", nullable: false),
                    HasSearchPriority = table.Column<bool>(type: "boolean", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasProductFeedback = table.Column<bool>(type: "boolean", nullable: false),
                    MaxSlots = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Features = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuddySubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuddyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuddySubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuddySubscriptions_Buddies_BuddyId",
                        column: x => x.BuddyId,
                        principalTable: "Buddies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BuddySubscriptions_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ServicePackages",
                columns: new[] { "Id", "CommissionRate", "CreatedAt", "Currency", "Description", "Features", "HasChatAccess", "HasPrioritySupport", "HasProductFeedback", "HasSearchPriority", "IsActive", "MaxSlots", "Name", "PricePerMonth", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), 0.20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Dành cho buddy mới bắt đầu, muốn thử trải nghiệm nền tảng.", "[\"Profile xu\\u1EA5t hi\\u1EC7n tr\\u00EAn n\\u1EC1n t\\u1EA3ng\",\"Kh\\u00F4ng gi\\u1EDBi h\\u1EA1n booking\",\"Commission 20%\",\"Chat v\\u1EDBi kh\\u00E1ch qua app\"]", true, false, false, false, true, null, "Starter", 49000m, 1, null },
                    { new Guid("00000000-0000-0000-0002-000000000002"), 0.10m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Dành cho buddy muốn tăng thu nhập và xây dựng thương hiệu cá nhân.", "[\"Profile xu\\u1EA5t hi\\u1EC7n tr\\u00EAn n\\u1EC1n t\\u1EA3ng\",\"Kh\\u00F4ng gi\\u1EDBi h\\u1EA1n booking\",\"Commission ch\\u1EC9 10%\",\"Chat v\\u1EDBi kh\\u00E1ch qua app\",\"\\u01AFu ti\\u00EAn xu\\u1EA5t hi\\u1EC7n trong t\\u00ECm ki\\u1EBFm\"]", true, false, false, true, true, null, "Pro", 99000m, 2, null },
                    { new Guid("00000000-0000-0000-0002-000000000003"), 0.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VND", "Chỉ 30 suất. Đặc quyền early adopter dành cho buddy đầu tiên của Bonddy.", "[\"T\\u1EA5t c\\u1EA3 t\\u00EDnh n\\u0103ng c\\u1EE7a Pro\",\"0% commission \\u2014 gi\\u1EEF 100% thu nh\\u1EADp\",\"Xu\\u1EA5t hi\\u1EC7n \\u0111\\u1EA7u ti\\u00EAn trong t\\u00ECm ki\\u1EBFm\",\"H\\u1ED7 tr\\u1EE3 tr\\u1EF1c ti\\u1EBFp t\\u1EEB team Bonddy\",\"G\\u00F3p \\u00FD \\u0111\\u1ECBnh h\\u01B0\\u1EDBng s\\u1EA3n ph\\u1EA9m\",\"Ch\\u1EC9 30 su\\u1EA5t \\u2014 \\u0111\\u00F3ng khi \\u0111\\u1EE7\"]", true, true, true, true, true, 30, "Founder", 149000m, 3, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuddySubscriptions_BuddyId_Status",
                table: "BuddySubscriptions",
                columns: new[] { "BuddyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BuddySubscriptions_EndDate",
                table: "BuddySubscriptions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_BuddySubscriptions_ServicePackageId_Status",
                table: "BuddySubscriptions",
                columns: new[] { "ServicePackageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_IsActive",
                table: "ServicePackages",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Name",
                table: "ServicePackages",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuddySubscriptions");

            migrationBuilder.DropTable(
                name: "ServicePackages");
        }
    }
}
