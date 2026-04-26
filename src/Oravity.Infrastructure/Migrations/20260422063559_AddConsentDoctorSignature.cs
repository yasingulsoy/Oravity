using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentDoctorSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "doctor_signature_data_base64",
                table: "consent_instances",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "doctor_signature_data_base64",
                table: "consent_instances");
        }
    }
}
