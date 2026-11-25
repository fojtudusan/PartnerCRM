
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Position { get; set; }

        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        // Poznámka ke kontaktu
        [StringLength(5000)]
        public string? Notes { get; set; }

        // Cesta k fotografii kontaktu
        [StringLength(500)]
        public string? PhotoPath { get; set; }

        // Aktivita kontaktu
        public bool IsActive { get; set; } = true;

        // Evidence sloučení kontaktů
        public bool IsMerged { get; set; } = false;

        public int? MergedIntoContactId { get; set; }

        public Contact? MergedIntoContact { get; set; }

        public ICollection<Contact> MergedContacts { get; set; }
            = new List<Contact>();

        public DateTime? MergedAt { get; set; }

        // Vazba na firmu
        [ValidateNever]
        public Company Company { get; set; } = null!;
        public ICollection<ContactEmployment> Employments { get; set; }
    = new List<ContactEmployment>();
    }
}
