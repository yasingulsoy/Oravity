using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientInstitutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_patients_institutions_LastInstitutionId",
                table: "patients");

            migrationBuilder.RenameColumn(
                name: "LastInstitutionId",
                table: "patients",
                newName: "InsuranceInstitutionId");

            migrationBuilder.RenameIndex(
                name: "IX_patients_LastInstitutionId",
                table: "patients",
                newName: "IX_patients_InsuranceInstitutionId");

            migrationBuilder.AddColumn<long>(
                name: "AgreementInstitutionId",
                table: "patients",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_patients_AgreementInstitutionId",
                table: "patients",
                column: "AgreementInstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_patients_institutions_AgreementInstitutionId",
                table: "patients",
                column: "AgreementInstitutionId",
                principalTable: "institutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_patients_institutions_InsuranceInstitutionId",
                table: "patients",
                column: "InsuranceInstitutionId",
                principalTable: "institutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_patients_institutions_AgreementInstitutionId",
                table: "patients");

            migrationBuilder.DropForeignKey(
                name: "FK_patients_institutions_InsuranceInstitutionId",
                table: "patients");

            migrationBuilder.DropIndex(
                name: "IX_patients_AgreementInstitutionId",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "AgreementInstitutionId",
                table: "patients");

            migrationBuilder.RenameColumn(
                name: "InsuranceInstitutionId",
                table: "patients",
                newName: "LastInstitutionId");

            migrationBuilder.RenameIndex(
                name: "IX_patients_InsuranceInstitutionId",
                table: "patients",
                newName: "IX_patients_LastInstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_patients_institutions_LastInstitutionId",
                table: "patients",
                column: "LastInstitutionId",
                principalTable: "institutions",
                principalColumn: "Id");
        }
    }
}
