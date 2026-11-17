using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<CrmTask> Tasks { get; set; }
            = new List<CrmTask>();

        public async Task OnGetAsync()
        {
            Tasks = await _context.Tasks
                .Include(t => t.Company)
                .Include(t => t.Contact)
                .OrderBy(t => t.Status == "Splněno")
                .ThenBy(t => t.DueDate)
                .ToListAsync();
        }
    }
}