using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Direction = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "ltr"),
                    FlagEmoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsDangerous = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PreferredLanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verticals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HasBodyChart = table.Column<bool>(type: "boolean", nullable: false),
                    BodyChartType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DefaultModules = table.Column<string[]>(type: "text[]", nullable: false),
                    ProviderLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Hekim"),
                    PatientLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Hasta"),
                    TreatmentLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "Tedavi"),
                    RequiresKts = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verticals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_template_permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_template_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_template_permissions_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_template_permissions_role_templates_RoleTemplateId",
                        column: x => x.RoleTemplateId,
                        principalTable: "role_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VerticalId = table.Column<long>(type: "bigint", nullable: false),
                    DefaultLanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "tr"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_companies_verticals_VerticalId",
                        column: x => x.VerticalId,
                        principalTable: "verticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    VerticalId = table.Column<long>(type: "bigint", nullable: true),
                    DefaultLanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false, defaultValue: "tr"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branches_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_branches_verticals_VerticalId",
                        column: x => x.VerticalId,
                        principalTable: "verticals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_permission_overrides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permission_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_permission_overrides_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permission_overrides_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permission_overrides_permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_permission_overrides_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_role_templates_RoleTemplateId",
                        column: x => x.RoleTemplateId,
                        principalTable: "role_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_role_assignments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "languages",
                columns: new[] { "Id", "Code", "CreatedAt", "Direction", "FlagEmoji", "IsActive", "IsDefault", "IsDeleted", "Name", "NativeName", "PublicId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "tr", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ltr", "🇹🇷", true, true, false, "Türkçe", "Türkçe", new Guid("00000001-0000-0000-0000-000000000001"), 0, null },
                    { 2L, "en", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ltr", "🇬🇧", true, false, false, "İngilizce", "English", new Guid("00000001-0000-0000-0000-000000000002"), 1, null },
                    { 3L, "ar", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "rtl", "🇸🇦", true, false, false, "Arapça", "العربية", new Guid("00000001-0000-0000-0000-000000000003"), 2, null },
                    { 4L, "ru", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ltr", "🇷🇺", true, false, false, "Rusça", "Русский", new Guid("00000001-0000-0000-0000-000000000004"), 3, null },
                    { 5L, "de", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ltr", "🇩🇪", true, false, false, "Almanca", "Deutsch", new Guid("00000001-0000-0000-0000-000000000005"), 4, null }
                });

            migrationBuilder.InsertData(
                table: "role_templates",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "IsDeleted", "Name", "PublicId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "BRANCH_MANAGER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Şube içindeki tüm işlemleri yönetir", true, false, "Şube Yöneticisi", new Guid("00000003-0000-0000-0000-000000000001"), null },
                    { 2L, "DOCTOR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Klinik muayene ve tedavi işlemlerini yürütür", true, false, "Hekim", new Guid("00000003-0000-0000-0000-000000000002"), null },
                    { 3L, "ASSISTANT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hekime yardımcı klinik personel", true, false, "Asistan", new Guid("00000003-0000-0000-0000-000000000003"), null },
                    { 4L, "RECEPTIONIST", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Randevu ve hasta kayıt işlemlerini yönetir", true, false, "Resepsiyonist", new Guid("00000003-0000-0000-0000-000000000004"), null },
                    { 5L, "ACCOUNTANT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Mali işlemler ve raporlama", true, false, "Muhasebeci", new Guid("00000003-0000-0000-0000-000000000005"), null },
                    { 6L, "READONLY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Yalnızca görüntüleme yetkisi", true, false, "Salt Okunur", new Guid("00000003-0000-0000-0000-000000000006"), null }
                });

            migrationBuilder.InsertData(
                table: "verticals",
                columns: new[] { "Id", "BodyChartType", "Code", "CreatedAt", "DefaultModules", "HasBodyChart", "IsActive", "IsDeleted", "Name", "PatientLabel", "ProviderLabel", "PublicId", "RequiresKts", "SortOrder", "TreatmentLabel", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, "DENTAL_FDI", "DENTAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "FINANCE", "APPOINTMENT", "TREATMENT" }, true, true, false, "Diş Hekimliği", "Hasta", "Hekim", new Guid("00000002-0000-0000-0000-000000000001"), true, 0, "Tedavi", null },
                    { 2L, null, "AESTHETIC", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, false, false, false, "Estetik & Güzellik", "Müşteri", "Uzman", new Guid("00000002-0000-0000-0000-000000000002"), false, 1, "Uygulama", null },
                    { 3L, null, "NUTRITION", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, false, false, false, "Diyetisyen", "Danışan", "Diyetisyen", new Guid("00000002-0000-0000-0000-000000000003"), false, 2, "Seans", null },
                    { 4L, "HAIR_MAP", "HAIR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, true, false, false, "Saç Ekim", "Hasta", "Uzman", new Guid("00000002-0000-0000-0000-000000000004"), false, 3, "Operasyon", null },
                    { 5L, "BODY_REGION", "PODOLOGY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, true, false, false, "Ayak Bakımı", "Hasta", "Podolog", new Guid("00000002-0000-0000-0000-000000000005"), false, 4, "Uygulama", null },
                    { 6L, "BODY_REGION", "PHYSIO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, true, false, false, "Fizik Tedavi", "Hasta", "Fizyoterapist", new Guid("00000002-0000-0000-0000-000000000006"), true, 5, "Seans", null },
                    { 7L, null, "PSYCHOLOGY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, false, false, false, "Psikoloji", "Danışan", "Terapist", new Guid("00000002-0000-0000-0000-000000000007"), false, 6, "Seans", null },
                    { 8L, null, "VETERINARY", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, false, false, false, "Veteriner", "Hasta", "Veteriner", new Guid("00000002-0000-0000-0000-000000000008"), false, 7, "Tedavi", null },
                    { 9L, null, "GENERAL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new[] { "CORE", "APPOINTMENT" }, false, false, false, "Genel Muayenehane", "Hasta", "Hekim", new Guid("00000002-0000-0000-0000-000000000009"), true, 8, "Muayene", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_branches_CompanyId",
                table: "branches",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_branches_PublicId",
                table: "branches",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_VerticalId",
                table: "branches",
                column: "VerticalId");

            migrationBuilder.CreateIndex(
                name: "IX_companies_PublicId",
                table: "companies",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_VerticalId",
                table: "companies",
                column: "VerticalId");

            migrationBuilder.CreateIndex(
                name: "IX_languages_Code",
                table: "languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_languages_PublicId",
                table: "languages",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_pending",
                table: "outbox_messages",
                columns: new[] { "Status", "NextRetryAt" },
                filter: "\"Status\" IN (1, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_Code",
                table: "permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_PublicId",
                table: "permissions",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_template_permissions_PermissionId",
                table: "role_template_permissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_role_template_permissions_RoleTemplateId_PermissionId",
                table: "role_template_permissions",
                columns: new[] { "RoleTemplateId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_templates_Code",
                table: "role_templates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_templates_PublicId",
                table: "role_templates",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_overrides_BranchId",
                table: "user_permission_overrides",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_overrides_CompanyId",
                table: "user_permission_overrides",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_overrides_PermissionId",
                table: "user_permission_overrides",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_permission_overrides_UserId",
                table: "user_permission_overrides",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_BranchId",
                table: "user_role_assignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_CompanyId",
                table: "user_role_assignments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_PublicId",
                table: "user_role_assignments",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_RoleTemplateId",
                table: "user_role_assignments",
                column: "RoleTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_user_role_assignments_UserId",
                table: "user_role_assignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PublicId",
                table: "users",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verticals_Code",
                table: "verticals",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_verticals_PublicId",
                table: "verticals",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "languages");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "role_template_permissions");

            migrationBuilder.DropTable(
                name: "user_permission_overrides");

            migrationBuilder.DropTable(
                name: "user_role_assignments");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "role_templates");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "verticals");
        }
    }
}
