
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
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

        public async Task OnGetAsync(int? companyId)
        {
            await LoadCompaniesAsync();

            if (companyId.HasValue)
            {
                var companyExists = await _context.Companies
                    .AnyAsync(c =>
                        c.Id == companyId.Value &&
                        !c.IsMerged);

                if (companyExists)
                    Contact.CompanyId = companyId.Value;
            }
        }

        // =====================================
        // PRŮBĚŽNÉ VYHLEDÁVÁNÍ KONTAKTŮ
        // =====================================

        public async Task<JsonResult> OnGetSearchContactsAsync(
            string? firstName,
            string? lastName,
            string? email)
        {
            var query = _context.Contacts
                .AsNoTracking()
                .Include(c => c.Company)
                .Where(c =>
                    !c.IsMerged &&
                    !c.Company.IsMerged)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var searchEmail = email.Trim().ToLower();

                if (searchEmail.Length < 3)
                    return new JsonResult(new List<object>());

                query = query.Where(c =>
                    c.Email != null &&
                    c.Email.ToLower().Contains(searchEmail));
            }
            else
            {
                firstName = firstName?.Trim();
                lastName = lastName?.Trim();

                if (string.IsNullOrWhiteSpace(firstName) &&
                    string.IsNullOrWhiteSpace(lastName))
                {
                    return new JsonResult(new List<object>());
                }

                if (!string.IsNullOrWhiteSpace(firstName) &&
                    !string.IsNullOrWhiteSpace(lastName))
                {
                    var first = firstName.ToUpper();
                    var last = lastName.ToUpper();

                    query = query.Where(c =>
                        c.FirstName.ToUpper().Contains(first) &&
                        c.LastName.ToUpper().Contains(last));
                }
                else if (!string.IsNullOrWhiteSpace(firstName))
                {
                    if (firstName.Length < 2)
                        return new JsonResult(new List<object>());

                    var first = firstName.ToUpper();

                    query = query.Where(c =>
                        c.FirstName.ToUpper().Contains(first));
                }
                else
                {
                    if (lastName!.Length < 2)
                        return new JsonResult(new List<object>());

                    var last = lastName.ToUpper();

                    query = query.Where(c =>
                        c.LastName.ToUpper().Contains(last));
                }
            }

            var contacts = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Take(10)
                .Select(c => new ContactMatch
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Position = c.Position,
                    CompanyName = c.Company.Name
                })
                .ToListAsync();

            var results = contacts.Select(c => new
            {
                id = c.Id,
                firstName = c.FirstName,
                lastName = c.LastName,
                email = c.Email,
                position = c.Position,
                companyName = c.CompanyName,
                editUrl = Url.Page(
                    "./Edit",
                    new { id = c.Id })
            });

            return new JsonResult(results);
        }

        // =====================================
        // ULOŽENÍ NOVÉHO KONTAKTU
        // =====================================

        public async Task<IActionResult> OnPostAsync()
        {
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

            // Vytvoření prvního aktivního působení
            var employment = new ContactEmployment
            {
                Contact = Contact,
                CompanyId = Contact.CompanyId,
                Position = Contact.Position,
                DateFrom = DateTime.Today,
                IsCurrent = true
            };

            Contact.Employments.Add(employment);

            // Kontakt i jeho první působení uloží EF společně
            _context.Contacts.Add(Contact);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        // =====================================
        // NAČTENÍ AKTIVNÍCH FIREM
        // =====================================

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
