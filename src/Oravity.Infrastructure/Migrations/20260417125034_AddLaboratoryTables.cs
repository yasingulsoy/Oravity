using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLaboratoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LabDefaultCategory",
                table: "treatments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "laboratories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    WorkingDays = table.Column<string>(type: "jsonb", nullable: true),
                    WorkingHours = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentTerms = table.Column<string>(type: "text", nullable: true),
                    PaymentDays = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_laboratories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_approval_authorities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    CanApprove = table.Column<bool>(type: "boolean", nullable: false),
                    CanReject = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationEnabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_laboratory_approval_authorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_approval_authorities_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_laboratory_approval_authorities_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_branch_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LaboratoryId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_laboratory_branch_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_branch_assignments_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_laboratory_branch_assignments_laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_price_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LaboratoryId = table.Column<long>(type: "bigint", nullable: false),
                    ItemName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    PricingType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EstimatedDeliveryDays = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("PK_laboratory_price_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_price_items_laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_works",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    WorkNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    LaboratoryId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentPlanItemId = table.Column<long>(type: "bigint", nullable: true),
                    WorkType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DeliveryType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToothNumbers = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShadeColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentToLabAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDeliveryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReceivedFromLabAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FittedToPatientAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CostDetails = table.Column<string>(type: "jsonb", nullable: true),
                    DoctorNotes = table.Column<string>(type: "text", nullable: true),
                    LabNotes = table.Column<string>(type: "text", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "text", nullable: true),
                    Attachments = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("PK_laboratory_works", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_works_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_laboratory_works_laboratories_LaboratoryId",
                        column: x => x.LaboratoryId,
                        principalTable: "laboratories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_laboratory_works_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_laboratory_works_treatment_plan_items_TreatmentPlanItemId",
                        column: x => x.TreatmentPlanItemId,
                        principalTable: "treatment_plan_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_laboratory_works_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_work_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkId = table.Column<long>(type: "bigint", nullable: false),
                    OldStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laboratory_work_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_work_history_laboratory_works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "laboratory_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "laboratory_work_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkId = table.Column<long>(type: "bigint", nullable: false),
                    LabPriceItemId = table.Column<long>(type: "bigint", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laboratory_work_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_laboratory_work_items_laboratory_price_items_LabPriceItemId",
                        column: x => x.LabPriceItemId,
                        principalTable: "laboratory_price_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_laboratory_work_items_laboratory_works_WorkId",
                        column: x => x.WorkId,
                        principalTable: "laboratory_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_company_active",
                table: "laboratories",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_company_code",
                table: "laboratories",
                columns: new[] { "CompanyId", "Code" },
                unique: true,
                filter: "\"Code\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_public_id",
                table: "laboratories",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_approval_authorities_BranchId",
                table: "laboratory_approval_authorities",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_approval_user_branch",
                table: "laboratory_approval_authorities",
                columns: new[] { "UserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lab_branch_branch",
                table: "laboratory_branch_assignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_branch_lab",
                table: "laboratory_branch_assignments",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_branch_unique",
                table: "laboratory_branch_assignments",
                columns: new[] { "LaboratoryId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lab_price_items_public_id",
                table: "laboratory_price_items",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lab_prices_category",
                table: "laboratory_price_items",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "ix_lab_prices_lab",
                table: "laboratory_price_items",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_history_work",
                table: "laboratory_work_history",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_work_items_LabPriceItemId",
                table: "laboratory_work_items",
                column: "LabPriceItemId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_work_items_work",
                table: "laboratory_work_items",
                column: "WorkId");

            migrationBuilder.CreateIndex(
                name: "IX_laboratory_works_TreatmentPlanItemId",
                table: "laboratory_works",
                column: "TreatmentPlanItemId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_branch_status",
                table: "laboratory_works",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_company_no",
                table: "laboratory_works",
                columns: new[] { "CompanyId", "WorkNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_dates",
                table: "laboratory_works",
                columns: new[] { "SentToLabAt", "EstimatedDeliveryDate" });

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_doctor",
                table: "laboratory_works",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_lab",
                table: "laboratory_works",
                column: "LaboratoryId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_patient",
                table: "laboratory_works",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_public_id",
                table: "laboratory_works",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lab_works_status",
                table: "laboratory_works",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "laboratory_approval_authorities");

            migrationBuilder.DropTable(
                name: "laboratory_branch_assignments");

            migrationBuilder.DropTable(
                name: "laboratory_work_history");

            migrationBuilder.DropTable(
                name: "laboratory_work_items");

            migrationBuilder.DropTable(
                name: "laboratory_price_items");

            migrationBuilder.DropTable(
                name: "laboratory_works");

            migrationBuilder.DropTable(
                name: "laboratories");

            migrationBuilder.DropColumn(
                name: "LabDefaultCategory",
                table: "treatments");
        }
    }
}
