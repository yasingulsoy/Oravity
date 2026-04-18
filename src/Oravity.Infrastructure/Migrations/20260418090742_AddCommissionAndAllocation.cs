using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionAndAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "PaymentId",
                table: "payment_allocations",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "AllocatedByUserId",
                table: "payment_allocations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ApprovalId",
                table: "payment_allocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BranchId",
                table: "payment_allocations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "InstitutionPaymentId",
                table: "payment_allocations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "payment_allocations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "payment_allocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "payment_allocations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "BonusApplied",
                table: "doctor_commissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraExpenseAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExtraExpenseRate",
                table: "doctor_commissions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FixedFee",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvRate",
                table: "doctor_commissions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LabCostDeducted",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetBaseAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetCommissionAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PeriodMonth",
                table: "doctor_commissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PeriodYear",
                table: "doctor_commissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PosCommissionAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PosCommissionRate",
                table: "doctor_commissions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "TemplateId",
                table: "doctor_commissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TreatmentCostDeducted",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TreatmentPlanCommissionDeducted",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTaxAmount",
                table: "doctor_commissions",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTaxRate",
                table: "doctor_commissions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "allocation_approvals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentPlanItemId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionPaymentId = table.Column<long>(type: "bigint", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    RequestNotes = table.Column<string>(type: "text", nullable: true),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    PaymentAllocationId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_allocation_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_allocation_approvals_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_allocation_approvals_treatment_plan_items_TreatmentPlanItem~",
                        column: x => x.TreatmentPlanItemId,
                        principalTable: "treatment_plan_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branch_targets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
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
                    table.PrimaryKey("PK_branch_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branch_targets_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_commission_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WorkingStyle = table.Column<int>(type: "integer", nullable: false),
                    PaymentType = table.Column<int>(type: "integer", nullable: false),
                    JobStartCalculation = table.Column<int>(type: "integer", nullable: true),
                    FixedFee = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 0m),
                    PrimRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    ClinicTargetEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ClinicTargetBonusRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DoctorTargetEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DoctorTargetBonusRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    InstitutionPayOnInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    DeductTreatmentPlanCommission = table.Column<bool>(type: "boolean", nullable: false),
                    DeductLabCost = table.Column<bool>(type: "boolean", nullable: false),
                    DeductTreatmentCost = table.Column<bool>(type: "boolean", nullable: false),
                    DeductCreditCardCommission = table.Column<bool>(type: "boolean", nullable: false),
                    KdvEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    KdvRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    KdvAppliedPaymentTypes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExtraExpenseEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraExpenseRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WithholdingTaxEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WithholdingTaxRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_doctor_commission_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_targets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
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
                    table.PrimaryKey("PK_doctor_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_targets_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_targets_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "institution_invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    InvoiceNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false, defaultValue: 0m),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaymentReferenceNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TreatmentItemIdsJson = table.Column<string>(type: "text", nullable: true),
                    FollowUpStatus = table.Column<int>(type: "integer", nullable: false),
                    LastFollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextFollowUpDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_institution_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_institution_invoices_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_institution_invoices_institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_institution_invoices_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "commission_template_job_start_prices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentId = table.Column<long>(type: "bigint", nullable: false),
                    PriceType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_template_job_start_prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_template_job_start_prices_doctor_commission_temp~",
                        column: x => x.TemplateId,
                        principalTable: "doctor_commission_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_commission_template_job_start_prices_treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_template_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_doctor_template_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_template_assignments_doctor_commission_templates_Tem~",
                        column: x => x.TemplateId,
                        principalTable: "doctor_commission_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_template_assignments_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "institution_payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_institution_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_institution_payments_institution_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "institution_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_ApprovalId",
                table: "payment_allocations",
                column: "ApprovalId");

            migrationBuilder.CreateIndex(
                name: "ix_payment_alloc_branch_created",
                table: "payment_allocations",
                columns: new[] { "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_alloc_inst_payment",
                table: "payment_allocations",
                column: "InstitutionPaymentId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_commission_period",
                table: "doctor_commissions",
                columns: new[] { "DoctorId", "BranchId", "PeriodYear", "PeriodMonth" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_approvals_branch_status",
                table: "allocation_approvals",
                columns: new[] { "BranchId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_allocation_approvals_item",
                table: "allocation_approvals",
                column: "TreatmentPlanItemId");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_approvals_patient",
                table: "allocation_approvals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_allocation_approvals_public_id",
                table: "allocation_approvals",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branch_targets_branch_ym",
                table: "branch_targets",
                columns: new[] { "BranchId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_template_job_start_prices_TreatmentId",
                table: "commission_template_job_start_prices",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "ix_job_start_prices_template_treatment",
                table: "commission_template_job_start_prices",
                columns: new[] { "TemplateId", "TreatmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_templates_company_name",
                table: "doctor_commission_templates",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_commission_templates_public_id",
                table: "doctor_commission_templates",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_targets_BranchId",
                table: "doctor_targets",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_targets_doctor_branch_ym",
                table: "doctor_targets",
                columns: new[] { "DoctorId", "BranchId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_template_assignments_TemplateId",
                table: "doctor_template_assignments",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_template_assign_doctor_active",
                table: "doctor_template_assignments",
                columns: new[] { "DoctorId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_doctor_template_assign_public_id",
                table: "doctor_template_assignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_institution_invoices_branch_no",
                table: "institution_invoices",
                columns: new[] { "BranchId", "InvoiceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_institution_invoices_branch_status_due",
                table: "institution_invoices",
                columns: new[] { "BranchId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "ix_institution_invoices_institution",
                table: "institution_invoices",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "ix_institution_invoices_patient",
                table: "institution_invoices",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_institution_invoices_public_id",
                table: "institution_invoices",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_institution_payments_invoice",
                table: "institution_payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "ix_institution_payments_public_id",
                table: "institution_payments",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_allocation_approvals_ApprovalId",
                table: "payment_allocations",
                column: "ApprovalId",
                principalTable: "allocation_approvals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_institution_payments_InstitutionPayment~",
                table: "payment_allocations",
                column: "InstitutionPaymentId",
                principalTable: "institution_payments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_allocation_approvals_ApprovalId",
                table: "payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_institution_payments_InstitutionPayment~",
                table: "payment_allocations");

            migrationBuilder.DropTable(
                name: "allocation_approvals");

            migrationBuilder.DropTable(
                name: "branch_targets");

            migrationBuilder.DropTable(
                name: "commission_template_job_start_prices");

            migrationBuilder.DropTable(
                name: "doctor_targets");

            migrationBuilder.DropTable(
                name: "doctor_template_assignments");

            migrationBuilder.DropTable(
                name: "institution_payments");

            migrationBuilder.DropTable(
                name: "doctor_commission_templates");

            migrationBuilder.DropTable(
                name: "institution_invoices");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_ApprovalId",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_payment_alloc_branch_created",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_payment_alloc_inst_payment",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "ix_doctor_commission_period",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "AllocatedByUserId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "ApprovalId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "InstitutionPaymentId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "BonusApplied",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "ExtraExpenseAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "ExtraExpenseRate",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "FixedFee",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "KdvAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "KdvRate",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "LabCostDeducted",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "NetBaseAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "NetCommissionAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "PeriodMonth",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "PeriodYear",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "PosCommissionAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "PosCommissionRate",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "TreatmentCostDeducted",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "TreatmentPlanCommissionDeducted",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxAmount",
                table: "doctor_commissions");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxRate",
                table: "doctor_commissions");

            migrationBuilder.AlterColumn<long>(
                name: "PaymentId",
                table: "payment_allocations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
