
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Activities
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Activity Activity { get; set; } = null!;

        public SelectList CompaniesList { get; set; } = null!;
        public SelectList ContactsList { get; set; } = null!;
        public SelectList MarketingCampaignsList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == id);

            if (activity == null)
                return NotFound();

            Activity = activity;

            await LoadListsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.Id == Activity.Id);

            if (activity == null)
                return NotFound();

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
                var employmentExists = await _context.ContactEmployments
                    .AnyAsync(e =>
                        e.ContactId == Activity.ContactId.Value &&
                        e.CompanyId == Activity.CompanyId &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
                        e.Contact.IsActive &&
                        !e.Company.IsMerged);

                if (!employmentExists)
                {
                    ModelState.AddModelError(
                        "Activity.ContactId",
                        "Kontakt v této firmě aktuálně nepůsobí nebo není aktivní.");
                }
            }
            // Kontrola marketingové kampaně
            if (Activity.MarketingCampaignId.HasValue)
            {
                var campaignExists = await _context.MarketingCampaigns
                    .AnyAsync(c => c.Id == Activity.MarketingCampaignId.Value);

                if (!campaignExists)
                {
                    ModelState.AddModelError(
                        "Activity.MarketingCampaignId",
                        "Vybraná marketingová kampaň neexistuje.");
                }
            }
            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            activity.CompanyId = Activity.CompanyId;
            activity.ContactId = Activity.ContactId;
            activity.MarketingCampaignId = Activity.MarketingCampaignId;
            activity.Type = Activity.Type;
            activity.Subject = Activity.Subject;
            activity.ActivityDate = Activity.ActivityDate;
            activity.Description = Activity.Description;
            activity.Result = Activity.Result;

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
                    e.Contact.IsActive &&
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
                    e.Contact.IsActive &&
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
            // Marketingové kampaně
            var campaigns = await _context.MarketingCampaigns
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            MarketingCampaignsList = new SelectList(
                campaigns,
                "Id",
                "Name",
                Activity.MarketingCampaignId);
        }
    }
}
