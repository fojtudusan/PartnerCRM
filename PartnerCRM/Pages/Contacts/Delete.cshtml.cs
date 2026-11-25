
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Contact Contact { get; set; } = new();

        public int ActivityCount { get; set; }

        public int TaskCount { get; set; }

        public int EmploymentCount { get; set; }

        public bool HasHistory =>
            ActivityCount > 0 ||
            TaskCount > 0 ||
            EmploymentCount > 0;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await LoadContactAsync(id);

            if (!result)
                return NotFound();

            return Page();
        }


        public async Task<IActionResult> OnPostAsync(int id)
        {
            var result = await LoadContactAsync(id);

            if (!result)
                return NotFound();

            if (!Contact.IsActive)
            {
                TempData["SuccessMessage"] =
                    "Kontakt již není aktivní.";

                return RedirectToPage("./Index");
            }

            // Zneaktivnění kontaktu.
            Contact.IsActive = false;

            // Ukončení všech aktuálních působení.
            foreach (var employment in Contact.Employments
                .Where(e => e.IsCurrent))
            {
                employment.IsCurrent = false;
                employment.IsPrimary = false;
                employment.DateTo = DateTime.Today;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Kontakt byl zneaktivněn a jeho aktuální působení byla ukončena. Historie zůstala zachována.";

            return RedirectToPage("./Index");
        }


        private async Task<bool> LoadContactAsync(int id)
        {
            var contact = await _context.Contacts
     .Include(c => c.Company)
     .Include(c => c.Employments)
     .FirstOrDefaultAsync(c =>
         c.Id == id &&
         !c.IsMerged);

            if (contact == null)
                return false;

            Contact = contact;

            ActivityCount = await _context.Activities
                .CountAsync(a => a.ContactId == id);

            TaskCount = await _context.Tasks
                .CountAsync(t => t.ContactId == id);

            EmploymentCount = await _context.ContactEmployments
                .CountAsync(e => e.ContactId == id);

            return true;
        }
    }
}
