using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSsoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SsoEmail",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SsoProvider",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SsoSubject",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_sso",
                table: "users",
                columns: new[] { "SsoProvider", "SsoSubject" },
                unique: true,
                filter: "\"SsoProvider\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_users_sso",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SsoEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SsoProvider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SsoSubject",
                table: "users");
        }
    }
}
