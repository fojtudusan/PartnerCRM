
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class EmailTemplatesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EmailTemplatesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<EmailTemplate> EmailTemplates { get; set; } = new();

        [BindProperty]
        public string NewName { get; set; } = string.Empty;

        [BindProperty]
        public string NewSubject { get; set; } = string.Empty;

        [BindProperty]
        public string NewBody { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            await LoadTemplatesAsync();
        }

        private async Task LoadTemplatesAsync()
        {
            EmailTemplates = await _context.EmailTemplates
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            NewName = NewName.Trim();
            NewSubject = NewSubject.Trim();
            NewBody = NewBody.Trim();

            if (string.IsNullOrWhiteSpace(NewName) ||
                string.IsNullOrWhiteSpace(NewSubject) ||
                string.IsNullOrWhiteSpace(NewBody))
            {
                TempData["Error"] = "Vyplňte název, předmět i text e-mailu.";
                return RedirectToPage();
            }

            if (NewName.Length > 150 || NewSubject.Length > 250)
            {
                TempData["Error"] = "Název nebo předmět překračuje povolenou délku.";
                return RedirectToPage();
            }

            var template = new EmailTemplate
            {
                Name = NewName,
                Subject = NewSubject,
                Body = NewBody,
                IsActive = true
            };

            _context.EmailTemplates.Add(template);
            await _context.SaveChangesAsync();

            TempData["Success"] = "E-mailová šablona byla přidána.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            int id,
            string name,
            string subject,
            string body)
        {
            name = name.Trim();
            subject = subject.Trim();
            body = body.Trim();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(body))
            {
                TempData["Error"] = "Vyplňte název, předmět i text e-mailu.";
                return RedirectToPage();
            }

            if (name.Length > 150 || subject.Length > 250)
            {
                TempData["Error"] = "Název nebo předmět překračuje povolenou délku.";
                return RedirectToPage();
            }

            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == id);

            if (template == null)
                return NotFound();

            template.Name = name;
            template.Subject = subject;
            template.Body = body;

            await _context.SaveChangesAsync();

            TempData["Success"] = "E-mailová šablona byla upravena.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id)
        {
            var template = await _context.EmailTemplates
                .FirstOrDefaultAsync(e => e.Id == id);

            if (template == null)
                return NotFound();

            template.IsActive = !template.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = template.IsActive
                ? "E-mailová šablona byla aktivována."
                : "E-mailová šablona byla deaktivována.";

            return RedirectToPage();
        }
    }
}
