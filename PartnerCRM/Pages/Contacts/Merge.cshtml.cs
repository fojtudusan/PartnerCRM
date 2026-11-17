
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class MergeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public MergeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public int KeepContactId { get; set; }

        [BindProperty]
        public int MergeContactId { get; set; }

        [BindProperty]
        public bool ConfirmMerge { get; set; }

        public SelectList ContactsList { get; set; } = null!;

        public Contact? KeepContact { get; set; }

        public Contact? MergeContact { get; set; }

        public async Task OnGetAsync()
        {
            await LoadContactsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadContactsAsync();

            if (!ConfirmMerge)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Pro sloučení je nutné potvrzení.");
                return Page();
            }

            if (KeepContactId == MergeContactId)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Nelze sloučit kontakt sám se sebou.");
                return Page();
            }

            var keepContact = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == KeepContactId && !c.IsMerged);

            var mergeContact = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == MergeContactId && !c.IsMerged);

            if (keepContact == null || mergeContact == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Jeden z vybraných kontaktů neexistuje nebo již byl sloučen.");

                return Page();
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Doplnění prázdných údajů zachovaného kontaktu

                if (string.IsNullOrWhiteSpace(keepContact.Position))
                    keepContact.Position = mergeContact.Position;

                if (string.IsNullOrWhiteSpace(keepContact.Email))
                    keepContact.Email = mergeContact.Email;

                if (string.IsNullOrWhiteSpace(keepContact.Phone))
                    keepContact.Phone = mergeContact.Phone;

                // 2. Převod působení kontaktů

                var employments = await _context.ContactEmployments
                    .Where(e =>
                        e.ContactId == keepContact.Id ||
                        e.ContactId == mergeContact.Id)
                    .Include(e => e.Company)
                    .ToListAsync();

                var employmentsToMove = employments
                    .Where(e => e.ContactId == mergeContact.Id)
                    .ToList();

                foreach (var employment in employmentsToMove)
                {
                    // Historická působení převedeme beze změny.
                    if (!employment.IsCurrent)
                    {
                        employment.ContactId = keepContact.Id;
                        continue;
                    }

                    // Hledáme aktivní působení stejného kontaktu
                    // ve stejné firmě.
                    var existingActive = employments.FirstOrDefault(e =>
                        e.ContactId == keepContact.Id &&
                        e.CompanyId == employment.CompanyId &&
                        e.IsCurrent &&
                        e.Id != employment.Id);

                    // Pokud duplicita neexistuje, převedeme záznam.
                    if (existingActive == null)
                    {
                        employment.ContactId = keepContact.Id;
                        continue;
                    }

                    // Zachováme nejstarší datum začátku.
                    if (employment.DateFrom < existingActive.DateFrom)
                    {
                        existingActive.DateFrom = employment.DateFrom;
                    }

                    // Zachováme informace o původním působení.
                    var originalInfo =
                        $"Původní působení (od {employment.DateFrom:dd.MM.yyyy})";

                    if (!string.IsNullOrWhiteSpace(employment.Position))
                    {
                        originalInfo +=
                            $", pozice: {employment.Position}";

                        if (string.IsNullOrWhiteSpace(existingActive.Position))
                        {
                            existingActive.Position = employment.Position;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(employment.Notes))
                    {
                        originalInfo +=
                            $". Poznámky: {employment.Notes}";
                    }

                    // Připojíme informace k zachovávanému záznamu.
                    existingActive.Notes =
                        string.IsNullOrWhiteSpace(existingActive.Notes)
                            ? originalInfo
                            : existingActive.Notes +
                              Environment.NewLine +
                              originalInfo;

                    // Duplicitní aktivní záznam odstraníme.
                    _context.ContactEmployments.Remove(employment);
                }

                // 3. Převod aktivit na zachovaný kontakt

                var activities = await _context.Activities
                    .Where(a => a.ContactId == mergeContact.Id)
                    .ToListAsync();

                foreach (var activity in activities)
                {
                    activity.ContactId = keepContact.Id;
                }

                // 4. Převod úkolů na zachovaný kontakt

                var tasks = await _context.Tasks
                    .Where(t => t.ContactId == mergeContact.Id)
                    .ToListAsync();

                foreach (var task in tasks)
                {
                    task.ContactId = keepContact.Id;
                }

                // 5. Archivace původního kontaktu

                mergeContact.IsMerged = true;
                mergeContact.MergedIntoContactId = keepContact.Id;
                mergeContact.MergedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Kontakt {mergeContact.FirstName} {mergeContact.LastName} " +
                    $"byl úspěšně sloučen s kontaktem " +
                    $"{keepContact.FirstName} {keepContact.LastName}.";

                return RedirectToPage("./Index");
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "Při slučování kontaktů došlo k chybě. Změny nebyly uloženy.");

                await LoadContactsAsync();
                return Page();
            }
        }

        private async Task LoadContactsAsync()
        {
            var contacts = await _context.Contacts
                .Where(c => !c.IsMerged)
                .Include(c => c.Company)
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();

            ContactsList = new SelectList(
                contacts.Select(c => new
                {
                    c.Id,
                    Name = c.FirstName + " " + c.LastName
                           + " – " + c.Company.Name
                }),
                "Id",
                "Name");
        }
    }
}
