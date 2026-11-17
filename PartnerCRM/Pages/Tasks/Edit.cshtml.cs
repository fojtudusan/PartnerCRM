
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CrmTask TaskItem { get; set; } = new();

        public SelectList CompaniesList { get; set; } = null!;
        public SelectList ContactsList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            TaskItem = task;

            await LoadListsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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

            if (TaskItem.ContactId.HasValue)
            {
                var employments = await _context.ContactEmployments
                    .Where(e =>
                        e.ContactId == TaskItem.ContactId.Value &&
                        e.IsCurrent &&
                        !e.Contact.IsMerged &&
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

            var existingTask = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == TaskItem.Id);

            if (existingTask == null)
                return NotFound();

            existingTask.Title = TaskItem.Title;
            existingTask.CompanyId = TaskItem.CompanyId;
            existingTask.ContactId = TaskItem.ContactId;
            existingTask.DueDate = TaskItem.DueDate;
            existingTask.Priority = TaskItem.Priority;
            existingTask.Status = TaskItem.Status;
            existingTask.Description = TaskItem.Description;

            if (existingTask.Status == "Splněno")
            {
                existingTask.CompletedAt ??= DateTime.Now;
            }
            else
            {
                existingTask.CompletedAt = null;
            }

            await _context.SaveChangesAsync();

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
    }
}
