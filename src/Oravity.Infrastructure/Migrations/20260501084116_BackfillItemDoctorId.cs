using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillItemDoctorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut kalemlerin DoctorId'sini planın DoctorId'siyle doldur
            migrationBuilder.Sql(@"
                UPDATE treatment_plan_items tpi
                SET ""DoctorId"" = tp.""DoctorId""
                FROM treatment_plans tp
                WHERE tpi.""PlanId"" = tp.""Id""
                  AND tpi.""DoctorId"" IS NULL
                  AND tp.""DoctorId"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
