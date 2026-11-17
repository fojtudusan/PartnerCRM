
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Companies
{
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Company Company { get; set; } = new();

        public int ContactCount { get; set; }
        public int EmploymentCount { get; set; }
        public int ActivityCount { get; set; }
        public int TaskCount { get; set; }
        public int BranchCount { get; set; }
        public int MergedCompanyCount { get; set; }

        public bool HasDependencies =>
            ContactCount > 0 ||
            EmploymentCount > 0 ||
            ActivityCount > 0 ||
            TaskCount > 0 ||
            BranchCount > 0 ||
            MergedCompanyCount > 0;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await LoadCompanyAsync(id);

            if (!result)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var result = await LoadCompanyAsync(id);

            if (!result)
                return NotFound();

            if (HasDependencies)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Firmu nelze smazat, protože má navázané kontakty, působení kontaktů, aktivity, úkoly nebo podřízené firmy. Doporučujeme ji ponechat nebo sloučit s jinou firmou.");

                return Page();
            }

            _context.Companies.Remove(Company);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                await LoadCompanyAsync(id);

                ModelState.AddModelError(
                    string.Empty,
                    "Firmu nelze smazat, protože na ni odkazují jiné záznamy.");

                return Page();
            }

            TempData["SuccessMessage"] =
                "Firma byla úspěšně smazána.";

            return RedirectToPage("./Index");
        }

        private async Task<bool> LoadCompanyAsync(int id)
        {
            var company = await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (company == null)
                return false;

            Company = company;

            ContactCount = await _context.Contacts
                .CountAsync(c => c.CompanyId == id);

            EmploymentCount = await _context.ContactEmployments
                .CountAsync(e => e.CompanyId == id);

            ActivityCount = await _context.Activities
                .CountAsync(a => a.CompanyId == id);

            TaskCount = await _context.Tasks
                .CountAsync(t => t.CompanyId == id);

            BranchCount = await _context.Companies
                .CountAsync(c => c.ParentCompanyId == id);

            MergedCompanyCount = await _context.Companies
                .CountAsync(c => c.MergedIntoCompanyId == id);

            return true;
        }
    }
}
