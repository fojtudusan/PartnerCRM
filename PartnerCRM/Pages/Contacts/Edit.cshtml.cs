
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Contacts
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditModel(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public Contact Contact { get; set; } = new();

        [BindProperty]
        public IFormFile? ContactPhoto { get; set; }

        [BindProperty]
        public bool RemovePhoto { get; set; }

        [BindProperty]
        public bool IsPrimary { get; set; }

        [BindProperty]
        public bool ConfirmEmailDuplicate { get; set; }

        public string? CurrentPhotoPath { get; set; }

        public SelectList CompaniesList { get; set; } = null!;

        public bool EmailDuplicateWarning { get; set; }

        public ContactMatch? EmailMatch { get; set; }

        public class ContactMatch
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string? Email { get; set; }
            public string? Position { get; set; }
            public string CompanyName { get; set; } = "";
        }

        // =====================================
        // NAČTENÍ KONTAKTU
        // =====================================

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (contact == null)
                return NotFound();

            Contact = contact;

            var currentEmployment = await _context.ContactEmployments
                .Where(e =>
                    e.ContactId == id &&
                    e.IsCurrent)
                .OrderByDescending(e => e.DateFrom)
                .FirstOrDefaultAsync();

            if (currentEmployment != null)
            {
                Contact.CompanyId = currentEmployment.CompanyId;
                IsPrimary = currentEmployment.IsPrimary;
            }

            CurrentPhotoPath = Contact.PhotoPath;

            await LoadCompaniesAsync();

            return Page();
        }

        // =====================================
        // ULOŽENÍ ZMĚN
        // =====================================

        public async Task<IActionResult> OnPostAsync()
        {
            var contactToUpdate = await _context.Contacts
                .FirstOrDefaultAsync(c =>
                    c.Id == Contact.Id &&
                    !c.IsMerged);

            if (contactToUpdate == null)
                return NotFound();

            // Kontrola existence aktivní firmy
            var companyExists = await _context.Companies
                .AnyAsync(c =>
                    c.Id == Contact.CompanyId &&
                    !c.IsMerged);

            if (!companyExists)
            {
                ModelState.AddModelError(
                    "Contact.CompanyId",
                    "Vyberte platnou aktivní firmu.");
            }

            // Kontrola duplicitního e-mailu
            if (!string.IsNullOrWhiteSpace(Contact.Email))
            {
                var normalizedEmail = Contact.Email.Trim().ToLower();

                EmailMatch = await _context.Contacts
                    .AsNoTracking()
                    .Where(c =>
                        c.Id != Contact.Id &&
                        !c.IsMerged &&
                        !c.Company.IsMerged &&
                        c.Email != null &&
                        c.Email.Trim().ToLower() == normalizedEmail)
                    .Select(c => new ContactMatch
                    {
                        Id = c.Id,
                        FirstName = c.FirstName,
                        LastName = c.LastName,
                        Email = c.Email,
                        Position = c.Position,
                        CompanyName = c.Company.Name
                    })
                    .FirstOrDefaultAsync();

                EmailDuplicateWarning = EmailMatch != null;
            }

            // Kontrola fotografie
            if (ContactPhoto != null && ContactPhoto.Length > 0)
            {
                var extension = Path.GetExtension(ContactPhoto.FileName)
                    .ToLowerInvariant();

                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".webp"
                };

                if (!allowedExtensions.Contains(extension) ||
                    ContactPhoto.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        nameof(ContactPhoto),
                        "Fotografie musí být JPG, PNG nebo WEBP a maximálně 5 MB.");
                }
            }

            // Kontrola formuláře a potvrzení duplicity
            if (!ModelState.IsValid ||
                (EmailDuplicateWarning && !ConfirmEmailDuplicate))
            {
                CurrentPhotoPath = contactToUpdate.PhotoPath;

                await LoadCompaniesAsync();

                return Page();
            }

            // =====================================
            // AKTUALIZACE OSOBNÍCH ÚDAJŮ
            // =====================================

            contactToUpdate.FirstName = Contact.FirstName;
            contactToUpdate.LastName = Contact.LastName;
            contactToUpdate.Email = Contact.Email;
            contactToUpdate.Phone = Contact.Phone;
            contactToUpdate.Position = Contact.Position;
            contactToUpdate.Notes = Contact.Notes;

            // =====================================
            // AKTUALIZACE PŮSOBENÍ VE FIRMĚ
            // =====================================

            var currentEmployment = await _context.ContactEmployments
                .Where(e =>
                    e.ContactId == Contact.Id &&
                    e.IsCurrent)
                .OrderByDescending(e => e.DateFrom)
                .FirstOrDefaultAsync();

            if (currentEmployment == null)
            {
                // Kontakt zatím nemá aktivní působení.
                // Vytvoříme nové.

                var newEmployment = new ContactEmployment
                {
                    ContactId = contactToUpdate.Id,
                    CompanyId = Contact.CompanyId,
                    Position = Contact.Position,
                    DateFrom = DateTime.Today,
                    IsCurrent = true,
                    IsPrimary = IsPrimary
                };

                _context.ContactEmployments.Add(newEmployment);
            }
            else if (currentEmployment.CompanyId != Contact.CompanyId)
            {
                // Změna firmy:
                // uzavřeme původní působení.

                currentEmployment.DateTo = DateTime.Today;
                currentEmployment.IsCurrent = false;

                // A založíme nové působení.

                var newEmployment = new ContactEmployment
                {
                    ContactId = contactToUpdate.Id,
                    CompanyId = Contact.CompanyId,
                    Position = Contact.Position,
                    DateFrom = DateTime.Today,
                    IsCurrent = true,
                    IsPrimary = IsPrimary
                };

                _context.ContactEmployments.Add(newEmployment);
            }
            else
            {
                // Stejná firma:
                // aktualizujeme aktuální pozici a příznak.

                currentEmployment.Position = Contact.Position;
                currentEmployment.IsPrimary = IsPrimary;
            }

            // Aktuální firma kontaktu
            contactToUpdate.CompanyId = Contact.CompanyId;

            // =====================================
            // FOTOGRAFIE
            // =====================================

            var oldPhotoPath = contactToUpdate.PhotoPath;

            if (ContactPhoto != null && ContactPhoto.Length > 0)
            {
                var extension = Path.GetExtension(ContactPhoto.FileName)
                    .ToLowerInvariant();

                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "contacts");

                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await ContactPhoto.CopyToAsync(stream);
                }

                contactToUpdate.PhotoPath =
                    $"/uploads/contacts/{fileName}";
            }
            else if (RemovePhoto)
            {
                contactToUpdate.PhotoPath = null;
            }

            // =====================================
            // ULOŽENÍ DO DATABÁZE
            // =====================================

            await _context.SaveChangesAsync();

            // Odstraníme původní fotografii až po úspěšném uložení.
            if (oldPhotoPath != contactToUpdate.PhotoPath)
            {
                DeleteOldPhoto(oldPhotoPath);
            }

            return RedirectToPage("./Index");
        }

        // =====================================
        // NAČTENÍ AKTIVNÍCH FIREM
        // =====================================

        private async Task LoadCompaniesAsync()
        {
            var companies = await _context.Companies
                .Where(c => !c.IsMerged)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompaniesList = new SelectList(
                companies,
                "Id",
                "Name",
                Contact.CompanyId);
        }

        // =====================================
        // ODSTRANĚNÍ STARÉ FOTOGRAFIE
        // =====================================

        private void DeleteOldPhoto(string? photoPath)
        {
            if (string.IsNullOrWhiteSpace(photoPath))
                return;

            if (!photoPath.StartsWith(
                "/uploads/contacts/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var filePath = Path.Combine(
                _environment.WebRootPath,
                photoPath.TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}
