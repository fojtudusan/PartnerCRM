using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
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
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            TaskItem = task;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}