using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? ContactId { get; set; }

        [Required]
        [StringLength(30)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }
            = DateTime.Now;

        [StringLength(3000)]
        public string? Description { get; set; }

        [StringLength(1000)]
        public string? Result { get; set; }

        [ValidateNever]
        public Company Company { get; set; } = null!;

        public Contact? Contact { get; set; }
    }
}