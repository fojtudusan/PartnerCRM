
using System.ComponentModel.DataAnnotations;

namespace PartnerCRM.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ICO { get; set; }

        [StringLength(20)]
        public string? DIC { get; set; }

        [StringLength(150)]
        public string? Street { get; set; }

        [StringLength(30)]
        public string? HouseNumber { get; set; }

        // Město a PSČ
        public int? CityId { get; set; }
        public City? City { get; set; }

        // Stát
        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(30)]
        public string? Fax { get; set; }

        [StringLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        // Bankovní spojení
        [StringLength(50)]
        public string? AccountNumber { get; set; }

        public int? BankId { get; set; }
        public Bank? Bank { get; set; }

        [StringLength(200)]
        public string? Website { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // NDA - dohoda o mlčenlivosti
        public bool NdaSigned { get; set; } = false;

        [StringLength(500)]
        public string? NdaDocumentUrl { get; set; }

        public DateTime? NdaValidFrom { get; set; }

        public DateTime? NdaValidTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Evidence sloučení firem
        public bool IsMerged { get; set; } = false;

        public int? MergedIntoCompanyId { get; set; }

        public Company? MergedIntoCompany { get; set; }

        public ICollection<Company> MergedCompanies { get; set; }
            = new List<Company>();

        public DateTime? MergedAt { get; set; }
        // Organizační struktura firmy
        public int? ParentCompanyId { get; set; }

        public Company? ParentCompany { get; set; }

        public ICollection<Company> Branches { get; set; }
            = new List<Company>();
        // Typy firmy
        public ICollection<CompanyType> CompanyTypes { get; set; }
            = new List<CompanyType>();

        // Kontaktní osoby firmy
        public ICollection<Contact> Contacts { get; set; }
            = new List<Contact>();
        public ICollection<ContactEmployment> ContactEmployments { get; set; }
    = new List<ContactEmployment>();
    }
}
