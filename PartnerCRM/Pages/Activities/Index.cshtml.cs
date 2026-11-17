using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Activities
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Activity> Activities { get; set; }
            = new List<Activity>();

        public SelectList CompaniesList { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public int? CompanyId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? ActivityType { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateTo { get; set; }

        public async Task OnGetAsync()
        {
            CompaniesList = new SelectList(
                await _context.Companies
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                CompanyId);

            var query = _context.Activities
                .Include(a => a.Company)
                .Include(a => a.Contact)
                .AsQueryable();

            if (CompanyId.HasValue)
            {
                query = query.Where(a => a.CompanyId == CompanyId.Value);
            }
            if (DateFrom.HasValue)
            {
                query = query.Where(a => a.ActivityDate >= DateFrom.Value);
            }

            if (DateTo.HasValue)
            {
                var endDate = DateTo.Value.Date.AddDays(1);
                query = query.Where(a => a.ActivityDate < endDate);
            }

            Activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();
        }
    }
}