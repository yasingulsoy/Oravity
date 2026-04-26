using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstitutionPaymentModelAndContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InstitutionContributionAmount",
                table: "treatment_plan_items",
                type: "numeric(12,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PatientAmount",
                table: "treatment_plan_items",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PaymentModel",
                table: "institutions",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstitutionContributionAmount",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "PatientAmount",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "PaymentModel",
                table: "institutions");
        }
    }
}
