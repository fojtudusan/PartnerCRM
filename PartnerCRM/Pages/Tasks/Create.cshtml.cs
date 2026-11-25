
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CrmTask TaskItem { get; set; } = new();

        public SelectList CompaniesList { get; set; } = null!;
        public SelectList ContactsList { get; set; } = null!;
        public SelectList ActivitiesList { get; set; } = null!;
        public SelectList ParentTasksList { get; set; } = null!;

        public async Task OnGetAsync(
            int? companyId,
            DateTime? dueDate,
            int? activityId,
            int? parentTaskId)
        {
            if (dueDate.HasValue)
                TaskItem.DueDate = dueDate.Value;

            // Vytvoření podúkolu
            if (parentTaskId.HasValue)
            {
                var parentTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == parentTaskId.Value);

                if (parentTask != null)
                {
                    TaskItem.ParentTaskId = parentTask.Id;
                    TaskItem.CompanyId = parentTask.CompanyId;
                    TaskItem.ContactId = parentTask.ContactId;
                    TaskItem.ActivityId = parentTask.ActivityId;
                }
            }
            // Vytvoření úkolu z aktivity
            else if (activityId.HasValue)
            {
                var activity = await _context.Activities
                    .Include(a => a.Company)
                    .FirstOrDefaultAsync(a =>
                        a.Id == activityId.Value &&
                        !a.Company.IsMerged);

                if (activity != null)
                {
                    TaskItem.ActivityId = activity.Id;
                    TaskItem.CompanyId = activity.CompanyId;
                    TaskItem.ContactId = activity.ContactId;
                }
            }
            // Vytvoření úkolu z firmy
            else if (companyId.HasValue)
            {
                var companyExists = await _context.Companies
                    .AnyAsync(c =>
                        c.Id == companyId.Value &&
                        !c.IsMerged);

                if (companyExists)
                    TaskItem.CompanyId = companyId.Value;
            }

            await LoadListsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CrmTask? parentTask = null;

            // Kontrola nadřazeného úkolu
            if (TaskItem.ParentTaskId.HasValue)
            {
                parentTask = await _context.Tasks
                    .FirstOrDefaultAsync(t =>
                        t.Id == TaskItem.ParentTaskId.Value);

                if (parentTask == null)
                {
                    ModelState.AddModelError(
                        "TaskItem.ParentTaskId",
                        "Vybraný nadřazený úkol neexistuje.");
                }
                else
                {
                    // Převzetí údajů z nadřazeného úkolu
                    TaskItem.CompanyId = parentTask.CompanyId;
                    TaskItem.ContactId = parentTask.ContactId;
                    TaskItem.ActivityId = parentTask.ActivityId;
                }
            }

            // Kontrola zdrojové aktivity
            if (TaskItem.ActivityId.HasValue)
            {
                var activity = await _context.Activities
                    .Include(a => a.Company)
                    .FirstOrDefaultAsync(a =>
                        a.Id == TaskItem.ActivityId.Value &&
                        !a.Company.IsMerged);

                if (activity == null)
                {
                    ModelState.AddModelError(
                        "TaskItem.ActivityId",
                        "Vybraná aktivita neexistuje.");
                }
                else
                {
                    if (TaskItem.CompanyId.HasValue &&
                        TaskItem.CompanyId != activity.CompanyId)
                    {
                        ModelState.AddModelError(
                            "TaskItem.CompanyId",
                            "Firma neodpovídá vybrané aktivitě.");
                    }
                    else
                    {
                        TaskItem.CompanyId = activity.CompanyId;

                        if (!TaskItem.ContactId.HasValue)
                            TaskItem.ContactId = activity.ContactId;
                    }
                }
            }

            // Kontrola firmy
            if (TaskItem.CompanyId.HasValue)
            {
                var companyExists = await _context.Companies
                    .AnyAsync(c =>
                        c.Id == TaskItem.CompanyId.Value &&
                        !c.IsMerged);

                if (!companyExists)
                {
                    ModelState.AddModelError(
                        "TaskItem.CompanyId",
                        "Vybraná firma neexistuje nebo je archivovaná.");
                }
            }

            // Kontrola kontaktní osoby
            if (TaskItem.ContactId.HasValue)
            {
                var employments = await _context.ContactEmployments
                    .Where(e =>
                        e.ContactId == TaskItem.ContactId.Value &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
                        e.Contact.IsActive &&
                        !e.Company.IsMerged)
                    .ToListAsync();

                if (!employments.Any())
                {
                    ModelState.AddModelError(
                        "TaskItem.ContactId",
                        "Kontakt nemá žádné aktivní působení ve firmě.");
                }
                else if (TaskItem.CompanyId.HasValue)
                {
                    var employmentExists = employments.Any(e =>
                        e.CompanyId == TaskItem.CompanyId.Value);

                    if (!employmentExists)
                    {
                        ModelState.AddModelError(
                            "TaskItem.ContactId",
                            "Kontakt v této firmě aktuálně nepůsobí.");
                    }
                }
                else if (employments.Count == 1)
                {
                    TaskItem.CompanyId = employments[0].CompanyId;
                }
                else
                {
                    ModelState.AddModelError(
                        "TaskItem.CompanyId",
                        "Kontakt působí ve více firmách. Vyberte firmu.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            if (TaskItem.Status == "Splněno")
            {
                TaskItem.CompletedAt = DateTime.Now;
            }

            _context.Tasks.Add(TaskItem);

            await _context.SaveChangesAsync();

            // Při vytvoření běžného úkolu z aktivity
            // se vrátíme na detail zdrojové aktivity.
            if (!TaskItem.ParentTaskId.HasValue &&
                TaskItem.ActivityId.HasValue)
            {
                return RedirectToPage(
                    "/Activities/Details",
                    new { id = TaskItem.ActivityId.Value });
            }

            return RedirectToPage("./Index");
        }

        private async Task LoadListsAsync()
        {
            CompaniesList = new SelectList(
                await _context.Companies
                    .Where(c => !c.IsMerged)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Id",
                "Name",
                TaskItem.CompanyId);

            var contactsQuery = _context.ContactEmployments
                .Where(e =>
                    e.IsCurrent &&
                    !e.Contact.IsMerged &&
                    e.Contact.IsActive &&
                    !e.Company.IsMerged);

            if (TaskItem.CompanyId.HasValue)
            {
                contactsQuery = contactsQuery.Where(
                    e => e.CompanyId == TaskItem.CompanyId.Value);
            }

            var contacts = await contactsQuery
                .Select(e => e.Contact)
                .Distinct()
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Select(c => new
                {
                    c.Id,
                    Name = c.LastName + " " + c.FirstName
                })
                .ToListAsync();

            ContactsList = new SelectList(
                contacts,
                "Id",
                "Name",
                TaskItem.ContactId);

            var activities = await _context.Activities
                .Where(a => !a.Company.IsMerged)
                .OrderByDescending(a => a.ActivityDate)
                .Select(a => new
                {
                    a.Id,
                    Name = a.Subject + " – " + a.Company.Name
                })
                .ToListAsync();

            ActivitiesList = new SelectList(
                activities,
                "Id",
                "Name",
                TaskItem.ActivityId);

            var parentTasks = await _context.Tasks
                .Include(t => t.Company)
                .OrderBy(t => t.Title)
                .Select(t => new
                {
                    t.Id,
                    Name = t.Title + " – " +
                        (t.Company != null ? t.Company.Name : "bez firmy")
                })
                .ToListAsync();

            ParentTasksList = new SelectList(
                parentTasks,
                "Id",
                "Name",
                TaskItem.ParentTaskId);
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
    }
}
