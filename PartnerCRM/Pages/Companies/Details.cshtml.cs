
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Companies
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Company Company { get; set; } = null!;

        public IList<ContactEmployment> Employments { get; set; }
            = new List<ContactEmployment>();

        public IList<Activity> Activities { get; set; }
            = new List<Activity>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var company = await _context.Companies
                .Include(c => c.CompanyTypes)
                .Include(c => c.City)
                .Include(c => c.Country)
                .Include(c => c.Bank)
                .Include(c => c.MergedIntoCompany)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
                return NotFound();

            Company = company;

            // Aktivní kontakty působící v této firmě
            Employments = await _context.ContactEmployments
                .Where(e =>
                    e.CompanyId == id &&
                    e.IsCurrent &&
                    !e.Contact.IsMerged)
                .Include(e => e.Contact)
                .OrderBy(e => e.Contact.LastName)
                .ThenBy(e => e.Contact.FirstName)
                .ToListAsync();

            // Historie aktivit firmy
            Activities = await _context.Activities
                .Where(a => a.CompanyId == id)
                .Include(a => a.Contact)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return Page();
        }
    }
}
