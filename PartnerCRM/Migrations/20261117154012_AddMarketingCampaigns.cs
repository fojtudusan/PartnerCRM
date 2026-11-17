using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EmailTemplateId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingCampaigns_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketingRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MarketingCampaignId = table.Column<int>(type: "INTEGER", nullable: false),
                    CompanyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactId = table.Column<int>(type: "INTEGER", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingRecipients_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketingRecipients_Contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketingRecipients_MarketingCampaigns_MarketingCampaignId",
                        column: x => x.MarketingCampaignId,
                        principalTable: "MarketingCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingCampaigns_EmailTemplateId",
                table: "MarketingCampaigns",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingRecipients_CompanyId",
                table: "MarketingRecipients",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingRecipients_ContactId",
                table: "MarketingRecipients",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingRecipients_MarketingCampaignId",
                table: "MarketingRecipients",
                column: "MarketingCampaignId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingRecipients");

            migrationBuilder.DropTable(
                name: "MarketingCampaigns");
        }
    }
}
