
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Souhrnné statistiky
        public int CompaniesCount { get; set; }
        public int ContactsCount { get; set; }
        public int ActivitiesCount { get; set; }
        public int OpenTasksCount { get; set; }
        public int OverdueTasksCount { get; set; }

        // Seznamy pro dashboard
        public IList<CrmTask> UpcomingTasks { get; set; }
            = new List<CrmTask>();

        public IList<Activity> RecentActivities { get; set; }
            = new List<Activity>();

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;

            CompaniesCount = await _context.Companies.CountAsync();

            ContactsCount = await _context.Contacts.CountAsync();

            ActivitiesCount = await _context.Activities.CountAsync();

            OpenTasksCount = await _context.Tasks
                .CountAsync(t =>
                    t.Status != "Splněno" &&
                    t.Status != "Zrušeno");

            OverdueTasksCount = await _context.Tasks
                .CountAsync(t =>
                    t.Status != "Splněno" &&
                    t.Status != "Zrušeno" &&
                    t.DueDate < today);

            UpcomingTasks = await _context.Tasks
                .Include(t => t.Company)
                .Where(t =>
                    t.Status != "Splněno" &&
                    t.Status != "Zrušeno")
                .OrderBy(t => t.DueDate)
                .Take(5)
                .ToListAsync();

            RecentActivities = await _context.Activities
                .Include(a => a.Company)
                .OrderByDescending(a => a.ActivityDate)
                .Take(5)
                .ToListAsync();
        }
    }
}
