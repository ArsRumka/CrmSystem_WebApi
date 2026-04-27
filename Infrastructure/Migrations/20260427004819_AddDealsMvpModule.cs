using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDealsMvpModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DealStages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Deals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsibleUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusPointsUsed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deals_DealStages_StageId",
                        column: x => x.StageId,
                        principalTable: "DealStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DealItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageId = table.Column<Guid>(type: "uuid", nullable: true),
                    NameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PriceAtMoment = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountType = table.Column<int>(type: "integer", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealItems_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DealStageHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    OldStageId = table.Column<Guid>(type: "uuid", nullable: true),
                    NewStageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealStageHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealStageHistories_DealStages_NewStageId",
                        column: x => x.NewStageId,
                        principalTable: "DealStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealStageHistories_DealStages_OldStageId",
                        column: x => x.OldStageId,
                        principalTable: "DealStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DealStageHistories_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DealItems_DealId",
                table: "DealItems",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_DealItems_OrganizationId",
                table: "DealItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DealItems_OrganizationId_DealId",
                table: "DealItems",
                columns: new[] { "OrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealItems_OrganizationId_ItemType_ItemId",
                table: "DealItems",
                columns: new[] { "OrganizationId", "ItemType", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealItems_OrganizationId_StorageId",
                table: "DealItems",
                columns: new[] { "OrganizationId", "StorageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId",
                table: "Deals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId_ClientId",
                table: "Deals",
                columns: new[] { "OrganizationId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId_CreatedAt",
                table: "Deals",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId_IsActive",
                table: "Deals",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId_ResponsibleUserId",
                table: "Deals",
                columns: new[] { "OrganizationId", "ResponsibleUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_OrganizationId_StageId",
                table: "Deals",
                columns: new[] { "OrganizationId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_StageId",
                table: "Deals",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_DealId",
                table: "DealStageHistories",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_NewStageId",
                table: "DealStageHistories",
                column: "NewStageId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_OldStageId",
                table: "DealStageHistories",
                column: "OldStageId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_OrganizationId",
                table: "DealStageHistories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_OrganizationId_ChangedAt",
                table: "DealStageHistories",
                columns: new[] { "OrganizationId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DealStageHistories_OrganizationId_DealId",
                table: "DealStageHistories",
                columns: new[] { "OrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_DealStages_OrganizationId",
                table: "DealStages",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DealStages_OrganizationId_IsActive",
                table: "DealStages",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DealStages_OrganizationId_Name",
                table: "DealStages",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DealStages_OrganizationId_Order",
                table: "DealStages",
                columns: new[] { "OrganizationId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DealItems");

            migrationBuilder.DropTable(
                name: "DealStageHistories");

            migrationBuilder.DropTable(
                name: "Deals");

            migrationBuilder.DropTable(
                name: "DealStages");
        }
    }
}
