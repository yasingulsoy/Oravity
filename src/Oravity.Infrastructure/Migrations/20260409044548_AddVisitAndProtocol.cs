using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitAndProtocol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_patient_anamnesis_patient_unique",
                table: "patient_anamnesis");

            migrationBuilder.AddColumn<long>(
                name: "ProtocolId",
                table: "treatment_plans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProtocolId",
                table: "patient_anamnesis",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "protocol_sequences",
                columns: table => new
                {
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastSeq = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_protocol_sequences", x => new { x.BranchId, x.Year });
                    table.ForeignKey(
                        name: "FK_protocol_sequences_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    IsWalkIn = table.Column<bool>(type: "boolean", nullable: false),
                    VisitDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CheckOutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visits_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_visits_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_visits_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "protocols",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VisitId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    ProtocolYear = table.Column<int>(type: "integer", nullable: false),
                    ProtocolSeq = table.Column<int>(type: "integer", nullable: false),
                    ProtocolNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ProtocolType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChiefComplaint = table.Column<string>(type: "text", nullable: true),
                    Diagnosis = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_protocols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_protocols_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_protocols_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_protocols_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_protocols_visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_treatment_plans_ProtocolId",
                table: "treatment_plans",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_anamnesis_ProtocolId",
                table: "patient_anamnesis",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "ix_patient_anamnesis_patient",
                table: "patient_anamnesis",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_protocols_branch_year",
                table: "protocols",
                columns: new[] { "BranchId", "ProtocolYear" });

            migrationBuilder.CreateIndex(
                name: "ix_protocols_doctor_status",
                table: "protocols",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_protocols_no_unique",
                table: "protocols",
                columns: new[] { "BranchId", "ProtocolYear", "ProtocolSeq" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_protocols_patient",
                table: "protocols",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_protocols_public_id",
                table: "protocols",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_protocols_visit",
                table: "protocols",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_visits_AppointmentId",
                table: "visits",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "ix_visits_branch_date",
                table: "visits",
                columns: new[] { "BranchId", "VisitDate" });

            migrationBuilder.CreateIndex(
                name: "ix_visits_patient",
                table: "visits",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "ix_visits_public_id",
                table: "visits",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_patient_anamnesis_protocols_ProtocolId",
                table: "patient_anamnesis",
                column: "ProtocolId",
                principalTable: "protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_treatment_plans_protocols_ProtocolId",
                table: "treatment_plans",
                column: "ProtocolId",
                principalTable: "protocols",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_patient_anamnesis_protocols_ProtocolId",
                table: "patient_anamnesis");

            migrationBuilder.DropForeignKey(
                name: "FK_treatment_plans_protocols_ProtocolId",
                table: "treatment_plans");

            migrationBuilder.DropTable(
                name: "protocol_sequences");

            migrationBuilder.DropTable(
                name: "protocols");

            migrationBuilder.DropTable(
                name: "visits");

            migrationBuilder.DropIndex(
                name: "IX_treatment_plans_ProtocolId",
                table: "treatment_plans");

            migrationBuilder.DropIndex(
                name: "IX_patient_anamnesis_ProtocolId",
                table: "patient_anamnesis");

            migrationBuilder.DropIndex(
                name: "ix_patient_anamnesis_patient",
                table: "patient_anamnesis");

            migrationBuilder.DropColumn(
                name: "ProtocolId",
                table: "treatment_plans");

            migrationBuilder.DropColumn(
                name: "ProtocolId",
                table: "patient_anamnesis");

            migrationBuilder.CreateIndex(
                name: "ix_patient_anamnesis_patient_unique",
                table: "patient_anamnesis",
                column: "PatientId",
                unique: true);
        }
    }
}
