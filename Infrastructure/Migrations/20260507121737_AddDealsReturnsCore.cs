using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDealsReturnsCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceReturnId",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceReturnId",
                table: "BonusTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DealReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusPointsReturned = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    BonusAccrualReversed = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    MoneyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealReturns_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DealReturnItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    ReturnAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealReturnItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealReturnItems_DealItems_DealItemId",
                        column: x => x.DealItemId,
                        principalTable: "DealItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealReturnItems_DealReturns_DealReturnId",
                        column: x => x.DealReturnId,
                        principalTable: "DealReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OrganizationId_SourceReturnId",
                table: "StockMovements",
                columns: new[] { "OrganizationId", "SourceReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_BonusTransactions_OrganizationId_DealId_SourceReturnId_Type",
                table: "BonusTransactions",
                columns: new[] { "OrganizationId", "DealId", "SourceReturnId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_DealItemId",
                table: "DealReturnItems",
                column: "DealItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_DealReturnId",
                table: "DealReturnItems",
                column: "DealReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_OrganizationId",
                table: "DealReturnItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_OrganizationId_DealId",
                table: "DealReturnItems",
                columns: new[] { "OrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_OrganizationId_DealItemId",
                table: "DealReturnItems",
                columns: new[] { "OrganizationId", "DealItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_OrganizationId_DealReturnId",
                table: "DealReturnItems",
                columns: new[] { "OrganizationId", "DealReturnId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturnItems_OrganizationId_ItemId",
                table: "DealReturnItems",
                columns: new[] { "OrganizationId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturns_DealId",
                table: "DealReturns",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_DealReturns_OrganizationId",
                table: "DealReturns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DealReturns_OrganizationId_CreatedAt",
                table: "DealReturns",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturns_OrganizationId_DealId",
                table: "DealReturns",
                columns: new[] { "OrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealReturns_OrganizationId_Status",
                table: "DealReturns",
                columns: new[] { "OrganizationId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealReturnItems");

            migrationBuilder.DropTable(
                name: "DealReturns");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_OrganizationId_SourceReturnId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_BonusTransactions_OrganizationId_DealId_SourceReturnId_Type",
                table: "BonusTransactions");

            migrationBuilder.DropColumn(
                name: "SourceReturnId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "SourceReturnId",
                table: "BonusTransactions");
        }
    }
}
