using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanInstitution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "treatment_plans",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_treatment_plans_institution",
                table: "treatment_plans",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_treatment_plans_institutions_InstitutionId",
                table: "treatment_plans",
                column: "InstitutionId",
                principalTable: "institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatment_plans_institutions_InstitutionId",
                table: "treatment_plans");

            migrationBuilder.DropIndex(
                name: "ix_treatment_plans_institution",
                table: "treatment_plans");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "treatment_plans");
        }
    }
}
