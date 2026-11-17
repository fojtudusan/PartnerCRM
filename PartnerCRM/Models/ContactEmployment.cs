
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class ContactEmployment
    {
        public int Id { get; set; }

        [Required]
        public int ContactId { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [StringLength(150)]
        public string? Position { get; set; }

        public DateTime DateFrom { get; set; } = DateTime.Today;

        public DateTime? DateTo { get; set; }

        public bool IsCurrent { get; set; } = true;

        [StringLength(1000)]
        public string? Notes { get; set; }

        [ValidateNever]
        public Contact Contact { get; set; } = null!;

        [ValidateNever]
        public Company Company { get; set; } = null!;
    }
}
