
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PartnerCRM.Data;
using PartnerCRM.Models;

namespace PartnerCRM.Pages.Companies
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Company> Companies { get; set; }
            = new List<Company>();

        public SelectList CompanyTypes { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedCompanyTypeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowArchived { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Companies
                .Where(c => c.IsMerged == ShowArchived)
                .Include(c => c.CompanyTypes)
                .Include(c => c.MergedIntoCompany)
                .AsQueryable();

            // Vyhledávání podle názvu firmy nebo kontaktní osoby
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.Trim();
                var searchUpper = search.ToUpper();

                query = query.Where(c =>
                    c.Name.ToUpper().Contains(searchUpper) ||
                    c.Contacts.Any(contact =>
                        (contact.FirstName + " " +
                         contact.LastName).ToUpper().Contains(searchUpper) ||
                        (contact.LastName + " " +
                         contact.FirstName).ToUpper().Contains(searchUpper) ||
                        contact.FirstName.ToUpper().Contains(searchUpper) ||
                        contact.LastName.ToUpper().Contains(searchUpper)
                    )
                );
            }

            // Filtrování podle typu firmy
            if (SelectedCompanyTypeId.HasValue)
            {
                query = query.Where(c =>
                    c.CompanyTypes.Any(t =>
                        t.Id == SelectedCompanyTypeId.Value));
            }

            Companies = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            var types = await _context.CompanyTypes
                .OrderBy(t => t.Name)
                .ToListAsync();

            CompanyTypes = new SelectList(
                types,
                "Id",
                "Name",
                SelectedCompanyTypeId);
        }
    }
}
