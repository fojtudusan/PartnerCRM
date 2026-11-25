
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public List<SelectListItem> CompaniesList { get; set; }
            = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CompanyFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DueDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DueDateTo { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCompaniesAsync();

            var query = _context.Tasks
                .Include(t => t.Company)
                .Include(t => t.Contact)
                .Include(t => t.ChildTasks)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                query = query.Where(t => t.Status == StatusFilter);
            }

            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                query = query.Where(t => t.Priority == PriorityFilter);
            }

            if (CompanyFilter.HasValue)
            {
                query = query.Where(t => t.CompanyId == CompanyFilter.Value);
            }

            if (DueDateFrom.HasValue)
            {
                var from = DueDateFrom.Value.Date;
                query = query.Where(t => t.DueDate >= from);
            }

            if (DueDateTo.HasValue)
            {
                var to = DueDateTo.Value.Date.AddDays(1);
                query = query.Where(t => t.DueDate < to);
            }

            Tasks = await query
                .OrderBy(t => t.Status == "Splněno")
                .ThenBy(t => t.DueDate)
                .ToListAsync();
        }

        private async Task LoadCompaniesAsync()
        {
            CompaniesList = await _context.Companies
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }
    }
}
