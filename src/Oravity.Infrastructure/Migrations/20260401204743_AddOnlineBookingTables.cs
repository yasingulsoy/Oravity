using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnlineBookingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branch_online_booking_settings",
                columns: table => new
                {
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WidgetSlug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PatientTypeSplit = table.Column<bool>(type: "boolean", nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#2563eb"),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancellationHours = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_online_booking_settings", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_branch_online_booking_settings_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_online_blocks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    StartDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDatetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_online_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_online_blocks_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_online_blocks_users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_online_blocks_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_online_booking_settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    IsOnlineVisible = table.Column<bool>(type: "boolean", nullable: false),
                    SlotDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    AutoApprove = table.Column<bool>(type: "boolean", nullable: false),
                    MaxAdvanceDays = table.Column<int>(type: "integer", nullable: false),
                    BookingNote = table.Column<string>(type: "text", nullable: true),
                    PatientTypeFilter = table.Column<int>(type: "integer", nullable: false),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_online_booking_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_online_booking_settings_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_online_booking_settings_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "doctor_online_schedule",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    IsWorking = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    BreakStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    BreakEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_online_schedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_online_schedule_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_doctor_online_schedule_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "online_booking_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    PatientType = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequestedTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    SlotDuration = table.Column<int>(type: "integer", nullable: false),
                    PatientNote = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AppointmentId = table.Column<long>(type: "bigint", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    PhoneVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    VerificationExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<long>(type: "bigint", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_online_booking_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_online_booking_requests_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_online_booking_requests_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_online_booking_requests_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_online_booking_requests_users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_online_settings_slug",
                table: "branch_online_booking_settings",
                column: "WidgetSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_online_blocks_BranchId",
                table: "doctor_online_blocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_doctor_online_blocks_CreatedBy",
                table: "doctor_online_blocks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_online_blocks_doctor_date",
                table: "doctor_online_blocks",
                columns: new[] { "DoctorId", "BranchId", "StartDatetime" });

            migrationBuilder.CreateIndex(
                name: "IX_doctor_online_booking_settings_BranchId",
                table: "doctor_online_booking_settings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_online_settings_unique",
                table: "doctor_online_booking_settings",
                columns: new[] { "DoctorId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_online_schedule_BranchId",
                table: "doctor_online_schedule",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_online_schedule_unique",
                table: "doctor_online_schedule",
                columns: new[] { "DoctorId", "BranchId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_online_booking_requests_DoctorId",
                table: "online_booking_requests",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_online_booking_requests_PatientId",
                table: "online_booking_requests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_online_booking_requests_ReviewedBy",
                table: "online_booking_requests",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "ix_online_booking_requests_branch_status",
                table: "online_booking_requests",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_online_booking_requests_public_id",
                table: "online_booking_requests",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_online_booking_settings");

            migrationBuilder.DropTable(
                name: "doctor_online_blocks");

            migrationBuilder.DropTable(
                name: "doctor_online_booking_settings");

            migrationBuilder.DropTable(
                name: "doctor_online_schedule");

            migrationBuilder.DropTable(
                name: "online_booking_requests");
        }
    }
}
