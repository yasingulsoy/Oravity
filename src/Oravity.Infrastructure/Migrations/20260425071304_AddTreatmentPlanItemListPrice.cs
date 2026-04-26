using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPlanItemListPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ListPrice",
                table: "treatment_plan_items",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListPrice",
                table: "treatment_plan_items");
        }
    }
}
