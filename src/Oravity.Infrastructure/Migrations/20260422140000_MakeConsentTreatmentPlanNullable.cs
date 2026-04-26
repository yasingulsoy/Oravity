using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Oravity.Infrastructure.Database;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260422140000_MakeConsentTreatmentPlanNullable")]
    public partial class MakeConsentTreatmentPlanNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FK'yı düşür
            migrationBuilder.DropForeignKey(
                name: "FK_consent_instances_treatment_plans_TreatmentPlanId",
                table: "consent_instances");

            // Kolonu nullable yap
            migrationBuilder.AlterColumn<long>(
                name: "TreatmentPlanId",
                table: "consent_instances",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // FK'yı nullable olarak yeniden ekle (plan silinince SET NULL)
            migrationBuilder.AddForeignKey(
                name: "FK_consent_instances_treatment_plans_TreatmentPlanId",
                table: "consent_instances",
                column: "TreatmentPlanId",
                principalTable: "treatment_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_consent_instances_treatment_plans_TreatmentPlanId",
                table: "consent_instances");

            migrationBuilder.AlterColumn<long>(
                name: "TreatmentPlanId",
                table: "consent_instances",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldNullable: true,
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_consent_instances_treatment_plans_TreatmentPlanId",
                table: "consent_instances",
                column: "TreatmentPlanId",
                principalTable: "treatment_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
