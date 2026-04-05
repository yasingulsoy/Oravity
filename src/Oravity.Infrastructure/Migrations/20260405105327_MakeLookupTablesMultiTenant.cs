using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeLookupTablesMultiTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_referral_sources_code",
                table: "referral_sources");

            migrationBuilder.DropIndex(
                name: "ix_citizenship_types_code",
                table: "citizenship_types");

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "referral_sources",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CompanyId",
                table: "citizenship_types",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_referral_sources_company_code",
                table: "referral_sources",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_citizenship_types_company_code",
                table: "citizenship_types",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_citizenship_types_companies_CompanyId",
                table: "citizenship_types",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_referral_sources_companies_CompanyId",
                table: "referral_sources",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_citizenship_types_companies_CompanyId",
                table: "citizenship_types");

            migrationBuilder.DropForeignKey(
                name: "FK_referral_sources_companies_CompanyId",
                table: "referral_sources");

            migrationBuilder.DropIndex(
                name: "ix_referral_sources_company_code",
                table: "referral_sources");

            migrationBuilder.DropIndex(
                name: "ix_citizenship_types_company_code",
                table: "citizenship_types");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "referral_sources");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "citizenship_types");

            migrationBuilder.CreateIndex(
                name: "ix_referral_sources_code",
                table: "referral_sources",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_citizenship_types_code",
                table: "citizenship_types",
                column: "Code",
                unique: true);
        }
    }
}
