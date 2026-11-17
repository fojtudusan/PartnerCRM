
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace PartnerCRM.Pages.Companies
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly HttpClient _aresHttpClient = new HttpClient();

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Company Company { get; set; } = new();

        [BindProperty]
        public List<int> SelectedCompanyTypeIds { get; set; }
            = new();

        [BindProperty]
        public string? PostalCodeSearch { get; set; }

        [BindProperty]
        public string? CityNameSearch { get; set; }

        [BindProperty]
        public bool ConfirmNameDuplicate { get; set; }

        public bool NameDuplicateWarning { get; set; }

        public CompanyMatch? IcoMatch { get; set; }

        public List<CompanyMatch> NameMatches { get; set; }
            = new();

        public SelectList Countries { get; set; } = null!;
        public SelectList Banks { get; set; } = null!;
        public SelectList ParentCompanies { get; set; } = null!;

        public List<CompanyType> CompanyTypes { get; set; }
            = new();

        public class CompanyMatch
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? ICO { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var company = await _context.Companies
                .Include(c => c.CompanyTypes)
                .Include(c => c.City)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    !c.IsMerged);

            if (company == null)
            {
                return NotFound();
            }

            Company = company;

            SelectedCompanyTypeIds = company.CompanyTypes
                .Select(t => t.Id)
                .ToList();

            if (company.City != null)
            {
                PostalCodeSearch = company.City.PostalCode;
                CityNameSearch = company.City.Name;
            }

            await LoadListsAsync();

            return Page();
        }

        public async Task<JsonResult> OnGetSearchCitiesAsync(
            string? term,
            int? countryId)
        {
            if (string.IsNullOrWhiteSpace(term) ||
                !countryId.HasValue)
            {
                return new JsonResult(new List<object>());
            }

            term = term.Trim();

            var cities = await _context.Cities
                .AsNoTracking()
                .Where(c =>
                    c.CountryId == countryId.Value &&
                    (c.PostalCode.StartsWith(term) ||
                     c.Name.ToUpper().Contains(term.ToUpper())))
                .OrderBy(c => c.Name)
                .ThenBy(c => c.PostalCode)
                .Take(10)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    postalCode = c.PostalCode,
                    label = c.PostalCode + " – " + c.Name
                })
                .ToListAsync();

            return new JsonResult(cities);
        }
        public async Task<JsonResult> OnGetAresAsync(string? ico)
        {
            var normalizedIco = NormalizeIco(ico);

            if (normalizedIco.Length != 8)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "IČO musí obsahovat přesně 8 číslic."
                });
            }

            try
            {
                var url = $"https://ares.gov.cz/ekonomicke-subjekty-v-be/rest/ekonomicke-subjekty/{normalizedIco}";

                var response = await _aresHttpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Firma s tímto IČO nebyla v ARES nalezena."
                    });
                }

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var document = await JsonDocument.ParseAsync(stream);

                var root = document.RootElement;

                JsonElement address = default;

                if (root.TryGetProperty("sidlo", out var addressElement))
                {
                    address = addressElement;
                }

                var street = GetAresString(address, "nazevUlice");
                var houseNumber = GetAresString(address, "cisloDomovni");
                var orientationNumber = GetAresString(address, "cisloOrientacni");

                if (!string.IsNullOrWhiteSpace(orientationNumber))
                {
                    houseNumber += "/" + orientationNumber;
                }

                return new JsonResult(new
                {
                    success = true,
                    name = GetAresString(root, "obchodniJmeno"),
                    ico = GetAresString(root, "ico"),
                    dic = GetAresString(root, "dic"),
                    street,
                    houseNumber,
                    postalCode = GetAresString(address, "psc"),
                    city = GetAresString(address, "nazevObce"),
                    countryCode = GetAresString(address, "kodStatu")
                });
            }
            catch (HttpRequestException)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "ARES momentálně neodpovídá."
                });
            }
            catch (JsonException)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Nepodařilo se zpracovat odpověď z ARES."
                });
            }
        }

        private static string? GetAresString(
            JsonElement element,
            string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.ValueKind switch
            {
                JsonValueKind.String => property.GetString(),
                JsonValueKind.Number => property.ToString(),
                _ => null
            };
        }
        public async Task<IActionResult> OnPostAsync()
        {

            // Kontrola nadřazené firmy a ochrana proti cyklické hierarchii
            if (Company.ParentCompanyId.HasValue)
            {
                var parentExists = await _context.Companies
                    .AnyAsync(c =>
                        c.Id == Company.ParentCompanyId.Value &&
                        !c.IsMerged &&
                        c.Id != Company.Id);

                if (!parentExists)
                {
                    ModelState.AddModelError(
                        "Company.ParentCompanyId",
                        "Vyberte platnou aktivní nadřazenou firmu.");
                }
                else
                {
                    var currentParentId = Company.ParentCompanyId;
                    var visited = new HashSet<int>();

                    while (currentParentId.HasValue)
                    {
                        // Nadřazená firma je sama upravovaná firma
                        if (currentParentId.Value == Company.Id)
                        {
                            ModelState.AddModelError(
                                "Company.ParentCompanyId",
                                "Toto přiřazení by vytvořilo cyklickou hierarchii firem.");
                            break;
                        }

                        // Ochrana také proti případné již existující smyčce
                        if (!visited.Add(currentParentId.Value))
                        {
                            ModelState.AddModelError(
                                "Company.ParentCompanyId",
                                "V existující hierarchii firem byla nalezena cyklická vazba.");
                            break;
                        }

                        currentParentId = await _context.Companies
                            .Where(c => c.Id == currentParentId.Value)
                            .Select(c => c.ParentCompanyId)
                            .FirstOrDefaultAsync();
                    }
                }
            }

            var companyToUpdate = await _context.Companies
                .Include(c => c.CompanyTypes)
                .FirstOrDefaultAsync(c =>
                    c.Id == Company.Id &&
                    !c.IsMerged);

            // Archivovanou firmu nelze upravovat.
            if (companyToUpdate == null)
            {
                return NotFound();
            }

            // Kontrola města a státu
            if (Company.CityId.HasValue)
            {
                var city = await _context.Cities
                    .FirstOrDefaultAsync(c =>
                        c.Id == Company.CityId.Value);

                if (city == null ||
                    city.CountryId != Company.CountryId)
                {
                    ModelState.AddModelError(
                        "Company.CityId",
                        "Vybrané město nepatří do zvoleného státu.");
                }
            }

            // Ověření typů firmy
            var selectedTypes = await _context.CompanyTypes
                .Where(t => SelectedCompanyTypeIds.Contains(t.Id))
                .ToListAsync();

            if (selectedTypes.Count !=
                SelectedCompanyTypeIds.Distinct().Count())
            {
                ModelState.AddModelError(
                    "SelectedCompanyTypeIds",
                    "Některý z vybraných typů firmy neexistuje.");
            }

            // Načtení ostatních aktivních firem pro kontrolu duplicit
            var otherCompanies = await _context.Companies
                .AsNoTracking()
                .Where(c =>
                    c.Id != Company.Id &&
                    !c.IsMerged)
                .Select(c => new CompanyMatch
                {
                    Id = c.Id,
                    Name = c.Name,
                    ICO = c.ICO
                })
                .ToListAsync();

            // Kontrola duplicitního IČO
            var normalizedIco = NormalizeIco(Company.ICO);

            if (!string.IsNullOrEmpty(normalizedIco))
            {
                IcoMatch = otherCompanies.FirstOrDefault(c =>
                    NormalizeIco(c.ICO) == normalizedIco);

                if (IcoMatch != null)
                {
                    ModelState.AddModelError(
                        "Company.ICO",
                        "Firma s tímto IČO již v databázi existuje.");
                }
            }

            // Kontrola duplicitního názvu
            var normalizedName = NormalizeName(Company.Name);

            if (!string.IsNullOrEmpty(normalizedName))
            {
                NameMatches = otherCompanies
                    .Where(c =>
                        NormalizeName(c.Name) == normalizedName)
                    .ToList();

                NameDuplicateWarning = NameMatches.Count > 0;
            }

            // Při duplicitním názvu je nutné potvrzení uživatele
            if (NameDuplicateWarning && !ConfirmNameDuplicate)
            {
                await LoadListsAsync();
                return Page();
            }

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return Page();
            }

            // Základní údaje
            companyToUpdate.Name = Company.Name;
            companyToUpdate.ICO = Company.ICO;
            companyToUpdate.DIC = Company.DIC;

            // Adresa
            companyToUpdate.Street = Company.Street;
            companyToUpdate.HouseNumber = Company.HouseNumber;
            companyToUpdate.CountryId = Company.CountryId;
            companyToUpdate.CityId = Company.CityId;

            // Kontaktní údaje
            companyToUpdate.Phone = Company.Phone;
            companyToUpdate.Fax = Company.Fax;
            companyToUpdate.Email = Company.Email;
            companyToUpdate.Website = Company.Website;

            // Bankovní spojení
            companyToUpdate.AccountNumber = Company.AccountNumber;
            companyToUpdate.BankId = Company.BankId;

            // Ostatní
            companyToUpdate.Notes = Company.Notes;
            companyToUpdate.ParentCompanyId = Company.ParentCompanyId;

            // Typy firmy
            companyToUpdate.CompanyTypes.Clear();

            foreach (var type in selectedTypes)
            {
                companyToUpdate.CompanyTypes.Add(type);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private static string NormalizeIco(string? ico)
        {
            if (string.IsNullOrWhiteSpace(ico))
            {
                return string.Empty;
            }

            return new string(ico.Where(char.IsDigit).ToArray());
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return Regex.Replace(name.Trim(), @"\s+", " ")
                .ToUpperInvariant();
        }

        private async Task LoadListsAsync()
        {
            var countries = await _context.Countries
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .ToListAsync();

            Countries = new SelectList(
                countries,
                "Id",
                "Name",
                Company.CountryId);

            var banks = await _context.Banks
                .OrderBy(b => b.Name)
                .ToListAsync();

            Banks = new SelectList(
                banks,
                "Id",
                "Name",
                Company.BankId);

            CompanyTypes = await _context.CompanyTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Nadřazené firmy – pouze aktivní, bez právě upravované firmy
            var parentCompanies = await _context.Companies
                .Where(c =>
                    !c.IsMerged &&
                    c.Id != Company.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ParentCompanies = new SelectList(
                parentCompanies,
                "Id",
                "Name",
                Company.ParentCompanyId);
        }
    }
}
