using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanItemKdvSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "KdvAmount",
                table: "treatment_plan_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "KdvRate",
                table: "treatment_plan_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "treatment_plan_items",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KdvAmount",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "KdvRate",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "treatment_plan_items");
        }
    }
}
