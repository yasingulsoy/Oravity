using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrencySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patient_portal_accounts_patient",
                table: "patient_portal_accounts");

            migrationBuilder.DropIndex(
                name: "ix_einvoices_einvoice_no",
                table: "einvoices");

            migrationBuilder.DropIndex(
                name: "ix_einvoices_gib_uuid",
                table: "einvoices");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceBaseAmount",
                table: "treatment_plan_items",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PriceCurrency",
                table: "treatment_plan_items",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExchangeRate",
                table: "treatment_plan_items",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "RateLockType",
                table: "treatment_plan_items",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "RateLockedAt",
                table: "treatment_plan_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RateLockedValue",
                table: "treatment_plan_items",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "payments",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "payments",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "einvoices",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "einvoices",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "doctor_commissions",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "doctor_commissions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "TRY");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "doctor_commissions",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "exchange_rate_differences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OriginalRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    ActualRate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    ForeignAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DifferenceAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DifferenceType = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rate_differences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exchange_rate_differences_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exchange_rate_differences_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rate_overrides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rate_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exchange_rate_overrides_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_exchange_rate_overrides_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ToCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    RateDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_accounts_patient",
                table: "patient_portal_accounts",
                column: "PatientId",
                unique: true,
                filter: "\"PatientId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_einvoices_einvoice_no",
                table: "einvoices",
                column: "EInvoiceNo",
                unique: true,
                filter: "\"EInvoiceNo\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_einvoices_gib_uuid",
                table: "einvoices",
                column: "GibUuid",
                unique: true,
                filter: "\"GibUuid\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_differences_BranchId",
                table: "exchange_rate_differences",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_differences_CompanyId",
                table: "exchange_rate_differences",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_diffs_recorded",
                table: "exchange_rate_differences",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_diffs_source",
                table: "exchange_rate_differences",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_overrides_BranchId",
                table: "exchange_rate_overrides",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rate_overrides_lookup",
                table: "exchange_rate_overrides",
                columns: new[] { "CompanyId", "BranchId", "Currency", "ValidFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_date",
                table: "exchange_rates",
                column: "RateDate");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_rates_from_to_date",
                table: "exchange_rates",
                columns: new[] { "FromCurrency", "ToCurrency", "RateDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange_rate_differences");

            migrationBuilder.DropTable(
                name: "exchange_rate_overrides");

            migrationBuilder.DropTable(
                name: "exchange_rates");

            migrationBuilder.DropIndex(
                name: "ix_patient_portal_accounts_patient",
                table: "patient_portal_accounts");

            migrationBuilder.DropIndex(
                name: "ix_einvoices_einvoice_no",
                table: "einvoices");

            migrationBuilder.DropIndex(
                name: "ix_einvoices_gib_uuid",
                table: "einvoices");

            migrationBuilder.DropColumn(
                name: "PriceBaseAmount",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "PriceCurrency",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "PriceExchangeRate",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "RateLockType",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "RateLockedAt",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "RateLockedValue",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "einvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "einvoices");

            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "doctor_commissions");

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_accounts_patient",
                table: "patient_portal_accounts",
                column: "PatientId",
                unique: true,
                filter: "patient_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_einvoices_einvoice_no",
                table: "einvoices",
                column: "EInvoiceNo",
                unique: true,
                filter: "einvoice_no IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_einvoices_gib_uuid",
                table: "einvoices",
                column: "GibUuid",
                unique: true,
                filter: "gib_uuid IS NOT NULL");
        }
    }
}
