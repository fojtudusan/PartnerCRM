
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MarketingCampaign> Campaigns { get; set; } = new();
        public List<EmailTemplate> EmailTemplates { get; set; } = new();
        public async Task OnGetAsync()
        {
            Campaigns = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .Include(c => c.Recipients)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            EmailTemplates = await _context.EmailTemplates
                .Where(e => e.IsActive)
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync(
            string name,
            string? description,
            int? emailTemplateId)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vyplňte název marketingové akce.";
                return RedirectToPage();
            }

            if (name.Length > 150)
            {
                TempData["Error"] = "Název může mít maximálně 150 znaků.";
                return RedirectToPage();
            }

            if (description?.Length > 1000)
            {
                TempData["Error"] = "Popis může mít maximálně 1000 znaků.";
                return RedirectToPage();
            }

            if (emailTemplateId.HasValue)
            {
                bool templateExists = await _context.EmailTemplates
                    .AnyAsync(e => e.Id == emailTemplateId.Value && e.IsActive);

                if (!templateExists)
                {
                    TempData["Error"] = "Vybraná e-mailová šablona neexistuje nebo není aktivní.";
                    return RedirectToPage();
                }
            }

            var campaign = new MarketingCampaign
            {
                Name = name,
                Description = description?.Trim(),
                EmailTemplateId = emailTemplateId,
                Status = "Příprava",
                CreatedAt = DateTime.Now
            };

            _context.MarketingCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Marketingová akce byla vytvořena.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.Recipients)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Smazat lze pouze marketingovou akci ve stavu Příprava.";

                return RedirectToPage();
            }

            _context.MarketingCampaigns.Remove(campaign);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Marketingová akce byla smazána.";

            return RedirectToPage();
        }

    }
}
