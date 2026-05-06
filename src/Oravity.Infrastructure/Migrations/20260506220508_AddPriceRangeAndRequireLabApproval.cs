using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceRangeAndRequireLabApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAlert",
                table: "patient_notes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireLabApproval",
                table: "doctor_commission_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "commission_template_price_ranges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    MaxAmount = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_template_price_ranges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_commission_template_price_ranges_doctor_commission_template~",
                        column: x => x.TemplateId,
                        principalTable: "doctor_commission_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_template_price_ranges_TemplateId",
                table: "commission_template_price_ranges",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_template_price_ranges");

            migrationBuilder.DropColumn(
                name: "IsAlert",
                table: "patient_notes");

            migrationBuilder.DropColumn(
                name: "RequireLabApproval",
                table: "doctor_commission_templates");
        }
    }
}
