using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchInvoiceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountPublicId",
                table: "institution_payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUuid",
                table: "institution_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntegratorStatus",
                table: "institution_invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branch_invoice_settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    IntegratorType = table.Column<int>(type: "integer", nullable: false),
                    CompanyVkn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    IntegratorEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IntegratorCompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IntegratorUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IntegratorPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NormalPrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    NormalCounter = table.Column<long>(type: "bigint", nullable: false),
                    EArchivePrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EArchiveCounter = table.Column<long>(type: "bigint", nullable: false),
                    EInvoicePrefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EInvoiceCounter = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_invoice_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_invoice_settings_branch",
                table: "branch_invoice_settings",
                column: "BranchId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_invoice_settings");

            migrationBuilder.DropColumn(
                name: "BankAccountPublicId",
                table: "institution_payments");

            migrationBuilder.DropColumn(
                name: "ExternalUuid",
                table: "institution_invoices");

            migrationBuilder.DropColumn(
                name: "IntegratorStatus",
                table: "institution_invoices");
        }
    }
}
