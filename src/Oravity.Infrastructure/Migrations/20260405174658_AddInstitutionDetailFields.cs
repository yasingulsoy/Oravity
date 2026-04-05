using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "institutions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "institutions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "institutions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "institutions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "institutions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountRate",
                table: "institutions",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "institutions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "institutions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "institutions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentDays",
                table: "institutions",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "institutions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "institutions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "institutions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "institutions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "institutions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_institutions_code",
                table: "institutions",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_institutions_code",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "City",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "DiscountRate",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "District",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "PaymentDays",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "institutions");
        }
    }
}
