using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateIcdToProtocolJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "protocol_diagnoses");

            migrationBuilder.AddColumn<string>(
                name: "IcdDiagnosesJson",
                table: "protocols",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IcdDiagnosesJson",
                table: "protocols");

            migrationBuilder.CreateTable(
                name: "protocol_diagnoses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IcdCodeId = table.Column<long>(type: "bigint", nullable: false),
                    ProtocolId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_protocol_diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_protocol_diagnoses_icd_codes_IcdCodeId",
                        column: x => x.IcdCodeId,
                        principalTable: "icd_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_protocol_diagnoses_protocols_ProtocolId",
                        column: x => x.ProtocolId,
                        principalTable: "protocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_protocol_diagnoses_IcdCodeId",
                table: "protocol_diagnoses",
                column: "IcdCodeId");

            migrationBuilder.CreateIndex(
                name: "ix_protocol_diagnoses_protocol",
                table: "protocol_diagnoses",
                column: "ProtocolId");

            migrationBuilder.CreateIndex(
                name: "ix_protocol_diagnoses_public_id",
                table: "protocol_diagnoses",
                column: "PublicId",
                unique: true);
        }
    }
}
