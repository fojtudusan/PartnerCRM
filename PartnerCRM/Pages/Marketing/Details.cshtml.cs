
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MarketingCampaign Campaign { get; set; } = null!;

        public List<Company> Companies { get; set; } = new();

        public List<ContactEmployment> CurrentEmployments { get; set; } = new();
        public List<EmailTemplate> EmailTemplates { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Company)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Contact)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            Campaign = campaign;

            Companies = await _context.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            CurrentEmployments = await _context.ContactEmployments
                .Include(e => e.Contact)
                .Include(e => e.Company)
                .Where(e => e.IsCurrent && e.Contact.Email != null
                    && e.Contact.Email != "")
                .OrderBy(e => e.Company.Name)
                .ThenBy(e => e.Contact.LastName)
                .ThenBy(e => e.Contact.FirstName)
                .ToListAsync();
    
            EmailTemplates = await _context.EmailTemplates
                .Where(e => e.IsActive)
                .OrderBy(e => e.Name)
                .ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddRecipientAsync(
            int id,
            int companyId,
            int contactId)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Příjemce lze přidávat pouze do kampaně ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            var employment = await _context.ContactEmployments
                .Include(e => e.Contact)
                .FirstOrDefaultAsync(e =>
                    e.CompanyId == companyId &&
                    e.ContactId == contactId &&
                    e.IsCurrent);

            if (employment == null ||
                string.IsNullOrWhiteSpace(employment.Contact.Email))
            {
                TempData["Error"] =
                    "Kontakt není aktuálně přiřazen k vybrané firmě nebo nemá e-mailovou adresu.";

                return RedirectToPage(new { id });
            }

            string emailAddress = employment.Contact.Email.Trim();

            bool alreadyExists = await _context.MarketingRecipients
                .AnyAsync(r =>
                    r.MarketingCampaignId == id &&
                    (
                        (r.CompanyId == companyId &&
                         r.ContactId == contactId)
                        ||
                        r.EmailAddress.ToLower() == emailAddress.ToLower()
                    ));

            if (alreadyExists)
            {
                TempData["Error"] =
                    "Tento kontakt již byl do marketingové akce přidán.";

                return RedirectToPage(new { id });
            }

            var recipient = new MarketingRecipient
            {
                MarketingCampaignId = id,
                CompanyId = companyId,
                ContactId = contactId,
                EmailAddress = emailAddress,
                Status = "Připraveno"
            };

            _context.MarketingRecipients.Add(recipient);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kontakt byl přidán mezi příjemce marketingové akce.";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostRemoveRecipientAsync(
            int id,
            int recipientId)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Příjemce lze odebrat pouze z kampaně ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            var recipient = await _context.MarketingRecipients
                .FirstOrDefaultAsync(r =>
                    r.Id == recipientId &&
                    r.MarketingCampaignId == id);

            if (recipient == null)
                return NotFound();

            _context.MarketingRecipients.Remove(recipient);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kontakt byl odebrán z marketingové akce.";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEditCampaignAsync(
            int id,
            string name,
            string? description,
            int? emailTemplateId)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            {
                TempData["Error"] = "Vyplňte název akce (maximálně 150 znaků).";
                return RedirectToPage(new { id });
            }

            if (description?.Length > 1000)
            {
                TempData["Error"] = "Popis může mít maximálně 1000 znaků.";
                return RedirectToPage(new { id });
            }

            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] = "Upravit lze pouze akci ve stavu Příprava.";
                return RedirectToPage(new { id });
            }

            if (emailTemplateId.HasValue)
            {
                bool templateExists = await _context.EmailTemplates
                    .AnyAsync(e => e.Id == emailTemplateId.Value && e.IsActive);

                if (!templateExists)
                {
                    TempData["Error"] = "Vybraná šablona neexistuje nebo není aktivní.";
                    return RedirectToPage(new { id });
                }
            }

            campaign.Name = name;
            campaign.Description = description?.Trim();
            campaign.EmailTemplateId = emailTemplateId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Marketingová akce byla upravena.";

            return RedirectToPage(new { id });
        }

    }
}
