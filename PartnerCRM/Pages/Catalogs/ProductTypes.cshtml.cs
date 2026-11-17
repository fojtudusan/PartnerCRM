using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Catalogs
{
    public class ProductTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ProductType> ProductTypes { get; set; } = new();

        [BindProperty]
        public string NewCode { get; set; } = string.Empty;

        [BindProperty]
        public string NewName { get; set; } = string.Empty;

        [BindProperty]
        public string? NewDescription { get; set; }

        public string? Message { get; set; }

        public async Task OnGetAsync()
        {
            await SeedDefaultTypesAsync();
            await LoadTypesAsync();
        }

        // Vložení výchozích položek při prvním otevření
        private async Task SeedDefaultTypesAsync()
        {
            if (await _context.ProductTypes.AnyAsync())
                return;

            var defaults = new List<ProductType>
            {
                new() { Code = "O", Name = "Odběratel" },
                new() { Code = "Z", Name = "Zákazník" },
                new() { Code = "k", Name = "Konstrukční celek" },
                new() { Code = "v", Name = "Výrobek" },
                new() { Code = "m", Name = "Materiál" },
                new() { Code = "p", Name = "Práce" },
                new() { Code = "s", Name = "Skladovatelné" },
                new() { Code = "b", Name = "Bonusový produkt" },
                new() { Code = "o", Name = "Vratný obal" }
            };

            _context.ProductTypes.AddRange(defaults);
            await _context.SaveChangesAsync();
        }

        private async Task LoadTypesAsync()
        {
            ProductTypes = await _context.ProductTypes
                .OrderBy(p => p.Code)
                .ToListAsync();
        }

        // Přidání nového typu
        public async Task<IActionResult> OnPostAddAsync()
        {
            NewCode = NewCode.Trim();
            NewName = NewName.Trim();

            if (string.IsNullOrWhiteSpace(NewCode) ||
                string.IsNullOrWhiteSpace(NewName))
            {
                TempData["Error"] = "Vyplňte zkratku i název.";
                return RedirectToPage();
            }

            if (NewCode.Length > 10)
            {
                TempData["Error"] = "Zkratka může mít maximálně 10 znaků.";
                return RedirectToPage();
            }

            if (NewName.Length > 150)
            {
                TempData["Error"] = "Název může mít maximálně 150 znaků.";
                return RedirectToPage();
            }

            bool exists = await _context.ProductTypes
                .AnyAsync(p => p.Code == NewCode);

            if (exists)
            {
                TempData["Error"] = "Tato zkratka již existuje.";
                return RedirectToPage();
            }

            var productType = new ProductType
            {
                Code = NewCode,
                Name = NewName,
                Description = NewDescription?.Trim(),
                IsActive = true
            };

            _context.ProductTypes.Add(productType);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Typ zboží byl přidán.";

            return RedirectToPage();
        }

        // Úprava existující položky
        public async Task<IActionResult> OnPostEditAsync(
            int id,
            string code,
            string name,
            string? description)
        {
            code = code.Trim();
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vyplňte zkratku i název.";
                return RedirectToPage();
            }

            if (code.Length > 10 || name.Length > 150)
            {
                TempData["Error"] = "Zkratka nebo název překračuje povolenou délku.";
                return RedirectToPage();
            }

            bool exists = await _context.ProductTypes
                .AnyAsync(p => p.Code == code && p.Id != id);

            if (exists)
            {
                TempData["Error"] = "Tuto zkratku již používá jiná položka.";
                return RedirectToPage();
            }

            var productType = await _context.ProductTypes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productType == null)
                return NotFound();

            productType.Code = code;
            productType.Name = name;
            productType.Description = description?.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Typ zboží byl upraven.";

            return RedirectToPage();
        }

        // Aktivace / deaktivace položky
        public async Task<IActionResult> OnPostToggleActiveAsync(int id)
        {
            var productType = await _context.ProductTypes
                .FirstOrDefaultAsync(p => p.Id == id);

            if (productType == null)
                return NotFound();

            productType.IsActive = !productType.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = productType.IsActive
                ? "Typ zboží byl aktivován."
                : "Typ zboží byl deaktivován.";

            return RedirectToPage();
        }
    }
}