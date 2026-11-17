using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class CrmTask
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public int? CompanyId { get; set; }

        public int? ContactId { get; set; }

        [Required]
        public DateTime DueDate { get; set; }
            = DateTime.Now.AddDays(1);

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Normální";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Nový";

        [StringLength(3000)]
        public string? Description { get; set; }

        public DateTime? CompletedAt { get; set; }

        [ValidateNever]
        public Company? Company { get; set; }

        [ValidateNever]
        public Contact? Contact { get; set; }
    }
}