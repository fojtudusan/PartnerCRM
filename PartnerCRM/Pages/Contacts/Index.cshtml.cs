
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Contact> Contacts { get; set; }
            = new List<Contact>();

        public bool ShowArchived { get; set; }

        public async Task OnGetAsync(bool showArchived = false)
        {
            ShowArchived = showArchived;

            Contacts = await _context.Contacts
                .Where(c => c.IsMerged == ShowArchived)
                .Include(c => c.Company)
                .Include(c => c.MergedIntoContact)
                .Include(c => c.Employments)
                    .ThenInclude(e => e.Company)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
        }
    }
}
