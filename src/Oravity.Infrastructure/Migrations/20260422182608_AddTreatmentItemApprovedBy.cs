using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentItemApprovedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "treatment_plan_items",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedByUserId",
                table: "treatment_plan_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_treatment_plan_items_ApprovedByUserId",
                table: "treatment_plan_items",
                column: "ApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_treatment_plan_items_users_ApprovedByUserId",
                table: "treatment_plan_items",
                column: "ApprovedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_treatment_plan_items_users_ApprovedByUserId",
                table: "treatment_plan_items");

            migrationBuilder.DropIndex(
                name: "IX_treatment_plan_items_ApprovedByUserId",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "treatment_plan_items");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "treatment_plan_items");
        }
    }
}
