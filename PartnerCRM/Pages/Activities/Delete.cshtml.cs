using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Activities
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Activity Activity { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var activity = await _context.Activities
                .Include(a => a.Company)
                .Include(a => a.Contact)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            Activity = activity;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
            {
                return NotFound();
            }

            _context.Activities.Remove(activity);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}