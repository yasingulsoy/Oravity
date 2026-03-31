using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treatment_plans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_treatment_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_plans_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treatment_plans_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treatment_plans_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "treatment_plan_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentId = table.Column<long>(type: "bigint", nullable: false),
                    ToothNumber = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ToothSurfaces = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BodyRegionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    FinalPrice = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treatment_plan_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_treatment_plan_items_treatment_plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "treatment_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_treatment_plan_items_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treatment_plan_items_DoctorId",
                table: "treatment_plan_items",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_plan",
                table: "treatment_plan_items",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_public_id",
                table: "treatment_plan_items",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_status",
                table: "treatment_plan_items",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plan_items_treatment",
                table: "treatment_plan_items",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_plans_BranchId",
                table: "treatment_plans",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_doctor",
                table: "treatment_plans",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_patient",
                table: "treatment_plans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_public_id",
                table: "treatment_plans",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_status",
                table: "treatment_plans",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treatment_plan_items");

            migrationBuilder.DropTable(
                name: "treatment_plans");
        }
    }
}
