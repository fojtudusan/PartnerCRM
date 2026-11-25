
using Microsoft.AspNetCore.Mvc;
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
        public bool ShowInactive { get; set; }

        public async Task OnGetAsync(
            bool showArchived = false,
            bool showInactive = false)
        {
            ShowArchived = showArchived;
            ShowInactive = showInactive && !showArchived;

            var query = _context.Contacts
                .Where(c => c.IsMerged == ShowArchived);

            if (!ShowArchived)
            {
                query = query.Where(c => c.IsActive != ShowInactive);
            }

            Contacts = await query
                .Include(c => c.Company)
                .Include(c => c.MergedIntoContact)
                .Include(c => c.Employments)
                    .ThenInclude(e => e.Company)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsMerged);

            if (contact == null)
            {
                return NotFound();
            }

            if (contact.IsActive)
            {
                TempData["SuccessMessage"] =
                    "Kontakt je již aktivní.";
            }
            else
            {
                contact.IsActive = true;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Kontakt byl úspěšně znovu aktivován.";
            }

            return RedirectToPage("./Index");
        }
    }
}
