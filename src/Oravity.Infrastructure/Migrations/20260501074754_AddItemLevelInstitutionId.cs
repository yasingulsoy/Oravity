using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemLevelInstitutionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InstitutionId",
                table: "treatment_plan_items",
                type: "bigint",
                nullable: true);

            // Mevcut kalemleri plan'ın InstitutionId'siyle doldur
            migrationBuilder.Sql(@"
                UPDATE treatment_plan_items tpi
                SET ""InstitutionId"" = tp.""InstitutionId""
                FROM treatment_plans tp
                WHERE tpi.""PlanId"" = tp.""Id""
                  AND tp.""InstitutionId"" IS NOT NULL;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_treatment_plan_items_InstitutionId",
                table: "treatment_plan_items",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_treatment_plan_items_institutions_InstitutionId",
                table: "treatment_plan_items",
                column: "InstitutionId",
                principalTable: "institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatment_plan_items_institutions_InstitutionId",
                table: "treatment_plan_items");

            migrationBuilder.DropIndex(
                name: "IX_treatment_plan_items_InstitutionId",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "treatment_plan_items");
        }
    }
}
