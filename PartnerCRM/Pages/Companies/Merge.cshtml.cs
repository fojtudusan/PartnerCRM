
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Companies
{
    public class MergeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MergeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int? KeepCompanyId { get; set; }

        [BindProperty]
        public int? MergeCompanyId { get; set; }

        [BindProperty]
        public bool ConfirmMerge { get; set; }

        public SelectList KeepCompanies { get; set; } = null!;
        public SelectList MergeCompanies { get; set; } = null!;

        public async Task OnGetAsync()
        {
            await LoadCompaniesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!KeepCompanyId.HasValue ||
                !MergeCompanyId.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Vyberte obě firmy.");
            }
            else if (KeepCompanyId == MergeCompanyId)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Nelze sloučit firmu samu se sebou.");
            }

            if (!ConfirmMerge)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Před sloučením musíte potvrdit, že jste zkontrolovali vybrané firmy.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCompaniesAsync();
                return Page();
            }
            int keepCompanyId = KeepCompanyId.GetValueOrDefault();
            int mergeCompanyId = MergeCompanyId.GetValueOrDefault();

            var keepCompany = await _context.Companies
                 .Include(c => c.CompanyTypes)
                 .FirstOrDefaultAsync(c =>
                     c.Id == keepCompanyId);

            var mergeCompany = await _context.Companies
                .Include(c => c.CompanyTypes)
                .FirstOrDefaultAsync(c =>
                    c.Id == mergeCompanyId);

            if (keepCompany == null || mergeCompany == null)
            {
                return NotFound();
            }

            if (keepCompany.IsMerged || mergeCompany.IsMerged)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Jednu z vybraných firem již nelze sloučit, protože je archivovaná.");

                await LoadCompaniesAsync();
                return Page();
            }

            // Zabránění vzniku cyklu v organizační struktuře.
            if (await IsDescendantAsync(
                keepCompany.Id,
                mergeCompany.Id))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Sloučení není možné: zachovávaná firma je podřízená firmě, kterou chcete archivovat. Nejprve upravte organizační strukturu.");

                await LoadCompaniesAsync();
                return Page();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 1. Převod kontaktů
            var contacts = await _context.Contacts
                .Where(c => c.CompanyId == mergeCompany.Id)
                .ToListAsync();

            foreach (var contact in contacts)
            {
                contact.CompanyId = keepCompany.Id;
            }

            // 2. Převod aktivit
            var activities = await _context.Activities
                .Where(a => a.CompanyId == mergeCompany.Id)
                .ToListAsync();

            foreach (var activity in activities)
            {
                activity.CompanyId = keepCompany.Id;
            }

            // 3. Převod úkolů
            var tasks = await _context.Tasks
                .Where(t => t.CompanyId == mergeCompany.Id)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.CompanyId = keepCompany.Id;
            }

            // 4. Převod působení kontaktů
            var companyEmployments = await _context.ContactEmployments
                .Where(e =>
                    e.CompanyId == keepCompany.Id ||
                    e.CompanyId == mergeCompany.Id)
                .ToListAsync();

            var employmentsToMove = companyEmployments
                .Where(e => e.CompanyId == mergeCompany.Id)
                .ToList();

            foreach (var employment in employmentsToMove)
            {
                // Historická působení převádíme beze změny.
                if (!employment.IsCurrent)
                {
                    employment.CompanyId = keepCompany.Id;
                    continue;
                }

                // Hledáme již existující aktivní působení
                // stejného kontaktu v cílové firmě.
                var existingActive = companyEmployments.FirstOrDefault(e =>
                    e.ContactId == employment.ContactId &&
                    e.CompanyId == keepCompany.Id &&
                    e.IsCurrent &&
                    e.Id != employment.Id);

                // Pokud duplicita neexistuje, pouze změníme firmu.
                if (existingActive == null)
                {
                    employment.CompanyId = keepCompany.Id;
                    continue;
                }

                // Zachováme nejstarší datum začátku působení.
                if (employment.DateFrom < existingActive.DateFrom)
                {
                    existingActive.DateFrom = employment.DateFrom;
                }

                // Zachováme informace o převáděném působení.
                var originalInfo =
                    $"Původní působení ve slučované firmě " +
                    $"(od {employment.DateFrom:dd.MM.yyyy})";

                if (!string.IsNullOrWhiteSpace(employment.Position))
                {
                    originalInfo += $", pozice: {employment.Position}";

                    if (string.IsNullOrWhiteSpace(existingActive.Position))
                    {
                        existingActive.Position = employment.Position;
                    }
                }

                if (!string.IsNullOrWhiteSpace(employment.Notes))
                {
                    originalInfo += $". Poznámky: {employment.Notes}";
                }

                // Připojíme informace k zachovávanému záznamu.
                existingActive.Notes =
                    string.IsNullOrWhiteSpace(existingActive.Notes)
                        ? originalInfo
                        : existingActive.Notes +
                          Environment.NewLine +
                          originalInfo;

                // Duplicitní aktivní záznam již není potřeba.
                _context.ContactEmployments.Remove(employment);
            }

            // 5. Převod podřízených firem
            var branches = await _context.Companies
                .Where(c => c.ParentCompanyId == mergeCompany.Id)
                .ToListAsync();

            foreach (var branch in branches)
            {
                branch.ParentCompanyId = keepCompany.Id;
            }

            // 6. Sjednocení typů firem
            var existingTypeIds = keepCompany.CompanyTypes
                .Select(t => t.Id)
                .ToHashSet();

            foreach (var type in mergeCompany.CompanyTypes)
            {
                if (existingTypeIds.Add(type.Id))
                {
                    keepCompany.CompanyTypes.Add(type);
                }
            }

            // 7. Doplnění chybějících údajů
            // Název zachovávané firmy neměníme.

            if (string.IsNullOrWhiteSpace(keepCompany.ICO))
                keepCompany.ICO = mergeCompany.ICO;

            if (string.IsNullOrWhiteSpace(keepCompany.DIC))
                keepCompany.DIC = mergeCompany.DIC;

            if (string.IsNullOrWhiteSpace(keepCompany.Street))
                keepCompany.Street = mergeCompany.Street;

            if (string.IsNullOrWhiteSpace(keepCompany.HouseNumber))
                keepCompany.HouseNumber = mergeCompany.HouseNumber;

            if (keepCompany.CountryId == null)
                keepCompany.CountryId = mergeCompany.CountryId;

            if (keepCompany.CityId == null &&
                (mergeCompany.CountryId == null ||
                 keepCompany.CountryId == mergeCompany.CountryId))
            {
                keepCompany.CityId = mergeCompany.CityId;
            }

            if (string.IsNullOrWhiteSpace(keepCompany.Phone))
                keepCompany.Phone = mergeCompany.Phone;

            if (string.IsNullOrWhiteSpace(keepCompany.Fax))
                keepCompany.Fax = mergeCompany.Fax;

            if (string.IsNullOrWhiteSpace(keepCompany.Email))
                keepCompany.Email = mergeCompany.Email;

            if (string.IsNullOrWhiteSpace(keepCompany.Website))
                keepCompany.Website = mergeCompany.Website;

            if (string.IsNullOrWhiteSpace(keepCompany.AccountNumber))
                keepCompany.AccountNumber = mergeCompany.AccountNumber;

            if (keepCompany.BankId == null)
                keepCompany.BankId = mergeCompany.BankId;

            if (string.IsNullOrWhiteSpace(keepCompany.Notes))
                keepCompany.Notes = mergeCompany.Notes;

            // 8. Zachování informací o NDA
            keepCompany.NdaSigned =
                keepCompany.NdaSigned || mergeCompany.NdaSigned;

            if (string.IsNullOrWhiteSpace(keepCompany.NdaDocumentUrl))
                keepCompany.NdaDocumentUrl = mergeCompany.NdaDocumentUrl;

            if (keepCompany.NdaValidFrom == null)
                keepCompany.NdaValidFrom = mergeCompany.NdaValidFrom;

            if (keepCompany.NdaValidTo == null)
                keepCompany.NdaValidTo = mergeCompany.NdaValidTo;

            // 9. Archivace původní firmy
            mergeCompany.IsMerged = true;
            mergeCompany.MergedIntoCompanyId = keepCompany.Id;
            mergeCompany.MergedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return RedirectToPage("./Index");
        }

        private async Task<bool> IsDescendantAsync(
            int companyId,
            int possibleAncestorId)
        {
            var currentId = companyId;
            var visited = new HashSet<int>();

            while (true)
            {
                if (!visited.Add(currentId))
                {
                    // Existující cyklus v organizační struktuře.
                    return true;
                }

                var parentId = await _context.Companies
                    .Where(c => c.Id == currentId)
                    .Select(c => c.ParentCompanyId)
                    .FirstOrDefaultAsync();

                if (!parentId.HasValue)
                {
                    return false;
                }

                if (parentId.Value == possibleAncestorId)
                {
                    return true;
                }

                currentId = parentId.Value;
            }
        }

        private async Task LoadCompaniesAsync()
        {
            var companies = await _context.Companies
                .AsNoTracking()
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .ToListAsync();

            KeepCompanies = new SelectList(
                companies,
                "Id",
                "Name",
                KeepCompanyId);

            MergeCompanies = new SelectList(
                companies,
                "Id",
                "Name",
                MergeCompanyId);
        }
    }
}
