
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
        public SelectList ParentActivitiesList { get; set; } = null!;
        public SelectList MarketingCampaignsList { get; set; } = null!;

        public async Task OnGetAsync(
            int? companyId,
            DateTime? activityDate,
            int? parentActivityId,
            int? taskId)
        {
            if (activityDate.HasValue)
                Activity.ActivityDate = activityDate.Value;


            // Aktivita vytvořená z úkolu
            if (taskId.HasValue)
            {
                var task = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == taskId.Value);

                if (task != null)
                {
                    Activity.TaskId = task.Id;

                    if (task.CompanyId.HasValue)
                    {
                        int taskCompanyId = task.CompanyId.Value;

                        var companyExists = await _context.Companies
                        .AnyAsync(c =>
                            c.Id == taskCompanyId &&
                            !c.IsMerged);

                        if (companyExists)
                        {
                            Activity.CompanyId = taskCompanyId;

                            if (task.ContactId.HasValue)
                            {
                                int contactId = task.ContactId.Value;

                                var contactIsValid =
                                    await _context.ContactEmployments
                                        .AnyAsync(e =>
                                            e.ContactId == contactId &&
                                            e.CompanyId == taskCompanyId &&
                                            e.IsCurrent &&
                                            !e.Contact.IsMerged &&
                                            e.Contact.IsActive &&
                                            !e.Company.IsMerged);

                                if (contactIsValid)
                                    Activity.ContactId = contactId;
                            }
                        }
                    }
                }
            }

            // Aktivita vytvořená z jiné aktivity
            else if (parentActivityId.HasValue)
            {
                var parent = await _context.Activities
                    .Include(a => a.Company)
                    .FirstOrDefaultAsync(a =>
                        a.Id == parentActivityId.Value &&
                        !a.Company.IsMerged);

                if (parent != null)
                {
                    Activity.ParentActivityId = parent.Id;
                    Activity.CompanyId = parent.CompanyId;
                    Activity.ContactId = parent.ContactId;
                }
            }
            else if (companyId.HasValue)
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
            // Kontrola firmy
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

            // Kontrola vazby na úkol
            if (Activity.TaskId.HasValue)
            {
                var task = await _context.Tasks
                    .FirstOrDefaultAsync(t =>
                        t.Id == Activity.TaskId.Value);

                if (task == null)
                {
                    ModelState.AddModelError(
                        "Activity.TaskId",
                        "Přiřazený úkol neexistuje.");
                }
                else if (task.CompanyId.HasValue &&
                         task.CompanyId != Activity.CompanyId)
                {
                    ModelState.AddModelError(
                        "Activity.CompanyId",
                        "Firma musí odpovídat firmě přiřazeného úkolu.");
                }
            }

            // Kontrola nadřazené aktivity
            if (Activity.ParentActivityId.HasValue)
            {
                var parent = await _context.Activities
                    .FirstOrDefaultAsync(a =>
                        a.Id == Activity.ParentActivityId.Value);

                if (parent == null)
                {
                    ModelState.AddModelError(
                        "Activity.ParentActivityId",
                        "Vybraná nadřazená aktivita neexistuje.");
                }
                else if (parent.CompanyId != Activity.CompanyId)
                {
                    ModelState.AddModelError(
                        "Activity.ParentActivityId",
                        "Nadřazená aktivita musí patřit stejné firmě.");
                }
                else if (parent.Id == Activity.Id)
                {
                    ModelState.AddModelError(
                        "Activity.ParentActivityId",
                        "Aktivita nemůže být sama sobě nadřazená.");
                }
            }

            // Kontrola kontaktní osoby
            if (Activity.ContactId.HasValue)
            {
                var contactExists = await _context.ContactEmployments
                    .AnyAsync(e =>
                        e.ContactId == Activity.ContactId.Value &&
                        e.CompanyId == Activity.CompanyId &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
                        e.Contact.IsActive &&
                        !e.Company.IsMerged);

                if (!contactExists)
                {
                    ModelState.AddModelError(
                        "Activity.ContactId",
                        "Vyberte aktivní kontaktní osobu působící v této firmě.");
                }
            }

            // Kontrola marketingové kampaně
            if (Activity.MarketingCampaignId.HasValue)
            {
                var campaignExists = await _context.MarketingCampaigns
                    .AnyAsync(c =>
                        c.Id == Activity.MarketingCampaignId.Value);

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

            _context.Activities.Add(Activity);
            await _context.SaveChangesAsync();

            // Návrat na detail úkolu, pokud aktivita vznikla z úkolu
            if (Activity.TaskId.HasValue)
            {
                return RedirectToPage(
                    "/Tasks/Details",
                    new { id = Activity.TaskId.Value });
            }

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

        public async Task<JsonResult> OnGetParentActivityAsync(int id)
        {
            var parent = await _context.Activities
                .FirstOrDefaultAsync(a =>
                    a.Id == id &&
                    !a.Company.IsMerged);

            if (parent == null)
                return new JsonResult(null);

            int? contactId = null;

            if (parent.ContactId.HasValue)
            {
                var contactIsValid = await _context.ContactEmployments
                    .AnyAsync(e =>
                        e.ContactId == parent.ContactId.Value &&
                        e.CompanyId == parent.CompanyId &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
                        e.Contact.IsActive &&
                        !e.Company.IsMerged);

                if (contactIsValid)
                    contactId = parent.ContactId;
            }

            return new JsonResult(new
            {
                companyId = parent.CompanyId,
                contactId = contactId
            });
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

            // Všechny existující aktivity bez ohledu na úroveň hierarchie
            var activities = await _context.Activities
                .Include(a => a.Company)
                .Where(a => !a.Company.IsMerged)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            ParentActivitiesList = new SelectList(
                activities.Select(a => new
                {
                    Id = a.Id,
                    Name = a.Subject + " – " + a.Company.Name
                }),
                "Id",
                "Name",
                Activity.ParentActivityId);

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
