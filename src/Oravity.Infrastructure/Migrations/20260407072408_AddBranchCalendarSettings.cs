using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCalendarSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_calendar_settings",
                columns: table => new
                {
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    SlotIntervalMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    DayStartHour = table.Column<int>(type: "integer", nullable: false, defaultValue: 8),
                    DayEndHour = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_calendar_settings", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_branch_calendar_settings_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_calendar_settings");
        }
    }
}
