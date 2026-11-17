
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class ResultsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ResultsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MarketingCampaign> Campaigns { get; set; } = new();

        public int TotalRecipients { get; set; }
        public int SentCount { get; set; }
        public int ReadyCount { get; set; }
        public int ErrorCount { get; set; }

        public async Task OnGetAsync()
        {
            Campaigns = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Company)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Contact)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var recipients = Campaigns
                .SelectMany(c => c.Recipients)
                .ToList();

            TotalRecipients = recipients.Count;

            SentCount = recipients.Count(r => r.Status == "Odesláno");

            ReadyCount = recipients.Count(r => r.Status == "Připraveno");

            ErrorCount = recipients.Count(r => r.Status == "Chyba");
        }
    }
}
