
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public IList<CompanyType> CompanyTypes { get; set; }
            = new List<CompanyType>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<int> SelectedCompanyTypeIds { get; set; }
            = new List<int>();

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

            // Filtrování podle více typů firmy (OR)
            if (SelectedCompanyTypeIds.Any())
            {
                query = query.Where(c =>
                    c.CompanyTypes.Any(t =>
                        SelectedCompanyTypeIds.Contains(t.Id)));
            }

            Companies = await query
                .OrderBy(c => c.Name)
                .ToListAsync();

            CompanyTypes = await _context.CompanyTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
