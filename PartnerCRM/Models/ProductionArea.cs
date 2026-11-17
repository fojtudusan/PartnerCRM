using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PartnerCRM.Models
{
    public class ProductionArea
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Nadřazená oblast – umožňuje hierarchii
        public int? ParentProductionAreaId { get; set; }

        [ValidateNever]
        public ProductionArea? ParentProductionArea { get; set; }

        [ValidateNever]
        public ICollection<ProductionArea> SubAreas { get; set; }
            = new List<ProductionArea>();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}