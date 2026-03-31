using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientRecordTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_anamnesis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    BloodType = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    IsPregnant = table.Column<bool>(type: "boolean", nullable: false),
                    IsBreastfeeding = table.Column<bool>(type: "boolean", nullable: false),
                    HasDiabetes = table.Column<bool>(type: "boolean", nullable: false),
                    HasHypertension = table.Column<bool>(type: "boolean", nullable: false),
                    HasHeartDisease = table.Column<bool>(type: "boolean", nullable: false),
                    HasPacemaker = table.Column<bool>(type: "boolean", nullable: false),
                    HasAsthma = table.Column<bool>(type: "boolean", nullable: false),
                    HasEpilepsy = table.Column<bool>(type: "boolean", nullable: false),
                    HasKidneyDisease = table.Column<bool>(type: "boolean", nullable: false),
                    HasLiverDisease = table.Column<bool>(type: "boolean", nullable: false),
                    HasHiv = table.Column<bool>(type: "boolean", nullable: false),
                    HasHepatitisB = table.Column<bool>(type: "boolean", nullable: false),
                    HasHepatitisC = table.Column<bool>(type: "boolean", nullable: false),
                    OtherSystemicDiseases = table.Column<string>(type: "text", nullable: true),
                    LocalAnesthesiaAllergy = table.Column<bool>(type: "boolean", nullable: false),
                    LocalAnesthesiaAllergyNote = table.Column<string>(type: "text", nullable: true),
                    BleedingTendency = table.Column<bool>(type: "boolean", nullable: false),
                    OnAnticoagulant = table.Column<bool>(type: "boolean", nullable: false),
                    AnticoagulantDrug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BisphosphonateUse = table.Column<bool>(type: "boolean", nullable: false),
                    HasPenicillinAllergy = table.Column<bool>(type: "boolean", nullable: false),
                    HasAspirinAllergy = table.Column<bool>(type: "boolean", nullable: false),
                    HasLatexAllergy = table.Column<bool>(type: "boolean", nullable: false),
                    OtherAllergies = table.Column<string>(type: "text", nullable: true),
                    PreviousSurgeries = table.Column<string>(type: "text", nullable: true),
                    BrushingFrequency = table.Column<int>(type: "integer", nullable: true),
                    UsesFloss = table.Column<bool>(type: "boolean", nullable: false),
                    SmokingStatus = table.Column<int>(type: "integer", nullable: true),
                    SmokingAmount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AlcoholUse = table.Column<int>(type: "integer", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "text", nullable: true),
                    FilledBy = table.Column<long>(type: "bigint", nullable: false),
                    FilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_anamnesis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_anamnesis_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_anamnesis_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_anamnesis_users_FilledBy",
                        column: x => x.FilledBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_anamnesis_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_files",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<int>(type: "integer", nullable: true),
                    FileExt = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadedBy = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_files_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_files_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_files_users_UploadedBy",
                        column: x => x.UploadedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_medications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    DrugName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Dose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AddedBy = table.Column<long>(type: "bigint", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_medications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_medications_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_medications_users_AddedBy",
                        column: x => x.AddedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_notes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    NoteUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_notes_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_notes_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_notes_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_patient_notes_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_patient_anamnesis_BranchId",
                table: "patient_anamnesis",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_anamnesis_FilledBy",
                table: "patient_anamnesis",
                column: "FilledBy");

            migrationBuilder.CreateIndex(
                name: "IX_patient_anamnesis_UpdatedBy",
                table: "patient_anamnesis",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "ix_patient_anamnesis_patient_unique",
                table: "patient_anamnesis",
                column: "PatientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_anamnesis_public_id",
                table: "patient_anamnesis",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_files_BranchId",
                table: "patient_files",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_files_UploadedBy",
                table: "patient_files",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "ix_patient_files_patient_type",
                table: "patient_files",
                columns: new[] { "PatientId", "FileType" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_files_public_id",
                table: "patient_files",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patient_medications_AddedBy",
                table: "patient_medications",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "ix_patient_medications_patient",
                table: "patient_medications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_notes_BranchId",
                table: "patient_notes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_patient_notes_CreatedBy",
                table: "patient_notes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_patient_notes_UpdatedBy",
                table: "patient_notes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "ix_patient_notes_patient_pinned",
                table: "patient_notes",
                columns: new[] { "PatientId", "IsPinned", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_notes_public_id",
                table: "patient_notes",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_anamnesis");

            migrationBuilder.DropTable(
                name: "patient_files");

            migrationBuilder.DropTable(
                name: "patient_medications");

            migrationBuilder.DropTable(
                name: "patient_notes");
        }
    }
}
