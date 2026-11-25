
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CrmTask TaskItem { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await _context.Tasks
                .Include(t => t.Company)
                .Include(t => t.Contact)
                .Include(t => t.Activity)
                    .ThenInclude(a => a!.Company)
                .Include(t => t.ParentTask)
                .Include(t => t.ChildTasks)
                    .ThenInclude(t => t.Company)
                .Include(t => t.Activities)
                    .ThenInclude(a => a.Company)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            TaskItem = task;

            return Page();
        }
    }
}
