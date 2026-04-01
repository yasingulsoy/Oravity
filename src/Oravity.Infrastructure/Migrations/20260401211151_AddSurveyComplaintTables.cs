using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyComplaintTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    AssignedTo = table.Column<long>(type: "bigint", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SlaDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SurveyResponseId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaints_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_users_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_complaints_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TriggerType = table.Column<int>(type: "integer", nullable: false),
                    TriggerDelayHours = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_templates_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_templates_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "complaint_notes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComplaintId = table.Column<long>(type: "bigint", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    IsInternal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_complaint_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_complaint_notes_complaints_ComplaintId",
                        column: x => x.ComplaintId,
                        principalTable: "complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_complaint_notes_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_questions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    QuestionType = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "jsonb", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_questions_survey_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "survey_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    NpsScore = table.Column<int>(type: "integer", nullable: true),
                    AverageScore = table.Column<decimal>(type: "numeric(3,1)", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_responses_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_responses_survey_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "survey_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_answers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ResponseId = table.Column<long>(type: "bigint", nullable: false),
                    QuestionId = table.Column<long>(type: "bigint", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    AnswerScore = table.Column<int>(type: "integer", nullable: true),
                    AnswerBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    SelectedOption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "survey_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "survey_responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_complaint_notes_ComplaintId",
                table: "complaint_notes",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_complaint_notes_CreatedBy",
                table: "complaint_notes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_AssignedTo",
                table: "complaints",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_BranchId",
                table: "complaints",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_CreatedBy",
                table: "complaints",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_complaints_PatientId",
                table: "complaints",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_company_status",
                table: "complaints",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_complaints_public_id",
                table: "complaints",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_sla_due",
                table: "complaints",
                column: "SlaDueAt");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_QuestionId",
                table: "survey_answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_ResponseId",
                table: "survey_answers",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "ix_survey_questions_template_sort",
                table: "survey_questions",
                columns: new[] { "TemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_AppointmentId",
                table: "survey_responses",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_BranchId",
                table: "survey_responses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_CompanyId",
                table: "survey_responses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_TemplateId",
                table: "survey_responses",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "ix_survey_responses_patient_template",
                table: "survey_responses",
                columns: new[] { "PatientId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "ix_survey_responses_public_id",
                table: "survey_responses",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_survey_responses_token",
                table: "survey_responses",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_templates_CreatedBy",
                table: "survey_templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "ix_survey_templates_company_active",
                table: "survey_templates",
                columns: new[] { "CompanyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_survey_templates_public_id",
                table: "survey_templates",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "complaint_notes");

            migrationBuilder.DropTable(
                name: "survey_answers");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "survey_questions");

            migrationBuilder.DropTable(
                name: "survey_responses");

            migrationBuilder.DropTable(
                name: "survey_templates");
        }
    }
}
