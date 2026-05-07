using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatCoreModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OwnerOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatContactRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RespondedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatContactRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatContactRequests_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChatConversationOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatConversationOrganizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatConversationOrganizations_ChatConversations_Conversatio~",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderOrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReadMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_ChatConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "ChatConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatContactRequests_ConversationId",
                table: "ChatContactRequests",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatContactRequests_RequesterOrganizationId",
                table: "ChatContactRequests",
                column: "RequesterOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatContactRequests_RequesterOrganizationId_TargetOrganizat~",
                table: "ChatContactRequests",
                columns: new[] { "RequesterOrganizationId", "TargetOrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatContactRequests_TargetOrganizationId",
                table: "ChatContactRequests",
                column: "TargetOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatContactRequests_TargetOrganizationId_Status",
                table: "ChatContactRequests",
                columns: new[] { "TargetOrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversationOrganizations_ConversationId",
                table: "ChatConversationOrganizations",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversationOrganizations_ConversationId_OrganizationId",
                table: "ChatConversationOrganizations",
                columns: new[] { "ConversationId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversationOrganizations_OrganizationId",
                table: "ChatConversationOrganizations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversationOrganizations_OrganizationId_IsActive",
                table: "ChatConversationOrganizations",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_CreatedAt",
                table: "ChatConversations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_IsActive",
                table: "ChatConversations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_OwnerOrganizationId",
                table: "ChatConversations",
                column: "OwnerOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_OwnerOrganizationId_ClientId",
                table: "ChatConversations",
                columns: new[] { "OwnerOrganizationId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_OwnerOrganizationId_DealId",
                table: "ChatConversations",
                columns: new[] { "OwnerOrganizationId", "DealId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_OwnerOrganizationId_Type",
                table: "ChatConversations",
                columns: new[] { "OwnerOrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId",
                table: "ChatMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ConversationId_CreatedAt",
                table: "ChatMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_IsDeleted",
                table: "ChatMessages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderOrganizationId",
                table: "ChatMessages",
                column: "SenderOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderUserId",
                table: "ChatMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ConversationId",
                table: "ChatParticipants",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ConversationId_OrganizationId",
                table: "ChatParticipants",
                columns: new[] { "ConversationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_ConversationId_UserId",
                table: "ChatParticipants",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_IsActive",
                table: "ChatParticipants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_OrganizationId",
                table: "ChatParticipants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_OrganizationId_UserId",
                table: "ChatParticipants",
                columns: new[] { "OrganizationId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_UserId",
                table: "ChatParticipants",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatContactRequests");

            migrationBuilder.DropTable(
                name: "ChatConversationOrganizations");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChatParticipants");

            migrationBuilder.DropTable(
                name: "ChatConversations");
        }
    }
}
