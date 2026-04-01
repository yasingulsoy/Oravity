using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientPortalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "patient_portal_accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsPhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerificationToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneVerificationCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    VerificationExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreferredLanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "tr"),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_portal_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_portal_accounts_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "patient_portal_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patient_portal_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_patient_portal_sessions_patient_portal_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "patient_portal_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_accounts_email",
                table: "patient_portal_accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_accounts_patient",
                table: "patient_portal_accounts",
                column: "PatientId",
                unique: true,
                filter: "patient_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_accounts_public_id",
                table: "patient_portal_accounts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_sessions_account_active",
                table: "patient_portal_sessions",
                columns: new[] { "AccountId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "ix_patient_portal_sessions_token",
                table: "patient_portal_sessions",
                column: "TokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "patient_portal_sessions");

            migrationBuilder.DropTable(
                name: "patient_portal_accounts");
        }
    }
}
