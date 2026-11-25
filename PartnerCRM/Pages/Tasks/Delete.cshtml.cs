
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Tasks
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CrmTask TaskItem { get; set; } = null!;

        public bool CanDelete { get; set; }

        public List<string> DeleteRestrictions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var task = await LoadTaskAsync(id.Value);

            if (task == null)
                return NotFound();

            TaskItem = task;

            await CheckDeleteRestrictionsAsync(task.Id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            await CheckDeleteRestrictionsAsync(task.Id);

            if (!CanDelete)
            {
                TaskItem = await LoadTaskAsync(id) ?? task;
                return Page();
            }

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task<CrmTask?> LoadTaskAsync(int id)
        {
            return await _context.Tasks
                .Include(t => t.Company)
                .Include(t => t.Contact)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        private async Task CheckDeleteRestrictionsAsync(int taskId)
        {
            DeleteRestrictions.Clear();

            bool hasChildTasks = await _context.Tasks
                .AnyAsync(t => t.ParentTaskId == taskId);

            bool hasActivities = await _context.Activities
                .AnyAsync(a => a.TaskId == taskId);

            bool hasSourceActivity = await _context.Tasks
                .AnyAsync(t => t.Id == taskId && t.ActivityId != null);

            if (hasChildTasks)
            {
                DeleteRestrictions.Add(
                    "Úkol obsahuje podúkoly. Nejdříve je nutné vyřešit nebo odstranit podúkoly.");
            }

            if (hasActivities)
            {
                DeleteRestrictions.Add(
                    "K úkolu jsou přiřazeny související aktivity. Je nutné zachovat jejich historii.");
            }

            if (hasSourceActivity)
            {
                DeleteRestrictions.Add(
                    "Úkol vznikl z aktivity. Nelze jej odstranit, protože je součástí historie CRM.");
            }

            CanDelete = DeleteRestrictions.Count == 0;
        }
    }
}
