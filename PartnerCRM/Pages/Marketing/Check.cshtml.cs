
using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Marketing
{
    public class CheckModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CheckModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public MarketingCampaign Campaign { get; set; } = null!;

        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public int RecipientCount { get; set; }
        public int ValidEmailCount { get; set; }

        public bool IsReady =>
            Errors.Count == 0 && Warnings.Count == 0;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var campaign = await _context.MarketingCampaigns
                .Include(c => c.EmailTemplate)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Contact)
                .Include(c => c.Recipients)
                    .ThenInclude(r => r.Company)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            Campaign = campaign;

            RecipientCount = campaign.Recipients.Count;

            if (campaign.EmailTemplate == null)
            {
                Errors.Add("Marketingová akce nemá přiřazenou e-mailovou šablonu.");
                return Page();
            }

            if (RecipientCount == 0)
            {
                Errors.Add("Marketingová akce nemá žádné příjemce.");
                return Page();
            }

            if (campaign.Status != "Příprava")
            {
                Errors.Add("Kontrolu lze provést pouze u kampaně ve stavu Příprava.");
                return Page();
            }

            var template = campaign.EmailTemplate;

            var templateText = template.Subject + "\n" + template.Body;

            var placeholders = Regex.Matches(
                templateText,
                @"\{([^{}]+)\}")
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            var supportedPlaceholders = new[]
            {
                "Jmeno",
                "Prijmeni",
                "Firma",
                "Email"
            };

            foreach (var placeholder in placeholders)
            {
                if (!supportedPlaceholders.Contains(placeholder))
                {
                    Errors.Add(
                        $"Šablona obsahuje nepodporovanou proměnnou: {{{placeholder}}}.");
                }
            }

            foreach (var recipient in campaign.Recipients)
            {
                var recipientLabel =
                    $"{recipient.Contact.FirstName} {recipient.Contact.LastName}".Trim();

                if (string.IsNullOrWhiteSpace(recipient.EmailAddress) ||
                    !MailAddress.TryCreate(recipient.EmailAddress, out _))
                {
                    Errors.Add(
                        $"Neplatná e-mailová adresa u kontaktu {recipientLabel} ({recipient.Company.Name}).");

                    continue;
                }

                ValidEmailCount++;

                foreach (var placeholder in placeholders)
                {
                    string? value = placeholder switch
                    {
                        "Jmeno" => recipient.Contact.FirstName,
                        "Prijmeni" => recipient.Contact.LastName,
                        "Firma" => recipient.Company.Name,
                        "Email" => recipient.EmailAddress,
                        _ => null
                    };

                    if (supportedPlaceholders.Contains(placeholder) &&
                        string.IsNullOrWhiteSpace(value))
                    {
                        Warnings.Add(
                            $"Kontakt {recipientLabel} ({recipient.Company.Name}) nemá vyplněnou hodnotu pro {{{placeholder}}}.");
                    }
                }
            }

            Errors = Errors.Distinct().ToList();
            Warnings = Warnings.Distinct().ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostPrepareAsync(int id)
        {
            // Znovu provedeme kontrolu kampaně
            var result = await OnGetAsync(id);

            if (result is NotFoundResult)
                return NotFound();

            // Při chybách nedovolíme přípravu k odeslání
            if (Errors.Any())
            {
                TempData["Error"] =
                    "Kampaň obsahuje chyby. Nejprve je nutné je odstranit.";

                return RedirectToPage(new { id });
            }

            Campaign.Status = "Připraveno k odeslání";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Kampaň byla úspěšně připravena k odeslání.";

            return RedirectToPage("/Marketing/Details",
                new { id });
        }

    }
}
