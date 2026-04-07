using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentSchedulingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_appointments_slot_unique",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "appointments",
                newName: "StatusId");

            migrationBuilder.AddColumn<string>(
                name: "CalendarColor",
                table: "users",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultAppointmentDuration",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SpecializationId",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "PatientId",
                table: "appointments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<string>(
                name: "AppointmentNo",
                table: "appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppointmentTypeId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingSource",
                table: "appointments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<DateTime>(
                name: "EnteredRoomAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNewPatient",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUrgent",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftClinicAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftRoomAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PatientArrivedAt",
                table: "appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendSmsNotification",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SpecializationId",
                table: "appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "appointment_statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TitleColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#3598DC"),
                    ContainerColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#4c4cff"),
                    BorderColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#3333ff"),
                    TextColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#ffffff"),
                    ClassName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "cl-white"),
                    IsPatientStatus = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedNextStatusIds = table.Column<string>(type: "text", nullable: false, defaultValue: "[]"),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "appointment_types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false, defaultValue: "#3598DC"),
                    IsPatientAppointment = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "doctor_on_call_settings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Monday = table.Column<bool>(type: "boolean", nullable: false),
                    Tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    Wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    Thursday = table.Column<bool>(type: "boolean", nullable: false),
                    Friday = table.Column<bool>(type: "boolean", nullable: false),
                    Saturday = table.Column<bool>(type: "boolean", nullable: false),
                    Sunday = table.Column<bool>(type: "boolean", nullable: false),
                    PeriodType = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_on_call_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_on_call_settings_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_doctor_on_call_settings_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doctor_schedules",
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
                    BreakEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_schedules_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_doctor_schedules_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "doctor_special_days",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoctorId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    SpecificDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_doctor_special_days", x => x.Id);
                    table.ForeignKey(
                        name: "FK_doctor_special_days_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_doctor_special_days_users_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "specializations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_specializations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_SpecializationId",
                table: "users",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_AppointmentTypeId",
                table: "appointments",
                column: "AppointmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_SpecializationId",
                table: "appointments",
                column: "SpecializationId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_StatusId",
                table: "appointments",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "ix_appointments_slot_unique",
                table: "appointments",
                columns: new[] { "DoctorId", "BranchId", "StartTime" },
                unique: true,
                filter: "\"StatusId\" NOT IN (4, 6, 10)");

            migrationBuilder.CreateIndex(
                name: "ix_appointment_statuses_code",
                table: "appointment_statuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointment_types_code",
                table: "appointment_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_on_call_settings_BranchId",
                table: "doctor_on_call_settings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_on_call_settings_unique",
                table: "doctor_on_call_settings",
                columns: new[] { "DoctorId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_schedules_BranchId",
                table: "doctor_schedules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_schedules_unique",
                table: "doctor_schedules",
                columns: new[] { "DoctorId", "BranchId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_doctor_special_days_BranchId",
                table: "doctor_special_days",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "ix_doctor_special_days_unique",
                table: "doctor_special_days",
                columns: new[] { "DoctorId", "BranchId", "SpecificDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_specializations_code",
                table: "specializations",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_appointment_statuses_StatusId",
                table: "appointments",
                column: "StatusId",
                principalTable: "appointment_statuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_appointment_types_AppointmentTypeId",
                table: "appointments",
                column: "AppointmentTypeId",
                principalTable: "appointment_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_specializations_SpecializationId",
                table: "appointments",
                column: "SpecializationId",
                principalTable: "specializations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_specializations_SpecializationId",
                table: "users",
                column: "SpecializationId",
                principalTable: "specializations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_appointment_statuses_StatusId",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_appointment_types_AppointmentTypeId",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_appointments_specializations_SpecializationId",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_users_specializations_SpecializationId",
                table: "users");

            migrationBuilder.DropTable(
                name: "appointment_statuses");

            migrationBuilder.DropTable(
                name: "appointment_types");

            migrationBuilder.DropTable(
                name: "doctor_on_call_settings");

            migrationBuilder.DropTable(
                name: "doctor_schedules");

            migrationBuilder.DropTable(
                name: "doctor_special_days");

            migrationBuilder.DropTable(
                name: "specializations");

            migrationBuilder.DropIndex(
                name: "IX_users_SpecializationId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_appointments_AppointmentTypeId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_SpecializationId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "IX_appointments_StatusId",
                table: "appointments");

            migrationBuilder.DropIndex(
                name: "ix_appointments_slot_unique",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "CalendarColor",
                table: "users");

            migrationBuilder.DropColumn(
                name: "DefaultAppointmentDuration",
                table: "users");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "users");

            migrationBuilder.DropColumn(
                name: "AppointmentNo",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentTypeId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "BookingSource",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "EnteredRoomAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "IsNewPatient",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "IsUrgent",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "LeftClinicAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "LeftRoomAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "PatientArrivedAt",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "SendSmsNotification",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "StatusId",
                table: "appointments",
                newName: "Status");

            migrationBuilder.AlterColumn<long>(
                name: "PatientId",
                table: "appointments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_appointments_slot_unique",
                table: "appointments",
                columns: new[] { "DoctorId", "BranchId", "StartTime" },
                unique: true,
                filter: "\"Status\" NOT IN (6, 7)");
        }
    }
}
