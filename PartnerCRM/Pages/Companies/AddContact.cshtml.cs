
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Companies
{
    public class AddContactModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddContactModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Company Company { get; set; } = null!;

        public SelectList ContactsList { get; set; } = null!;

        [BindProperty]
        public int ContactId { get; set; }

        [BindProperty]
        public string? Position { get; set; }

        [BindProperty]
        public DateTime DateFrom { get; set; } = DateTime.Today;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (!await LoadDataAsync(id))
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (company == null)
                return NotFound();

            var contactExists = await _context.Contacts
                .AnyAsync(c =>
                    c.Id == ContactId &&
                    !c.IsMerged);

            if (!contactExists)
            {
                ModelState.AddModelError(
                    nameof(ContactId),
                    "Vyberte platný existující kontakt.");
            }

            var alreadyExists = await _context.ContactEmployments
                .AnyAsync(e =>
                    e.ContactId == ContactId &&
                    e.CompanyId == id &&
                    e.IsCurrent);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    nameof(ContactId),
                    "Tento kontakt již v dané firmě aktivně působí.");
            }

            if (DateFrom == default)
            {
                ModelState.AddModelError(
                    nameof(DateFrom),
                    "Vyplňte datum začátku působení.");
            }

            if (!ModelState.IsValid)
            {
                if (!await LoadDataAsync(id))
                    return NotFound();

                return Page();
            }

            var employment = new ContactEmployment
            {
                ContactId = ContactId,
                CompanyId = id,
                Position = Position?.Trim(),
                DateFrom = DateFrom,
                IsCurrent = true
            };

            _context.ContactEmployments.Add(employment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Existující kontakt byl úspěšně přidán k firmě.";

            return RedirectToPage("./Details", new { id });
        }

        private async Task<bool> LoadDataAsync(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (company == null)
                return false;

            Company = company;

            var contacts = await _context.Contacts
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            ContactsList = new SelectList(
                contacts,
                "Id",
                "LastName",
                ContactId);

            return true;
        }
    }
}
