using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEInvoiceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "einvoice_integrations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Vkn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Config = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    AutoSendEArchive = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsTestMode = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "einvoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceType = table.Column<int>(type: "integer", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    EInvoiceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Series = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "GBS"),
                    Sequence = table.Column<int>(type: "integer", nullable: true),
                    ReceiverType = table.Column<int>(type: "integer", nullable: false),
                    ReceiverName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ReceiverTc = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    ReceiverVkn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ReceiverTaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReceiverAddress = table.Column<string>(type: "text", nullable: true),
                    ReceiverEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    TaxableAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    TaxAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    LanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "tr"),
                    GibUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GibStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GibResponse = table.Column<string>(type: "jsonb", nullable: true),
                    SentToGibAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PdfPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SentToReceiverAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_einvoices_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_einvoices_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "einvoice_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,3)", nullable: false, defaultValue: 1m),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Adet"),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    DiscountAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    TaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 10m),
                    TaxAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_einvoice_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_einvoice_items_einvoices_EInvoiceId",
                        column: x => x.EInvoiceId,
                        principalTable: "einvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_einvoice_integrations_company",
                table: "einvoice_integrations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_einvoice_items_EInvoiceId",
                table: "einvoice_items",
                column: "EInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_einvoices_CreatedBy",
                table: "einvoices",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_einvoices_PaymentId",
                table: "einvoices",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "ix_einvoices_company_date",
                table: "einvoices",
                columns: new[] { "CompanyId", "InvoiceDate" });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "einvoice_integrations");

            migrationBuilder.DropTable(
                name: "einvoice_items");

            migrationBuilder.DropTable(
                name: "einvoices");
        }
    }
}
