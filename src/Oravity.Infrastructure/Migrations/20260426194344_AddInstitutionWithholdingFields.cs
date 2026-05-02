using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionWithholdingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEInvoiceTaxpayer",
                table: "institutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "WithholdingApplies",
                table: "institutions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WithholdingCode",
                table: "institutions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingDenominator",
                table: "institutions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingNumerator",
                table: "institutions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvAmount",
                table: "institution_invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvRate",
                table: "institution_invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetPayableAmount",
                table: "institution_invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingAmount",
                table: "institution_invoices",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "WithholdingApplies",
                table: "institution_invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WithholdingCode",
                table: "institution_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingDenominator",
                table: "institution_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WithholdingNumerator",
                table: "institution_invoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEInvoiceTaxpayer",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "WithholdingApplies",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "WithholdingCode",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "WithholdingDenominator",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "WithholdingNumerator",
                table: "institutions");

            migrationBuilder.DropColumn(
                name: "KdvAmount",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "KdvRate",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "NetPayableAmount",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingAmount",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingApplies",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingCode",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingDenominator",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "WithholdingNumerator",
                table: "institution_invoices");
        }
    }
}
