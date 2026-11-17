
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;
using System.Text.Json;

namespace PartnerCRM.Pages.Companies
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private static readonly HttpClient _aresHttpClient = new HttpClient();
        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Company Company { get; set; } = new();

        [BindProperty]
        public List<int> SelectedCompanyTypeIds { get; set; }
            = new();

        [BindProperty]
        public bool ConfirmNameDuplicate { get; set; }

        public bool NameDuplicateWarning { get; set; }

        public CompanyMatch? IcoMatch { get; set; }

        public List<CompanyMatch> NameMatches { get; set; }
            = new();

        public class CompanyMatch
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string? ICO { get; set; }
        }

        public SelectList Countries { get; set; } = null!;
        public SelectList Cities { get; set; } = null!;
        public SelectList Banks { get; set; } = null!;
        public SelectList ParentCompanies { get; set; } = null!;

        public List<CompanyType> CompanyTypes { get; set; }
            = new();

        public async Task OnGetAsync()
        {
            // Výchozí stát – Česká republika
            var defaultCountry = await _context.Countries
                .FirstOrDefaultAsync(c => c.IsDefault);

            if (defaultCountry != null)
            {
                Company.CountryId = defaultCountry.Id;
            }

            await LoadListsAsync();
        }
        public async Task<JsonResult> OnGetAresAsync(string? ico)
        {
            var normalizedIco = new string(
                (ico ?? "").Where(char.IsDigit).ToArray());

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
                var url =
                    $"https://ares.gov.cz/ekonomicke-subjekty-v-be/rest/ekonomicke-subjekty/{normalizedIco}";

                using var response = await _aresHttpClient.GetAsync(url);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "Firma s tímto IČO nebyla v ARES nalezena."
                    });
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = "ARES momentálně neodpovídá. Zkuste to později."
                    });
                }

                await using var stream =
                    await response.Content.ReadAsStreamAsync();

                using var document =
                    await JsonDocument.ParseAsync(stream);

                var root = document.RootElement;

                string? GetString(JsonElement element, string property)
                {
                    if (element.TryGetProperty(property, out var value))
                    {
                        return value.ValueKind == JsonValueKind.String
                            ? value.GetString()
                            : value.ToString();
                    }

                    return null;
                }

                var address = root.TryGetProperty("sidlo", out var sidlo)
                    ? sidlo
                    : default;

                var street = GetString(address, "nazevUlice");

                var houseNumber = GetString(address, "cisloDomovni");
                var orientationNumber = GetString(address, "cisloOrientacni");

                if (!string.IsNullOrWhiteSpace(orientationNumber))
                {
                    houseNumber += "/" + orientationNumber;
                }

                var postalCode = GetString(address, "psc");

                if (!string.IsNullOrWhiteSpace(postalCode))
                {
                    postalCode = postalCode.PadLeft(5, '0');
                }

                return new JsonResult(new
                {
                    success = true,
                    name = GetString(root, "obchodniJmeno"),
                    ico = GetString(root, "ico"),
                    dic = GetString(root, "dic"),
                    street,
                    houseNumber,
                    postalCode,
                    city = GetString(address, "nazevObce"),
                    countryCode = GetString(address, "kodStatu")
                });
            }
            catch (HttpRequestException)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Nepodařilo se spojit se serverem ARES."
                });
            }
            catch (JsonException)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "ARES vrátil neočekávaná data."
                });
            }
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

        public async Task<JsonResult> OnGetSearchCompaniesAsync(
            string? term,
            string? searchType)
        {
            if (string.IsNullOrWhiteSpace(term) ||
                string.IsNullOrWhiteSpace(searchType))
            {
                return new JsonResult(new List<object>());
            }

            term = term.Trim();

            var companies = new List<CompanyMatch>();

            if (searchType == "name")
            {
                if (term.Length < 2)
                {
                    return new JsonResult(companies);
                }

                var upperTerm = term.ToUpper();

                companies = await _context.Companies
                    .AsNoTracking()
                    .Where(c =>
                        c.Name != null &&
                        c.Name.ToUpper().Contains(upperTerm))
                    .OrderBy(c => c.Name)
                    .Take(10)
                    .Select(c => new CompanyMatch
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ICO = c.ICO
                    })
                    .ToListAsync();
            }
            else if (searchType == "ico")
            {
                // Normalizace IČO – pouze číslice
                var normalizedTerm = new string(
                    term.Where(char.IsDigit).ToArray());

                if (normalizedTerm.Length < 3)
                {
                    return new JsonResult(companies);
                }

                // Stejná normalizace jako při ukládání firmy
                var allCompanies = await _context.Companies
                    .AsNoTracking()
                    .Where(c => c.ICO != null && c.ICO != "")
                    .Select(c => new CompanyMatch
                    {
                        Id = c.Id,
                        Name = c.Name,
                        ICO = c.ICO
                    })
                    .ToListAsync();

                companies = allCompanies
                    .Where(c =>
                        new string(
                            (c.ICO ?? "")
                            .Where(char.IsDigit)
                            .ToArray()) == normalizedTerm)
                    .Take(10)
                    .ToList();
            }

            var results = companies.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                ico = c.ICO,
                detailsUrl = Url.Page(
                    "./Details",
                    new { id = c.Id })
            });

            return new JsonResult(results);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Kontrola duplicit firem
            var existingCompanies = await _context.Companies
                .AsNoTracking()
                .Select(c => new CompanyMatch
                {
                    Id = c.Id,
                    Name = c.Name,
                    ICO = c.ICO
                })
                .ToListAsync();

            // Normalizace IČO – odstranění mezer a jiných znaků
            string NormalizeIco(string? ico)
            {
                return new string(
                    (ico ?? "").Where(char.IsDigit).ToArray());
            }

            // Normalizace názvu – sjednocení mezer a velikosti písmen
            string NormalizeName(string? name)
            {
                return string.Join(
                    " ",
                    (name ?? "").Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries))
                    .ToUpperInvariant();
            }

            var enteredIco = NormalizeIco(Company.ICO);
            var enteredName = NormalizeName(Company.Name);

            // Kontrola shody IČO
            if (!string.IsNullOrEmpty(enteredIco))
            {
                IcoMatch = existingCompanies.FirstOrDefault(c =>
                    NormalizeIco(c.ICO) == enteredIco);

                if (IcoMatch != null)
                {
                    ModelState.AddModelError(
                        "Company.ICO",
                        "Firma s tímto IČO již v databázi existuje.");
                }
            }

            // Kontrola shody názvu
            if (!string.IsNullOrEmpty(enteredName))
            {
                NameMatches = existingCompanies
                    .Where(c =>
                        NormalizeName(c.Name) == enteredName)
                    .ToList();

                NameDuplicateWarning = NameMatches.Count > 0;
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

            // Kontrola existence nadřazené firmy
            if (Company.ParentCompanyId.HasValue)
            {
                var parentExists = await _context.Companies
                .AnyAsync(c =>
                    c.Id == Company.ParentCompanyId.Value &&
                    !c.IsMerged);

                if (!parentExists)
                {
                    ModelState.AddModelError(
                        "Company.ParentCompanyId",
                        "Vybraná nadřazená firma neexistuje.");
                }
            }

            // Načtení skutečně existujících typů firmy
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

            // Při chybě nebo nepotvrzené duplicitě
            // se firma neuloží
            if (!ModelState.IsValid ||
            (NameDuplicateWarning && !ConfirmNameDuplicate))
            {
                if (NameDuplicateWarning)
                {
                    ConfirmNameDuplicate = true;
                }

                await LoadListsAsync();
                return Page();
            }

            Company.CreatedAt = DateTime.UtcNow;

            Company.CompanyTypes = selectedTypes;

            _context.Companies.Add(Company);

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task LoadListsAsync()
        {
            // Státy
            var countries = await _context.Countries
                .OrderByDescending(c => c.IsDefault)
                .ThenBy(c => c.Name)
                .ToListAsync();

            Countries = new SelectList(
                countries,
                "Id",
                "Name",
                Company.CountryId);

            // Města podle vybraného státu
            var citiesQuery = _context.Cities.AsQueryable();

            if (Company.CountryId.HasValue)
            {
                citiesQuery = citiesQuery.Where(
                    c => c.CountryId == Company.CountryId.Value);
            }
            else
            {
                citiesQuery = citiesQuery.Where(c => false);
            }

            var cities = await citiesQuery
                .OrderBy(c => c.Name)
                .ThenBy(c => c.PostalCode)
                .ToListAsync();

            Cities = new SelectList(
                cities,
                "Id",
                "Name",
                Company.CityId);

            // Banky
            var banks = await _context.Banks
                .OrderBy(b => b.Name)
                .ToListAsync();

            Banks = new SelectList(
                banks,
                "Id",
                "Name",
                Company.BankId);

            // Typy firem
            CompanyTypes = await _context.CompanyTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Nadřazené firmy
            var parentCompanies = await _context.Companies
            .Where(c => !c.IsMerged)
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
