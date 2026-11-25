
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Contact Contact { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var contact = await _context.Contacts
                .Include(c => c.Employments)
                    .ThenInclude(e => e.Company)
                .Include(c => c.MergedIntoContact)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contact == null)
                return NotFound();

            Contact = contact;

            return Page();
        }
    }
}
