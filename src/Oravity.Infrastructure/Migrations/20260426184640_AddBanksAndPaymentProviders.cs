using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Oravity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBanksAndPaymentProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BankAccountId",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PosTerminalId",
                table: "payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "banks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BicCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payment_providers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    BankId = table.Column<long>(type: "bigint", nullable: true),
                    AccountName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "TRY"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bank_accounts_banks_BankId",
                        column: x => x.BankId,
                        principalTable: "banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bank_accounts_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pos_terminals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankId = table.Column<long>(type: "bigint", nullable: true),
                    TerminalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_terminals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pos_terminals_banks_BankId",
                        column: x => x.BankId,
                        principalTable: "banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_pos_terminals_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payments_BankAccountId",
                table: "payments",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PosTerminalId",
                table: "payments",
                column: "PosTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_BankId",
                table: "bank_accounts",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_branch",
                table: "bank_accounts",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_public_id",
                table: "bank_accounts",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_banks_public_id",
                table: "banks",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_banks_short_name",
                table: "banks",
                column: "ShortName");

            migrationBuilder.CreateIndex(
                name: "ix_payment_providers_public_id",
                table: "payment_providers",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_terminals_BankId",
                table: "pos_terminals",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_branch",
                table: "pos_terminals",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_pos_terminals_public_id",
                table: "pos_terminals",
                column: "PublicId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_bank_accounts_BankAccountId",
                table: "payments",
                column: "BankAccountId",
                principalTable: "bank_accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_payments_pos_terminals_PosTerminalId",
                table: "payments",
                column: "PosTerminalId",
                principalTable: "pos_terminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payments_bank_accounts_BankAccountId",
                table: "payments");

            migrationBuilder.DropForeignKey(
                name: "FK_payments_pos_terminals_PosTerminalId",
                table: "payments");

            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "payment_providers");

            migrationBuilder.DropTable(
                name: "pos_terminals");

            migrationBuilder.DropTable(
                name: "banks");

            migrationBuilder.DropIndex(
                name: "IX_payments_BankAccountId",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_PosTerminalId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "PosTerminalId",
                table: "payments");
        }
    }
}
