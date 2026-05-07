using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBonusCoreModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "BonusPointsUsed",
                table: "Deals",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateTable(
                name: "BonusAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonusSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PointValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AccrualType = table.Column<int>(type: "integer", nullable: false),
                    AccrualValue = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MaxPaymentPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    AccrueOnBonusPayment = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BonusTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BonusAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MonetaryAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PointValueAtMoment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BonusTransactions_BonusAccounts_BonusAccountId",
                        column: x => x.BonusAccountId,
                        principalTable: "BonusAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusAccounts_OrganizationId",
                table: "BonusAccounts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusAccounts_OrganizationId_ClientId",
                table: "BonusAccounts",
                columns: new[] { "OrganizationId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusAccounts_OrganizationId_IsActive",
                table: "BonusAccounts",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusSettings_OrganizationId",
                table: "BonusSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_BonusAccountId",
                table: "BonusTransactions",
                column: "BonusAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId",
                table: "BonusTransactions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_BonusAccountId",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "BonusAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_ClientId",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_CreatedAt",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_DealId",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_Type",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusSettings");

            migrationBuilder.DropTable(
                name: "BonusTransactions");

            migrationBuilder.DropTable(
                name: "BonusAccounts");

            migrationBuilder.AlterColumn<decimal>(
                name: "BonusPointsUsed",
                table: "Deals",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)",
                oldPrecision: 18,
                oldScale: 3);
        }
    }
}
