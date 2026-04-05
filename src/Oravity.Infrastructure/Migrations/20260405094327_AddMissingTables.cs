using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BackupCodesAt",
                table: "user_2fa_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TotpVerifiedAt",
                table: "user_2fa_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "reference_price_items",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupCodesAt",
                table: "user_2fa_settings");

            migrationBuilder.DropColumn(
                name: "TotpVerifiedAt",
                table: "user_2fa_settings");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "reference_price_items");
        }
    }
}
