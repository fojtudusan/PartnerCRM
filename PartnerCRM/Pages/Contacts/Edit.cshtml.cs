
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Contact Contact { get; set; } = new();

        [BindProperty]
        public bool ConfirmEmailDuplicate { get; set; }

        public bool EmailDuplicateWarning { get; set; }

        public ContactMatch? EmailMatch { get; set; }

        public class ContactMatch
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string? Email { get; set; }
            public string? Position { get; set; }
            public string CompanyName { get; set; } = "";
        }

        public SelectList CompaniesList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (contact == null)
            {
                return NotFound();
            }

            Contact = contact;

            await LoadCompaniesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var contactToUpdate = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == Contact.Id &&
                    !c.IsMerged);

            // Archivovaný nebo neexistující kontakt nelze upravit.
            if (contactToUpdate == null)
            {
                return NotFound();
            }

            // Kontrola existence aktivní firmy
            var companyExists = await _context.Companies
                .AnyAsync(c =>
                    c.Id == Contact.CompanyId &&
                    !c.IsMerged);

            if (!companyExists)
            {
                ModelState.AddModelError(
                    "Contact.CompanyId",
                    "Vyberte platnou aktivní firmu.");
            }

            // Kontrola duplicitního e-mailu
            if (!string.IsNullOrWhiteSpace(Contact.Email))
            {
                var normalizedEmail = Contact.Email.Trim().ToLower();

                EmailMatch = await _context.Contacts
                    .AsNoTracking()
                    .Where(c =>
                        c.Id != Contact.Id &&
                        !c.IsMerged &&
                        !c.Company.IsMerged &&
                        c.Email != null &&
                        c.Email.Trim().ToLower() == normalizedEmail)
                    .Select(c => new ContactMatch
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        Email = c.Email,
                        Position = c.Position,
                        CompanyName = c.Company.Name
                    })
                    .FirstOrDefaultAsync();

                EmailDuplicateWarning = EmailMatch != null;
            }

            // Kontrola formuláře a potvrzení duplicity
            if (!ModelState.IsValid ||
                (EmailDuplicateWarning && !ConfirmEmailDuplicate))
            {
                await LoadCompaniesAsync();
                return Page();
            }

            // Aktualizace existujícího kontaktu
            contactToUpdate.FirstName = Contact.FirstName;
            contactToUpdate.LastName = Contact.LastName;
            contactToUpdate.CompanyId = Contact.CompanyId;
            contactToUpdate.Position = Contact.Position;
            contactToUpdate.Email = Contact.Email;
            contactToUpdate.Phone = Contact.Phone;
            contactToUpdate.IsPrimary = Contact.IsPrimary;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadCompaniesAsync()
        {
            var companies = await _context.Companies
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompaniesList = new SelectList(
                companies,
                "Id",
                "Name",
                Contact.CompanyId);
        }
    }
}
