
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Catalogs
{
    public class ProductionAreasModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ProductionAreasModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ProductionArea> ProductionAreas { get; set; } = new();
        public List<(ProductionArea Area, int Depth)> HierarchicalAreas
        { get; set; } = new();

        [BindProperty]
        public string NewName { get; set; } = string.Empty;

        [BindProperty]
        public string? NewDescription { get; set; }

        [BindProperty]
        public int? NewParentProductionAreaId { get; set; }

        public async Task OnGetAsync()
        {
            await SeedDefaultAreasAsync();
            await LoadAreasAsync();
        }

        private async Task SeedDefaultAreasAsync()
        {
            if (await _context.ProductionAreas.AnyAsync())
                return;

            // Hlavní oblasti
            var robot = new ProductionArea { Name = "Robot" };
            var exoskeleton = new ProductionArea { Name = "Exoskelet" };
            var integration = new ProductionArea { Name = "Integrace" };
            var chassis = new ProductionArea { Name = "Podvozek" };
            var grippers = new ProductionArea { Name = "Uchopovače" };

            _context.ProductionAreas.AddRange(
                robot,
                exoskeleton,
                integration,
                chassis,
                grippers
            );

            await _context.SaveChangesAsync();

            // Podkategorie
            var robotType = new ProductionArea
            {
                Name = "Typ robota",
                ParentProductionAreaId = robot.Id
            };

            var fixedChassis = new ProductionArea
            {
                Name = "Pevný",
                ParentProductionAreaId = chassis.Id
            };

            var mobileChassis = new ProductionArea
            {
                Name = "Pojízdný",
                ParentProductionAreaId = chassis.Id
            };

            var extendableChassis = new ProductionArea
            {
                Name = "S výsuvem",
                ParentProductionAreaId = chassis.Id
            };

            var scissorChassis = new ProductionArea
            {
                Name = "S nůžkami",
                ParentProductionAreaId = chassis.Id
            };

            _context.ProductionAreas.AddRange(
                robotType,
                fixedChassis,
                mobileChassis,
                extendableChassis,
                scissorChassis
            );

            await _context.SaveChangesAsync();

            // Konkrétní typ robota
            _context.ProductionAreas.Add(new ProductionArea
            {
                Name = "Školní",
                ParentProductionAreaId = robotType.Id
            });

            await _context.SaveChangesAsync();
        }


        private async Task LoadAreasAsync()
        {
            ProductionAreas = await _context.ProductionAreas
                .AsNoTracking()
                .ToListAsync();

            HierarchicalAreas = new List<(ProductionArea Area, int Depth)>();

            var areasByParent = ProductionAreas
                .GroupBy(a => a.ParentProductionAreaId ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(a => a.Name).ToList()
                );

            var visited = new HashSet<int>();

            void AddChildren(int parentId, int depth)
            {
                if (!areasByParent.TryGetValue(parentId, out var children))
                    return;

                foreach (var area in children)
                {
                    if (!visited.Add(area.Id))
                        continue;

                    HierarchicalAreas.Add((area, depth));

                    AddChildren(area.Id, depth + 1);
                }
            }

            // Nula představuje hlavní oblasti bez nadřazené položky.
            AddChildren(0, 0);
        }


        public async Task<IActionResult> OnPostAddAsync()
        {
            NewName = NewName.Trim();

            if (string.IsNullOrWhiteSpace(NewName))
            {
                TempData["Error"] = "Vyplňte název oblasti produkce.";
                return RedirectToPage();
            }

            if (NewName.Length > 150)
            {
                TempData["Error"] = "Název může mít maximálně 150 znaků.";
                return RedirectToPage();
            }

            if (NewParentProductionAreaId.HasValue)
            {
                bool parentExists = await _context.ProductionAreas
                    .AnyAsync(p => p.Id == NewParentProductionAreaId.Value);

                if (!parentExists)
                {
                    TempData["Error"] = "Vybraná nadřazená oblast neexistuje.";
                    return RedirectToPage();
                }
            }

            var area = new ProductionArea
            {
                Name = NewName,
                Description = NewDescription?.Trim(),
                ParentProductionAreaId = NewParentProductionAreaId,
                IsActive = true
            };

            _context.ProductionAreas.Add(area);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Oblast produkce byla přidána.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            int id,
            string name,
            string? description,
            int? parentProductionAreaId)
        {
            name = name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Vyplňte název oblasti produkce.";
                return RedirectToPage();
            }

            if (name.Length > 150)
            {
                TempData["Error"] = "Název může mít maximálně 150 znaků.";
                return RedirectToPage();
            }

            var area = await _context.ProductionAreas
                .FirstOrDefaultAsync(p => p.Id == id);

            if (area == null)
                return NotFound();

            if (parentProductionAreaId.HasValue)
            {
                if (parentProductionAreaId.Value == id)
                {
                    TempData["Error"] = "Oblast nemůže být sama sobě nadřazenou oblastí.";
                    return RedirectToPage();
                }

                bool parentExists = await _context.ProductionAreas
                    .AnyAsync(p => p.Id == parentProductionAreaId.Value);

                if (!parentExists)
                {
                    TempData["Error"] = "Vybraná nadřazená oblast neexistuje.";
                    return RedirectToPage();
                }

                // Kontrola, zda nevznikne cyklická vazba
                if (WouldCreateCycle(id, parentProductionAreaId.Value))
                {
                    TempData["Error"] = "Tuto nadřazenou oblast nelze nastavit, protože by vznikla cyklická vazba.";
                    return RedirectToPage();
                }
            }

            area.Name = name;
            area.Description = description?.Trim();
            area.ParentProductionAreaId = parentProductionAreaId;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Oblast produkce byla upravena.";

            return RedirectToPage();
        }

        private bool WouldCreateCycle(int areaId, int proposedParentId)
        {
            var areas = _context.ProductionAreas
                .AsNoTracking()
                .ToDictionary(p => p.Id, p => p.ParentProductionAreaId);

            int? currentId = proposedParentId;
            var visited = new HashSet<int>();

            while (currentId.HasValue)
            {
                if (currentId.Value == areaId)
                    return true;

                if (!visited.Add(currentId.Value))
                    return true;

                if (!areas.TryGetValue(currentId.Value, out currentId))
                    break;
            }

            return false;
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id)
        {
            var area = await _context.ProductionAreas
                .FirstOrDefaultAsync(p => p.Id == id);

            if (area == null)
                return NotFound();

            area.IsActive = !area.IsActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = area.IsActive
                ? "Oblast produkce byla aktivována."
                : "Oblast produkce byla deaktivována.";

            return RedirectToPage();
        }
    }
}
