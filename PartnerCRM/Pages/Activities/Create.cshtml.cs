
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Activities
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Activity Activity { get; set; } = new();

        public SelectList CompaniesList { get; set; } = null!;
        public SelectList ContactsList { get; set; } = null!;

        public async Task OnGetAsync(int? companyId, DateTime? activityDate)
        {
            if (activityDate.HasValue)
                Activity.ActivityDate = activityDate.Value;

            if (companyId.HasValue)
            {
                var companyExists = await _context.Companies
                    .AnyAsync(c =>
                        c.Id == companyId.Value &&
                        !c.IsMerged);

                if (companyExists)
                    Activity.CompanyId = companyId.Value;
            }

            await LoadListsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var companyExists = await _context.Companies
                .AnyAsync(c =>
                    c.Id == Activity.CompanyId &&
                    !c.IsMerged);

            if (!companyExists)
            {
                ModelState.AddModelError(
                    "Activity.CompanyId",
                    "Vyberte platnou aktivní firmu.");
            }

            if (Activity.ContactId.HasValue)
            {
                var contactExists = await _context.ContactEmployments
                    .AnyAsync(e =>
                        e.ContactId == Activity.ContactId.Value &&
                        e.CompanyId == Activity.CompanyId &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
                        !e.Company.IsMerged);

                if (!contactExists)
                {
                    ModelState.AddModelError(
                        "Activity.ContactId",
                        "Vyberte aktivní kontaktní osobu působící v této firmě.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            _context.Activities.Add(Activity);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<JsonResult> OnGetContactsAsync(int companyId)
        {
            var contacts = await _context.ContactEmployments
                .Where(e =>
                    e.CompanyId == companyId &&
                    e.IsCurrent &&
                    !e.Contact.IsMerged &&
                    !e.Company.IsMerged)
                .Select(e => e.Contact)
                .Distinct()
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.FirstName + " " + c.LastName
                })
                .ToListAsync();

            return new JsonResult(contacts);
        }

        private async Task LoadListsAsync()
        {
            var companies = await _context.Companies
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompaniesList = new SelectList(
                companies,
                "Id",
                "Name",
                Activity.CompanyId);

            var contacts = await _context.ContactEmployments
                .Where(e =>
                    e.IsCurrent &&
                    !e.Contact.IsMerged &&
                    !e.Company.IsMerged)
                .Select(e => new
                {
                    Contact = e.Contact,
                    Company = e.Company
                })
                .OrderBy(x => x.Contact.LastName)
                .ThenBy(x => x.Contact.FirstName)
                .ToListAsync();

            ContactsList = new SelectList(
                contacts.Select(x => new
                {
                    Id = x.Contact.Id,
                    Name = x.Contact.FirstName + " " +
                           x.Contact.LastName + " – " +
                           x.Company.Name
                })
                .GroupBy(x => new { x.Id, x.Name })
                .Select(g => g.First()),
                "Id",
                "Name",
                Activity.ContactId);
        }
    }
}
