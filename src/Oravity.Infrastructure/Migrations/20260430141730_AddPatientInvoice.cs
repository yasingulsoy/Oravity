using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "EARCHIVE"),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    KdvRate = table.Column<decimal>(type: "numeric(5,4)", nullable: false, defaultValue: 0.10m),
                    KdvAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 0m),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 0m),
                    RecipientType = table.Column<int>(type: "integer", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecipientTcNo = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    RecipientVkn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    RecipientTaxOffice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TreatmentItemIdsJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ExternalUuid = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IntegratorStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_invoices_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_invoices_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patient_invoices_branch_no",
                table: "patient_invoices",
                columns: new[] { "BranchId", "InvoiceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_invoices_branch_status",
                table: "patient_invoices",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_invoices_patient",
                table: "patient_invoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_patient_invoices_public_id",
                table: "patient_invoices",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_invoices");
        }
    }
}
