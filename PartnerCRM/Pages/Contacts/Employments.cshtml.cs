
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class EmploymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EmploymentsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int CompanyId { get; set; }

        [BindProperty]
        public string? Position { get; set; }

        [BindProperty]
        public DateTime DateFrom { get; set; } = DateTime.Today;

        public Contact Contact { get; set; } = null!;

        public IList<ContactEmployment> Employments { get; set; }
            = new List<ContactEmployment>();

        public SelectList CompaniesList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await LoadDataAsync(id))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int id)
        {
            var contactExists = await _context.Contacts
                .AnyAsync(c => c.Id == id && !c.IsMerged);

            if (!contactExists)
                return NotFound();

            var companyExists = await _context.Companies
                .AnyAsync(c => c.Id == CompanyId && !c.IsMerged);

            if (!companyExists)
            {
                ModelState.AddModelError(
                    nameof(CompanyId),
                    "Vyberte platnou aktivní firmu.");
            }

            var alreadyExists = await _context.ContactEmployments
                .AnyAsync(e =>
                    e.ContactId == id &&
                    e.CompanyId == CompanyId &&
                    e.IsCurrent);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    nameof(CompanyId),
                    "Kontakt již má v této firmě evidované aktivní působení.");
            }

            if (DateFrom == default)
            {
                ModelState.AddModelError(
                    nameof(DateFrom),
                    "Vyplňte datum začátku působení.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDataAsync(id);
                return Page();
            }

            var employment = new ContactEmployment
            {
                ContactId = id,
                CompanyId = CompanyId,
                Position = Position?.Trim(),
                DateFrom = DateFrom,
                IsCurrent = true
            };

            _context.ContactEmployments.Add(employment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Nové působení kontaktu bylo přidáno.";

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostEndAsync(
            int id,
            int employmentId)
        {
            var employment = await _context.ContactEmployments
                .FirstOrDefaultAsync(e =>
                    e.Id == employmentId &&
                    e.ContactId == id &&
                    e.IsCurrent);

            if (employment == null)
                return NotFound();

            employment.IsCurrent = false;
            employment.DateTo = DateTime.Today;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Působení kontaktu bylo ukončeno.";

            return RedirectToPage(new { id });
        }

        private async Task<bool> LoadDataAsync(int id)
        {
            var contact = await _context.Contacts
                .Include(c => c.Company)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (contact == null)
                return false;

            Contact = contact;

            Employments = await _context.ContactEmployments
                .Where(e => e.ContactId == id)
                .Include(e => e.Company)
                .OrderByDescending(e => e.IsCurrent)
                .ThenByDescending(e => e.DateFrom)
                .ToListAsync();

            var companies = await _context.Companies
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompaniesList = new SelectList(
                companies,
                "Id",
                "Name",
                CompanyId);

            return true;
        }
    }
}
