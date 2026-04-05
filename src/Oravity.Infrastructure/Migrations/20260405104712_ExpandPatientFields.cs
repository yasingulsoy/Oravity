using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPatientFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // v_appointment_stats view'u FirstName/LastName kolonlarına bağlı — önce drop et
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_appointment_stats;");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<bool>(
                name: "CampaignOptIn",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "CitizenshipTypeId",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomePhone",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastInstitutionId",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherName",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Occupation",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportNoEncrypted",
                table: "patients",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PregnancyStatus",
                table: "patients",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralPerson",
                table: "patients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReferralSourceId",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SmokingType",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SmsOptIn",
                table: "patients",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkPhone",
                table: "patients",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "citizenship_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizenship_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "patient_emergency_contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_emergency_contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_emergency_contacts_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "referral_sources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_sources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patients_CitizenshipTypeId",
                table: "patients",
                column: "CitizenshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_patients_ReferralSourceId",
                table: "patients",
                column: "ReferralSourceId");

            migrationBuilder.CreateIndex(
                name: "ix_citizenship_types_code",
                table: "citizenship_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_emergency_sort",
                table: "patient_emergency_contacts",
                columns: new[] { "PatientId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referral_sources_code",
                table: "referral_sources",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_patients_citizenship_types_CitizenshipTypeId",
                table: "patients",
                column: "CitizenshipTypeId",
                principalTable: "citizenship_types",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_patients_referral_sources_ReferralSourceId",
                table: "patients",
                column: "ReferralSourceId",
                principalTable: "referral_sources",
                principalColumn: "Id");

            // v_appointment_stats view'unu yeniden oluştur (güncel kolon tipleriyle)
            migrationBuilder.Sql(@"
CREATE OR REPLACE VIEW v_appointment_stats AS
SELECT
    a.""Id""                                                          AS appointment_id,
    a.""BranchId""                                                    AS branch_id,
    b.""CompanyId""                                                   AS company_id,
    DATE(a.""StartTime"" AT TIME ZONE 'UTC')                          AS appointment_date,
    a.""DoctorId""                                                    AS doctor_id,
    u.""FullName""                                                    AS doctor_name,
    a.""PatientId""                                                   AS patient_id,
    CONCAT(pat.""FirstName"", ' ', pat.""LastName"")                  AS patient_name,
    a.""Status""                                                      AS status,
    EXTRACT(EPOCH FROM (a.""EndTime"" - a.""StartTime"")) / 60.0     AS duration_minutes
FROM appointments a
JOIN branches b    ON b.""Id""    = a.""BranchId""
JOIN users    u    ON u.""Id""    = a.""DoctorId""
JOIN patients pat  ON pat.""Id""  = a.""PatientId""
WHERE a.""IsDeleted"" = FALSE;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_patients_citizenship_types_CitizenshipTypeId",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "FK_patients_referral_sources_ReferralSourceId",
                table: "patients");

            migrationBuilder.DropTable(
                name: "citizenship_types");

            migrationBuilder.DropTable(
                name: "patient_emergency_contacts");

            migrationBuilder.DropTable(
                name: "referral_sources");

            migrationBuilder.DropIndex(
                name: "IX_patients_CitizenshipTypeId",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "IX_patients_ReferralSourceId",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "CampaignOptIn",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "CitizenshipTypeId",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "City",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "District",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "FatherName",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "HomePhone",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "LastInstitutionId",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "MotherName",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Nationality",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "Occupation",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "PassportNoEncrypted",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "PregnancyStatus",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "ReferralPerson",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "ReferralSourceId",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "SmokingType",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "SmsOptIn",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "WorkPhone",
                table: "patients");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "patients",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }
    }
}
