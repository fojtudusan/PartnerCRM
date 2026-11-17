
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Calendar
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public DateTime CurrentMonth { get; set; }

        public IList<Activity> Activities { get; set; }
            = new List<Activity>();

        public IList<CrmTask> Tasks { get; set; }
            = new List<CrmTask>();

        public async Task OnGetAsync(int? year, int? month)
        {
            var today = DateTime.Today;

            int selectedYear = year ?? today.Year;
            int selectedMonth = month ?? today.Month;

            CurrentMonth = new DateTime(
                selectedYear, selectedMonth, 1);

            var monthStart = CurrentMonth;
            var monthEnd = monthStart.AddMonths(1);

            Activities = await _context.Activities
                .Include(a => a.Company)
                .Where(a => a.ActivityDate >= monthStart &&
                            a.ActivityDate < monthEnd)
                .OrderBy(a => a.ActivityDate)
                .ToListAsync();

            Tasks = await _context.Tasks
                .Include(t => t.Company)
                .Where(t => t.DueDate >= monthStart &&
                            t.DueDate < monthEnd)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }
    }
}
