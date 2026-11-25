
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MarketingCampaign Campaign { get; set; } = null!;

        public List<Company> Companies { get; set; } = new();

        public List<ContactEmployment> CurrentEmployments { get; set; } = new();

        public List<EmailTemplate> EmailTemplates { get; set; } = new();

        [BindProperty]
        public List<int> SelectedEmploymentIds { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Company)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Contact)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            Campaign = campaign;

            Companies = await _context.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            CurrentEmployments = await _context.ContactEmployments
                .Include(e => e.Contact)
                .Include(e => e.Company)
                .Where(e => e.IsCurrent &&
                    e.Contact.Email != null &&
                    e.Contact.Email != "")
                .OrderBy(e => e.Company.Name)
                .ThenBy(e => e.Contact.LastName)
                .ThenBy(e => e.Contact.FirstName)
                .ToListAsync();

            EmailTemplates = await _context.EmailTemplates
                .Where(e => e.IsActive)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Page();
        }

        // HROMADNÉ PŘIDÁNÍ PŘÍJEMCŮ
        public async Task<IActionResult> OnPostAddRecipientsAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Příjemce lze přidávat pouze do kampaně ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            var selectedIds = SelectedEmploymentIds
                .Distinct()
                .ToList();

            if (!selectedIds.Any())
            {
                TempData["Error"] =
                    "Nejdříve vyberte alespoň jeden kontakt.";

                return RedirectToPage(new { id });
            }

            var employments = await _context.ContactEmployments
                .Include(e => e.Contact)
                .Where(e =>
                    selectedIds.Contains(e.Id) &&
                    e.IsCurrent &&
                    e.Contact.Email != null &&
                    e.Contact.Email != "")
                .ToListAsync();

            var existingRecipients = await _context.MarketingRecipients
                .Where(r => r.MarketingCampaignId == id)
                .Select(r => new
                {
                    r.CompanyId,
                    r.ContactId,
                    r.EmailAddress
                })
                .ToListAsync();

            var existingEmails = existingRecipients
                .Select(r => r.EmailAddress.Trim().ToLower())
                .ToHashSet();

            var existingContacts = existingRecipients
                .Select(r => $"{r.CompanyId}:{r.ContactId}")
                .ToHashSet();

            int added = 0;
            int skipped = selectedIds.Count - employments.Count;

            var newEmails = new HashSet<string>();

            foreach (var employment in employments)
            {
                string email = employment.Contact.Email!.Trim();
                string normalizedEmail = email.ToLower();

                string contactKey =
                    $"{employment.CompanyId}:{employment.ContactId}";

                if (existingContacts.Contains(contactKey) ||
                    existingEmails.Contains(normalizedEmail) ||
                    newEmails.Contains(normalizedEmail))
                {
                    skipped++;
                    continue;
                }

                var recipient = new MarketingRecipient
                {
                    MarketingCampaignId = id,
                    CompanyId = employment.CompanyId,
                    ContactId = employment.ContactId,
                    EmailAddress = email,
                    Status = "Připraveno"
                };

                _context.MarketingRecipients.Add(recipient);

                newEmails.Add(normalizedEmail);
                added++;
            }

            if (added > 0)
                await _context.SaveChangesAsync();

            if (added > 0 && skipped == 0)
                TempData["Success"] =
                    $"Úspěšně přidáno {added} příjemců.";
            else if (added > 0)
                TempData["Success"] =
                    $"Přidáno {added} příjemců. " +
                    $"Přeskočeno {skipped} kontaktů (duplicitní, neplatné nebo již neaktuální).";
            else
                TempData["Error"] =
                    "Nepodařilo se přidat žádné kontakty. " +
                    "Vybrané kontakty již mohou být mezi příjemci.";

            return RedirectToPage(new { id });
        }

        // PŮVODNÍ JEDNOTLIVÉ PŘIDÁNÍ
        public async Task<IActionResult> OnPostAddRecipientAsync(
            int id,
            int companyId,
            int contactId)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Příjemce lze přidávat pouze do kampaně ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            var employment = await _context.ContactEmployments
                .Include(e => e.Contact)
                .FirstOrDefaultAsync(e =>
                    e.CompanyId == companyId &&
                    e.ContactId == contactId &&
                    e.IsCurrent);

            if (employment == null ||
                string.IsNullOrWhiteSpace(employment.Contact.Email))
            {
                TempData["Error"] =
                    "Kontakt není aktuálně přiřazen k vybrané firmě nebo nemá e-mailovou adresu.";

                return RedirectToPage(new { id });
            }

            string emailAddress = employment.Contact.Email.Trim();

            bool alreadyExists = await _context.MarketingRecipients
                .AnyAsync(r =>
                    r.MarketingCampaignId == id &&
                    (
                        (r.CompanyId == companyId &&
                         r.ContactId == contactId)
                        ||
                        r.EmailAddress.ToLower() == emailAddress.ToLower()
                    ));

            if (alreadyExists)
            {
                TempData["Error"] =
                    "Tento kontakt již byl do marketingové akce přidán.";

                return RedirectToPage(new { id });
            }

            var recipient = new MarketingRecipient
            {
                MarketingCampaignId = id,
                CompanyId = companyId,
                ContactId = contactId,
                EmailAddress = emailAddress,
                Status = "Připraveno"
            };

            _context.MarketingRecipients.Add(recipient);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kontakt byl přidán mezi příjemce marketingové akce.";

            return RedirectToPage(new { id });
        }

        // ODEBRÁNÍ PŘÍJEMCE
        public async Task<IActionResult> OnPostRemoveRecipientAsync(
            int id,
            int recipientId)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Příjemce lze odebrat pouze z kampaně ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            var recipient = await _context.MarketingRecipients
                .FirstOrDefaultAsync(r =>
                    r.Id == recipientId &&
                    r.MarketingCampaignId == id);

            if (recipient == null)
                return NotFound();

            // Ochrana historie úspěšně odeslaných e-mailů
            if (recipient.Status == "Odesláno")
            {
                TempData["Error"] =
                    "Příjemce s již odeslaným e-mailem nelze odebrat. " +
                    "Jeho historie musí zůstat zachována.";

                return RedirectToPage(new { id });
            }

            _context.MarketingRecipients.Remove(recipient);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kontakt byl odebrán z marketingové akce.";

            return RedirectToPage(new { id });
        }

        // ÚPRAVA KAMPANĚ
        public async Task<IActionResult> OnPostEditCampaignAsync(
            int id,
            string name,
            string? description,
            int? emailTemplateId)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > 150)
            {
                TempData["Error"] =
                    "Vyplňte název akce (maximálně 150 znaků).";

                return RedirectToPage(new { id });
            }

            if (description?.Length > 1000)
            {
                TempData["Error"] =
                    "Popis může mít maximálně 1000 znaků.";

                return RedirectToPage(new { id });
            }

            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Příprava")
            {
                TempData["Error"] =
                    "Upravit lze pouze akci ve stavu Příprava.";

                return RedirectToPage(new { id });
            }

            if (emailTemplateId.HasValue)
            {
                bool templateExists = await _context.EmailTemplates
                    .AnyAsync(e =>
                        e.Id == emailTemplateId.Value &&
                        e.IsActive);

                if (!templateExists)
                {
                    TempData["Error"] =
                        "Vybraná šablona neexistuje nebo není aktivní.";

                    return RedirectToPage(new { id });
                }
            }

            campaign.Name = name;
            campaign.Description = description?.Trim();
            campaign.EmailTemplateId = emailTemplateId;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Marketingová akce byla upravena.";

            return RedirectToPage(new { id });
        }

        // NÁVRAT KAMPANĚ DO PŘÍPRAVY
        public async Task<IActionResult> OnPostReturnToPreparationAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Připraveno k odeslání")
            {
                TempData["Error"] =
                    "Do přípravy lze vrátit pouze kampaň připravenou k odeslání.";

                return RedirectToPage(new { id });
            }

            campaign.Status = "Příprava";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kampaň byla vrácena do přípravy. " +
                "Můžete upravovat kampaň a přidávat další příjemce.";

            return RedirectToPage(new { id });
        }

        // SIMULOVANÉ ODESLÁNÍ – ŽÁDNÝ E-MAIL SE SKUTEČNĚ NEODESÍLÁ
        public async Task<IActionResult> OnPostSimulateSendAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.Recipients)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            if (campaign.Status != "Připraveno k odeslání")
            {
                TempData["Error"] =
                    "Simulovat odeslání lze pouze u kampaně připravené k odeslání.";

                return RedirectToPage(new { id });
            }

            if (campaign.EmailTemplateId == null)
            {
                TempData["Error"] =
                    "Kampaň nemá přiřazenou e-mailovou šablonu.";

                return RedirectToPage(new { id });
            }

            var recipientsToSend = campaign.Recipients
                .Where(r => r.Status != "Odesláno")
                .ToList();

            if (!recipientsToSend.Any())
            {
                TempData["Error"] =
                    "Všichni příjemci již byli úspěšně zpracováni. " +
                    "Není komu simulovat odeslání.";

                return RedirectToPage(new { id });
            }

            DateTime now = DateTime.Now;

            foreach (var recipient in recipientsToSend)
            {
                recipient.Status = "Odesláno";
                recipient.SentAt = now;
                recipient.ErrorMessage = null;
            }

            // Po simulaci se kampaň automaticky vrátí do přípravy
            campaign.Status = "Příprava";
            campaign.SentAt = now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Simulace odeslání byla dokončena. " +
                $"Úspěšně zpracováno {recipientsToSend.Count} příjemců. " +
                "Žádné skutečné e-maily nebyly odeslány. " +
                "Kampaň je nyní zpět ve stavu Příprava.";

            return RedirectToPage(new { id });
        }
    }
}
