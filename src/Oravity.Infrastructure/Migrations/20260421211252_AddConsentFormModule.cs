using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentFormModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consent_form_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "TR"),
                    Version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "1.0"),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    CheckboxesJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    AppliesToAllTreatments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TreatmentCategoryIdsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ShowDentalChart = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowTreatmentTable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequireDoctorSignature = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_form_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "consent_instances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    TreatmentPlanId = table.Column<long>(type: "bigint", nullable: false),
                    FormTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    ConsentCode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ItemPublicIdsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    DeliveryMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "qr"),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    QrToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    QrTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SmsToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SmsTokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignerIp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SignerDevice = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SignatureDataBase64 = table.Column<string>(type: "text", nullable: true),
                    CheckboxAnswersJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consent_instances_consent_form_templates_FormTemplateId",
                        column: x => x.FormTemplateId,
                        principalTable: "consent_form_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consent_instances_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_consent_instances_treatment_plans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "treatment_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_consent_template_company_active",
                table: "consent_form_templates",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_consent_template_company_code",
                table: "consent_form_templates",
                columns: new[] { "CompanyId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_consent_instances_FormTemplateId",
                table: "consent_instances",
                column: "FormTemplateId");

            migrationBuilder.CreateIndex(
                name: "ix_consent_instance_code",
                table: "consent_instances",
                column: "ConsentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_consent_instance_patient_status",
                table: "consent_instances",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_consent_instance_plan",
                table: "consent_instances",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "ix_consent_instance_qr_token",
                table: "consent_instances",
                column: "QrToken");

            migrationBuilder.CreateIndex(
                name: "ix_consent_instance_sms_token",
                table: "consent_instances",
                column: "SmsToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consent_instances");

            migrationBuilder.DropTable(
                name: "consent_form_templates");
        }
    }
}
