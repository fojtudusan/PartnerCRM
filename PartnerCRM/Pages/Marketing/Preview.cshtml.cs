
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class PreviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PreviewModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CampaignName { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientEmail { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int campaignId)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign == null)
                return NotFound();

            if (campaign.EmailTemplate == null)
            {
                TempData["Error"] =
                    "Marketingová akce nemá přiřazenou e-mailovou šablonu.";

                return RedirectToPage("/Marketing/Details",
                    new { id = campaignId });
            }

            var recipient = await _context.MarketingRecipients
                .Include(r => r.Contact)
                .Include(r => r.Company)
                .Where(r => r.MarketingCampaignId == campaignId)
                .OrderBy(r => r.Company.Name)
                .ThenBy(r => r.Contact.LastName)
                .FirstOrDefaultAsync();

            if (recipient == null)
            {
                TempData["Error"] =
                    "Nejprve přidejte do marketingové akce alespoň jednoho příjemce.";

                return RedirectToPage("/Marketing/Details",
                    new { id = campaignId });
            }

            CampaignName = campaign.Name;
            RecipientName = $"{recipient.Contact.FirstName} {recipient.Contact.LastName}".Trim();
            RecipientEmail = recipient.EmailAddress;
            CompanyName = recipient.Company.Name;

            Subject = ReplacePlaceholders(campaign.EmailTemplate.Subject, recipient);
            Body = ReplacePlaceholders(campaign.EmailTemplate.Body, recipient);

            return Page();
        }

        private static string ReplacePlaceholders(
            string text,
            MarketingRecipient recipient)
        {
            return text
                .Replace("{Jmeno}", recipient.Contact.FirstName ?? "")
                .Replace("{Prijmeni}", recipient.Contact.LastName ?? "")
                .Replace("{Firma}", recipient.Company.Name ?? "")
                .Replace("{Email}", recipient.EmailAddress ?? "");
        }
    }
}
