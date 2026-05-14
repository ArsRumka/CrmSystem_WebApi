using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailCampaignsCoreModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SenderEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SmtpPort = table.Column<int>(type: "integer", nullable: false),
                    UseSsl = table.Column<bool>(type: "boolean", nullable: false),
                    Username = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordEncrypted = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    IsHtml = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAutomationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    InactivityDays = table.Column<int>(type: "integer", nullable: false),
                    RepeatAfterDays = table.Column<int>(type: "integer", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAutomationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAutomationRules_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalRecipients = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailCampaigns_EmailTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailCampaignRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    FullNameSnapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LastDealDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DaysSinceLastDeal = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailCampaignRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailCampaignRecipients_EmailCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "EmailCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAutomationRules_OrganizationId",
                table: "EmailAutomationRules",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAutomationRules_OrganizationId_IsEnabled",
                table: "EmailAutomationRules",
                columns: new[] { "OrganizationId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAutomationRules_TemplateId",
                table: "EmailAutomationRules",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_CampaignId",
                table: "EmailCampaignRecipients",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_OrganizationId",
                table: "EmailCampaignRecipients",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_OrganizationId_CampaignId_ClientId",
                table: "EmailCampaignRecipients",
                columns: new[] { "OrganizationId", "CampaignId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_OrganizationId_ClientId",
                table: "EmailCampaignRecipients",
                columns: new[] { "OrganizationId", "ClientId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_OrganizationId_SentAt",
                table: "EmailCampaignRecipients",
                columns: new[] { "OrganizationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaignRecipients_OrganizationId_Status",
                table: "EmailCampaignRecipients",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_OrganizationId",
                table: "EmailCampaigns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_OrganizationId_CreatedAt",
                table: "EmailCampaigns",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_OrganizationId_Status",
                table: "EmailCampaigns",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_OrganizationId_TemplateId",
                table: "EmailCampaigns",
                columns: new[] { "OrganizationId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_OrganizationId_Type",
                table: "EmailCampaigns",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailCampaigns_TemplateId",
                table: "EmailCampaigns",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailSettings_OrganizationId",
                table: "EmailSettings",
                column: "OrganizationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_OrganizationId",
                table: "EmailTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_OrganizationId_IsActive",
                table: "EmailTemplates",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_OrganizationId_Name",
                table: "EmailTemplates",
                columns: new[] { "OrganizationId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAutomationRules");

            migrationBuilder.DropTable(
                name: "EmailCampaignRecipients");

            migrationBuilder.DropTable(
                name: "EmailSettings");

            migrationBuilder.DropTable(
                name: "EmailCampaigns");

            migrationBuilder.DropTable(
                name: "EmailTemplates");
        }
    }
}
