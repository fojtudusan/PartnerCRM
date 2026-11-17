using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartnerCRM.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMarketingRecipientEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketingRecipients_MarketingCampaignId",
                table: "MarketingRecipients");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingRecipients_MarketingCampaignId_EmailAddress",
                table: "MarketingRecipients",
                columns: new[] { "MarketingCampaignId", "EmailAddress" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketingRecipients_MarketingCampaignId_EmailAddress",
                table: "MarketingRecipients");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingRecipients_MarketingCampaignId",
                table: "MarketingRecipients",
                column: "MarketingCampaignId");
        }
    }
}
